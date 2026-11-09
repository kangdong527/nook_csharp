using System.Globalization;
using Nook.SharedKernel;

namespace Nook.Domain.Users;

// 생년월일(R-US-05, 선택 입력). 형식은 YYYY-MM-DD — 원본 화면의 YY-MM-DD는 FE가 세기를 정해 바꿔 보낸다(FE-1 사용자 결정).
// 범위: MinValue ~ 오늘. 하한은 DB CHECK로도 이중화하고, "오늘"은 바뀌는 값이라 CHECK에 넣지 않는다(비결정적).
public sealed record Birthdate
{
    public static readonly DateOnly MinValue = new(1900, 1, 1);

    public DateOnly Value { get; }

    private Birthdate(DateOnly value) => Value = value;

    // today: 호출 쪽이 TimeProvider로 정한 UTC 날짜(R-RV-03과 같은 기준).
    public static Result<Birthdate> Create(string? input, DateOnly today)
    {
        if (!DateOnly.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
            return UserErrors.BirthdateFormat;

        if (value < MinValue || value > today)
            return UserErrors.BirthdateOutOfRange;

        return new Birthdate(value);
    }

    // DB 값 복원. 하한은 ck_users_birthdate가 보장하고, 상한(오늘)은 저장 당시 기준이라 다시 검사하지 않는다.
    public static Birthdate From(DateOnly value) => new(value);

    // 만 나이(R-US-06). 생일이 지나야 한 살 더한다. 2월 29일생은 평년에 2월 28일을 생일로 본다(DateOnly.AddYears 동작)
    // — 법적 기준(3월 1일 해석 여지)과 다를 수 있다. 14세 경계의 하루 차이라 수용, 처리방침 검토 때 PM 확인.
    public int AgeOn(DateOnly today)
    {
        var age = today.Year - Value.Year;
        return today < Value.AddYears(age) ? age - 1 : age;
    }
}
