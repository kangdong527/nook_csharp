using System.Buffers;
using System.Globalization;
using System.Text;
using Nook.SharedKernel;

namespace Nook.Domain.Reviews;

// 리뷰 본문 값 객체. 정규화(NFC → 앞뒤 공백·서식 문자 제거) → 검증 → 정규화된 값 보관. 길이 단위는 코드 포인트(DB char_length와 일치).
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
        // NFC 먼저: NFD 한글은 음절당 코드 포인트 2개라 그대로 세면 보이는 5자가 10자가 된다(D4 결정).
        var trimmed = TrimInvisible((raw ?? string.Empty).Normalize(NormalizationForm.FormC));
        var length = trimmed.EnumerateRunes().Count();
        if (length < MinLength || length > MaxLength)
        {
            return ReviewErrors.ContentLength;
        }

        return new ReviewContent(trimmed);
    }

    // string.Trim(공백) + 서식 문자(Cf: U+200B, U+FEFF 등)를 앞뒤에서만 지운다(D4 결정).
    // 안쪽은 건드리지 않는다 — ZWJ(U+200D)로 이은 이모지가 깨진다.
    private static string TrimInvisible(string s)
    {
        var span = s.AsSpan();
        while (Rune.DecodeFromUtf16(span, out var first, out var n) == OperationStatus.Done && IsInvisible(first))
            span = span[n..];
        while (Rune.DecodeLastFromUtf16(span, out var last, out var n) == OperationStatus.Done && IsInvisible(last))
            span = span[..^n];

        return span.ToString();
    }

    private static bool IsInvisible(Rune r) =>
        Rune.IsWhiteSpace(r) || Rune.GetUnicodeCategory(r) == UnicodeCategory.Format;
}
