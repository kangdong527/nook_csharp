using Nook.Domain.Common;

namespace Nook.Domain.Users;

public static class UserErrors
{
    public static readonly Error NicknameLengthOutOfRange = new(
        "User.NicknameLengthOutOfRange",
        $"닉네임은 {Nickname.MinLength}~{Nickname.MaxLength}자여야 합니다.",
        ErrorType.Validation);

    public static readonly Error NicknameInvalidCharacters = new(
        "User.NicknameInvalidCharacters",
        "닉네임에는 한글 완성형, 영문, 숫자만 사용할 수 있습니다.",
        ErrorType.Validation);
}
