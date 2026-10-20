using Nook.Domain.Users;

namespace Nook.Application.Auth;

// magic_link_tokens 저장 담당(erd.md). 유스케이스가 필요로 하는 일만 계약으로 두고, 구현은 Infrastructure가 한다.
public interface IMagicLinkTokenStore
{
    // erd.md magic_link_tokens Q2. 이메일은 대소문자 무시로 센다.
    Task<int> CountIssuedSinceAsync(Email email, DateTimeOffset since, CancellationToken ct);

    // 행 하나를 넣고 저장까지 끝낸다. 원문 토큰은 받지 않는다 — 해시만 DB로 간다.
    Task AddAsync(byte[] tokenHash, Email email, DateTimeOffset createdAt, DateTimeOffset expiresAt, CancellationToken ct);

    // erd.md magic_link_tokens Q1. 조건 검사와 소모가 한 문장 — 같은 링크 동시 요청은 1건만 이메일을 받고 나머지는 null.
    // null = 없음·만료·이미 사용(구분하지 않는다). 트랜잭션 안에서 부르면 롤백 시 소모도 되돌아간다.
    Task<Email?> ConsumeAsync(byte[] tokenHash, DateTimeOffset now, CancellationToken ct);

    // erd.md magic_link_tokens Q3. 로그인 성공 후 같은 이메일의 남은 링크를 모두 소모 처리한다.
    Task RevokeRemainingAsync(Email email, DateTimeOffset now, CancellationToken ct);
}
