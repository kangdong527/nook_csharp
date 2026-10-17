using Nook.Domain.Common;

namespace Nook.Domain.Reviews;

// 리뷰 본문 값 객체. 정규화(trim) → 검증 → 정규화된 값 보관. 길이 단위는 코드 포인트(DB char_length와 일치).
public sealed record ReviewContent
{
    public const int MinLength = 10;
    public const int MaxLength = 50;

    private ReviewContent(string value)
    {
        Value = value;
    }

    public string Value { get; }

    // DB 값 복원(EF Core 값 변환기 입력). 사용자 입력에는 Create를 쓴다.
    // 길이 규칙은 다시 검사하지 않고 값도 바꾸지 않는다(Nickname.From과 같은 기준, 저장된 값은 ck_reviews_content가 보장). 빈 값은 데이터 손상으로 보고 예외.
    public static ReviewContent From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new ReviewContent(value);
    }

    public static Result<ReviewContent> Create(string? raw)
    {
        var trimmed = raw?.Trim() ?? string.Empty;
        var length = trimmed.EnumerateRunes().Count();
        if (length < MinLength || length > MaxLength)
        {
            return ReviewErrors.ContentLength;
        }

        return new ReviewContent(trimmed);
    }
}
