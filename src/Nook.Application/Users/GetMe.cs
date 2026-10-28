using Nook.Application.Auth;
using Nook.Application.Reviews;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Users;

// AU-04 로그인한 나를 확인한다. 프론트의 첫 화면·가드가 부른다(원본 getCurrentUser).
public sealed record GetMeQuery(Guid UserId);

// 본인 자원이라 이메일까지 준다. AvatarUrl: null = 기본 이미지(FE 몫), 있으면 서버가 만든 표시 주소(FE-2).
public sealed record MeView(Guid Id, string Email, string Nickname, string? AvatarUrl)
{
    internal static MeView From(User user, IReviewImageUrls urls) =>
        new(user.Id.Value, user.Email.Value, user.Nickname.Value, user.AvatarKey is { } key ? urls.For(key) : null);
}

public sealed class GetMeHandler(IUserRepository users, IReviewImageUrls urls)
{
    public async Task<Result<MeView>> HandleAsync(GetMeQuery query, CancellationToken ct)
    {
        var userId = UserId.From(query.UserId);
        var user = await users.FindByIdAsync(userId, ct);

        if (user == null) 
            return AuthErrors.UnknownUser;

        return MeView.From(user, urls);
    }
}
