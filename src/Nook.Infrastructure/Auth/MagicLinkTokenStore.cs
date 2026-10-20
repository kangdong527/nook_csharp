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

    public async Task<Email?> ConsumeAsync(byte[] tokenHash, DateTimeOffset now, CancellationToken ct)
    {
        // erd.md magic_link_tokens Q1. 받는 인덱스: pk_magic_link_tokens.
        // ToListAsync인 이유: SingleAsync 등은 EF가 SQL을 서브쿼리로 감싸는데, PostgreSQL은 서브쿼리 안의 UPDATE를 허용하지 않는다.
        var emails = await db.Database.SqlQuery<string>($"""
            UPDATE magic_link_tokens
            SET consumed_at = {now}
            WHERE token_hash = {tokenHash} AND consumed_at IS NULL AND expires_at > {now}
            RETURNING email AS "Value"
            """).ToListAsync(ct);

        // 저장된 값은 ck·Email.Create를 거쳤으므로 복원(From)이다.
        return emails is [var email] ? Email.From(email) : null;
    }

    public Task RevokeRemainingAsync(Email email, DateTimeOffset now, CancellationToken ct) =>
        // erd.md magic_link_tokens Q3. 받는 인덱스: ix_magic_link_tokens_email_lower_created(앞 열 lower(email)만).
        db.Database.ExecuteSqlAsync($"""
            UPDATE magic_link_tokens
            SET consumed_at = {now}
            WHERE lower(email) = lower({email.Value}) AND consumed_at IS NULL
            """, ct);
}
