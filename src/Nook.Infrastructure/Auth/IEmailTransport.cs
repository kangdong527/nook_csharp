namespace Nook.Infrastructure.Auth;

// 실제 메일 전송 수단. 운영 발송기(SMTP·외부 API)는 D12에 붙인다.
internal interface IEmailTransport
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct);
}
