using Amazon.Runtime;
using Amazon.S3;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Nook.Integration.Tests.Infrastructure;

// S3 호환 테스트 서버(adobe/s3mock). MinIO 공개 이미지는 더 이상 받을 수 없어 대체(2026-11-09 확인 — Docker Hub 없음, quay 401).
// 실제 AWS SDK 클라이언트로 붙는다 — 서명·경로 스타일·404 매핑까지 SDK 경로를 그대로 지난다.
// 실제 S3와 다를 수 있는 것(권한 403·버킷 정책·SSE)은 5단계 실제 버킷에서 확인한다.
public sealed class S3MockFixture : IAsyncLifetime
{
    public const string Bucket = "nook-images-test";
    public const string Region = "ap-northeast-2";
    private const int Port = 9090;

    private readonly IContainer _container = new ContainerBuilder("adobe/s3mock:5.2.3")
        .WithPortBinding(Port, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(Port).ForPath("/")))
        .Build();

    public string ServiceUrl => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(Port)}";

    // s3mock은 자격 증명을 검사하지 않는다. 기본 체인이 개발자 PC의 실제 AWS 프로필을 집어 오지 않게 고정 값을 넘긴다.
    public IAmazonS3 CreateClient() => new AmazonS3Client(
        new BasicAWSCredentials("test", "test"),
        new AmazonS3Config { ServiceURL = ServiceUrl, AuthenticationRegion = Region, ForcePathStyle = true });

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        using var s3 = CreateClient();
        await s3.PutBucketAsync(Bucket);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
