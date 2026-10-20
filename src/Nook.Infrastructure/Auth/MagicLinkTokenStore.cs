using Microsoft.EntityFrameworkCore;
using Nook.Application.Auth;
using Nook.Domain.Users;
using Nook.Infrastructure.Persistence;
using Nook.Infrastructure.Persistence.Auth;

namespace Nook.Infrastructure.Auth;

internal sealed class MagicLinkTokenStore(NookDbContext db) : IMagicLinkTokenStore
{
    public Task<int> CountIssuedSinceAsync(Email email, DateTimeOffset since, CancellationToken ct) =>
        // erd.md magic_link_tokens Q2. 받는 인덱스: ix_magic_link_tokens_email_lower_created
        // LINQ ToLower()는 인자 쪽을 C#에서 소문자로 바꿔 파라미터로 보낸다 → DB lower()와 다를 수 있다(progress 미결: 비ASCII).
        // 양쪽 모두 DB가 lower()를 계산하도록 SQL로 쓴다. 스칼라 결과는 열 이름이 "Value"여야 한다(EF 규칙).
        db.Database.SqlQuery<int>($"""
            SELECT count(*)::int AS "Value"
            FROM magic_link_tokens
            WHERE lower(email) = lower({email.Value}) AND created_at > {since}
            """).SingleAsync(ct);

    public async Task AddAsync(byte[] tokenHash, Email email, DateTimeOffset createdAt, DateTimeOffset expiresAt, CancellationToken ct)
    {
        db.Set<MagicLinkToken>().Add(new MagicLinkToken
        {
            TokenHash = tokenHash,
            Email = email.Value,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
        });
        await db.SaveChangesAsync(ct);
    }
}
