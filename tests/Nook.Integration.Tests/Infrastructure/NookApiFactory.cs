using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nook.Application.Auth;
using Nook.Domain.Users;
using Nook.Infrastructure;
using Nook.Infrastructure.Reviews;

namespace Nook.Integration.Tests.Infrastructure;

// Program.cs의 파이프라인(인증·인가 미들웨어, 컨트롤러, 상태 코드 번역)을 메모리 안에서 띄운다. 포트를 열지 않는다.
// 바꾸는 것은 바깥 경계 넷뿐이다: DB(컨테이너), 비밀값(테스트 키), 메일(받은 토큰을 기록만), 이미지 스토리지(메모리).
// configureServices: 테스트가 경계를 하나 더 바꿀 때(고정 시계, 업로드를 붙잡는 스토리지). 기본 등록 뒤에 실행되므로 마지막 등록이 이긴다.
public sealed class NookApiFactory(string connectionString, Action<IServiceCollection>? configureServices = null)
    : WebApplicationFactory<Program>
{
    // Development가 아니라 별도 환경: user-secrets(로컬 DB·실제 키)와 개발 전용 분기(콘솔 발송기, OpenAPI)가 끼어들지 않는다.
    private const string EnvironmentName = "Testing";

    // HS256 최소 32바이트(JwtOptions). 테스트 전용 값이라 커밋해도 된다.
    public const string SigningKey = "nook-integration-tests-signing-key-0123456789";

    public RecordingMailQueue Mail => Services.GetRequiredService<RecordingMailQueue>();

    public InMemoryReviewImageStorage ImageStorage => Services.GetRequiredService<InMemoryReviewImageStorage>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);

        // UseSetting은 Program.cs가 builder.Configuration을 읽기 전에 들어간다(Build 전에 읽는 연결 문자열도 바뀐다).
        builder.UseSetting("ConnectionStrings:Nook", connectionString);
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("MagicLink:LinkBaseUrl", "http://localhost/auth/magic-link");
        // Development 밖에서는 구현체 설정이 필수(fail-fast). 워커가 발송기 없이는 시작하지 않으므로 콘솔로 채운다 — 큐가 비어 실제로 쓰이지 않는다.
        // 이미지 저장소는 아래에서 메모리 구현으로 덮어쓴다(마지막 등록이 이긴다). LocalDisk 생성자는 디스크를 건드리지 않는다.
        builder.UseSetting("Email:Provider", "Console");
        builder.UseSetting("ImageStorage:Provider", "LocalDisk");

        builder.ConfigureTestServices(services =>
        {
            // 마지막 등록이 이긴다: 핸들러는 기록용 큐를 받는다. 운영 큐·워커는 남지만 아무것도 받지 않는다.
            services.AddSingleton<RecordingMailQueue>();
            services.RemoveAll<IMagicLinkMailQueue>();
            services.AddSingleton<IMagicLinkMailQueue>(sp => sp.GetRequiredService<RecordingMailQueue>());
            // 리뷰 이미지는 메모리에 — 테스트가 ImageStorage.Objects로 올라간 것을 확인한다.
            services.AddInMemoryReviewImageStorage();

            configureServices?.Invoke(services);
        });
    }
}

// 메일 대신 "누구에게 어떤 원문 토큰을 보내려 했는지"만 남긴다. 테스트가 메일함 대신 여기서 링크를 꺼낸다.
public sealed class RecordingMailQueue : IMagicLinkMailQueue
{
    private readonly List<(string To, string RawToken)> _sent = [];

    public IReadOnlyList<(string To, string RawToken)> Sent
    {
        get { lock (_sent) return [.. _sent]; }
    }

    public void Enqueue(Email email, string rawToken)
    {
        lock (_sent) _sent.Add((email.Value, rawToken));
    }
}
