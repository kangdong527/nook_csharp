using Nook.Domain.Users;

namespace Nook.Domain.Tests.Users;

// R-US-04 성별·R-US-05 생년월일 — 가입 폼 입력의 도메인 규칙.
public class UserProfileTests
{
    // 테스트 목록
    //
    // - GenderCode_ParseAndToCode_RoundTrip                (female·male·none)
    // - User_Create_WithUndefinedGender_Throws             ((Gender)0이 조용히 통과하지 않는다)
    // - User_Create_WithRequiredConsentOnly_RecordsConsentAndNoOptional       (R-US-06)
    // - User_Create_WithOptionalConsent_RecordsOptionalConsentTime
    // - User_Create_OptionalItemsWithoutOptionalConsent_Throws               (불변식 — 회원 입력은 Application 이 먼저 거른다)
    // - Birthdate_AgeOn_CountsFullYears / _LeapDayBirth_UsesFeb28InCommonYears (만 14세 경계)
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
        // 성별이 선택(Gender?)이 된 뒤 default 는 null(없음)이라 유효하다 → 정의되지 않은 값은 (Gender)0 으로 명시
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            User.Create(Nickname.From("누크"), Email.From("nook@example.com"), TestConsent.WithOptional, (Gender)0));

        Assert.Equal("gender", ex.ParamName);
    }

    // ── R-US-06 가입 동의 ──────────────────────────────────

    [Fact]
    public void User_Create_WithRequiredConsentOnly_RecordsConsentAndNoOptional()
    {
        var user = User.Create(Nickname.From("누크"), Email.From("nook@example.com"), TestConsent.Required);

        Assert.NotNull(user.PrivacyConsentedAt);
        Assert.Equal("test", user.PrivacyPolicyVersion);
        Assert.Null(user.OptionalConsentedAt);
        Assert.Null(user.Gender);
        Assert.Null(user.Birthdate);
    }

    [Fact]
    public void User_Create_WithOptionalConsent_RecordsOptionalConsentTime()
    {
        var user = User.Create(Nickname.From("누크"), Email.From("nook@example.com"), TestConsent.WithOptional, Gender.Female);

        Assert.Equal(user.PrivacyConsentedAt, user.OptionalConsentedAt);
        Assert.Equal(Gender.Female, user.Gender);
    }

    [Theory]
    [InlineData(true, false)]    // 성별만
    [InlineData(false, true)]    // 생년월일만
    public void User_Create_OptionalItemsWithoutOptionalConsent_Throws(bool withGender, bool withBirthdate)
    {
        Assert.Throws<InvalidOperationException>(() => User.Create(
            Nickname.From("누크"), Email.From("nook@example.com"), TestConsent.Required,
            withGender ? Gender.None : null, withBirthdate ? Birthdate.From(new DateOnly(1995, 3, 1)) : null));
    }

    [Theory]
    [InlineData("2012-10-27", 14)]   // 오늘이 14번째 생일
    [InlineData("2012-10-28", 13)]   // 내일이 14번째 생일
    [InlineData("2012-10-26", 14)]
    [InlineData("2026-10-27", 0)]
    public void Birthdate_AgeOn_CountsFullYears(string birth, int expected)
    {
        Assert.Equal(expected, Birthdate.From(DateOnly.Parse(birth)).AgeOn(Today));
    }

    [Theory]
    [InlineData("2027-02-27", 14)]   // 평년: 2월 28일이 생일(AddYears) — 하루 전은 아직 14세
    [InlineData("2027-02-28", 15)]
    public void Birthdate_AgeOn_LeapDayBirth_UsesFeb28InCommonYears(string today, int expected)
    {
        Assert.Equal(expected, Birthdate.From(new DateOnly(2012, 2, 29)).AgeOn(DateOnly.Parse(today)));
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
