using Nook.Domain.Common;

namespace Nook.Domain.Users;

// 가입 때의 개인정보 동의(R-US-06, DEP 7단계 2026-11-09). 시각·방침 버전·선택 동의는 늘 함께 생기므로 하나로 묶는다 —
// 따로 받으면 "시각은 있는데 버전이 없는" 조합이 생길 수 있다.
//   필수: 개인정보 수집·이용 동의 + 만 14세 이상 확인 → 이 값이 만들어졌다는 것 자체가 두 필수 동의를 뜻한다
//   선택: 성별·생년월일 수집 동의(Optional) — 없으면 두 항목을 받지 않는다(User.Create 불변식)
// 방침 버전은 서버가 시행 중인 값을 넣는다(클라이언트가 보낸 값을 믿지 않는다 — 옛 화면에서 가입해도 새 방침 동의로 기록되지 않게).
public sealed record PrivacyConsent
{
    public const int MaxPolicyVersionLength = 20;

    public DateTimeOffset At { get; }
    public string PolicyVersion { get; }
    public bool Optional { get; }

    private PrivacyConsent(DateTimeOffset at, string policyVersion, bool optional)
    {
        At = at;
        PolicyVersion = policyVersion;
        Optional = optional;
    }

    // 회원 입력 검사(필수 체크 두 개)는 Application 이 끝냈다 → 여기 들어오는 잘못된 값은 호출 경로 결함이라 예외.
    public static PrivacyConsent Agree(DateTimeOffset at, string policyVersion, bool optional)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyVersion);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(policyVersion.Length, MaxPolicyVersionLength, nameof(policyVersion));
        return new PrivacyConsent(TimeGuard.Utc(at), policyVersion, optional);
    }
}
