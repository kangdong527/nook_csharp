using Microsoft.Extensions.Logging;

namespace Nook.Infrastructure.Auth;

// 개발 전용: 메일 대신 로그로 찍는다. 본문에 로그인 링크가 들어 있으므로 운영에 등록되면 안 된다
// → Email:Provider=Console 일 때만 등록된다(ADR-003 결정 1). 운영 compose(compose.prod.yaml)는 Ses 로 덮어쓴다.
//   로컬 compose 는 Console 이라 링크가 api 로그에 남는다 — 그 로그를 운영 로그 수집에 보내지 않는다.
internal sealed class ConsoleEmailTransport(ILogger<ConsoleEmailTransport> logger) : IEmailTransport
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        logger.LogInformation("[개발 메일] 받는 사람 {To} / 제목 {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
