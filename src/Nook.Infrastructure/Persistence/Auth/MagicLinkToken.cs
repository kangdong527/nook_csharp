namespace Nook.Infrastructure.Persistence.Auth;

// erd.md §2 magic_link_tokens. Domain 엔티티 없이 Infrastructure에만 두는 첫 테이블이다(지킬 업무 규칙이 없다 — erd.md 메모, ADR 후보).
// 소모(Q1)·남은 링크 무효화(Q3)는 조건부 UPDATE 한 문장으로 하고 변경 추적으로 고치지 않는다 → 상태를 바꾸는 메서드를 두지 않는다.
internal sealed class MagicLinkToken
{
    // 원문이 아니라 SHA-256(32바이트). 원문은 메일에만 나간다.
    public required byte[] TokenHash { get; init; }

    // 요청 표기 그대로(trim 후). 가입 전 이메일도 들어가므로 users를 참조하지 않는다.
    public required string Email { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    // null = 미사용
    public DateTimeOffset? ConsumedAt { get; init; }
}
