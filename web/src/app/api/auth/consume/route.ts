// POST /api/auth/consume — 메일 링크의 토큰을 C# AU-02 consume 으로 보낸다.
//
//   기존 회원 → Access 토큰을 httpOnly 쿠키에 → { next: "/map" }
//   새 회원   → 토큰을 가입 대기 쿠키에 보관 → { next: "/signup/profile" }
//               (C# 는 닉네임이 없으면 롤백해 토큰을 살려 둔다 — 프로필 제출 때 같은 토큰을 다시 쓴다)
//   실패      → C# ProblemDetails 그대로(400 Auth.InvalidLink 등)

import { NextResponse } from "next/server";
import { apiFetch, readProblem } from "@/lib/api/server";
import {
  AUTH_COOKIE,
  AUTH_MAX_AGE_SECONDS,
  SIGNUP_COOKIE,
  SIGNUP_MAX_AGE_SECONDS,
  cookieOptions,
  type SignupPending,
} from "@/lib/api/cookies";

type ConsumeResponse = { accessToken: string | null; needsNickname: boolean; email: string | null };

export async function POST(request: Request) {
  const { token } = (await request.json()) as { token?: string };

  const res = await apiFetch("/auth/magic-link/consume", { method: "POST", body: JSON.stringify({ token }) });
  if (!res.ok) {
    const problem = await readProblem(res);
    return NextResponse.json(problem, { status: problem.status });
  }

  const body = (await res.json()) as ConsumeResponse;

  if (body.accessToken) {
    const response = NextResponse.json({ next: "/map" });
    response.cookies.set(AUTH_COOKIE, body.accessToken, cookieOptions(AUTH_MAX_AGE_SECONDS));
    response.cookies.delete(SIGNUP_COOKIE);
    return response;
  }

  const pending: SignupPending = { token: token!, email: body.email ?? "" };
  const response = NextResponse.json({ next: "/signup/profile" });
  response.cookies.set(SIGNUP_COOKIE, JSON.stringify(pending), cookieOptions(SIGNUP_MAX_AGE_SECONDS));
  response.cookies.delete(AUTH_COOKIE);
  return response;
}
