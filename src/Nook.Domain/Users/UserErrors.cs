using Nook.SharedKernel;

namespace Nook.Domain.Users;

public static class UserErrors
{
    public static readonly Error NicknameLengthOutOfRange = new(
        "User.NicknameLengthOutOfRange",
        $"닉네임은 {Nickname.MinLength}~{Nickname.MaxLength}자여야 합니다.",
        ErrorType.Validation);

    public static readonly Error NicknameInvalidCharacters = new(
        "User.NicknameInvalidCharacters",
        "닉네임에는 한글 완성형, 영문, 숫자만 사용할 수 있습니다.",
        ErrorType.Validation);

    // R-US-02. 다른 회원과 비교하는 규칙이라 DB UNIQUE(ux_users_nickname_lower)가 발견하고 저장소가 이 오류로 바꾼다.
    public static readonly Error NicknameTaken = new(
        "User.NicknameTaken",
        "이미 사용 중인 닉네임입니다.",
        ErrorType.Conflict);

    public static readonly Error EmailFormat = new(
        "User.EmailFormat",
        $"이메일 형식이 올바르지 않습니다. 이메일 형식은 공백 없이 '@'를 하나 포함해 최대 {Email.MaxLength}자 입니다.",
        ErrorType.Validation);

    // R-US-04. 성별은 선택(2026-11-09) — 이 오류는 값이 있는데 모르는 값일 때만.
    public static readonly Error GenderInvalid = new(
        "User.GenderInvalid",
        "성별은 female, male, none 중 하나여야 합니다.",
        ErrorType.Validation);

    // R-US-06. 필수 동의(개인정보 수집·이용 + 만 14세 이상 확인) 중 하나라도 없음. 둘을 나누지 않는다 — 화면은 둘 다 체크해야 제출된다.
    public static readonly Error ConsentRequired = new(
        "User.ConsentRequired",
        "개인정보 수집·이용 동의와 만 14세 이상 확인이 필요합니다.",
        ErrorType.Validation);

    // R-US-06. 선택 항목(성별·생년월일)을 보냈는데 선택 동의가 없음.
    public static readonly Error OptionalConsentRequired = new(
        "User.OptionalConsentRequired",
        "성별·생년월일을 입력하려면 선택 항목 수집·이용에 동의해야 합니다.",
        ErrorType.Validation);

    // R-US-06. 생년월일로 계산한 만 나이가 하한 미만(자기 신고와 모순).
    public static readonly Error UnderMinimumAge = new(
        "User.UnderMinimumAge",
        $"만 {User.MinimumAge}세 미만은 가입할 수 없습니다.",
        ErrorType.Validation);

    // R-US-05
    public static readonly Error BirthdateFormat = new(
        "User.BirthdateFormat",
        "생년월일은 YYYY-MM-DD 형식이어야 합니다.",
        ErrorType.Validation);

    public static readonly Error BirthdateOutOfRange = new(
        "User.BirthdateOutOfRange",
        $"생년월일은 {Birthdate.MinValue:yyyy-MM-dd}부터 오늘까지여야 합니다.",
        ErrorType.Validation);
}
