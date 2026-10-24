using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Nook.Api.Auth;
using Nook.Application.Places;
using Nook.Application.Reviews;
using Nook.Domain.Bookmarks;
using Nook.Domain.Likes;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Integration.Tests.Infrastructure;
using Nook.SharedKernel;

namespace Nook.Integration.Tests.Api;

// PL-02를 HTTP로. 장소 상세 + 리뷰 목록(키셋 무한 스크롤). 페이지를 끝까지 넘겼을 때 빠짐·겹침이 없는지가 핵심이다.
public sealed class PlaceDetailApiTests : IntegrationTestBase, IAsyncLifetime, IDisposable
{
    // 테스트 목록
    //
    // 장소 상세
    // - Get_ReturnsPlaceWithMyStar                          (북마크만 → gray)
    // - Get_UnknownPlace_Returns404
    //
    // 리뷰 목록
    // - Reviews_ReturnsNewestFirstWithAuthorLikesAndMine    (좋아요 수·내 좋아요·내 리뷰·이미지 순서)
    // - Reviews_PagingToEnd_NoGapNoDuplicateEvenWithTies    (같은 시각 리뷰가 페이지 경계에 걸림 → id 동점 깨기)
    // - Reviews_NewReviewDuringScroll_DoesNotRepeatItems    (OFFSET이었다면 앞 페이지 마지막 항목이 다시 나온다)
    // - Reviews_InvalidCursorOrLimit_Returns400
    // - Reviews_UnknownPlace_Returns404

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    private readonly NookApiFactory _factory;
    private readonly HttpClient _client;
    private readonly User _me = NewUser(0);
    private readonly Place _place = Place.Create(
        KakaoPlaceId.From("8130394"), "누크 카페", "서울", Coordinate.Create(37.5665, 126.978).Unwrap()).Unwrap();

