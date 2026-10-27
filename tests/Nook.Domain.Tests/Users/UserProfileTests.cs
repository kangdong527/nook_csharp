using Nook.Domain.Users;

namespace Nook.Domain.Tests.Users;

// R-US-04 성별·R-US-05 생년월일 — 가입 폼 입력의 도메인 규칙.
public class UserProfileTests
{
    // 테스트 목록
    //
    // - GenderCode_ParseAndToCode_RoundTrip                (female·male·none)
    // - User_Create_WithUndefinedGender_Throws             (default(Gender) = 0이 조용히 통과하지 않는다)
    // - Birthdate_Create_AcceptsBoundaries                 (1900-01-01, 오늘)
    // - Birthdate_Create_RejectsOutOfRange                 (1899-12-31, 내일)
    // - Birthdate_Create_RejectsWrongFormat                (null·YY-MM-DD·없는 날짜·구분자)

    private static readonly DateOnly Today = new(2026, 10, 27);

    [Theory]
    [InlineData("female", Gender.Female)]
    [InlineData("male", Gender.Male)]
    [InlineData("none", Gender.None)]
    public void GenderCode_ParseAndToCode_RoundTrip(string code, Gender gender)
    {
        Assert.Equal(gender, Value(GenderCode.Parse(code)));
        Assert.Equal(code, GenderCode.ToCode(gender));
    }

    [Fact]
    public void User_Create_WithUndefinedGender_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            User.Create(Nickname.From("누크"), Email.From("nook@example.com"), default, null));

        Assert.Equal("gender", ex.ParamName);
    }

    [Theory]
    [InlineData("1900-01-01")]
    [InlineData("2026-10-27")]
    public void Birthdate_Create_AcceptsBoundaries(string input)
    {
        Assert.Equal(DateOnly.Parse(input), Value(Birthdate.Create(input, Today)).Value);
    }

    [Theory]
    [InlineData("1899-12-31")]
    [InlineData("2026-10-28")]
    public void Birthdate_Create_RejectsOutOfRange(string input)
    {
        Assert.Equal(UserErrors.BirthdateOutOfRange, Error(Birthdate.Create(input, Today)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("95-03-01")]
    [InlineData("2026-02-30")]
    [InlineData("1995/03/01")]
    [InlineData("1995-3-1")]
    public void Birthdate_Create_RejectsWrongFormat(string? input)
    {
        Assert.Equal(UserErrors.BirthdateFormat, Error(Birthdate.Create(input, Today)));
    }

    private static T Value<T>(Nook.SharedKernel.Result<T> result)
    {
        Assert.True(result.TryGetValue(out var value, out _));
        return value;
    }

    private static Nook.SharedKernel.Error Error<T>(Nook.SharedKernel.Result<T> result)
    {
        Assert.False(result.TryGetValue(out _, out var error));
        return error;
    }
}
