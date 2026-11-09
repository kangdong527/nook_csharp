// POST /api/auth/signup — 프로필 폼 제출. 가입 대기 쿠키의 토큰 + 폼 값으로 C# consume 을 다시 부른다.
// 성공하면 Access 토큰 쿠키를 심고 가입 대기 쿠키를 지운다.
// 실패(닉네임 중복 409 등)는 C# 이 롤백해 토큰이 살아 있으므로 가입 대기 쿠키를 그대로 둔다 — 폼에서 다시 제출할 수 있다.

import { NextResponse, type NextRequest } from "next/server";
import { apiFetch, problemResponse, readSignupPending } from "@/lib/api/server";
import { rejectCrossOrigin } from "@/lib/api/origin";
import { AUTH_COOKIE, AUTH_MAX_AGE_SECONDS, SIGNUP_COOKIE, cookieOptions } from "@/lib/api/cookies";

type SignupRequest = {
  nickname?: string;
  gender?: string | null;
  birthdate?: string | null;
  agreePrivacy?: boolean;
  confirmAge14?: boolean;
  agreeOptional?: boolean;
};

export async function POST(request: NextRequest) {
  const rejected = rejectCrossOrigin(request);
  if (rejected) return rejected;

  const pending = await readSignupPending();
  if (!pending) {
    // 가입 대기 쿠키 만료(15분) 또는 없음 — 매직 링크부터 다시.
    return NextResponse.json({ status: 401, title: "Signup.NotPending" }, { status: 401 });
  }

  const form = (await request.json()) as SignupRequest;
  const res = await apiFetch("/auth/magic-link/consume", {
    method: "POST",
    body: JSON.stringify({
      token: pending.token,
      nickname: form.nickname,
      gender: form.gender ?? null,
      birthdate: form.birthdate ?? null,
      // 동의(R-US-06)는 boolean 만 그대로 넘긴다. 방침 버전은 보내지 않는다 — C# 이 시행 중인 버전을 기록한다
      agreePrivacy: form.agreePrivacy === true,
      confirmAge14: form.confirmAge14 === true,
      agreeOptional: form.agreeOptional === true,
    }),
  });

  if (!res.ok) return problemResponse(res);

  const body = (await res.json()) as { accessToken: string | null };
  if (!body.accessToken) {
    // 닉네임을 보냈는데 needsNickname — 빈 닉네임이 넘어온 경우뿐이다.
    return NextResponse.json({ status: 400, title: "User.NicknameLengthOutOfRange" }, { status: 400 });
  }

  const response = new NextResponse(null, { status: 204 });
  response.cookies.set(AUTH_COOKIE, body.accessToken, cookieOptions(AUTH_MAX_AGE_SECONDS));
  response.cookies.delete(SIGNUP_COOKIE);
  return response;
}
