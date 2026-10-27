using Nook.SharedKernel;

namespace Nook.Domain.Users;

// 성별(R-US-04). 가입 폼에서 셋 중 하나를 반드시 고른다 — None은 "밝히지 않음"이라는 선택이지 미입력이 아니다.
// 0을 쓰지 않는다: default(Gender)가 유효한 값으로 조용히 통과하지 않게 한다(MemberRemoval.NotMember와 같은 이유, 반대 방향).
public enum Gender
{
    Female = 1,
    Male = 2,
    None = 3,
}

// 문자열 표기 ↔ Gender. API·DB 모두 같은 소문자 표기를 쓴다(원본 nook 'female'|'male'|'none').
public static class GenderCode
{
    public static Result<Gender> Parse(string? input) => input switch
    {
        "female" => Gender.Female,
        "male" => Gender.Male,
        "none" => Gender.None,
        _ => UserErrors.GenderInvalid,
    };

    public static string ToCode(Gender gender) => gender switch
    {
        Gender.Female => "female",
        Gender.Male => "male",
        Gender.None => "none",
        _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null),
    };

    // DB 값 복원. 저장된 값은 ck_users_gender가 보장하므로 모르는 값은 데이터 손상 → 예외.
    public static Gender From(string code) =>
        Parse(code).TryGetValue(out var gender, out _)
            ? gender
            : throw new ArgumentException($"알 수 없는 성별 코드입니다: {code}", nameof(code));
}
