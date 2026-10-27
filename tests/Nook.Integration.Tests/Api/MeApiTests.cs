using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Nook.Api.Auth;
using Microsoft.AspNetCore.Mvc;
using Nook.Application.Bookmarks;
using Nook.Application.Reviews;
using Nook.Application.Users;
using Nook.Domain.Bookmarks;
using Nook.Domain.Likes;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Integration.Tests.Infrastructure;

namespace Nook.Integration.Tests.Api;

// BM-03 내 지도를 HTTP로. 합집합·중복 제거·회원 구분이 SQL 한 번에서 맞게 나오는지 실제 PostgreSQL에서 본다.
public sealed class MeApiTests : IntegrationTestBase, IAsyncLifetime, IDisposable
{
    // 테스트 목록
    //
    // - Places_WhenNothingSaved_ReturnsEmpty
    // - Places_ReturnsUnionOncePerPlaceWithStar   (저장만 → gray, 리뷰만 → fill, 둘 다 + 리뷰 2개 → fill 1행, 남의 기록 → 없음)
    // - Places_WithoutToken_Returns401
    //
    // AU-04 GET /me
    // - Get_ReturnsMyIdEmailNickname
    // - Get_WhenUserRowMissing_Returns401UnknownUser   (서명 유효 + 회원 행 없음 → 핸들러 401, title로 구분)
    // - Get_WithoutToken_Returns401WithoutErrorCode    (미들웨어 401 — 핸들러까지 오지 않는다)
    //
    // PF-04 GET /me/reviews
    // - Reviews_PagesNewestFirstFiveAtATime_OnlyMine     (6개 → 5 + 1, 겹침·빠짐 없음, 장소 이름·좋아요, 남의 리뷰 제외)
    // - Reviews_InvalidCursor_Returns400
    //
    // PF-01 PATCH /me (닉네임)
    // - Patch_ChangesNickname_Returns200AndPersists
    // - Patch_SameNickname_Returns200                     (멱등 — 자기 닉네임은 중복이 아니다)
    // - Patch_NicknameOfOther_Returns409
    // - Patch_InvalidFormat_Returns400
    // - Patch_TwoMembersSameNicknameConcurrently_One200One409   (DB UNIQUE가 최종 판정 — 미리 확인 없음)
    //
    // PF-01 PUT·DELETE /me/avatar (프로필 사진)
    // - PutAvatar_StoresAndShowsUrlInMeAndReviewAuthor     (avatars/ 키, /me·PL-02 작성자에 같은 Url, 이미지 엔드포인트로 받아짐)
    // - PutAvatar_Twice_DeletesPreviousAfterSave           (스토리지에 한 장만 남는다)
    // - PutAvatar_UnsupportedFormat_Returns400AndUploadsNothing
    // - DeleteAvatar_RemovesFileAndIsIdempotent

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    private readonly NookApiFactory _factory;
    private readonly HttpClient _client;
    private readonly User _me = User.Create(Nickname.From("누크"), Email.From("nook@example.com"), Gender.None, null);
    private readonly User _other = User.Create(Nickname.From("남의회원"), Email.From("other@example.com"), Gender.None, null);
    private readonly Place _bookmarkedOnly = NewPlace("1", "저장만 한 곳");
    private readonly Place _reviewedOnly = NewPlace("2", "리뷰만 쓴 곳");
    private readonly Place _both = NewPlace("3", "둘 다 한 곳");
    private readonly Place _othersOnly = NewPlace("4", "남만 간 곳");

