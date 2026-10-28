// POST /api/auth/logout — AU-03. 서버(C#)는 할 일이 없고(무상태 JWT) 쿠키만 지운다.
// 지운 토큰은 만료(최대 60분)까지 유효하다 — D5 에서 수용한 한계.

import { NextResponse } from "next/server";
import { AUTH_COOKIE, SIGNUP_COOKIE } from "@/lib/api/cookies";

export async function POST() {
  const response = new NextResponse(null, { status: 204 });
  response.cookies.delete(AUTH_COOKIE);
  response.cookies.delete(SIGNUP_COOKIE);
  return response;
}
