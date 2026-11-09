using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nook.Application.Auth;

namespace Nook.Infrastructure.Auth;

// 큐에서 하나씩 꺼내 발송한다. 요청 스레드와 분리되어 있어 발송 지연·실패가 응답에 드러나지 않는다.
internal sealed class MagicLinkMailWorker(
    ChannelMagicLinkMailQueue queue,
    IEmailTransport transport,
    IOptions<MagicLinkOptions> options,
    TimeProvider time,
    ILogger<MagicLinkMailWorker> logger) : BackgroundService
{
    // A1: 수신자별 발송 상한. 값은 Q2 와 같은 MagicLinkPolicy — 정상 요청은 Q2 에서 이미 걸러져 여기서 버려지지 않는다.
    // 이 워커만 쓴다(단일 소비자) → 잠금 없음(RecipientSendLimiter 주석).
    private readonly RecipientSendLimiter _limiter = new(MagicLinkPolicy.MaxIssuesPerWindow, MagicLinkPolicy.RateLimitWindow);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var mail in queue.Reader.ReadAllAsync(stoppingToken))
        {
            if (!_limiter.TryAcquire(mail.To, time.GetUtcNow()))
            {
                // 이메일·토큰은 남기지 않는다. 이 로그가 보이면 Q2 를 우회한 동시 요청(분산 공격 또는 경합)이 있었다는 뜻
                logger.LogWarning("수신자별 발송 상한({Limit}통/{Window})을 넘어 매직 링크 메일 1통을 버렸습니다.",
                    MagicLinkPolicy.MaxIssuesPerWindow, MagicLinkPolicy.RateLimitWindow);
                continue;
            }

            // 한 건의 실패가 워커를 죽이면 안 된다. .NET 8부터 BackgroundService의 처리 안 된 예외는 호스트 전체를 멈춘다.
            try
            {
                await transport.SendAsync(mail.To, "nook 로그인 링크", BuildBody(mail.RawToken), stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "매직 링크 메일 발송에 실패했습니다.");
            }
        }
    }

    // 토큰은 쿼리(?token=)가 아니라 프래그먼트(#)에 넣는다. 프래그먼트는 브라우저가 서버로 보내지 않아
    // 프론트 서버 접근 로그·프록시 로그에 남지 않는다. Base64url이라 URL 인코딩이 필요 없다.
    private string BuildBody(string rawToken) =>
        $"""
        아래 링크를 15분 안에 열어 로그인하세요. 요청하지 않았다면 이 메일을 무시하세요.
        {options.Value.LinkBaseUrl}#token={rawToken}
        """;
}