    public MeApiTests(PostgresFixture fixture) : base(fixture)
    {
        _factory = new NookApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
        var token = _factory.Services.GetRequiredService<AccessTokenIssuer>().Issue(_me.Id.Value);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await InitializeAsync();
        await using var db = Db.CreateContext();
        db.AddRange(_me, _other, _bookmarkedOnly, _reviewedOnly, _both, _othersOnly);
        await db.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Places_WhenNothingSaved_ReturnsEmpty()
    {
        var places = await _client.GetFromJsonAsync<List<MyPlaceView>>("/me/places");

        Assert.Empty(places!);
    }

    [Fact]
    public async Task Places_ReturnsUnionOncePerPlaceWithStar()
    {
        await using (var db = Db.CreateContext())
        {
            db.AddRange(
                Bookmark.Create(_me.Id, _bookmarkedOnly.Id, Now),
                Review(_me, _reviewedOnly, Now),
                Bookmark.Create(_me.Id, _both.Id, Now),
                // 같은 장소에 북마크 + 다른 날 리뷰 2개(R-RV-03은 하루 1개) — 조인으로 붙였다면 이 장소가 여러 번 나온다(장소당 1행 확인).
                Review(_me, _both, Now),
                Review(_me, _both, Now.AddDays(-1)),
                // 남의 기록 — 내 지도에 섞이면 안 된다. _both에도 남의 리뷰를 둬서 EXISTS가 회원으로 거르는지 본다.
                Bookmark.Create(_other.Id, _othersOnly.Id, Now),
                Review(_other, _othersOnly, Now),
                Review(_other, _bookmarkedOnly, Now));
            await db.SaveChangesAsync();
        }

        var places = await _client.GetFromJsonAsync<List<MyPlaceView>>("/me/places");
        Assert.NotNull(places);

        // 순서는 장소 ID 순이지만, .NET Guid와 PostgreSQL uuid의 정렬 규칙이 달라 여기선 순서 없이 비교한다.
        Assert.Equal(
            new Dictionary<Guid, string>
            {
                [_bookmarkedOnly.Id.Value] = "gray",
                [_reviewedOnly.Id.Value] = "fill",
                [_both.Id.Value] = "fill",
            },
            places.ToDictionary(p => p.Id, p => p.Star));

        var both = places.Single(p => p.Id == _both.Id.Value);
        Assert.Equal(new MyPlaceView(
            _both.Id.Value, "3", "둘 다 한 곳", "서울", 37.5665, 126.978, "fill"), both);
    }

    [Fact]
    public async Task Places_WithoutToken_Returns401()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/me/places");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsMyIdEmailNickname()
    {
        var me = await _client.GetFromJsonAsync<MeView>("/me");

        Assert.Equal(new MeView(_me.Id.Value, "nook@example.com", "누크", AvatarUrl: null), me);   // 사진 없음 = 기본 이미지
    }

    [Fact]
    public async Task Get_WhenUserRowMissing_Returns401UnknownUser()
    {
        // 서명은 진짜(같은 발급기)지만 DB에 없는 회원 — 탈퇴 후 남은 토큰, DB 초기화와 같은 상황.
        using var ghost = _factory.CreateClient();
        var token = _factory.Services.GetRequiredService<AccessTokenIssuer>().Issue(Guid.CreateVersion7());
        ghost.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await ghost.GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Auth.UnknownUser", problem!.Title);
    }

    [Fact]
    public async Task Get_WithoutToken_Returns401WithoutErrorCode()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("Auth.UnknownUser", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Reviews_PagesNewestFirstFiveAtATime_OnlyMine()
    {
        // 내 리뷰 6개: 장소 4곳 × 서로 다른 날(R-RV-03 UNIQUE를 피한다). i가 작을수록 최신.
        Place[] places = [_bookmarkedOnly, _reviewedOnly, _both, _othersOnly];
        var mine = Enumerable.Range(0, 6).Select(i => Review(_me, places[i % 4], Now.AddDays(-i))).ToList();
        await using (var db = Db.CreateContext())
        {
            db.AddRange(mine);
            db.Add(Review(_other, _both, Now.AddHours(1)));                 // 남의 리뷰 — 가장 최신이어도 섞이면 안 된다
            db.Add(Like.Create(_me.Id, mine[0].Id, Now));                   // 내 리뷰에 내 좋아요
            db.Add(Like.Create(_other.Id, mine[0].Id, Now));
            await db.SaveChangesAsync();
        }

        var first = await _client.GetFromJsonAsync<MyReviewPageView>("/me/reviews");
        var second = await _client.GetFromJsonAsync<MyReviewPageView>($"/me/reviews?cursor={Uri.EscapeDataString(first!.NextCursor!)}");

        Assert.Equal(mine.Take(5).Select(r => r.Id.Value), first.Items.Select(r => r.Id));
        Assert.Equal([mine[5].Id.Value], second!.Items.Select(r => r.Id));
        Assert.Null(second.NextCursor);

        var newest = first.Items[0];
        Assert.Equal(("저장만 한 곳", 2, true), (newest.PlaceName, newest.LikeCount, newest.LikedByMe));
    }

    [Fact]
    public async Task Reviews_InvalidCursor_Returns400()
    {
        var response = await _client.GetAsync("/me/reviews?cursor=not-a-cursor");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Review.InvalidCursor", problem!.Title);
    }

    [Fact]
    public async Task Patch_ChangesNickname_Returns200AndPersists()
    {
        var response = await _client.PatchAsJsonAsync("/me", new { nickname = "새닉네임" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("새닉네임", (await response.Content.ReadFromJsonAsync<MeView>())!.Nickname);
        Assert.Equal("새닉네임", (await _client.GetFromJsonAsync<MeView>("/me"))!.Nickname);
    }

    [Fact]
    public async Task Patch_SameNickname_Returns200()
    {
        var response = await _client.PatchAsJsonAsync("/me", new { nickname = "누크" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Patch_NicknameOfOther_Returns409()
    {
        var response = await _client.PatchAsJsonAsync("/me", new { nickname = "남의회원" });

        await AssertProblemTitleAsync(response, HttpStatusCode.Conflict, "User.NicknameTaken");
        Assert.Equal("누크", (await _client.GetFromJsonAsync<MeView>("/me"))!.Nickname);
    }

    [Theory]
    [InlineData("a")]           // 2자 미만
    [InlineData("nook!")]       // 허용 문자 밖
    [InlineData(null)]
    public async Task Patch_InvalidFormat_Returns400(string? nickname)
    {
        var response = await _client.PatchAsJsonAsync("/me", new { nickname });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patch_TwoMembersSameNicknameConcurrently_One200One409()
    {
        using var otherClient = _factory.CreateClient();
        var token = _factory.Services.GetRequiredService<AccessTokenIssuer>().Issue(_other.Id.Value);
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var responses = await Task.WhenAll(
            _client.PatchAsJsonAsync("/me", new { nickname = "동시닉" }),
            otherClient.PatchAsJsonAsync("/me", new { nickname = "동시닉" }));

        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.Conflict],
            responses.Select(r => r.StatusCode).OrderBy(c => (int)c));
    }

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D];

    private Task<HttpResponseMessage> PutAvatarAsync(byte[] bytes)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");   // 형식은 매직 바이트로 판별 — 헤더를 믿지 않는다
        return _client.PutAsync("/me/avatar", new MultipartFormDataContent { { file, "Image", "me.bin" } });
    }

    [Fact]
    public async Task PutAvatar_StoresAndShowsUrlInMeAndReviewAuthor()
    {
        await using (var db = Db.CreateContext())
        {
            db.Add(Review(_me, _both, Now));
            await db.SaveChangesAsync();
        }

        var response = await PutAvatarAsync(Png);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<MeView>();
        Assert.StartsWith("/api/nook/images/avatars/", me!.AvatarUrl);
        Assert.Equal(me.AvatarUrl, (await _client.GetFromJsonAsync<MeView>("/me"))!.AvatarUrl);

        var page = await _client.GetFromJsonAsync<ReviewPageView>($"/places/{_both.Id.Value}/reviews");
        Assert.Equal(me.AvatarUrl, page!.Items.Single().Author.AvatarUrl);

        var key = me.AvatarUrl!["/api/nook/images/".Length..];
        Assert.Equal(Png, (await _client.GetByteArrayAsync($"/images/{key}")));
    }

    [Fact]
    public async Task PutAvatar_Twice_DeletesPreviousAfterSave()
    {
        var first = (await (await PutAvatarAsync(Png)).Content.ReadFromJsonAsync<MeView>())!.AvatarUrl;
        var second = (await (await PutAvatarAsync(Png)).Content.ReadFromJsonAsync<MeView>())!.AvatarUrl;

        Assert.NotEqual(first, second);   // 교체 = 새 키(캐시 immutable과 맞다)
        Assert.Equal([second!["/api/nook/images/".Length..]], _factory.ImageStorage.Objects.Keys);
    }

    [Fact]
    public async Task PutAvatar_UnsupportedFormat_Returns400AndUploadsNothing()
    {
        var response = await PutAvatarAsync("GIF89a      "u8.ToArray());

        await AssertProblemTitleAsync(response, HttpStatusCode.BadRequest, "User.AvatarUnsupportedFormat");
        Assert.Empty(_factory.ImageStorage.Objects);
    }

    [Fact]
    public async Task DeleteAvatar_RemovesFileAndIsIdempotent()
    {
        await PutAvatarAsync(Png);

        var first = await _client.DeleteAsync("/me/avatar");
        var second = await _client.DeleteAsync("/me/avatar");

        Assert.Equal((HttpStatusCode.OK, HttpStatusCode.OK), (first.StatusCode, second.StatusCode));
        Assert.Null((await _client.GetFromJsonAsync<MeView>("/me"))!.AvatarUrl);
        Assert.Empty(_factory.ImageStorage.Objects);
    }

    private static async Task AssertProblemTitleAsync(HttpResponseMessage response, HttpStatusCode status, string title)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(title, (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Title);
    }

    private static Place NewPlace(string kakaoId, string name) =>
        Place.Create(KakaoPlaceId.From(kakaoId), name, "서울", Coordinate.Create(37.5665, 126.978).Unwrap()).Unwrap();

    private static Review Review(User author, Place place, DateTimeOffset at) =>
        Domain.Reviews.Review.Create(author.Id, place.Id, ReviewContent.From("조용하고 커피가 맛있는 곳"), [], at).Unwrap();
}
