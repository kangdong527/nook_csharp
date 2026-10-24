using Nook.SharedKernel;
using System.Text;

namespace Nook.Domain.Users;

// 닉네임 값 객체. 원래 표기를 보관한다. 중복 판단(대소문자 무시)은 DB UNIQUE와 Application이 맡는다.
public sealed record Nickname
{
    public const int MinLength = 2;
    public const int MaxLength = 12;

    private Nickname(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Nickname> Create(string? raw)
    {
        // 짝이 맞지 않는 서로게이트가 있으면 Normalize가 예외를 던지므로, 글자 오류로 처리
        if (!TryNormalize(raw ?? string.Empty, out var normalized))
            return UserErrors.NicknameInvalidCharacters;

        var value = normalized.Trim();

        // 길이는 코드 포인트 기준
        var length = value.EnumerateRunes().Count();
        if (length is not (>= MinLength and <= MaxLength))
            return UserErrors.NicknameLengthOutOfRange;

        if (!ConsistsOfAllowedCharacters(value))
            return UserErrors.NicknameInvalidCharacters;

        return new Nickname(value);   // 대소문자 변환 없이 원래 표기 그대로 저장
    }

    // DB 값 복원(EF Core 값 변환기 입력). 사용자 입력에는 Create를 쓴다.
    // 길이·문자 규칙은 다시 검사하지 않는다: 저장된 값은 ck_users_nickname이 보장하고, 규칙이 바뀌어도 기존 회원을 읽을 수 있어야 한다.
    // 값도 바꾸지 않는다(Trim·정규화 없음): 바꾸면 EF 스냅샷이 DB와 다른 값으로 시작한다. 빈 값은 데이터 손상으로 보고 예외.
    public static Nickname From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new Nickname(value);
    }

    private static bool TryNormalize(string raw, out string normalized)
    {
        try
        {
            normalized = raw.Normalize(NormalizationForm.FormC);
            return true;
        }
        catch (ArgumentException)
        {
            normalized = string.Empty;
            return false;
        }
    }

    private static bool ConsistsOfAllowedCharacters(string value)
    {
        foreach (var rune in value.EnumerateRunes())
        {
            if (!IsAllowed(rune))
                return false;
        }
        return true;
    }

    // 허용 목록을 긍정형으로 정의: 한글 완성형 또는 ASCII 영문·숫자만 true
    private static bool IsAllowed(Rune rune) =>
        rune.Value is >= '가' and <= '힣'
        || (rune.IsAscii && char.IsAsciiLetterOrDigit((char)rune.Value));
}
