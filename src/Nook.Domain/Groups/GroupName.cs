using System.Text;
using Nook.Domain.Common;

namespace Nook.Domain.Groups;

// 그룹명 값 객체(R-GR-10). Nickname(R-US-01)과 같은 순서: NFC 정규화 → trim → 길이 → 허용 문자.
// 차이: 2~6자, ASCII 공백(U+0020) 허용(가운데·연속 모두). 유일하지 않으므로 대소문자 비교 규칙이 없다.
// 허용 문자 검사는 Nickname과 공유하지 않고 복사한다: R-US-01과 R-GR-10은 서로 다른 이유로 바뀌는 규칙이다(이미 공백에서 갈렸다).
public sealed record GroupName
{
    public const int MinLength = 2;
    public const int MaxLength = 6;

    public string Value { get; }

    private GroupName(string value) => Value = value;

    // DB 값 복원(EF Core 값 변환기 입력). 사용자 입력에는 Create를 쓴다.
    // 규칙은 다시 검사하지 않고 값도 바꾸지 않는다(Nickname.From과 같은 기준, 저장된 값은 ck_groups_name이 보장). 빈 값은 데이터 손상으로 보고 예외.
    public static GroupName From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new GroupName(value);
    }

    public static Result<GroupName> Create(string? input)
    {
        // 1) NFC 정규화. 짝 없는 서로게이트가 있으면 Normalize가 ArgumentException을 던지므로 문자 오류(Result)로 바꾼다.
        string normalized;
        try
        {
            normalized = (input ?? string.Empty).Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            return GroupErrors.GroupNameInvalidCharacters;
        }

        // 2) 앞뒤 공백 제거
        normalized = normalized.Trim();

        // 3) 길이(코드 포인트 기준)
        var length = normalized.EnumerateRunes().Count();
        if (length < MinLength || length > MaxLength)
            return GroupErrors.GroupNameLengthOutOfRange;

        // 4) 허용 문자
        foreach (var rune in normalized.EnumerateRunes())
        {
            if (!IsAllowed(rune))
                return GroupErrors.GroupNameInvalidCharacters;
        }

        // 5) 보관
        return new GroupName(normalized);
    }

    // 한글 완성형(가~힣), 영문 대소문자, 숫자, ASCII 공백(U+0020)만 허용
    private static bool IsAllowed(Rune rune) =>
        rune.Value is >= 0xAC00 and <= 0xD7A3
        || rune.Value is >= 'A' and <= 'Z'
        || rune.Value is >= 'a' and <= 'z'
        || rune.Value is >= '0' and <= '9'
        || rune.Value == ' ';
}
