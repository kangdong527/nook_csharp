using Nook.SharedKernel;

namespace Nook.Application.Auth;

public static class AuthErrors
{
    // 없음·만료·이미 사용을 구분하지 않는다(D5-3 사용자 결정). 구분하면 "이 토큰이 존재했다"가 드러난다.
    // 400인 이유: 401은 프론트의 "Access 토큰 만료 → 로그아웃" 공통 처리와 겹친다. ErrorType에 Unauthorized가 없는 것(D2-1)과도 맞다.
    public static readonly Error InvalidLink = new(
        "Auth.InvalidLink",
        "로그인 링크가 유효하지 않습니다. 새 링크를 요청하세요.",
        ErrorType.Validation);
}
