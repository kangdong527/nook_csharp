using Nook.Domain.Users;

namespace Nook.Application.Auth;

// magic_link_tokens 저장 담당(erd.md). 유스케이스가 필요로 하는 일만 계약으로 두고, 구현은 Infrastructure가 한다.
public interface IMagicLinkTokenStore
{
    // erd.md magic_link_tokens Q2. 이메일은 대소문자 무시로 센다.
    Task<int> CountIssuedSinceAsync(Email email, DateTimeOffset since, CancellationToken ct);

    // 행 하나를 넣고 저장까지 끝낸다. 원문 토큰은 받지 않는다 — 해시만 DB로 간다.
    Task AddAsync(byte[] tokenHash, Email email, DateTimeOffset createdAt, DateTimeOffset expiresAt, CancellationToken ct);
}
