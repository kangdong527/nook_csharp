// =========================================================
// middleware — 모든 요청이 렌더링 **전에** 통과하는 지점
//
// FE-2(BFF): 보호 라우트에서 Access 토큰 쿠키가 **없으면** /login 으로 돌려보낸다.
//
// 여기선 토큰을 검증하지 않는다(쿠키가 있는지만 본다):
//   - 검증은 C# API 가 한다(JwtBearer). Next 가 서명 키를 갖지 않는 편이 키 관리가 단순하다
//   - 만료·위조 토큰은 화면 가드(requireProfile → GET /me 401)가 /login 으로 보낸다
//     → middleware 는 빠른 1차 거름망, 가드가 최종 방어선(검증 이중화)
//
// 원본의 "만료 직전 토큰 갱신"은 없다 — refresh 토큰이 없다(60분, FE-2 결정: DEP 전 재검토).
// =========================================================

import { NextResponse, type NextRequest } from "next/server";
import { AUTH_COOKIE } from "@/lib/api/cookies";

// 로그인이 필요한 라우트. 접두사로 판정한다(/review/edit/[id] 같은 동적 경로 포함).
const PROTECTED_PREFIXES = ["/map", "/review", "/profile"] as const;

export function middleware(request: NextRequest) {
  const path = request.nextUrl.pathname;
  const needsAuth = PROTECTED_PREFIXES.some((p) => path.startsWith(p));

  if (needsAuth && !request.cookies.has(AUTH_COOKIE)) {
    const redirectUrl = request.nextUrl.clone();
    redirectUrl.pathname = "/login";
    return NextResponse.redirect(redirectUrl);
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    "/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)",
  ],
};
