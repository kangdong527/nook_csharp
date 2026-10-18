using System.Text;
using Nook.Domain.Common;
using Nook.Domain.Reviews;

namespace Nook.Domain.Tests.Reviews;

// ReviewContent 단위 테스트.
// Create — 회원 입력 검증(SSoT: 리뷰 10~50자, 공백 포함, trim 후 코드 포인트 수).
// From — DB 값 복원. 기준은 Nickname.From과 같다(빈 값만 막고, 규칙을 다시 검사하지 않고, 값을 바꾸지 않는다).
public class ReviewContentTests
{
    // 테스트 목록
    //
    // Create — 길이 경계 (SSoT 10~50자)
    // - Create_WhenLengthIsWithinBounds_Succeeds            (10, 50)
    // - Create_WhenLengthIsOutOfBounds_ReturnsContentLength (9, 51)
    // - Create_WhenNullOrWhiteSpace_ReturnsContentLength    (null, "", "   ")
    // - Create_CountsInnerSpaces                            (안쪽 공백은 글자 수에 포함)
    // - Create_TrimsBeforeCounting                          (9자 + 앞뒤 공백 → 실패, 저장값은 trim된 값)
    // - Create_CountsEmojiAsOneCharacter                    (이모지 50개 = 50자, string.Length는 100)
    //
    // Create — 보이지 않는 문자·정규화 (D4 결정: 앞뒤 서식 문자 제거, NFC 정규화 후 세기)
    // - Create_WhenInvisibleCharsAtEdgesMakeLength_ReturnsContentLength (U+200B·U+FEFF가 앞뒤에서 길이를 채움)
    // - Create_KeepsInnerFormatCharacters                    (ZWJ 이모지 안의 U+200D는 지우지 않는다)
    // - Create_WhenNfdTooShortAfterNfc_ReturnsContentLength  (NFD 가×5 = 코드 포인트 10 → NFC 5자)
    // - Create_WhenNfdWithinBoundsAfterNfc_StoresNfc         (NFD 가×26 = 코드 포인트 52 → NFC 26자)
    //
    // From — 복원
    // - From_WhenValueIsEmptyOrWhiteSpace_Throws     ("", " ", "\t")
    // - From_WhenValueBreaksCreateRules_Restores      (5자 — 하한 미만)
    // - From_KeepsValueAsIs                           (그대로)

    // 경계값을 상수(ReviewContent.MinLength 등)가 아니라 숫자로 쓴다 — 상수를 잘못 바꾸면 테스트도 같이 바뀌어 잡지 못한다.
    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    public void Create_WhenLengthIsWithinBounds_Succeeds(int length)
    {
        var raw = new string('가', length);

        Assert.True(ReviewContent.Create(raw).TryGetValue(out var content, out var error), error?.Code);
        Assert.Equal(raw, content.Value);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(51)]
    public void Create_WhenLengthIsOutOfBounds_ReturnsContentLength(int length)
    {
        AssertContentLength(ReviewContent.Create(new string('가', length)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenNullOrWhiteSpace_ReturnsContentLength(string? raw)
    {
        AssertContentLength(ReviewContent.Create(raw));
    }

    [Fact]
    public void Create_CountsInnerSpaces()
    {
        var raw = "가가가가 가가가가가";   // 한글 9 + 안쪽 공백 1 = 10자
        Assert.Equal(10, raw.Length);

        Assert.True(ReviewContent.Create(raw).TryGetValue(out _, out var error), error?.Code);
    }

    [Fact]
    public void Create_TrimsBeforeCounting()
    {
        // 앞뒤 공백까지 세면 11자라 통과하지만, trim 뒤 9자이므로 거절해야 한다.
        AssertContentLength(ReviewContent.Create($" {new string('가', 9)} "));

        Assert.True(ReviewContent.Create($"  {new string('가', 10)}  ").TryGetValue(out var content, out _));
        Assert.Equal(new string('가', 10), content.Value);   // 저장되는 값은 trim된 값
    }

    [Fact]
    public void Create_CountsEmojiAsOneCharacter()
    {
        // 😀는 UTF-16 서로게이트 쌍(char 2개). char 수로 세면 100자라 거절된다 — DB char_length(코드 포인트)와 맞춰야 한다.
        var raw = string.Concat(Enumerable.Repeat("😀", 50));
        Assert.Equal(100, raw.Length);

        Assert.True(ReviewContent.Create(raw).TryGetValue(out _, out var error), error?.Code);
    }

    // string.Trim은 U+200B(폭 없는 공백)·U+FEFF(BOM)를 공백으로 보지 않는다 — 보이는 글자 9자 이하가 통과하던 입력.
    [Theory]
    [InlineData("​가가가가가가가가가")]
    [InlineData("가가가가가가가가가﻿")]
    [InlineData("​​​​​​​​​​")]   // 보이는 글자 0
    public void Create_WhenInvisibleCharsAtEdgesMakeLength_ReturnsContentLength(string raw)
    {
        AssertContentLength(ReviewContent.Create(raw));
    }

    [Fact]
    public void Create_KeepsInnerFormatCharacters()
    {
        // 👨‍👩‍👧 = 사람 이모지 3개 + U+200D(ZWJ, 서식 문자) 2개 = 코드 포인트 5. 안쪽 서식 문자까지 지우면 이모지가 깨진다.
        var raw = "가가가가가👨‍👩‍👧";

        Assert.True(ReviewContent.Create(raw).TryGetValue(out var content, out var error), error?.Code);
        Assert.Equal(raw, content.Value);
    }

    [Fact]
    public void Create_WhenNfdTooShortAfterNfc_ReturnsContentLength()
    {
        // NFD 한글은 음절 하나가 코드 포인트 2개(초성+중성) — 그대로 세면 보이는 5자가 10자로 하한을 통과한다.
        var raw = new string('가', 5).Normalize(NormalizationForm.FormD);
        Assert.Equal(10, raw.EnumerateRunes().Count());

        AssertContentLength(ReviewContent.Create(raw));
    }

    [Fact]
    public void Create_WhenNfdWithinBoundsAfterNfc_StoresNfc()
    {
        var raw = new string('가', 26).Normalize(NormalizationForm.FormD);
        Assert.Equal(52, raw.EnumerateRunes().Count());

        Assert.True(ReviewContent.Create(raw).TryGetValue(out var content, out var error), error?.Code);
        Assert.Equal(new string('가', 26), content.Value);   // 저장되는 값은 NFC
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void From_WhenValueIsEmptyOrWhiteSpace_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => ReviewContent.From(value));
    }

    [Fact]
    public void From_WhenValueBreaksCreateRules_Restores()
    {
        var value = "짧은 리뷰";
        Assert.False(ReviewContent.Create(value).TryGetValue(out _, out _));   // 전제 확인: Create는 거절하는 값

        Assert.Equal(value, ReviewContent.From(value).Value);
    }

    [Fact]
    public void From_KeepsValueAsIs()
    {
        const string value = "분위기가 좋고 커피가 맛있어요";

        Assert.Equal(value, ReviewContent.From(value).Value);
    }

    private static void AssertContentLength(Result<ReviewContent> result)
    {
        Assert.False(result.TryGetValue(out _, out var error));
        Assert.Equal(ReviewErrors.ContentLength.Code, error?.Code);
    }
}
