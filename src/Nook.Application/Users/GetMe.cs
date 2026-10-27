using Nook.Application.Auth;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Users;

// AU-04 로그인한 나를 확인한다. 프론트의 첫 화면·가드가 부른다(원본 getCurrentUser).
public sealed record GetMeQuery(Guid UserId);

// 본인 자원이라 이메일까지 준다.
public sealed record MeView(Guid Id, string Email, string Nickname);

public sealed class GetMeHandler(IUserRepository users)
{
    public async Task<Result<MeView>> HandleAsync(GetMeQuery query, CancellationToken ct)
    {
        var userId = UserId.From(query.UserId);
        var user = await users.FindByIdAsync(userId, ct);

        if (user == null) 
            return AuthErrors.UnknownUser;

        return new MeView(userId.Value, user.Email.Value, user.Nickname.Value);
    }
}
