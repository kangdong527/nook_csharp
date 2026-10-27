using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nook.Api.Auth;
using Nook.Application.Bookmarks;
using Nook.Domain.Bookmarks;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Integration.Tests.Infrastructure;

namespace Nook.Integration.Tests.Api;

// BM-01·02를 HTTP로. 저장소 SQL(ON CONFLICT·DELETE 0행)까지 실제 PostgreSQL에서 본다.
public sealed class BookmarksApiTests : IntegrationTestBase, IAsyncLifetime, IDisposable
{
    // 테스트 목록
    //
    // BM-01 저장
    // - Save_NewBookmark_ReturnsBookmarkedChangedGray
    // - Save_WhenAlreadyBookmarked_ReturnsChangedFalseStillGray   (2a 멱등 — changed와 bookmarked는 다른 질문)
    // - Save_WhenReviewed_ReturnsFill
    // - Save_UnknownPlace_Returns404AndAddsNothing                 (2b)
    // - Save_Concurrent_OneChangedAndOneRow                        (1b 따닥 클릭)
    //
    // BM-02 해제
    // - Remove_Bookmarked_ReturnsNotBookmarkedChangedEmpty
    // - Remove_WhenNotBookmarked_ReturnsChangedFalse                (2a)
    // - Remove_WhenReviewed_StaysFill                               (R-STAR X,O)
    // - Remove_UnknownPlace_Returns404                              (2b)
    //
    // 경로의 Guid.Empty → 500(사용자 결정)은 여기서 보지 않는다: 예외 처리 미들웨어가 없어 TestServer가 예외를 그대로 던진다.

    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    private readonly NookApiFactory _factory;
    private readonly HttpClient _client;
    private readonly User _user = User.Create(Nickname.From("누크"), Email.From("nook@example.com"), Gender.None, null);
    private readonly Place _place = Place.Create(
        KakaoPlaceId.From("8130394"), "누크 카페", "서울", Coordinate.Create(37.5665, 126.978).Unwrap()).Unwrap();

    public BookmarksApiTests(PostgresFixture fixture) : base(fixture)
    {
        _factory = new NookApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
        var token = _factory.Services.GetRequiredService<AccessTokenIssuer>().Issue(_user.Id.Value);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // 기반 클래스의 빈 DB 초기화 뒤에 회원·장소를 넣는다(bookmarks FK 둘 다).
    async Task IAsyncLifetime.InitializeAsync()
    {
        await InitializeAsync();
        await using var db = Db.CreateContext();
        db.AddRange(_user, _place);
        await db.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();

    // ── BM-01 저장 ─────────────────────────────────────────

    [Fact]
    public async Task Save_NewBookmark_ReturnsBookmarkedChangedGray()
    {
        var view = await SaveAsync();

        Assert.Equal(new BookmarkView(Bookmarked: true, Changed: true, Star: "gray"), view);
        Assert.Equal(1, await CountBookmarksAsync());
    }

    [Fact]
    public async Task Save_WhenAlreadyBookmarked_ReturnsChangedFalseStillGray()
    {
        await SaveAsync();

        var view = await SaveAsync();

        Assert.Equal(new BookmarkView(Bookmarked: true, Changed: false, Star: "gray"), view);
        Assert.Equal(1, await CountBookmarksAsync());
    }

    [Fact]
    public async Task Save_WhenReviewed_ReturnsFill()
    {
        await AddReviewAsync();

        var view = await SaveAsync();

        Assert.Equal("fill", view.Star);
    }

    [Fact]
    public async Task Save_UnknownPlace_Returns404AndAddsNothing()
    {
        var response = await _client.PutAsync($"/places/{Guid.NewGuid()}/bookmark", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await CountBookmarksAsync());
    }

    [Fact]
    public async Task Save_Concurrent_OneChangedAndOneRow()
    {
        var views = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => SaveAsync()));

        Assert.Single(views, v => v.Changed);
        Assert.All(views, v => Assert.True(v.Bookmarked));
        Assert.Equal(1, await CountBookmarksAsync());
    }

    // ── BM-02 해제 ─────────────────────────────────────────

    [Fact]
    public async Task Remove_Bookmarked_ReturnsNotBookmarkedChangedEmpty()
    {
        await SaveAsync();

        var view = await RemoveAsync();

        Assert.Equal(new BookmarkView(Bookmarked: false, Changed: true, Star: "empty"), view);
        Assert.Equal(0, await CountBookmarksAsync());
    }

    [Fact]
    public async Task Remove_WhenNotBookmarked_ReturnsChangedFalse()
    {
        var view = await RemoveAsync();

        Assert.Equal(new BookmarkView(Bookmarked: false, Changed: false, Star: "empty"), view);
    }

    [Fact]
    public async Task Remove_WhenReviewed_StaysFill()
    {
        await AddReviewAsync();
        await SaveAsync();

        var view = await RemoveAsync();

        Assert.Equal(new BookmarkView(Bookmarked: false, Changed: true, Star: "fill"), view);
    }

    [Fact]
    public async Task Remove_UnknownPlace_Returns404()
    {
        var response = await _client.DeleteAsync($"/places/{Guid.NewGuid()}/bookmark");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── 도우미 ─────────────────────────────────────────────

    private async Task<BookmarkView> SaveAsync()
    {
        var response = await _client.PutAsync($"/places/{_place.Id.Value}/bookmark", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<BookmarkView>())!;
    }

    private async Task<BookmarkView> RemoveAsync()
    {
        var response = await _client.DeleteAsync($"/places/{_place.Id.Value}/bookmark");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<BookmarkView>())!;
    }

    private async Task AddReviewAsync()
    {
        var review = Review.Create(
            _user.Id, _place.Id, ReviewContent.From("조용하고 커피가 맛있는 곳"), [], CreatedAt).Unwrap();
        await using var db = Db.CreateContext();
        db.Add(review);
        await db.SaveChangesAsync();
    }

    private async Task<int> CountBookmarksAsync()
    {
        await using var db = Db.CreateContext();
        return await db.Set<Bookmark>().CountAsync();
    }
}
