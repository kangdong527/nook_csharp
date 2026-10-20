using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Auth;

// AU-01·AU-02 흐름 1: 매직 링크 요청(erd.md magic_link_tokens "흐름").
// 결과는 형식 오류(Validation)와 성공 둘뿐이다. 가입 여부·속도 제한은 결과에 드러나지 않는다(D5 결정: 항상 202).
public sealed record RequestMagicLinkCommand(string? Email);

public sealed class RequestMagicLinkHandler(
    IMagicLinkTokenStore tokens,
    IMagicLinkMailQueue mail,
    TimeProvider time)
{
    public async Task<Result> HandleAsync(RequestMagicLinkCommand command, CancellationToken ct)
    {
        if (!Email.Create(command.Email).TryGetValue(out var email, out var error))
            return error;

        var now = time.GetUtcNow();

        // Q2 속도 제한. 넘어도 성공으로 답한다 — 실패로 답하면 "이 이메일로 최근 요청이 있었다"가 새어 나간다.
        if (await IsRateLimitedAsync(email, now, ct))
            return Result.Success();

        var (rawToken, hash) = MagicLinkSecret.Create();

        // 토큰 INSERT → 메일 전송 순서: 반대면 저장 전에 메일이 도착해 링크를 눌러도 인증이 실패할 수 있다.
        await tokens.AddAsync(hash, email, now, now + MagicLinkPolicy.TokenLifetime, ct);
        mail.Enqueue(email, rawToken);

        return Result.Success();
    }

    private async Task<bool> IsRateLimitedAsync(Email email, DateTimeOffset now, CancellationToken ct)
    {
        var issued = await tokens.CountIssuedSinceAsync(email, now - MagicLinkPolicy.RateLimitWindow, ct);
        return issued >= MagicLinkPolicy.MaxIssuesPerWindow;
    }
}
