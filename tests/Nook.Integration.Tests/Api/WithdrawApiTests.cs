using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nook.Api.Auth;
using Nook.Application.Reviews;
using Nook.Domain.Bookmarks;
using Nook.Domain.Groups;
using Nook.Domain.Likes;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Integration.Tests.Infrastructure;

namespace Nook.Integration.Tests.Api;

// PF-02 DELETE /me. PM 정책: 리뷰·리뷰 이미지·좋아요는 남기고(작성자·누른 사람만 끊긴다), 복구 없음.
public sealed class WithdrawApiTests : IntegrationTestBase, IAsyncLifetime, IDisposable
{
    // 테스트 목록
    //
    // - Withdraw_KeepsReviewsAndLikes_DeletesUserBookmarksAvatarAndLinks   (작성자 null로 보임, 좋아요 수 그대로)
    // - Withdraw_AsGroupOwner_PassesOwnershipToEarliestJoined              (R-GR-11 — 나가기와 같은 경로)
    // - Withdraw_AsLastMember_DeletesGroup                                 (R-GR-09)
    // - Withdraw_Twice_SecondReturns401
    // - AfterWithdraw_RemainingTokenWrite_Returns401UnknownUser            (회원 FK 위반 → 401, 미결 ②)

    private static readonly DateTimeOffset Past = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);

    private readonly NookApiFactory _factory;
    private readonly HttpClient _me;
    private readonly HttpClient _other;
    private readonly User _meUser = User.Create(Nickname.From("누크"), Email.From("nook@example.com"), Gender.None, null);
    private readonly User _otherUser = User.Create(Nickname.From("남의회원"), Email.From("other@example.com"), Gender.None, null);
    private readonly Place _place = Place.Create(
        KakaoPlaceId.From("1"), "누크 카페", "서울", Coordinate.Create(37.5, 127.0).Unwrap()).Unwrap();

    public WithdrawApiTests(PostgresFixture fixture) : base(fixture)
    {
        _factory = new NookApiFactory(fixture.ConnectionString);
        _me = ClientFor(_meUser);
        _other = ClientFor(_otherUser);
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await InitializeAsync();
        await SaveAsync(_meUser, _otherUser, _place);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Withdraw_KeepsReviewsAndLikes_DeletesUserBookmarksAvatarAndLinks()
    {
        var mine = NewReview(_meUser, Past);
        var others = NewReview(_otherUser, Past.AddDays(1));
        await SaveAsync(mine, others,
            Like.Create(_otherUser.Id, mine.Id, Past), Like.Create(_meUser.Id, others.Id, Past),
            Bookmark.Create(_meUser.Id, _place.Id, Past));
        await _me.PostAsJsonAsync("/auth/magic-link", new { email = "nook@example.com" });   // 링크 기록 1행
        var avatar = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D]);
        avatar.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        Assert.Equal(HttpStatusCode.OK, (await _me.PutAsync("/me/avatar", new MultipartFormDataContent { { avatar, "Image", "a.bin" } })).StatusCode);

        var response = await _me.DeleteAsync("/me");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var db = Db.CreateContext();
        Assert.False(await db.Users.AnyAsync(u => u.Id == _meUser.Id));
        Assert.False(await db.Bookmarks.AnyAsync());
        Assert.Equal(0, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM magic_link_tokens").SingleAsync());
        Assert.Empty(_factory.ImageStorage.Objects);                                         // 프로필 사진 파일
        Assert.Equal(2, await db.Reviews.CountAsync());                                       // 리뷰는 남는다
        Assert.Equal(2, await db.Likes.CountAsync());                                         // 좋아요도 남는다

        var page = await _other.GetFromJsonAsync<ReviewPageView>($"/places/{_place.Id.Value}/reviews");
        var orphan = page!.Items.Single(r => r.Id == mine.Id.Value);
        Assert.Null(orphan.Author);                                                           // "탈퇴한 사용자" — 이어 볼 단서 없음
        Assert.Equal(1, orphan.LikeCount);
        Assert.Equal(1, page.Items.Single(r => r.Id == others.Id.Value).LikeCount);           // 내가 누른 좋아요도 그대로
    }

    [Fact]
    public async Task Withdraw_AsGroupOwner_PassesOwnershipToEarliestJoined()
    {
        var late = User.Create(Nickname.From("늦게"), Email.From("late@example.com"), Gender.None, null);
        var group = Group.Create(GroupName.From("모임"), _meUser.Id, InviteCode.From("ABCDEF"), Past);
        Assert.True(group.AddMember(late.Id, Past.AddHours(2)).IsSuccess);
        Assert.True(group.AddMember(_otherUser.Id, Past.AddHours(1)).IsSuccess);
        await SaveAsync(late, group);

        Assert.Equal(HttpStatusCode.NoContent, (await _me.DeleteAsync("/me")).StatusCode);

        await using var db = Db.CreateContext();
        var saved = await db.Groups.SingleAsync(g => g.Id == group.Id);
        Assert.Equal(_otherUser.Id, saved.OwnerId);
        Assert.Equal(2, saved.Members.Count);
    }

    [Fact]
    public async Task Withdraw_AsLastMember_DeletesGroup()
    {
        await SaveAsync(Group.Create(GroupName.From("혼자"), _meUser.Id, InviteCode.From("ABCDEF"), Past));

        Assert.Equal(HttpStatusCode.NoContent, (await _me.DeleteAsync("/me")).StatusCode);

        await using var db = Db.CreateContext();
        Assert.False(await db.Groups.AnyAsync());
    }

    [Fact]
    public async Task Withdraw_Twice_SecondReturns401()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await _me.DeleteAsync("/me")).StatusCode);

        var second = await _me.DeleteAsync("/me");

        await AssertUnknownUserAsync(second);
    }

    [Fact]
    public async Task AfterWithdraw_RemainingTokenWrite_Returns401UnknownUser()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await _me.DeleteAsync("/me")).StatusCode);

        var response = await _me.PutAsync($"/places/{_place.Id.Value}/bookmark", null);   // 토큰은 아직 유효(60분)

        await AssertUnknownUserAsync(response);
    }

    private static async Task AssertUnknownUserAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Auth.UnknownUser", (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Title);
    }

    private HttpClient ClientFor(User user)
    {
        var client = _factory.CreateClient();
        var token = _factory.Services.GetRequiredService<AccessTokenIssuer>().Issue(user.Id.Value);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private Review NewReview(User author, DateTimeOffset at) =>
        Review.Create(author.Id, _place.Id, ReviewContent.From("조용하고 커피가 맛있는 곳"), [], at).Unwrap();

    private async Task SaveAsync(params object[] entities)
    {
        await using var db = Db.CreateContext();
        db.AddRange(entities);
        await db.SaveChangesAsync();
    }
}
