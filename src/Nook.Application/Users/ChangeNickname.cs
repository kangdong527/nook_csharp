using Nook.Application.Auth;
using Nook.Application.Reviews;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Users;

// PF-01 닉네임을 수정한다. 형식(R-US-01)은 Nickname이, 유일(R-US-02)은 DB UNIQUE(ux_users_nickname_lower)가 최종 판정한다 —
// 미리 확인(nicknameExists)을 두지 않는다(FE-2 사용자 결정): 동시 변경은 미리 확인으로 막을 수 없다(TOCTOU).
public sealed record ChangeNicknameCommand(Guid UserId, string? Nickname);

public sealed class ChangeNicknameHandler(IUserRepository users, IReviewImageUrls urls)
{
    public async Task<Result<MeView>> HandleAsync(ChangeNicknameCommand command, CancellationToken ct)
    {
        if (!Nickname.Create(command.Nickname).TryGetValue(out var nickname, out var formatError))
            return formatError;

        // 회원 없음 = 탈퇴 후 남은 토큰 — /me와 같은 401(AU-04).
        var user = await users.FindForUpdateAsync(UserId.From(command.UserId), ct);
        if (user is null)
            return AuthErrors.UnknownUser;

        user.ChangeNickname(nickname);   // 표기까지 같으면 아무것도 바꾸지 않는다(멱등) — SaveAsync도 UPDATE를 보내지 않는다
        if ((await users.SaveAsync(user, ct)).IsFailure(out var saveError))
            return saveError;

        return MeView.From(user, urls);
    }
}
