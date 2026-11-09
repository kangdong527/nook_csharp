namespace Nook.Infrastructure.Auth;

// A1(D5 결정 2026-10-20, 구현 DEP 7단계): 발송 지점의 수신자별 상한.
// Q2(RequestMagicLinkHandler 의 이메일별 COUNT)는 COUNT ↔ INSERT 사이 경합을 허용한다 → 여러 IP 에서 같은 이메일로 동시에 보내면
// Q2 를 여럿이 통과할 수 있다. 피해 지점(받은편지함·발송 도메인 평판) 바로 앞에서 다시 센다.
//
// 슬라이딩 로그: 수신자마다 최근 발송 시각을 최대 limit 개 둔다. 고정 창은 경계 앞뒤로 몰면 짧은 시간에 2배가 나간다.
// 잠금 없음 — 전제: 단일 인스턴스·단일 소비자(ChannelMagicLinkMailQueue SingleReader). 소비자·인스턴스가 늘면
// 이 전제가 깨진다 → 공유 저장소(Redis) 또는 DB 잠금으로 옮긴다(progress "확정된 결정 (D5)").
internal sealed class RecipientSendLimiter(int limit, TimeSpan window)
{
    // 창이 지난 수신자를 쓸어 내는 주기(호출 수). 이메일을 바꿔 가는 공격에도 기록이 "최근 창 안의 수신자 수"로 묶인다.
    private const int SweepEvery = 256;

    // 대소문자 무시(R-US-03 — 이메일 유일성도 대소문자 무시)
    private readonly Dictionary<string, Queue<DateTimeOffset>> _sent = new(StringComparer.OrdinalIgnoreCase);
    private int _calls;

    // 테스트·로그용: 지금 기록 중인 수신자 수
    public int TrackedRecipients => _sent.Count;

    public bool TryAcquire(string recipient, DateTimeOffset now)
    {
        if (++_calls % SweepEvery == 0)
            Sweep(now);

        if (!_sent.TryGetValue(recipient, out var times))
            _sent[recipient] = times = new Queue<DateTimeOffset>(limit);

        while (times.Count > 0 && now - times.Peek() >= window)
            times.Dequeue();

        if (times.Count >= limit)
            return false;

        times.Enqueue(now);
        return true;
    }

    private void Sweep(DateTimeOffset now)
    {
        // 가장 최근 발송도 창 밖이면 그 수신자는 기록할 필요가 없다. Queue 의 마지막 = 가장 최근(limit 개뿐이라 Last() 비용 무시).
        foreach (var recipient in _sent.Where(kv => kv.Value.Count == 0 || now - kv.Value.Last() >= window).Select(kv => kv.Key).ToList())
            _sent.Remove(recipient);
    }
}
