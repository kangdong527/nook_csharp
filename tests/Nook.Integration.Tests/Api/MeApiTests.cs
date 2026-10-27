using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Nook.Api.Auth;
using Microsoft.AspNetCore.Mvc;
using Nook.Application.Bookmarks;
using Nook.Application.Users;
using Nook.Domain.Bookmarks;
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

        Assert.Equal(new MeView(_me.Id.Value, "nook@example.com", "누크"), me);
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

    private static Place NewPlace(string kakaoId, string name) =>
        Place.Create(KakaoPlaceId.From(kakaoId), name, "서울", Coordinate.Create(37.5665, 126.978).Unwrap()).Unwrap();

    private static Review Review(User author, Place place, DateTimeOffset at) =>
        Domain.Reviews.Review.Create(author.Id, place.Id, ReviewContent.From("조용하고 커피가 맛있는 곳"), [], at).Unwrap();
}
