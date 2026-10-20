using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Nook.Application.Auth;
using Nook.Domain.Users;

namespace Nook.Infrastructure.Auth;

internal sealed record MagicLinkMail(string To, string RawToken);

// 같은 프로세스 안의 큐(D5-2 결정 B안). 싱글턴 하나를 핸들러(쓰기)와 MagicLinkMailWorker(읽기)가 공유한다.
// 재시작하면 큐에 남은 메일은 사라진다 — 수용(15분짜리 일회용, 회원이 다시 요청).
internal sealed class ChannelMagicLinkMailQueue(ILogger<ChannelMagicLinkMailQueue> logger) : IMagicLinkMailQueue
{
    // 상한이 없으면 발송이 막힐 때 메모리가 끝없이 는다. 가득 차면 버리고 경고만 남긴다.
    private const int Capacity = 1_000;

    private readonly Channel<MagicLinkMail> _channel = Channel.CreateBounded<MagicLinkMail>(
        new BoundedChannelOptions(Capacity) { SingleReader = true });

    public ChannelReader<MagicLinkMail> Reader => _channel.Reader;

    public void Enqueue(Email email, string rawToken)
    {
        // 로그에 이메일·토큰을 남기지 않는다.
        if (!_channel.Writer.TryWrite(new MagicLinkMail(email.Value, rawToken)))
            logger.LogWarning("매직 링크 메일 큐가 가득 차 발송 요청을 버렸습니다. 용량 {Capacity}", Capacity);
    }
}
