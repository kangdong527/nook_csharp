using Nook.Domain.Common;

namespace Nook.Domain.Users;

// 이메일(R-US-03). 형식은 RFC 5322가 정하므로 느슨하게 검사한다: 공백 없음, '@' 정확히 하나, 그 앞뒤가 비어 있지 않음.
// 유일성은 대소문자 무시 비교로 DB가 보장한다. 이 타입의 동등성(record)은 표기 그대로 비교한다.
public sealed record Email
{
    public const int MaxLength = 254;

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string? input)
    {
        // 1) 정규화
        var normalized = input?.Trim() ?? string.Empty;

        // 2) 허용 조건
        var length = normalized.EnumerateRunes().Count();
        if (length == 0 || length > MaxLength
            || normalized.Any(char.IsWhiteSpace)
            || !HasSingleAtWithBothSides(normalized))
            return UserErrors.EmailFormat;

        // 3) 보관
        return new Email(normalized);
    }

    // DB 값 복원(EF Core 값 변환기 입력). 사용자 입력에는 Create를 쓴다.
    // 형식은 다시 검사하지 않고 값도 바꾸지 않는다(Nickname.From과 같은 기준, 저장된 값은 ck_users_email이 보장). 빈 값은 데이터 손상으로 보고 예외.
    public static Email From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new Email(value);
    }

    private static bool HasSingleAtWithBothSides(string value)
    {
        var at = value.IndexOf('@');
        return at > 0 && at == value.LastIndexOf('@') && at < value.Length - 1;
    }
}
