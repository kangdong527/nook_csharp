using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nook.Api.Auth;
using Nook.Application.Likes;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Infrastructure.Persistence;
using Nook.Integration.Tests.Infrastructure;

namespace Nook.Integration.Tests.Api;

// LK-01을 HTTP로. BM과 다른 점 — 리뷰는 삭제될 수 있어 사전 확인과 쓰기 사이의 틈을 실제 PostgreSQL로 재현한다.
public sealed class LikesApiTests : IntegrationTestBase, IAsyncLifetime, IDisposable
{
    // 테스트 목록
    //
    // 좋아요
    // - Add_New_ReturnsLikedChanged
    // - Add_WhenAlreadyLiked_ReturnsChangedFalse                          (멱등)
    // - Add_UnknownReview_Returns404AndAddsNothing
    // - Add_Concurrent_OneChangedAndOneRow                                (따닥 클릭)
    // - Add_WhileReviewDeleteUncommitted_WaitsThenReturns404OnCommit      (틈 사이 삭제 → fk_likes_reviews 23503 → ReviewGone)
    // - Add_WhileReviewDeleteUncommitted_WaitsThenLikesOnRollback
    //
    // 취소
    // - Remove_Liked_ReturnsNotLikedChanged
    // - Remove_WhenNotLiked_ReturnsChangedFalse                           (멱등)
    // - Remove_UnknownReview_Returns404
    // - Remove_WhileReviewDeleteUncommitted_WaitsThenReturns200ChangedFalse (CASCADE가 먼저 지움 — 번역 안 함, 사용자 결정)

    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    private readonly NookApiFactory _factory;
    private readonly HttpClient _client;
    private readonly User _author = User.Create(Nickname.From("작성자"), Email.From("author@example.com"));
    private readonly User _user = User.Create(Nickname.From("누크"), Email.From("nook@example.com"));
    private readonly Place _place = Place.Create(
        KakaoPlaceId.From("8130394"), "누크 카페", "서울", Coordinate.Create(37.5665, 126.978).Unwrap()).Unwrap();
    private readonly Review _review;

    public LikesApiTests(PostgresFixture fixture) : base(fixture)
    {
        _review = Review.Create(
            _author.Id, _place.Id, ReviewContent.From("조용하고 커피가 맛있는 곳"), [], CreatedAt).Unwrap();

        _factory = new NookApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
        var token = _factory.Services.GetRequiredService<AccessTokenIssuer>().Issue(_user.Id.Value);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // 기반 클래스의 빈 DB 초기화 뒤에 넣는다(likes FK: users·reviews, reviews FK: users·places).
    async Task IAsyncLifetime.InitializeAsync()
    {
        await InitializeAsync();
        await using var db = Db.CreateContext();
        db.AddRange(_author, _user, _place, _review);
        await db.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();

    // ── 좋아요 ─────────────────────────────────────────────

    [Fact]
    public async Task Add_New_ReturnsLikedChanged()
    {
        var view = await AddAsync();

        Assert.Equal(new LikeView(Liked: true, Changed: true), view);
        Assert.Equal(1, await CountLikesAsync());
    }

    [Fact]
    public async Task Add_WhenAlreadyLiked_ReturnsChangedFalse()
    {
        await AddAsync();

        var view = await AddAsync();

        Assert.Equal(new LikeView(Liked: true, Changed: false), view);
        Assert.Equal(1, await CountLikesAsync());
    }

    [Fact]
    public async Task Add_UnknownReview_Returns404AndAddsNothing()
    {
        var response = await _client.PutAsync($"/reviews/{Guid.NewGuid()}/like", null);

        await AssertReviewNotFoundAsync(response);
        Assert.Equal(0, await CountLikesAsync());
    }

    [Fact]
    public async Task Add_Concurrent_OneChangedAndOneRow()
    {
        var views = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => AddAsync()));

        Assert.Single(views, v => v.Changed);
        Assert.All(views, v => Assert.True(v.Liked));
        Assert.Equal(1, await CountLikesAsync());
    }

