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
