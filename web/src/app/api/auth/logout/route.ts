// POST /api/auth/logout — AU-03. 서버(C#)는 할 일이 없고(무상태 JWT) 쿠키만 지운다.
// 지운 토큰은 만료(최대 60분)까지 유효하다 — D5 에서 수용한 한계.

import { NextResponse, type NextRequest } from "next/server";
import { AUTH_COOKIE, SIGNUP_COOKIE } from "@/lib/api/cookies";
import { rejectCrossOrigin } from "@/lib/api/origin";

export async function POST(request: NextRequest) {
  // 강제 로그아웃 CSRF 방어 — 쿠키 삭제(Set-Cookie)는 다른 사이트에서 시작한 최상위 이동 응답에도 적용될 수 있다
  const rejected = rejectCrossOrigin(request);
  if (rejected) return rejected;

  const response = new NextResponse(null, { status: 204 });
  response.cookies.delete(AUTH_COOKIE);
  response.cookies.delete(SIGNUP_COOKIE);
  return response;
}
