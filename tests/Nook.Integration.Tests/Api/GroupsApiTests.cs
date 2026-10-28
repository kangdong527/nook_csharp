using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nook.Api.Auth;
using Nook.Application.Groups;
using Nook.Domain.Bookmarks;
using Nook.Domain.Groups;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Integration.Tests.Infrastructure;
using Nook.SharedKernel;

namespace Nook.Integration.Tests.Api;

// GR-01·02·04를 HTTP로. 동시성·규칙은 Groups/ 핸들러 테스트가 본다 — 여기서는 경로·상태 코드·응답 모양(계약)만.
public sealed class GroupsApiTests : IntegrationTestBase, IAsyncLifetime, IDisposable
{
    // 테스트 목록
    //
    // GR-02 POST /groups
    // - Create_ValidName_Returns201WithLocationAndView
    // - Create_InvalidName_Returns400
    // - Create_WithoutToken_Returns401                                    (1a, Fallback)
    //
    // GR-01 POST /groups/join
    // - Join_ValidCode_Returns200WithGroupView
    // - Join_InvalidFormat_Returns400                                     (2a)
    // - Join_UnknownCode_Returns404                                       (4c)
    // - Join_FullGroup_Returns409                                         (4b)
    // - Join_AlreadyMember_Returns409                                     (4a)
    //
    // GR-04 DELETE /groups/{id}/membership
    // - Leave_Member_Returns204AndRemovesMembership
    // - Leave_NotMember_Returns204                                        (멱등)
    //
    // GR-03 PATCH /groups/{id} (방장만, R-GR-11)
    // - Rename_ByOwner_Returns200AndPersists
    // - Rename_ByMember_Returns403AndKeepsName
    // - Rename_UnknownGroup_Returns404 / Rename_InvalidName_Returns400
    //
    // GR-06 GET /me/groups
    // - MyGroups_ReturnsJoinOrderWithCountsSlotAndOwnerFlag   (멤버 수 한 쿼리, 내 슬롯, 방장 여부, 남의 그룹 제외)
    //
    // GR-05 GET /groups/{id}/places (장소마다 한 행 — 사실만, PM 그룹맵 표시 정책)
    // - Places_AggregatesPerPlace          (리뷰 멤버 수·같은 멤버 여러 날 = 1·첫 리뷰어 슬롯·북마크 수·단독 북마커 슬롯·남의 기록 제외)
    // - Places_AfterFirstReviewerLeaves_NextReviewerColor   (저장하지 않고 현재 멤버로 계산)
    // - Places_NotMember_Returns403 / Places_UnknownGroup_Returns404

