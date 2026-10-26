using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nook.Api.Auth;
using Nook.Application.Reviews;
using Nook.Domain.Bookmarks;
using Nook.Domain.Likes;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Infrastructure.Reviews;
using Nook.Integration.Tests.Infrastructure;
using Nook.SharedKernel;

namespace Nook.Integration.Tests.Api;

// RV-01을 HTTP로. 응답 코드와 함께 "스토리지에 무엇이 남았나"를 단언한다 —
// 사전 검사는 정확성이 아니라 고아 이미지를 줄이는 장치라, 순서가 틀려도 응답 코드만으로는 드러나지 않는다.
public sealed class ReviewsApiTests : IntegrationTestBase, IAsyncLifetime, IDisposable
{
    // 테스트 목록
    //
    // - Create_WithTwoImages_Returns201AndStoresReviewAndImages
    // - Create_UnknownPlace_Returns404AndUploadsNothing                    (3a)
    // - Create_ThirdImageUnsupported_Returns400AndUploadsNothing           (2c — 검증을 전부 끝낸 뒤에 올린다)
    // - Create_FourImages_Returns400AndUploadsNothing                      (2b)
    // - Create_WhenAlreadyReviewedToday_Returns409AndUploadsNothing        (4a — 사전 확인)
    // - Create_ConcurrentBothPassPreCheck_One201One409AndOneReview         (6a — UNIQUE가 최종 판정, 진 쪽 이미지는 고아)
    //
    // RV-02 — 시작 상태: 이미지 A·B·C가 붙은 리뷰(스토리지 3개)
    // - Edit_KeepCAAndAddOne_Returns200AndPersistsOrderAndDeletesRemoved   (1)
    // - Edit_ThenReorderAgain_PersistsNewOrderAfterReload                  (2 — 저장 → 다시 읽기 → 순서 변경 → 다시 읽기)
    // - Edit_ByOtherUser_Returns403AndUploadsNothing                       (4b)
    // - Edit_UnknownReview_Returns404AndUploadsNothing                     (4a)
    // - Edit_TotalExceedsMax_Returns400AndUploadsNothing                   (3c)
    // - Edit_KeepIdFromOtherReview_Returns400AndUploadsNothing             (4c)
    // - Edit_WhenDeleteFails_StillReturns200                               (커밋 후 삭제 실패는 응답을 바꾸지 않는다)
    // - Edit_ConcurrentWithDifferentKeep_LaterSaveReturns409(B·C 먼저)    (6c — version, 순서 고정)
    // - Edit_ConcurrentWithDifferentKeep_LaterSaveReturns409(A 먼저)      (6c — 0행, 순서 고정)
    // - Edit_ConcurrentBothAppend_One200One409                             (6c — sort_order 23505, 커밋 시점)
    // - Edit_WhenReviewDeletedAfterRead_Returns404                         (6b)
    // - Edit_WithVersionSeenBeforeOtherEdit_Returns409AndUploadsNothing    (6c — 화면을 연 사이의 수정, 업로드 전 거절)
    // - Edit_WithoutVersion_Returns400AndUploadsNothing                    (Version 필수)
    //
    // RV-03 — 시작 상태: 내 리뷰 R(오늘, 이미지 2장, 남의 좋아요 1개), 스토리지 2개, 북마크 없음
    // - Delete_Own_Returns200ChangedAndCascadesImagesAndLikes               (1)
    // - Delete_WhenOtherDayReviewRemains_StarStaysFill                     (2 — R-RV-03은 하루 1개)
    // - Delete_WhenBookmarked_StarBecomesGray                              (3)
    // - Delete_Twice_SecondReturns200Unchanged                             (4, 3a′ 멱등)
    // - Delete_UnknownReview_Returns200Unchanged                           (5, 3a′)
    // - Delete_ByOtherUser_Returns403AndKeepsEverything                    (6, 3b′)
    // - Delete_Concurrent_OneChangedOneUnchanged                           (7, 3c)
    // - Delete_WhenStorageDeleteFails_StillReturns200                      (8)
    // - Delete_ThenCreateSameDay_Returns201                                (9 — R-RV-03 미결의 현재 동작 기록)

