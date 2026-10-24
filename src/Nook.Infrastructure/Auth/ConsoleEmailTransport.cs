using Microsoft.Extensions.Logging;

namespace Nook.Infrastructure.Auth;

// 개발 전용: 메일 대신 로그로 찍는다. 본문에 로그인 링크가 들어 있으므로 운영에 등록되면 안 된다
// → DependencyInjection.AddConsoleEmailTransport를 Api가 Development에서만 부른다.
internal sealed class ConsoleEmailTransport(ILogger<ConsoleEmailTransport> logger) : IEmailTransport
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        logger.LogInformation("[개발 메일] 받는 사람 {To} / 제목 {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
