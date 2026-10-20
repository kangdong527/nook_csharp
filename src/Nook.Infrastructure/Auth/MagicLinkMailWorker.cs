using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nook.Infrastructure.Auth;

// 큐에서 하나씩 꺼내 발송한다. 요청 스레드와 분리되어 있어 발송 지연·실패가 응답에 드러나지 않는다.
internal sealed class MagicLinkMailWorker(
    ChannelMagicLinkMailQueue queue,
    IEmailTransport transport,
    IOptions<MagicLinkOptions> options,
    ILogger<MagicLinkMailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var mail in queue.Reader.ReadAllAsync(stoppingToken))
        {
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
