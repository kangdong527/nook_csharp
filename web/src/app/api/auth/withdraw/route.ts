// POST /api/auth/withdraw — PF-02 회원 탈퇴. C# DELETE /me 가 성공하면(204) 쿠키도 지운다.
// 401(이미 탈퇴·토큰 만료)도 쿠키를 지운다 — 어느 쪽이든 이 브라우저는 더 이상 로그인 상태가 아니다.
// 원본은 public.users 만 지우고 auth 계정이 남아 세션을 따로 끊어야 했다. 여기선 회원 행이 곧 계정이다.

import { NextResponse, type NextRequest } from "next/server";
import { apiFetch, readProblem } from "@/lib/api/server";
import { AUTH_COOKIE, SIGNUP_COOKIE } from "@/lib/api/cookies";
import { isAllowedOrigin } from "@/lib/api/origin";

export async function POST(request: NextRequest) {
  // 쿠키로 인증하는 상태 변경이라 중계(/api/nook)와 같은 Origin 검사를 한다(CSRF 두 겹째 — SameSite=Lax 가 첫 겹).
  if (!isAllowedOrigin(request)) {
    return NextResponse.json({ status: 403, title: "Bff.CrossOrigin" }, { status: 403 });
  }

  const res = await apiFetch("/me", { method: "DELETE" });
  if (!res.ok && res.status !== 401) {
    const problem = await readProblem(res);
    return NextResponse.json(problem, { status: problem.status });
  }

  const response = new NextResponse(null, { status: 204 });
  response.cookies.delete(AUTH_COOKIE);
  response.cookies.delete(SIGNUP_COOKIE);
  return response;
}
