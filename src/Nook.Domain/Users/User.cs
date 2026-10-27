namespace Nook.Domain.Users;

// 회원 애그리거트 루트. Nickname·Email(값)을 포함한다.
// 지키는 규칙: R-US-01(닉네임 형식, Nickname이 검증), R-US-02(닉네임 유일 — 다른 회원과 비교하므로 DB가 최종 보장),
// R-US-03(이메일 형식은 Email이 검증, 유일은 DB가 최종 보장), R-US-04(성별 필수), R-US-05(생년월일 선택, Birthdate가 검증).
// 그룹 합류 시 사용자별 직렬화 지점(R-GR-04 행 잠금 대상)이지만, 잠금은 Application·DB의 일이다.
// 시각 없음: 가입일을 쓰는 규칙·화면이 없다(Place와 같은 기준).
public sealed class User
{
    public UserId Id { get; }
    public Nickname Nickname { get; private set; }
    public Email Email { get; }
    public Gender Gender { get; }
    public Birthdate? Birthdate { get; }

    private User(UserId id, Nickname nickname, Email email, Gender gender, Birthdate? birthdate)
    {
        Id = id;
        Nickname = nickname;
        Email = email;
        Gender = gender;
        Birthdate = birthdate;
    }

    // 값 객체·GenderCode가 이미 형식을 검증했고 유일성은 DB가 맡으므로, User가 판단할 실패가 없어 User를 바로 반환한다.
    // 정의되지 않은 Gender(default 0 포함)는 호출 경로 이탈 → 예외.
    public static User Create(Nickname nickname, Email email, Gender gender, Birthdate? birthdate)
    {
        ArgumentNullException.ThrowIfNull(nickname);
        ArgumentNullException.ThrowIfNull(email);
        if (!Enum.IsDefined(gender))
            throw new ArgumentOutOfRangeException(nameof(gender), gender, null);

        return new User(UserId.New(), nickname, email, gender, birthdate);
    }

    // PF-01. 표기까지 같으면 아무것도 바꾸지 않는다(멱등, R-US-02). 대소문자만 다른 표기는 바꾼다.
    // Nickname의 동등성은 표기 그대로 비교하므로 "nook"과 "Nook"은 다른 값으로 판단된다.
    public void ChangeNickname(Nickname newNickname)
    {
        ArgumentNullException.ThrowIfNull(newNickname);

        if (newNickname == Nickname)
            return;

        Nickname = newNickname;
    }
}
