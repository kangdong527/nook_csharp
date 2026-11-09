using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Nook.Infrastructure.Auth;

namespace Nook.Integration.Tests.Auth;

// SES 발송기(DEP 7단계) — 실제 발송은 도메인 인증 뒤라, 지금은 SDK 로 나가는 요청 모양만 고정한다.
// 클라이언트를 상속해 SendEmailAsync 만 가로챈다(네트워크 없음).
public sealed class SesEmailTransportTests
{
    // 테스트 목록
    //
    // - Send_MapsAddressesSubjectAndTextBody
    // - Send_DeclaresUtf8ForSubjectAndBody   (SES 기본은 7비트 ASCII — 빠지면 한글이 깨지거나 거절된다)

    private const string From = "no-reply@nook.test";

    [Fact]
    public async Task Send_MapsAddressesSubjectAndTextBody()
    {
        var ses = new CapturingSesClient();

        await new SesEmailTransport(ses, From).SendAsync("user@nook.test", "nook 로그인 링크", "본문 https://x/#token=abc", CancellationToken.None);

        var request = Assert.Single(ses.Requests);
        Assert.Equal(From, request.FromEmailAddress);
        Assert.Equal(["user@nook.test"], request.Destination.ToAddresses);
        Assert.Equal("nook 로그인 링크", request.Content.Simple.Subject.Data);
        Assert.Equal("본문 https://x/#token=abc", request.Content.Simple.Body.Text.Data);
        Assert.Null(request.Content.Simple.Body.Html);   // 텍스트만
    }

    [Fact]
    public async Task Send_DeclaresUtf8ForSubjectAndBody()
    {
        var ses = new CapturingSesClient();

        await new SesEmailTransport(ses, From).SendAsync("user@nook.test", "제목", "본문", CancellationToken.None);

        var message = Assert.Single(ses.Requests).Content.Simple;
        Assert.Equal("UTF-8", message.Subject.Charset);
        Assert.Equal("UTF-8", message.Body.Text.Charset);
    }

    private sealed class CapturingSesClient()
        : AmazonSimpleEmailServiceV2Client(new BasicAWSCredentials("test", "test"), RegionEndpoint.APNortheast2)
    {
        public List<SendEmailRequest> Requests { get; } = [];

        public override Task<SendEmailResponse> SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(new SendEmailResponse { MessageId = "test" });
        }
    }
}
