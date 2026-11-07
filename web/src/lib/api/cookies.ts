// =========================================================
// BFF 쿠키 — 이름과 옵션을 한 곳에 (FE-2 결정: 토큰 저장 ③ BFF)
//
// 브라우저 ↔ Next 서버는 httpOnly 쿠키, Next 서버 → C# API 는 Bearer.
// 토큰이 브라우저 자바스크립트에 닿지 않는다(XSS 로 훔칠 수 없다).
//
// next/headers 를 import 하지 않는다 — proxy(구 middleware)도 이 파일을 쓴다(요청 쿠키는 request.cookies 로 읽는다).
// =========================================================

/** C# API 가 발급한 Access 토큰(JWT, 60분). */
export const AUTH_COOKIE = "nook_at";

/**
 * 가입 대기 — 매직 링크 토큰 + 이메일.
 * C# consume 은 닉네임이 없으면 롤백해 토큰을 살려 둔다(D5-3).
 * 프로필 폼 제출 때 같은 토큰을 다시 보내기 위해 보관한다.
 */
export const SIGNUP_COOKIE = "nook_signup";

export const AUTH_MAX_AGE_SECONDS = 60 * 60; // 백엔드 JWT 수명과 같게
export const SIGNUP_MAX_AGE_SECONDS = 15 * 60; // 매직 링크 수명(MagicLinkPolicy.TokenLifetime)과 같게

export function cookieOptions(maxAge: number) {
  return {
    httpOnly: true,
    sameSite: "lax" as const,
    secure: process.env.NODE_ENV === "production",
    path: "/",
    maxAge,
  };
}

export type SignupPending = { token: string; email: string };
