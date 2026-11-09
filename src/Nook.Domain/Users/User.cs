namespace Nook.Domain.Users;

// 회원 애그리거트 루트. Nickname·Email(값)을 포함한다.
// 지키는 규칙: R-US-01(닉네임 형식, Nickname이 검증), R-US-02(닉네임 유일 — 다른 회원과 비교하므로 DB가 최종 보장),
// R-US-03(이메일 형식은 Email이 검증, 유일은 DB가 최종 보장), R-US-04(성별 선택), R-US-05(생년월일 선택, Birthdate가 검증),
// R-US-06(가입 동의 — 성별·생년월일은 선택 동의가 있을 때만, 만 14세 이상).
// 그룹 합류 시 사용자별 직렬화 지점(R-GR-04 행 잠금 대상)이지만, 잠금은 Application·DB의 일이다.
// 시각 없음: 가입일을 쓰는 규칙·화면이 없다(Place와 같은 기준).
public sealed class User
{
    public UserId Id { get; }
    public Nickname Nickname { get; private set; }
    public Email Email { get; }
    public Gender? Gender { get; }
    public Birthdate? Birthdate { get; }

    // 가입 동의(R-US-06). DB 에서 NULL 인 행 = 동의 기록 도입(2026-11-09) 전에 가입한 로컬 개발 데이터뿐 — 운영 공개 전이라 운영 행은 없다.
    // 방침 개정 시 재동의 판단은 PrivacyPolicyVersion 으로(재동의 흐름은 미구현).
    public DateTimeOffset? PrivacyConsentedAt { get; }
    public string? PrivacyPolicyVersion { get; }
    public DateTimeOffset? OptionalConsentedAt { get; }

    // 프로필 사진의 스토리지 키(PF-01, FE-2). null = 기본 이미지(FE 몫). 키 규칙은 리뷰 이미지와 같은 모양에 prefix만 avatars/.
    public string? AvatarKey { get; private set; }

    // R-US-06 가입 하한(만 나이). 개인정보보호법 제22조의2 — 미만은 법정대리인 동의가 필요해 받지 않는다(2026-11-09 결정, PM 확인 대기).
    public const int MinimumAge = 14;

    // EF 가 물질화할 때도 이 생성자를 쓴다(매개변수 이름 = 속성 이름).
    private User(UserId id, Nickname nickname, Email email, Gender? gender, Birthdate? birthdate,
        DateTimeOffset? privacyConsentedAt, string? privacyPolicyVersion, DateTimeOffset? optionalConsentedAt)
    {
        Id = id;
        Nickname = nickname;
        Email = email;
        Gender = gender;
        Birthdate = birthdate;
        PrivacyConsentedAt = privacyConsentedAt;
        PrivacyPolicyVersion = privacyPolicyVersion;
        OptionalConsentedAt = optionalConsentedAt;
    }

    // 값 객체·GenderCode가 이미 형식을 검증했고 유일성은 DB가 맡으므로, User가 판단할 실패가 없어 User를 바로 반환한다.
    // 아래는 모두 호출 경로 이탈 → 예외: 정의되지 않은 Gender, 선택 동의 없이 선택 항목(회원 입력 검사는 Application 이 먼저 Result 로).
    public static User Create(Nickname nickname, Email email, PrivacyConsent consent, Gender? gender = null, Birthdate? birthdate = null)
    {
        ArgumentNullException.ThrowIfNull(nickname);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(consent);
        if (gender is { } g && !Enum.IsDefined(g))
            throw new ArgumentOutOfRangeException(nameof(gender), gender, null);
        if ((gender is not null || birthdate is not null) && !consent.Optional)
            throw new InvalidOperationException("선택 항목 동의 없이 성별·생년월일을 저장할 수 없습니다(R-US-06).");

        return new User(UserId.New(), nickname, email, gender, birthdate,
            consent.At, consent.PolicyVersion, consent.Optional ? consent.At : null);
    }

    // PF-01 프로필 사진 교체·삭제(null). 이전 키를 돌려준다 — 호출 쪽이 DB 커밋 뒤 스토리지에서 지운다(리뷰 이미지와 같은 규칙).
    // 키는 서버가 만드는 값이라 규칙 위반(빈 값·상한 초과)은 생성 코드의 버그 → 예외(ReviewImage와 같은 기준).
    public string? ReplaceAvatar(string? newKey)
    {
        if (newKey is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(newKey);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(newKey.Length, Reviews.ReviewImage.StorageKeyMaxLength, nameof(newKey));
        }

        var previous = AvatarKey;
        AvatarKey = newKey;
        return previous == newKey ? null : previous;
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