    public PlaceDetailApiTests(PostgresFixture fixture) : base(fixture)
    {
        _factory = new NookApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
        var token = _factory.Services.GetRequiredService<AccessTokenIssuer>().Issue(_me.Id.Value);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await InitializeAsync();
        await SaveAsync(_me, _place);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Get_ReturnsPlaceWithMyStar()
    {
        await SaveAsync(Bookmark.Create(_me.Id, _place.Id, Now));

        var view = await _client.GetFromJsonAsync<PlaceDetailView>($"/places/{_place.Id.Value}");

        Assert.Equal(new PlaceDetailView(_place.Id.Value, "8130394", "누크 카페", "서울", 37.5665, 126.978, "gray"), view);
    }

    [Fact]
    public async Task Get_UnknownPlace_Returns404()
    {
        var response = await _client.GetAsync($"/places/{Guid.NewGuid()}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, PlaceQueryErrors.PlaceNotFound);
    }

    [Fact]
    public async Task Reviews_ReturnsNewestFirstWithAuthorLikesAndMine()
    {
        var other = NewUser(1);
        var third = NewUser(2);
        var mine = NewReview(_me, Now.AddDays(-1), "mine-0.jpg", "mine-1.jpg");
        var others = NewReview(other, Now);
        await SaveAsync(other, third, mine, others,
            Like.Create(_me.Id, others.Id, Now), Like.Create(third.Id, others.Id, Now), Like.Create(other.Id, mine.Id, Now));

        var page = await GetPageAsync();

        Assert.Null(page.NextCursor);
        Assert.Collection(page.Items,
            first =>
            {
                Assert.Equal(others.Id.Value, first.Id);
                Assert.Equal(new ReviewAuthorView(other.Id.Value, "회원1"), first.Author);
                Assert.Equal((2, true, false), (first.LikeCount, first.LikedByMe, first.Mine));
            },
            second =>
            {
                Assert.Equal(mine.Id.Value, second.Id);
                Assert.Equal((1, false, true), (second.LikeCount, second.LikedByMe, second.Mine));
                Assert.Equal(["reviews/seed/mine-0.jpg", "reviews/seed/mine-1.jpg"], second.Images.Select(i => i.Key));
            });
    }

    [Fact]
    public async Task Reviews_PagingToEnd_NoGapNoDuplicateEvenWithTies()
    {
        // 회원 5명 × 3일 = 15개. 같은 날의 5개는 created_at이 완전히 같다 — 4개씩 넘기면 동점 묶음이 페이지 경계에 걸린다.
        var users = Enumerable.Range(1, 5).Select(NewUser).ToList();
        var reviews = users.SelectMany(u => Enumerable.Range(0, 3).Select(d => NewReview(u, Now.AddDays(-d)))).ToList();
        await SaveAsync([.. users, .. reviews]);

        var seen = new List<PlaceReviewView>();
        string? cursor = null;
        var pages = 0;
        do
        {
            var page = await GetPageAsync(cursor, limit: 4);
            seen.AddRange(page.Items);
            cursor = page.NextCursor;
            pages++;
        } while (cursor is not null && pages < 10);

        Assert.Equal(4, pages);   // 4 + 4 + 4 + 3
        Assert.Equal(reviews.Select(r => r.Id.Value).Order(), seen.Select(r => r.Id).Order());   // 빠짐·겹침 없음
        Assert.Equal(seen.Select(r => r.CreatedAt).OrderDescending(), seen.Select(r => r.CreatedAt));   // 최신순
    }

    [Fact]
    public async Task Reviews_NewReviewDuringScroll_DoesNotRepeatItems()
    {
        var users = Enumerable.Range(1, 4).Select(NewUser).ToList();
        var reviews = users.Select((u, i) => NewReview(u, Now.AddHours(-i))).ToList();
        await SaveAsync([.. users, .. reviews]);
        var first = await GetPageAsync(limit: 2);

        // 첫 페이지를 본 뒤 맨 앞에 새 리뷰가 생긴다. OFFSET 2였다면 한 칸씩 밀려 first의 마지막 항목이 다시 나온다.
        var late = NewUser(9);
        await SaveAsync(late, NewReview(late, Now.AddHours(1)));
        var second = await GetPageAsync(first.NextCursor, limit: 2);

        Assert.Equal(
            reviews.Select(r => r.Id.Value),
            first.Items.Concat(second.Items).Select(r => r.Id));
    }

    [Theory]
    [InlineData("?cursor=not-a-cursor", "Review.InvalidCursor")]
    [InlineData("?cursor=AAAA", "Review.InvalidCursor")]
    [InlineData("?limit=0", "Review.InvalidLimit")]
    [InlineData("?limit=31", "Review.InvalidLimit")]
    public async Task Reviews_InvalidCursorOrLimit_Returns400(string queryString, string code)
    {
        var response = await _client.GetAsync($"/places/{_place.Id.Value}/reviews{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(code, (await response.Content.ReadFromJsonAsync<ProblemDetails>())?.Title);
    }

    [Fact]
    public async Task Reviews_UnknownPlace_Returns404()
    {
        var response = await _client.GetAsync($"/places/{Guid.NewGuid()}/reviews");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, PlaceQueryErrors.PlaceNotFound);
    }

    private async Task<ReviewPageView> GetPageAsync(string? cursor = null, int? limit = null)
    {
        var query = new List<string>();
        if (cursor is not null) query.Add($"cursor={Uri.EscapeDataString(cursor)}");
        if (limit is not null) query.Add($"limit={limit}");
        var url = $"/places/{_place.Id.Value}/reviews" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        var page = await _client.GetFromJsonAsync<ReviewPageView>(url);
        Assert.NotNull(page);
        return page;
    }

    private async Task SaveAsync(params object[] entities)
    {
        await using var db = Db.CreateContext();
        db.AddRange(entities);
        await db.SaveChangesAsync();
    }

    private static User NewUser(int n) => User.Create(Nickname.From($"회원{n}"), Email.From($"user{n}@example.com"));

    private Review NewReview(User author, DateTimeOffset at, params string[] images) =>
        Review.Create(
            author.Id, _place.Id, ReviewContent.From("조용하고 커피가 맛있는 곳"),
            [.. images.Select(name => $"reviews/seed/{name}")], at).Unwrap();

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, Error error)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(error.Code, (await response.Content.ReadFromJsonAsync<ProblemDetails>())?.Title);
    }
}
