namespace Nook.Infrastructure.Auth;

// 실제 메일 전송 수단. 구현: ConsoleEmailTransport(개발)·SesEmailTransport(운영, DEP 7단계). 선택은 Program.cs 의 Email:Provider.
internal interface IEmailTransport
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct);
}
