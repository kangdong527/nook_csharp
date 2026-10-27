using Nook.SharedKernel;

namespace Nook.Application.Auth;

public static class AuthErrors
{
    // 없음·만료·이미 사용을 구분하지 않는다(D5-3 사용자 결정). 구분하면 "이 토큰이 존재했다"가 드러난다.
    // 400인 이유: 401은 프론트의 "Access 토큰 만료 → 로그아웃" 공통 처리와 겹친다. (FE-1에 ErrorType.Unauthorized가 생겼지만 이 오류는 그대로 400 — 링크 오류는 "로그인 상태"와 무관하다.)
    public static readonly Error InvalidLink = new(
        "Auth.InvalidLink",
        "로그인 링크가 유효하지 않습니다. 새 링크를 요청하세요.",
        ErrorType.Validation);

    // AU-04. 서명은 유효한데 sub의 회원이 없음(탈퇴 후 남은 토큰, DB 초기화). 401 — 프론트는 다른 401과 똑같이 토큰을 버린다(FE-1 사용자 결정).
    public static readonly Error UnknownUser = new(
        "Auth.UnknownUser",
        "회원 정보를 찾을 수 없습니다. 다시 로그인하세요.",
        ErrorType.Unauthorized);
}
