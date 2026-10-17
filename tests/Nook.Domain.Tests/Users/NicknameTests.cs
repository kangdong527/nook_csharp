using Nook.Domain.Users;

namespace Nook.Domain.Tests.Users;

// Nickname.From — DB 값 복원 단위 테스트.
// 규칙: 빈 값만 막고(데이터 손상 → 예외), 형식 규칙은 다시 검사하지 않으며, 값을 바꾸지 않는다.
public class NicknameTests
{
    // 테스트 목록
    //
    // From — 복원
    // - From_WhenValueIsEmptyOrWhiteSpace_Throws     ("", " ", "\t")
    // - From_WhenValueBreaksCreateRules_Restores      (1자 — 규칙이 바뀐 뒤 기존 회원 읽기 흉내)
    // - From_KeepsValueAsIs                           (Trim·대소문자 변환 없음)

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void From_WhenValueIsEmptyOrWhiteSpace_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => Nickname.From(value));
    }

    [Fact]
    public void From_WhenValueBreaksCreateRules_Restores()
    {
        const string value = "a";
        Assert.False(Nickname.Create(value).TryGetValue(out _, out _));   // 전제 확인: Create는 거절하는 값

        var nickname = Nickname.From(value);

        Assert.Equal(value, nickname.Value);
    }

    [Fact]
    public void From_KeepsValueAsIs()
    {
        const string value = "Nook";

        Assert.Equal(value, Nickname.From(value).Value);
    }
}
