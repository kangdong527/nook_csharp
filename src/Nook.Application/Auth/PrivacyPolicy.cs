namespace Nook.Application.Auth;

// 지금 시행 중인 개인정보 처리방침 버전(R-US-06). 가입 때 이 값이 users.privacy_policy_version 에 기록된다.
// 방침을 바꾸면: ① docs/design/privacy-policy.md 개정 ② web /privacy 페이지의 시행일 ③ 이 값 — 셋을 함께 바꾼다.
public static class PrivacyPolicy
{
    public const string CurrentVersion = "2026-11-09-draft";
}
