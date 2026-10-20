using Nook.SharedKernel;

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

    public static readonly Error EmailFormat = new(
        "User.EmailFormat",
        $"이메일 형식이 올바르지 않습니다. 이메일 형식은 공백 없이 '@'를 하나 포함해 최대 {Email.MaxLength}자 입니다.",
        ErrorType.Validation);
}