    private const string Code = "ABCDEF";
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero); // 과거 — 합류 시각(실제 시계)이 그룹 생성보다 앞서면 도메인이 거절한다

    private readonly NookApiFactory _factory;
    private readonly HttpClient _client;
    private readonly User _user = User.Create(Nickname.From("누크"), Email.From("nook@example.com"), Gender.None, null);
    private readonly User _creator = User.Create(Nickname.From("방장"), Email.From("creator@example.com"), Gender.None, null);

    public GroupsApiTests(PostgresFixture fixture) : base(fixture)
    {
        _factory = new NookApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
        var token = _factory.Services.GetRequiredService<AccessTokenIssuer>().Issue(_user.Id.Value);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await InitializeAsync();
        await using var db = Db.CreateContext();
        db.AddRange(_user, _creator);
        await db.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();

    // ── GR-02 ─────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidName_Returns201WithLocationAndView()
    {
        var response = await _client.PostAsJsonAsync("/groups", new { name = "산책모임" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var view = await response.Content.ReadFromJsonAsync<GroupView>();
        Assert.NotNull(view);
        Assert.Equal($"/groups/{view.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("산책모임", view.Name);
        Assert.Equal(InviteCode.Length, view.InviteCode.Length);
        // 응답이 아니라 DB가 기준 — 만든 회원이 첫 멤버(R-GR-08).
        Assert.Equal([_user.Id], await MemberIdsAsync(GroupId.From(view.Id)));
    }

    [Fact]
    public async Task Create_InvalidName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/groups", new { name = "산" });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, GroupErrors.GroupNameLengthOutOfRange);
        await using var db = Db.CreateContext();
        Assert.False(await db.Groups.AnyAsync());
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/groups", new { name = "산책모임" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── GR-01 ─────────────────────────────────────────────

    [Fact]
    public async Task Join_ValidCode_Returns200WithGroupView()
    {
        var group = await SeedGroupAsync(extraMembers: 0);

        var response = await JoinAsync(Code);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new GroupView(group.Id.Value, "산책모임", Code), await response.Content.ReadFromJsonAsync<GroupView>());
        Assert.Contains(_user.Id, await MemberIdsAsync(group.Id));
    }

    [Fact]
    public async Task Join_InvalidFormat_Returns400()
    {
        await SeedGroupAsync(extraMembers: 0);

        var response = await JoinAsync("ABC");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, GroupErrors.InviteCodeInvalidFormat);
    }

    [Fact]
    public async Task Join_UnknownCode_Returns404()
    {
        await SeedGroupAsync(extraMembers: 0);

        var response = await JoinAsync("ZZZZZZ");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, JoinGroupErrors.GroupNotFound);
    }

    [Fact]
    public async Task Join_FullGroup_Returns409()
    {
        var group = await SeedGroupAsync(extraMembers: Group.MaxGroupMembers - 1);

        var response = await JoinAsync(Code);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, GroupErrors.GroupFull);
        Assert.DoesNotContain(_user.Id, await MemberIdsAsync(group.Id));
    }

    [Fact]
    public async Task Join_AlreadyMember_Returns409()
    {
        await SeedGroupAsync(extraMembers: 0);
        Assert.Equal(HttpStatusCode.OK, (await JoinAsync(Code)).StatusCode);

        var response = await JoinAsync(Code);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, GroupErrors.AlreadyMember);
    }

    // ── GR-04 ─────────────────────────────────────────────

    [Fact]
    public async Task Leave_Member_Returns204AndRemovesMembership()
    {
        var group = await SeedGroupAsync(extraMembers: 0);
        Assert.Equal(HttpStatusCode.OK, (await JoinAsync(Code)).StatusCode);

        var response = await _client.DeleteAsync($"/groups/{group.Id.Value}/membership");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal([_creator.Id], await MemberIdsAsync(group.Id));
    }

    [Fact]
    public async Task Leave_NotMember_Returns204()
    {
        var group = await SeedGroupAsync(extraMembers: 0);

        var response = await _client.DeleteAsync($"/groups/{group.Id.Value}/membership");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal([_creator.Id], await MemberIdsAsync(group.Id));
    }

    // ── 도우미 ─────────────────────────────────────────────

    // ── GR-03 ─────────────────────────────────────────────

    [Fact]
    public async Task Rename_ByOwner_Returns200AndPersists()
    {
        var group = await SeedOwnGroupAsync("내모임", "QWERTY");

        var response = await _client.PatchAsJsonAsync($"/groups/{group.Id.Value}", new { name = "새모임" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("새모임", (await response.Content.ReadFromJsonAsync<GroupView>())!.Name);
        await using var db = Db.CreateContext();
        Assert.Equal("새모임", (await db.Groups.SingleAsync(g => g.Id == group.Id)).Name.Value);
    }

    [Fact]
    public async Task Rename_ByMember_Returns403AndKeepsName()
    {
        var group = await SeedGroupAsync(extraMembers: 0);   // _creator가 방장
        Assert.Equal(HttpStatusCode.OK, (await JoinAsync(Code)).StatusCode);

        var response = await _client.PatchAsJsonAsync($"/groups/{group.Id.Value}", new { name = "새모임" });

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, GroupErrors.NotOwner);
        await using var db = Db.CreateContext();
        Assert.Equal("산책모임", (await db.Groups.SingleAsync(g => g.Id == group.Id)).Name.Value);
    }

    [Fact]
    public async Task Rename_UnknownGroup_Returns404()
    {
        var response = await _client.PatchAsJsonAsync($"/groups/{Guid.CreateVersion7()}", new { name = "새모임" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Rename_InvalidName_Returns400()
    {
        var group = await SeedOwnGroupAsync("내모임", "QWERTY");

        var response = await _client.PatchAsJsonAsync($"/groups/{group.Id.Value}", new { name = "a" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── GR-06 ─────────────────────────────────────────────

    [Fact]
    public async Task MyGroups_ReturnsJoinOrderWithCountsSlotAndOwnerFlag()
    {
        var joined = await SeedGroupAsync(extraMembers: 2);                       // _creator + 2명 → 내가 들어가면 4명
        var own = await SeedOwnGroupAsync("내모임", "QWERTY", CreatedAt.AddDays(-1));   // 내가 방장, 1명 — 먼저 합류
        Assert.Equal(HttpStatusCode.OK, (await JoinAsync(Code)).StatusCode);
        await SeedStrangersGroupAsync();                                          // 내가 없는 그룹 — 섞이면 안 된다

        var groups = (await _client.GetFromJsonAsync<List<MyGroupView>>("/me/groups"))!;

        Assert.Equal([own.Id.Value, joined.Id.Value], groups.Select(g => g.Id));
        Assert.Equal((1, true), (groups[0].MemberCount, groups[0].IsOwner));
        Assert.Equal((4, false), (groups[1].MemberCount, groups[1].IsOwner));
        await using var db = Db.CreateContext();
        var mySlot = (await db.Groups.AsNoTracking().SingleAsync(g => g.Id == joined.Id)).Members.Single(m => m.UserId == _user.Id).ColorSlot;
        Assert.Equal(mySlot, groups[1].MyColorSlot);
    }

    // ── GR-05 ─────────────────────────────────────────────

    // 슬롯 고정: 방장 0, 나 1, 멤버 2(pickSlot _ => 0 = 남은 것 중 가장 작은 슬롯).
    private sealed record MapFixture(Group Group, User Member, Place Reviewed, Place SoleBookmark, Place TwoBookmarks, Place ReviewAndBookmark);

    private async Task<MapFixture> SeedMapAsync()
    {
        var member = User.Create(Nickname.From("멤버"), Email.From("member@example.com"), Gender.None, null);
        var stranger = User.Create(Nickname.From("남남"), Email.From("stranger@example.com"), Gender.None, null);
        var group = Group.Create(GroupName.From("지도모임"), _creator.Id, InviteCode.From(Code), CreatedAt, pickSlot: _ => 0);
        Assert.True(group.AddMember(_user.Id, CreatedAt, pickSlot: _ => 0).IsSuccess);
        Assert.True(group.AddMember(member.Id, CreatedAt, pickSlot: _ => 0).IsSuccess);

        Place NewPlace(string id) => Place.Create(KakaoPlaceId.From(id), $"장소{id}", "서울", Coordinate.Create(37.5, 127.0).Unwrap()).Unwrap();
        Review NewReview(User u, Place p, int daysAgo) =>
            Review.Create(u.Id, p.Id, ReviewContent.From("조용하고 커피가 맛있는 곳"), [], CreatedAt.AddDays(-daysAgo)).Unwrap();
        var (reviewed, sole, two, both, strangers) = (NewPlace("1"), NewPlace("2"), NewPlace("3"), NewPlace("4"), NewPlace("5"));

        await using var db = Db.CreateContext();
        db.AddRange(member, stranger, group, reviewed, sole, two, both, strangers);
        db.AddRange(
            NewReview(member, reviewed, daysAgo: 3),           // 첫 리뷰어 = 멤버(슬롯 2)
            NewReview(_creator, reviewed, daysAgo: 2),
            NewReview(_creator, reviewed, daysAgo: 1),         // 같은 멤버 다른 날 — 리뷰 멤버 수는 그대로 2
            Bookmark.Create(_creator.Id, sole.Id, CreatedAt),
            Bookmark.Create(_creator.Id, two.Id, CreatedAt),
            Bookmark.Create(member.Id, two.Id, CreatedAt),
            NewReview(_user, both, daysAgo: 1),
            Bookmark.Create(_creator.Id, both.Id, CreatedAt),
            NewReview(stranger, strangers, daysAgo: 1),        // 멤버 아님 — 나오면 안 된다
            Bookmark.Create(stranger.Id, reviewed.Id, CreatedAt));
        await db.SaveChangesAsync();
        return new MapFixture(group, member, reviewed, sole, two, both);
    }

    [Fact]
    public async Task Places_AggregatesPerPlace()
    {
        var map = await SeedMapAsync();

        var places = (await _client.GetFromJsonAsync<List<GroupPlaceView>>($"/groups/{map.Group.Id.Value}/places"))!;

        var byId = places.ToDictionary(p => p.PlaceId, p => (p.ReviewMemberCount, p.FirstReviewerSlot, p.BookmarkCount, p.SoleBookmarkerSlot));
        Assert.Equal(new Dictionary<Guid, (int, int?, int, int?)>
        {
            [map.Reviewed.Id.Value] = (2, 2, 0, null),           // 남의 북마크는 세지 않는다
            [map.SoleBookmark.Id.Value] = (0, null, 1, 0),
            [map.TwoBookmarks.Id.Value] = (0, null, 2, null),
            [map.ReviewAndBookmark.Id.Value] = (1, 1, 1, 0),
        }, byId);
    }

    [Fact]
    public async Task Places_AfterFirstReviewerLeaves_NextReviewerColor()
    {
        var map = await SeedMapAsync();
        using var memberClient = _factory.CreateClient();
        memberClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", _factory.Services.GetRequiredService<AccessTokenIssuer>().Issue(map.Member.Id.Value));
        Assert.Equal(HttpStatusCode.NoContent, (await memberClient.DeleteAsync($"/groups/{map.Group.Id.Value}/membership")).StatusCode);

        var places = (await _client.GetFromJsonAsync<List<GroupPlaceView>>($"/groups/{map.Group.Id.Value}/places"))!;

        var reviewed = places.Single(p => p.PlaceId == map.Reviewed.Id.Value);
        Assert.Equal((1, 0), (reviewed.ReviewMemberCount, reviewed.FirstReviewerSlot));   // 방장(슬롯 0)이 첫 리뷰어가 된다
        Assert.Equal((1, 0), (places.Single(p => p.PlaceId == map.TwoBookmarks.Id.Value).BookmarkCount,
                              places.Single(p => p.PlaceId == map.TwoBookmarks.Id.Value).SoleBookmarkerSlot!.Value));
    }

    [Fact]
    public async Task Places_NotMember_Returns403()
    {
        var group = await SeedGroupAsync(extraMembers: 0);   // 나는 멤버가 아니다

        var response = await _client.GetAsync($"/groups/{group.Id.Value}/places");

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, GroupQueryErrors.NotMember);
    }

    [Fact]
    public async Task Places_UnknownGroup_Returns404()
    {
        var response = await _client.GetAsync($"/groups/{Guid.CreateVersion7()}/places");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // 내가 만든 그룹(내가 방장).
    private async Task<Group> SeedOwnGroupAsync(string name, string code, DateTimeOffset? createdAt = null)
    {
        var group = Group.Create(GroupName.From(name), _user.Id, InviteCode.From(code), createdAt ?? CreatedAt);
        await using var db = Db.CreateContext();
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    private async Task SeedStrangersGroupAsync()
    {
        var stranger = User.Create(Nickname.From("남남"), Email.From("stranger@example.com"), Gender.None, null);
        var group = Group.Create(GroupName.From("남의모임"), stranger.Id, InviteCode.From("ZXCVBN"), CreatedAt);
        await using var db = Db.CreateContext();
        db.AddRange(stranger, group);
        await db.SaveChangesAsync();
    }

    private Task<HttpResponseMessage> JoinAsync(string code) =>
        _client.PostAsJsonAsync("/groups/join", new { inviteCode = code });

    // _creator가 만든 그룹 + extraMembers명(나 아님).
    private async Task<Group> SeedGroupAsync(int extraMembers)
    {
        var group = Group.Create(GroupName.From("산책모임"), _creator.Id, InviteCode.From(Code), CreatedAt);
        var others = new List<User>();
        for (var i = 1; i <= extraMembers; i++)
        {
            var member = User.Create(Nickname.From($"멤버{i}"), Email.From($"member{i}@example.com"), Gender.None, null);
            others.Add(member);
            Assert.True(group.AddMember(member.Id, CreatedAt).IsSuccess);
        }

        await using var db = Db.CreateContext();
        db.AddRange(others);
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    private async Task<List<UserId>> MemberIdsAsync(GroupId groupId)
    {
        await using var db = Db.CreateContext();
        var group = await db.Groups.AsNoTracking().SingleAsync(g => g.Id == groupId);
        return [.. group.Members.Select(m => m.UserId)];
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, Error error)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(error.Code, problem?.Title);
    }
}