    // 시계를 고정한다: 사전 확인 날짜·키 날짜·CreatedAt이 이 값 하나에서 나오는지 확인할 수 있다.
    // 20:00Z = KST 다음 날 05:00 — 로컬 날짜와 UTC 날짜가 갈리는 시각이라, 날짜를 로컬로 계산하면 드러난다.
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 20, 0, 0, TimeSpan.Zero);

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0, 1];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D];
    private static readonly byte[] Gif = "GIF89a\0\0\0\0\0\0"u8.ToArray();

    private const string Content = "조용하고 커피가 맛있는 곳";
    private const int SeededVersion = 0;
    private const string EditedContent = "다시 가 보니 디저트도 맛있는 곳";

    private readonly PostgresFixture _fixture;
    private readonly User _user = User.Create(Nickname.From("누크"), Email.From("nook@example.com"));
    // RV-03: R에 좋아요를 누른 다른 회원 — 남의 리뷰 삭제(403)의 요청자도 겸한다. SeedReviewToDeleteAsync가 저장한다.
    private readonly User _liker = User.Create(Nickname.From("좋아요"), Email.From("liker@example.com"));
    private readonly Place _place = Place.Create(
        KakaoPlaceId.From("8130394"), "누크 카페", "서울", Coordinate.Create(37.5665, 126.978).Unwrap()).Unwrap();

    private NookApiFactory _factory;
    private HttpClient _client;

    public ReviewsApiTests(PostgresFixture fixture) : base(fixture)
    {
        _fixture = fixture;
        (_factory, _client) = CreateClient();
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await InitializeAsync();
        await using var db = Db.CreateContext();
        db.AddRange(_user, _place);
        await db.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Create_WithTwoImages_Returns201AndStoresReviewAndImages()
    {
        var response = await CreateAsync(_place.Id.Value, Jpeg, Png);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var view = await response.Content.ReadFromJsonAsync<ReviewView>();
        Assert.NotNull(view);
        Assert.Equal($"/reviews/{view.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(Content, view.Content);
        Assert.Equal(Now, view.CreatedAt);
        Assert.Equal("fill", view.Star);

        // 키 날짜는 고정 시계의 UTC 날짜, 확장자는 매직 바이트로 정한 형식, 순서는 보낸 순서.
        Assert.Collection(view.Images,
            i => Assert.Matches(@"^reviews/2026/10/22/[0-9a-f]{32}\.jpg$", i.Key),
            i => Assert.Matches(@"^reviews/2026/10/22/[0-9a-f]{32}\.png$", i.Key));
        Assert.Equal("image/jpeg", _factory.ImageStorage.Objects[view.Images[0].Key].ContentType);
        Assert.Equal(Png, _factory.ImageStorage.Objects[view.Images[1].Key].Bytes);
        Assert.Equal(2, _factory.ImageStorage.Objects.Count);

        await using var db = Db.CreateContext();
        var saved = await db.Reviews.SingleAsync();
        Assert.Equal(DateOnly.FromDateTime(Now.UtcDateTime), saved.ReviewDate);
        // 응답의 이미지 ID가 저장된 ID다 — 클라이언트가 RV-02의 KeepImageIds로 그대로 돌려보낸다(ADR-009 결정 2).
        Assert.Equal(view.Images, saved.Images.Select(i => new ReviewImageView(i.Id.Value, i.StorageKey)));
    }

    [Fact]
    public async Task Create_UnknownPlace_Returns404AndUploadsNothing()
    {
        var response = await CreateAsync(Guid.NewGuid(), Jpeg);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, ReviewWriteErrors.PlaceNotFound);
        Assert.Empty(_factory.ImageStorage.Objects);
    }

    [Fact]
    public async Task Create_ThirdImageUnsupported_Returns400AndUploadsNothing()
    {
        var response = await CreateAsync(_place.Id.Value, Jpeg, Png, Gif);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, ReviewWriteErrors.UnsupportedImageFormat);
        Assert.Empty(_factory.ImageStorage.Objects);
    }

    [Fact]
    public async Task Create_FourImages_Returns400AndUploadsNothing()
    {
        var response = await CreateAsync(_place.Id.Value, Jpeg, Jpeg, Jpeg, Jpeg);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, ReviewErrors.TooManyImages);
        Assert.Empty(_factory.ImageStorage.Objects);
    }

    [Fact]
    public async Task Create_WhenAlreadyReviewedToday_Returns409AndUploadsNothing()
    {
        // 같은 UTC 날짜의 이른 시각 — 사전 확인이 "지금"과 같은 날을 보는지.
        await using (var db = Db.CreateContext())
        {
            db.Reviews.Add(Review.Create(
                _user.Id, _place.Id, ReviewContent.From(Content), [], Now.AddHours(-8)).Unwrap());
            await db.SaveChangesAsync();
        }

        var response = await CreateAsync(_place.Id.Value, Jpeg);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, ReviewWriteErrors.AlreadyReviewedToday);
        Assert.Empty(_factory.ImageStorage.Objects);
    }

    [Fact]
    public async Task Create_ConcurrentBothPassPreCheck_One201One409AndOneReview()
    {
        // 그냥 동시에 보내면 늦은 쪽이 사전 확인에서 걸리는지(업로드 0), UNIQUE에서 걸리는지(업로드 1)가 매번 달라진다.
        // 업로드에서 두 요청을 만나게 해 "둘 다 사전 확인 통과"를 고정한다 — 6a 경로만 시험한다.
        _factory.Dispose();
        var rendezvous = new Barrier(participantCount: 2);
        (_factory, _client) = CreateClient(services =>
        {
            services.RemoveAll<IReviewImageStorage>();
            services.AddSingleton<IReviewImageStorage>(sp =>
                new RendezvousStorage(sp.GetRequiredService<InMemoryReviewImageStorage>(), rendezvous));
        });

        var responses = await Task.WhenAll(CreateAsync(_place.Id.Value, Jpeg), CreateAsync(_place.Id.Value, Jpeg));

        Assert.Equal(
            [HttpStatusCode.Created, HttpStatusCode.Conflict],
            responses.Select(r => r.StatusCode).Order());
        await AssertProblemAsync(
            responses.Single(r => r.StatusCode == HttpStatusCode.Conflict),
            HttpStatusCode.Conflict, ReviewWriteErrors.AlreadyReviewedToday);

        await using var db = Db.CreateContext();
        Assert.Equal(1, await db.Reviews.CountAsync());
        // 진 쪽 이미지는 고아로 남는다 — 핸들러는 지우지 않고 배치가 정리한다(ADR-009).
        Assert.Equal(2, _factory.ImageStorage.Objects.Count);
    }

    [Fact]
    public async Task Edit_KeepCAAndAddOne_Returns200AndPersistsOrderAndDeletesRemoved()
    {
        var (reviewId, a, b, c) = await SeedReviewWithThreeImagesAsync();

        var response = await EditAsync(reviewId, Ids(c, a), Png);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var view = await response.Content.ReadFromJsonAsync<ReviewView>();
        Assert.NotNull(view);
        Assert.Equal(EditedContent, view.Content);
        Assert.Equal("fill", view.Star);
        Assert.Equal(SeededVersion + 1, view.Version);

        // 다시 읽은 값이 기준이다 — 응답은 메모리의 애그리거트라 저장이 틀려도 맞게 나온다.
        var saved = await ReloadAsync(reviewId);
        Assert.Equal(EditedContent, saved.Content.Value);
        Assert.Equal([c.Id.Value, a.Id.Value, view.Images[2].Id], saved.Images.Select(i => i.Id.Value));
        Assert.Equal(view.Images, saved.Images.Select(i => new ReviewImageView(i.Id.Value, i.StorageKey)));

        // B만 지워지고 새 이미지가 올라간다: 3 - 1 + 1.
        Assert.Equal(3, _factory.ImageStorage.Objects.Count);
        Assert.DoesNotContain(b.StorageKey, _factory.ImageStorage.Objects.Keys);
    }

    [Fact]
    public async Task Edit_ThenReorderAgain_PersistsNewOrderAfterReload()
    {
        // 두 번째 수정은 EF가 DB에서 복원한 애그리거트에서 시작한다 — 순서를 _images 물리 순서로 매기면 여기서 드러난다.
        // 클라이언트처럼 첫 응답의 이미지 ID로 두 번째 요청을 만든다(DB를 들여다보지 않는다).
        var (reviewId, a, _, c) = await SeedReviewWithThreeImagesAsync();
        var first = await (await EditAsync(reviewId, Ids(c, a), Png)).Content.ReadFromJsonAsync<ReviewView>();
        var x = first!.Images[2].Id;

        var response = await EditAsync(reviewId, [x, c.Id.Value], [], _client, first.Version);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await ReloadAsync(reviewId);
        Assert.Equal([x, c.Id.Value], saved.Images.Select(i => i.Id.Value));
        Assert.Equal([0, 1], saved.Images.Select(i => i.SortOrder));
        Assert.Equal(2, _factory.ImageStorage.Objects.Count);
    }

    [Fact]
    public async Task Edit_ByOtherUser_Returns403AndUploadsNothing()
    {
        var (reviewId, a, b, c) = await SeedReviewWithThreeImagesAsync();
        var other = User.Create(Nickname.From("남의회원"), Email.From("other@example.com"));
        await using (var db = Db.CreateContext())
        {
            db.Add(other);
            await db.SaveChangesAsync();
        }
        using var otherClient = _factory.CreateClient();
        Authorize(otherClient, _factory, other.Id.Value);

        var response = await EditAsync(reviewId, Ids(a), [Png], otherClient);

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, ReviewWriteErrors.NotReviewAuthor);
        Assert.Equal(3, _factory.ImageStorage.Objects.Count);
        Assert.Equal([a.Id, b.Id, c.Id], (await ReloadAsync(reviewId)).Images.Select(i => i.Id));
    }

    [Fact]
    public async Task Edit_UnknownReview_Returns404AndUploadsNothing()
    {
        await SeedReviewWithThreeImagesAsync();

        var response = await EditAsync(Guid.NewGuid(), [], Png);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, ReviewWriteErrors.ReviewNotFound);
        Assert.Equal(3, _factory.ImageStorage.Objects.Count);
    }

    [Fact]
    public async Task Edit_TotalExceedsMax_Returns400AndUploadsNothing()
    {
        // 요청 하나만 보면 새 이미지 1장 — 합계(3 + 1)로 판단해야 걸린다.
        var (reviewId, a, b, c) = await SeedReviewWithThreeImagesAsync();

        var response = await EditAsync(reviewId, Ids(a, b, c), Png);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, ReviewErrors.TooManyImages);
        Assert.Equal(3, _factory.ImageStorage.Objects.Count);
    }

    [Fact]
    public async Task Edit_KeepIdFromOtherReview_Returns400AndUploadsNothing()
    {
        var (reviewId, a, _, _) = await SeedReviewWithThreeImagesAsync();
        var otherImage = await SeedOtherUsersReviewImageAsync();

        var response = await EditAsync(reviewId, Ids(a, otherImage), Png);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, ReviewErrors.ImageNotInReview);
        Assert.Equal(3, _factory.ImageStorage.Objects.Count);
    }

    [Fact]
    public async Task Edit_WhenDeleteFails_StillReturns200()
    {
        _factory.Dispose();
        (_factory, _client) = CreateClient(services =>
        {
            services.RemoveAll<IReviewImageStorage>();
            services.AddSingleton<IReviewImageStorage>(sp =>
                new FailingDeleteStorage(sp.GetRequiredService<InMemoryReviewImageStorage>()));
        });
        var (reviewId, a, _, _) = await SeedReviewWithThreeImagesAsync();

        var response = await EditAsync(reviewId, Ids(a));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([a.Id], (await ReloadAsync(reviewId)).Images.Select(i => i.Id));
        // B·C는 DB에서 빠졌지만 스토리지에 남는다 — 배치가 정리(ADR-009).
        Assert.Equal(3, _factory.ImageStorage.Objects.Count);
    }

    [Theory]
    [InlineData(true)]  // keep [B, C]가 먼저 커밋 — 진 쪽(keep [A])의 문장은 전부 1행·23505 없음. version만 잡는다
    [InlineData(false)] // keep [A]가 먼저 커밋 — 진 쪽(keep [B, C])의 B sort_order UPDATE가 0행
    public async Task Edit_ConcurrentWithDifferentKeep_LaterSaveReturns409(bool keepBcCommitsFirst)
    {
        // 업로드 지점에서 만나게 해 "둘 다 조회를 마친 뒤 저장"을 고정하고, 늦은 쪽은 이긴 쪽이 커밋한 뒤에 풀어 준다.
        // 순서를 스케줄러에 맡기면 version 변이를 절반만 잡는다(incident 002 §7).
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        UseOrderedGateStorage(heldContentType: keepBcCommitsFirst ? "image/jpeg" : "image/png", release.Task);
        var (reviewId, a, b, c) = await SeedReviewWithThreeImagesAsync();

        var keepA = EditAsync(reviewId, Ids(a), Jpeg);
        var keepBc = EditAsync(reviewId, Ids(b, c), Png);
        var (first, later) = keepBcCommitsFirst ? (keepBc, keepA) : (keepA, keepBc);

        Assert.Equal(HttpStatusCode.OK, (await first).StatusCode);
        release.SetResult();
        Assert.Equal(HttpStatusCode.Conflict, (await later).StatusCode); // 본문(에러 코드)은 아래 공통 단언이 읽는다 — 두 번 읽으면 스트림이 닫혀 있다
        await AssertOneWinnerOneConflictAsync([await first, await later], reviewId);
    }

    [Fact]
    public async Task Edit_ConcurrentBothAppend_One200One409()
    {
        // 둘 다 기존 이미지를 전부 남기므로 0행이 나오지 않는다. 새 이미지가 같은 sort_order(2)를 노려
        // DEFERRED UNIQUE가 진 쪽 COMMIT에서 걸린다 — 시작 상태가 2장인 이유.
        UseRendezvousStorage();
        var (reviewId, a, b) = await SeedReviewAsync("a.jpg", "b.jpg") switch { var (id, i) => (id, i[0], i[1]) };

        var responses = await Task.WhenAll(EditAsync(reviewId, Ids(a, b), Jpeg), EditAsync(reviewId, Ids(a, b), Png));

        await AssertOneWinnerOneConflictAsync(responses, reviewId);
    }

    [Fact]
    public async Task Edit_WhenReviewDeletedAfterRead_Returns404()
    {
        // 조회(4단계)와 저장(6단계) 사이 — 업로드 지점 — 에서 리뷰를 지운다. RV-03이 먼저 커밋한 상황.
        Guid? deleteOnUpload = null;
        _factory.Dispose();
        (_factory, _client) = CreateClient(services =>
        {
            services.RemoveAll<IReviewImageStorage>();
            services.AddSingleton<IReviewImageStorage>(sp =>
                new BeforePutStorage(sp.GetRequiredService<InMemoryReviewImageStorage>(), async () =>
                {
                    await using var db = Db.CreateContext();
                    await db.Reviews.Where(r => r.Id == ReviewId.From(deleteOnUpload!.Value)).ExecuteDeleteAsync();
                }));
        });
        var (reviewId, a, _, _) = await SeedReviewWithThreeImagesAsync();
        deleteOnUpload = reviewId;

        var response = await EditAsync(reviewId, Ids(a), Png);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, ReviewWriteErrors.ReviewNotFound);
        await using var check = Db.CreateContext();
        Assert.False(await check.Reviews.AnyAsync());
        // 기존 3장은 이 테스트에서 지운 쪽(가짜 RV-03)이 맡을 몫, 새 1장은 고아.
        Assert.Equal(4, _factory.ImageStorage.Objects.Count);
    }

    [Fact]
    public async Task Edit_WithVersionSeenBeforeOtherEdit_Returns409AndUploadsNothing()
    {
        // 화면 A가 목록에서 버전을 받아 둔 사이 다른 기기의 수정이 커밋된다. 화면 A의 저장은 요청 안에서는 낡은 데이터가
        // 없으므로(최신을 읽는다) 화면이 본 버전을 돌려받지 않으면 다른 기기의 수정을 덮어쓴다(incident 002 미결 ②).
        var (reviewId, a, b, c) = await SeedReviewWithThreeImagesAsync();
        var seen = await GetPlaceReviewVersionAsync(reviewId);
        Assert.Equal(HttpStatusCode.OK, (await EditAsync(reviewId, Ids(a, b, c), [], _client, seen)).StatusCode);

        var response = await EditAsync(reviewId, Ids(a), [Png], _client, seen);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, ReviewWriteErrors.EditConflict);
        var saved = await ReloadAsync(reviewId);
        Assert.Equal(seen + 1, saved.Version);
        Assert.Equal([a.Id, b.Id, c.Id], saved.Images.Select(i => i.Id));
        // 거절은 업로드 전 — 고아 없음. 58줄의 version WHERE만으로도 409는 나지만 새 이미지가 고아로 남는다.
        Assert.Equal(3, _factory.ImageStorage.Objects.Count);
    }

    [Fact]
    public async Task Edit_WithoutVersion_Returns400AndUploadsNothing()
    {
        var (reviewId, a, _, _) = await SeedReviewWithThreeImagesAsync();

        var response = await EditAsync(reviewId, Ids(a), [Png], _client, version: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(3, (await ReloadAsync(reviewId)).Images.Count);
        Assert.Equal(3, _factory.ImageStorage.Objects.Count);
    }

    [Fact]
    public async Task Delete_Own_Returns200ChangedAndCascadesImagesAndLikes()
    {
        var reviewId = await SeedReviewToDeleteAsync();

        var response = await _client.DeleteAsync($"/reviews/{reviewId}");

        await AssertDeleteViewAsync(response, changed: true, star: "empty");
        await using var db = Db.CreateContext();
        Assert.False(await db.Reviews.AnyAsync());
        // 이미지 행은 owned라 Reviews와 함께 사라지고, 좋아요는 fk_likes_reviews CASCADE.
        Assert.Equal(0, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM review_images").SingleAsync());
        Assert.False(await db.Likes.AnyAsync());
        // CTE가 CASCADE 전의 이미지 키를 돌려줬다는 증거 — 키를 몰랐다면 스토리지에 남는다.
        Assert.Empty(_factory.ImageStorage.Objects);
    }

    [Fact]
    public async Task Delete_WhenOtherDayReviewRemains_StarStaysFill()
    {
        var reviewId = await SeedReviewToDeleteAsync();
        await SeedReviewAtAsync(Now.AddDays(-1), "yesterday.jpg");

        var response = await _client.DeleteAsync($"/reviews/{reviewId}");

        await AssertDeleteViewAsync(response, changed: true, star: "fill");
        Assert.Single(_factory.ImageStorage.Objects);
    }

    [Fact]
    public async Task Delete_WhenBookmarked_StarBecomesGray()
    {
        var reviewId = await SeedReviewToDeleteAsync();
        await using (var db = Db.CreateContext())
        {
            db.Bookmarks.Add(Bookmark.Create(_user.Id, _place.Id, Now));
            await db.SaveChangesAsync();
        }

        var response = await _client.DeleteAsync($"/reviews/{reviewId}");

        await AssertDeleteViewAsync(response, changed: true, star: "gray");
    }

    [Fact]
    public async Task Delete_Twice_SecondReturns200Unchanged()
    {
        var reviewId = await SeedReviewToDeleteAsync();
        await AssertDeleteViewAsync(await _client.DeleteAsync($"/reviews/{reviewId}"), changed: true, star: "empty");

        var second = await _client.DeleteAsync($"/reviews/{reviewId}");

        await AssertDeleteViewAsync(second, changed: false, star: null);
    }

    [Fact]
    public async Task Delete_UnknownReview_Returns200Unchanged()
    {
        await SeedReviewToDeleteAsync();

        var response = await _client.DeleteAsync($"/reviews/{Guid.NewGuid()}");

        await AssertDeleteViewAsync(response, changed: false, star: null);
        Assert.Equal(2, _factory.ImageStorage.Objects.Count);
    }

    [Fact]
    public async Task Delete_ByOtherUser_Returns403AndKeepsEverything()
    {
        var reviewId = await SeedReviewToDeleteAsync();
        using var otherClient = _factory.CreateClient();
        Authorize(otherClient, _factory, _liker.Id.Value);

        var response = await otherClient.DeleteAsync($"/reviews/{reviewId}");

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, ReviewWriteErrors.NotReviewAuthor);
        Assert.Equal(2, (await ReloadAsync(reviewId)).Images.Count);
        Assert.Equal(2, _factory.ImageStorage.Objects.Count);
    }

    [Fact]
    public async Task Delete_Concurrent_OneChangedOneUnchanged()
    {
        // DELETE 직전에서 두 요청을 만나게 한다 — 둘 다 "아직 있음"에서 출발해, 늦은 쪽이 행 잠금을 기다린 뒤 0행이 되는 3c 경로.
        _factory.Dispose();
        var rendezvous = new Barrier(participantCount: 2);
        (_factory, _client) = CreateClient(services =>
        {
            var original = services.Single(s => s.ServiceType == typeof(IReviewRepository));
            services.Remove(original);
            services.AddScoped<IReviewRepository>(sp => new RendezvousBeforeDeleteRepository(
                (IReviewRepository)ActivatorUtilities.CreateInstance(sp, original.ImplementationType!), rendezvous));
        });
        var reviewId = await SeedReviewToDeleteAsync();

        var responses = await Task.WhenAll(
            _client.DeleteAsync($"/reviews/{reviewId}"), _client.DeleteAsync($"/reviews/{reviewId}"));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var views = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<DeleteReviewView>()));
        Assert.Equal([false, true], views.Select(v => v!.Changed).Order());
        Assert.Empty(_factory.ImageStorage.Objects);
    }

    [Fact]
    public async Task Delete_WhenStorageDeleteFails_StillReturns200()
    {
        _factory.Dispose();
        (_factory, _client) = CreateClient(services =>
        {
            services.RemoveAll<IReviewImageStorage>();
            services.AddSingleton<IReviewImageStorage>(sp =>
                new FailingDeleteStorage(sp.GetRequiredService<InMemoryReviewImageStorage>()));
        });
        var reviewId = await SeedReviewToDeleteAsync();

        var response = await _client.DeleteAsync($"/reviews/{reviewId}");

        await AssertDeleteViewAsync(response, changed: true, star: "empty");
        await using var db = Db.CreateContext();
        Assert.False(await db.Reviews.AnyAsync());
        // DB가 기준이다 — 리뷰는 지워졌고, 남은 파일은 배치가 정리한다(ADR-009 결정 1).
        Assert.Equal(2, _factory.ImageStorage.Objects.Count);
    }

    [Fact]
    public async Task Delete_ThenCreateSameDay_Returns201()
    {
        // R-RV-03과 삭제의 관계는 미결(usecases.md RV-03, PM 확인 필요). 이 테스트는 결정이 아니라 현재 동작의 기록이다 —
        // ux_reviews_user_place_date는 남은 행만 보므로 오늘 다시 쓸 수 있다. 결정이 바뀌면 이 테스트부터 바꾼다.
        var reviewId = await SeedReviewToDeleteAsync();
        await _client.DeleteAsync($"/reviews/{reviewId}");

        var response = await CreateAsync(_place.Id.Value, Jpeg);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // RV-03 공통 시작 상태: 내 리뷰 R(오늘, 이미지 2장) + 다른 회원의 좋아요 1개.
    private async Task<Guid> SeedReviewToDeleteAsync()
    {
        var (reviewId, _) = await SeedReviewAsync("a.jpg", "b.jpg");
        await using var db = Db.CreateContext();
        db.AddRange(_liker, Like.Create(_liker.Id, ReviewId.From(reviewId), Now));
        await db.SaveChangesAsync();
        return reviewId;
    }

    private static async Task AssertDeleteViewAsync(HttpResponseMessage response, bool changed, string? star)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var view = await response.Content.ReadFromJsonAsync<DeleteReviewView>();
        Assert.Equal(new DeleteReviewView(changed, star), view);
    }

    // 6c 공통 단언: 한쪽만 반영되고, 이긴 쪽이 뺀 이미지만 지워지고, 진 쪽 새 이미지 1장이 고아로 남는다.
    private async Task AssertOneWinnerOneConflictAsync(HttpResponseMessage[] responses, Guid reviewId)
    {
        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.Conflict], responses.Select(r => r.StatusCode).Order());
        await AssertProblemAsync(
            responses.Single(r => r.StatusCode == HttpStatusCode.Conflict),
            HttpStatusCode.Conflict, ReviewWriteErrors.EditConflict);

        var winner = await responses.Single(r => r.StatusCode == HttpStatusCode.OK).Content.ReadFromJsonAsync<ReviewView>();
        var saved = await ReloadAsync(reviewId);
        Assert.Equal(winner!.Images.Select(i => i.Key), saved.Images.Select(i => i.StorageKey));
        Assert.Equal(saved.Images.Count + 1, _factory.ImageStorage.Objects.Count);
        Assert.All(saved.Images, i => Assert.Contains(i.StorageKey, _factory.ImageStorage.Objects.Keys));
    }

    private void UseRendezvousStorage()
    {
        _factory.Dispose();
        var rendezvous = new Barrier(participantCount: 2);
        (_factory, _client) = CreateClient(services =>
        {
            services.RemoveAll<IReviewImageStorage>();
            services.AddSingleton<IReviewImageStorage>(sp =>
                new RendezvousStorage(sp.GetRequiredService<InMemoryReviewImageStorage>(), rendezvous));
        });
    }

    private void UseOrderedGateStorage(string heldContentType, Task release)
    {
        _factory.Dispose();
        var rendezvous = new Barrier(participantCount: 2);
        (_factory, _client) = CreateClient(services =>
        {
            services.RemoveAll<IReviewImageStorage>();
            services.AddSingleton<IReviewImageStorage>(sp => new OrderedGateStorage(
                sp.GetRequiredService<InMemoryReviewImageStorage>(), rendezvous, heldContentType, release));
        });
    }

    // 시드는 DB와 메모리 스토리지에 직접 넣는다 — POST로 만들면 테스트용 스토리지(랑데부·삭제 실패)를 거쳐 버린다.
    private async Task<(Guid ReviewId, ReviewImage A, ReviewImage B, ReviewImage C)> SeedReviewWithThreeImagesAsync()
    {
        var (id, images) = await SeedReviewAsync("a.jpg", "b.jpg", "c.jpg");
        return (id, images[0], images[1], images[2]);
    }

    private Task<(Guid ReviewId, IReadOnlyList<ReviewImage> Images)> SeedReviewAsync(params string[] names) =>
        SeedReviewAtAsync(Now.AddHours(-1), names);

    private async Task<(Guid ReviewId, IReadOnlyList<ReviewImage> Images)> SeedReviewAtAsync(
        DateTimeOffset createdAt, params string[] names)
    {
        var keys = names.Select(n => $"reviews/seed/{n}").ToArray();
        var review = Review.Create(_user.Id, _place.Id, ReviewContent.From(Content), keys, createdAt).Unwrap();
        await using (var db = Db.CreateContext())
        {
            db.Reviews.Add(review);
            await db.SaveChangesAsync();
        }
        foreach (var key in keys)
            await _factory.ImageStorage.PutAsync(key, new MemoryStream(Jpeg), "image/jpeg", CancellationToken.None);
        return (review.Id.Value, review.Images);
    }

    // 다른 회원의 리뷰 이미지. 스토리지에는 넣지 않는다 — 단언하는 개수는 이 테스트 리뷰의 것만.
    private async Task<ReviewImage> SeedOtherUsersReviewImageAsync()
    {
        var other = User.Create(Nickname.From("남의회원"), Email.From("other@example.com"));
        var review = Review.Create(other.Id, _place.Id, ReviewContent.From(Content), ["reviews/seed/z.jpg"], Now).Unwrap();
        await using var db = Db.CreateContext();
        db.AddRange(other, review);
        await db.SaveChangesAsync();
        return review.Images[0];
    }

    private async Task<Review> ReloadAsync(Guid reviewId)
    {
        await using var db = Db.CreateContext();
        return await db.Reviews.AsNoTracking().SingleAsync(r => r.Id == ReviewId.From(reviewId));
    }

    // 클라이언트가 수정 화면에 들어가는 길 — 단건 GET이 없어 장소별 목록에서 버전을 얻는다.
    private async Task<int> GetPlaceReviewVersionAsync(Guid reviewId)
    {
        var page = await _client.GetFromJsonAsync<ReviewPageView>($"/places/{_place.Id.Value}/reviews");
        return page!.Items.Single(r => r.Id == reviewId).Version;
    }

    private static Guid[] Ids(params ReviewImage[] images) => [.. images.Select(i => i.Id.Value)];

    private Task<HttpResponseMessage> EditAsync(Guid reviewId, Guid[] keep, params byte[][] images) =>
        EditAsync(reviewId, keep, images, _client);

    // version: 화면이 받아 둔 값. 시드 직후 리뷰는 SeededVersion이라 기본값으로 둔다. null이면 폼에서 뺀다.
    private static Task<HttpResponseMessage> EditAsync(
        Guid reviewId, Guid[] keep, byte[][] images, HttpClient client, int? version = SeededVersion)
    {
        var form = new MultipartFormDataContent { { new StringContent(EditedContent), "Content" } };
        if (version is not null)
            form.Add(new StringContent(version.Value.ToString()), "Version");
        // 같은 이름을 여러 번 보내면 List<Guid>로 묶이고, 보낸 순서가 곧 KeepImageIds 순서다.
        foreach (var id in keep)
            form.Add(new StringContent(id.ToString()), "KeepImageIds");
        for (var i = 0; i < images.Length; i++)
        {
            var file = new ByteArrayContent(images[i]);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(file, "Images", $"photo{i}.bin");
        }
        return client.PutAsync($"/reviews/{reviewId}", form);
    }

    private (NookApiFactory, HttpClient) CreateClient(Action<IServiceCollection>? configure = null)
    {
        var factory = new NookApiFactory(_fixture.ConnectionString, services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedClock(Now));
            configure?.Invoke(services);
        });
        var client = factory.CreateClient();
        Authorize(client, factory, _user.Id.Value);
        return (factory, client);
    }

    // 토큰은 실제 시계로 발급한다 — JWT 검증은 DI 시계가 아니라 실제 시각을 보므로, 고정 시계로 발급하면 이미 만료된 토큰이 된다.
    private static void Authorize(HttpClient client, NookApiFactory factory, Guid userId)
    {
        var jwt = factory.Services.GetRequiredService<IOptions<JwtOptions>>();
        var token = new AccessTokenIssuer(jwt, TimeProvider.System).Issue(userId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private Task<HttpResponseMessage> CreateAsync(Guid placeId, params byte[][] images)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(placeId.ToString()), "PlaceId" },
            { new StringContent(Content), "Content" },
        };
        for (var i = 0; i < images.Length; i++)
        {
            // 파일명·Content-Type은 일부러 엉터리로 — 서버는 매직 바이트만 믿는다.
            var file = new ByteArrayContent(images[i]);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(file, "Images", $"photo{i}.bin");
        }
        return _client.PostAsync("/reviews", form);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, Error error)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(error.Code, problem?.Title);
    }

    // 로컬 시간대를 KST로 고정한다 — 테스트 결과가 실행 머신의 시간대에 따라 바뀌지 않게.
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        private static readonly TimeZoneInfo Kst = TimeZoneInfo.CreateCustomTimeZone("KST", TimeSpan.FromHours(9), "KST", "KST");

        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => Kst;
    }

    // 업로드 지점에서 두 요청이 서로를 기다린다. 둘 다 여기까지 왔다는 것은 둘 다 사전 확인(4단계)을 통과했다는 뜻.
    private sealed class RendezvousStorage(InMemoryReviewImageStorage inner, Barrier rendezvous) : IReviewImageStorage
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        public async Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken ct)
        {
            if (!await Task.Run(() => rendezvous.SignalAndWait(Timeout, ct), ct))
                throw new TimeoutException("상대 요청이 업로드 지점에 오지 않았다 — 사전 확인에서 걸렸거나 직렬화된 것.");
            await inner.PutAsync(storageKey, content, contentType, ct);
        }

        public Task DeleteAsync(string storageKey, CancellationToken ct) => inner.DeleteAsync(storageKey, ct);
    }

    // 랑데부 뒤 heldContentType 업로드만 붙잡는다 — 다른 요청이 커밋까지 끝낸 다음 테스트가 release로 풀어 준다.
    // 랑데부는 출발선(둘 다 조회 완료)을, release는 커밋 순서를 고정한다.
    private sealed class OrderedGateStorage(
        InMemoryReviewImageStorage inner, Barrier rendezvous, string heldContentType, Task release) : IReviewImageStorage
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        public async Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken ct)
        {
            if (!await Task.Run(() => rendezvous.SignalAndWait(Timeout, ct), ct))
                throw new TimeoutException("상대 요청이 업로드 지점에 오지 않았다 — 사전 확인에서 걸렸거나 직렬화된 것.");
            if (contentType == heldContentType)
                await release.WaitAsync(Timeout, ct);
            await inner.PutAsync(storageKey, content, contentType, ct);
        }

        public Task DeleteAsync(string storageKey, CancellationToken ct) => inner.DeleteAsync(storageKey, ct);
    }

    // 커밋 후 정리가 실패하는 스토리지. 업로드는 그대로.
    private sealed class FailingDeleteStorage(InMemoryReviewImageStorage inner) : IReviewImageStorage
    {
        public Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken ct) =>
            inner.PutAsync(storageKey, content, contentType, ct);

        public Task DeleteAsync(string storageKey, CancellationToken ct) =>
            throw new IOException("스토리지 삭제 실패(테스트)");
    }

    // 업로드 직전에 끼어든다 — 핸들러의 조회와 저장 사이에 다른 트랜잭션을 커밋시키는 지점.
    private sealed class BeforePutStorage(InMemoryReviewImageStorage inner, Func<Task> beforePut) : IReviewImageStorage
    {
        public async Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken ct)
        {
            await beforePut();
            await inner.PutAsync(storageKey, content, contentType, ct);
        }

        public Task DeleteAsync(string storageKey, CancellationToken ct) => inner.DeleteAsync(storageKey, ct);
    }

    // DELETE 직전에서 두 요청이 서로를 기다린다. 나머지는 원래 리포지토리 그대로.
    private sealed class RendezvousBeforeDeleteRepository(IReviewRepository inner, Barrier rendezvous) : IReviewRepository
    {
        public async Task<ReviewDeletion?> DeleteAsync(ReviewId reviewId, UserId userId, CancellationToken ct)
        {
            if (!await Task.Run(() => rendezvous.SignalAndWait(TimeSpan.FromSeconds(10), ct), ct))
                throw new TimeoutException("상대 요청이 DELETE 지점에 오지 않았다.");
            return await inner.DeleteAsync(reviewId, userId, ct);
        }

        public Task<bool> ExistsAsync(UserId userId, PlaceId placeId, CancellationToken ct) => inner.ExistsAsync(userId, placeId, ct);
        public Task<bool> ExistsAsync(ReviewId reviewId, CancellationToken ct) => inner.ExistsAsync(reviewId, ct);
        public Task<bool> ExistsOnDateAsync(UserId userId, PlaceId placeId, DateOnly reviewDate, CancellationToken ct) =>
            inner.ExistsOnDateAsync(userId, placeId, reviewDate, ct);
        public Task<AddReviewOutcome> TryAddAsync(Review review, CancellationToken ct) => inner.TryAddAsync(review, ct);
        public Task<Review?> GetAsync(ReviewId reviewId, CancellationToken ct) => inner.GetAsync(reviewId, ct);
        public Task<SaveReviewOutcome> TrySaveAsync(Review review, CancellationToken ct) => inner.TrySaveAsync(review, ct);
        public Task<IReadOnlyList<PlaceReview>> ListByPlaceAsync(
            PlaceId placeId, UserId me, ReviewCursor? after, int take, CancellationToken ct) =>
            inner.ListByPlaceAsync(placeId, me, after, take, ct);
    }
}