    [Fact]
    public async Task Add_WhileReviewDeleteUncommitted_WaitsThenReturns404OnCommit()
    {
        // 사전 확인(일반 SELECT)은 미커밋 DELETE를 보지 않아 통과한다 → INSERT의 FK 검사가 리뷰 행 잠금을 기다린다.
        await using var deleter = Db.CreateContext();
        await using var tx = await deleter.Database.BeginTransactionAsync();
        await DeleteReviewAsync(deleter);

        var add = _client.PutAsync($"/reviews/{_review.Id.Value}/like", null);

        await LockWait.AssertBlockedByAsync(Db, deleter, add);
        await tx.CommitAsync();

        await AssertReviewNotFoundAsync(await add);
        Assert.Equal(0, await CountLikesAsync());
    }

    [Fact]
    public async Task Add_WhileReviewDeleteUncommitted_WaitsThenLikesOnRollback()
    {
        await using var deleter = Db.CreateContext();
        await using var tx = await deleter.Database.BeginTransactionAsync();
        await DeleteReviewAsync(deleter);

        var add = _client.PutAsync($"/reviews/{_review.Id.Value}/like", null);

        await LockWait.AssertBlockedByAsync(Db, deleter, add);
        await tx.RollbackAsync();

        var response = await add;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new LikeView(Liked: true, Changed: true), await response.Content.ReadFromJsonAsync<LikeView>());
    }

    // ── 취소 ───────────────────────────────────────────────

    [Fact]
    public async Task Remove_Liked_ReturnsNotLikedChanged()
    {
        await AddAsync();

        var view = await RemoveAsync();

        Assert.Equal(new LikeView(Liked: false, Changed: true), view);
        Assert.Equal(0, await CountLikesAsync());
    }

    [Fact]
    public async Task Remove_WhenNotLiked_ReturnsChangedFalse()
    {
        var view = await RemoveAsync();

        Assert.Equal(new LikeView(Liked: false, Changed: false), view);
    }

    [Fact]
    public async Task Remove_UnknownReview_Returns404()
    {
        var response = await _client.DeleteAsync($"/reviews/{Guid.NewGuid()}/like");

        await AssertReviewNotFoundAsync(response);
    }

    [Fact]
    public async Task Remove_WhileReviewDeleteUncommitted_WaitsThenReturns200ChangedFalse()
    {
        await AddAsync();

        // 미커밋 DELETE reviews가 CASCADE로 like 행까지 잠근다 → 취소 DELETE는 그 행을 기다린 뒤, 커밋되면 0행.
        await using var deleter = Db.CreateContext();
        await using var tx = await deleter.Database.BeginTransactionAsync();
        await DeleteReviewAsync(deleter);

        var remove = _client.DeleteAsync($"/reviews/{_review.Id.Value}/like");

        await LockWait.AssertBlockedByAsync(Db, deleter, remove);
        await tx.CommitAsync();

        var response = await remove;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new LikeView(Liked: false, Changed: false), await response.Content.ReadFromJsonAsync<LikeView>());
    }

    // ── 도우미 ─────────────────────────────────────────────

    private async Task<LikeView> AddAsync()
    {
        var response = await _client.PutAsync($"/reviews/{_review.Id.Value}/like", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LikeView>())!;
    }

    private async Task<LikeView> RemoveAsync()
    {
        var response = await _client.DeleteAsync($"/reviews/{_review.Id.Value}/like");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LikeView>())!;
    }

    // RV-03 대신 SQL로 직접 — RV-03은 아직 없다. 트랜잭션은 호출한 쪽이 열고 닫는다.
    private Task DeleteReviewAsync(NookDbContext deleter) =>
        deleter.Database.ExecuteSqlAsync($"DELETE FROM reviews WHERE id = {_review.Id.Value}");

    // 라우트가 없어서 나는 404와 구분한다 — 핸들러가 낸 404인지 오류 코드로 확인.
    private static async Task AssertReviewNotFoundAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(LikeErrors.ReviewNotFound.Code, problem?.Title);
    }

    private async Task<int> CountLikesAsync()
    {
        await using var db = Db.CreateContext();
        return await db.Likes.CountAsync();
    }
}
