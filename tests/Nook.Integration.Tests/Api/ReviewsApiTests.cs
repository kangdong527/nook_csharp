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

    // 시계를 고정한다: 사전 확인 날짜·키 날짜·CreatedAt이 이 값 하나에서 나오는지 확인할 수 있다.
    // 20:00Z = KST 다음 날 05:00 — 로컬 날짜와 UTC 날짜가 갈리는 시각이라, 날짜를 로컬로 계산하면 드러난다.
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 20, 0, 0, TimeSpan.Zero);

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0, 1];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D];
    private static readonly byte[] Gif = "GIF89a\0\0\0\0\0\0"u8.ToArray();

    private const string Content = "조용하고 커피가 맛있는 곳";

    private readonly PostgresFixture _fixture;
    private readonly User _user = User.Create(Nickname.From("누크"), Email.From("nook@example.com"));
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
        Assert.Collection(view.ImageKeys,
            k => Assert.Matches(@"^reviews/2026/10/22/[0-9a-f]{32}\.jpg$", k),
            k => Assert.Matches(@"^reviews/2026/10/22/[0-9a-f]{32}\.png$", k));
        Assert.Equal("image/jpeg", _factory.ImageStorage.Objects[view.ImageKeys[0]].ContentType);
        Assert.Equal(Png, _factory.ImageStorage.Objects[view.ImageKeys[1]].Bytes);
        Assert.Equal(2, _factory.ImageStorage.Objects.Count);

        await using var db = Db.CreateContext();
        var saved = await db.Reviews.SingleAsync();
        Assert.Equal(DateOnly.FromDateTime(Now.UtcDateTime), saved.ReviewDate);
        Assert.Equal(view.ImageKeys, saved.Images.Select(i => i.StorageKey));
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

    private (NookApiFactory, HttpClient) CreateClient(Action<IServiceCollection>? configure = null)
    {
        var factory = new NookApiFactory(_fixture.ConnectionString, services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedClock(Now));
            configure?.Invoke(services);
        });
        var client = factory.CreateClient();
        // 토큰은 실제 시계로 발급한다 — JWT 검증은 DI 시계가 아니라 실제 시각을 보므로, 고정 시계로 발급하면 이미 만료된 토큰이 된다.
        var jwt = factory.Services.GetRequiredService<IOptions<JwtOptions>>();
        var token = new AccessTokenIssuer(jwt, TimeProvider.System).Issue(_user.Id.Value);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (factory, client);
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
}
