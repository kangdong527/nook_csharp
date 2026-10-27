// =========================================================
// 계층 ② 데이터 접근 — 현재 사용자 (서버)
//
// 🚫 클라이언트 컴포넌트에서 import 하지 않는다(lib/api/server.ts → next/headers).
//    (짝: ./user.ts — 브라우저용 프로필 생성)
//
// FE-2: Supabase 세션 → BFF 쿠키 + C# GET /me(AU-04).
// =========================================================

import { AUTH_COOKIE } from "../api/cookies";
import { apiFetch, readSignupPending } from "../api/server";
import { cookies } from "next/headers";
import type { User } from "../types";

/**
 * 현재 요청의 사용자 상태. 3-상태 구조는 원본 그대로 둔다(가드가 이 모양에 기대고 있다).
 *
 *   ready      — Access 토큰 쿠키가 있고 C# /me 가 200
 *   no_profile — 토큰은 없고 가입 대기 쿠키가 있다(링크는 열었고 프로필 폼 제출 전)
 *                원본의 "auth 계정은 있는데 프로필 행이 없음"과 같은 자리다.
 *                우리 구조에선 이 상태에 회원 행이 아예 없다(consume 이 롤백해 둠)
 *   signed_out — 둘 다 없거나, 토큰이 만료·위조·탈퇴 후 남은 것(/me 401)
 */
export type CurrentUser =
  | { status: "signed_out" }
  | { status: "no_profile"; email: string }
  | { status: "ready"; user: User };

type MeResponse = { id: string; email: string; nickname: string };

/**
 * ⚠️ 요청마다 C# 왕복 1회다. 한 요청에서 여러 번 부르지 말고
 *    page.tsx 에서 한 번 받아 props 로 내린다.
 */
export async function getCurrentUser(): Promise<CurrentUser> {
  if (cookies().get(AUTH_COOKIE)) {
    const res = await apiFetch("/me");
    if (res.ok) return { status: "ready", user: toUser((await res.json()) as MeResponse) };
    // 401 = 미들웨어(만료·위조) 또는 핸들러(Auth.UnknownUser) — 프론트는 둘을 구분하지 않는다(AU-04 결정).
    // 서버 컴포넌트는 쿠키를 지울 수 없다 — 남은 쿠키는 다음 로그인이 덮어쓴다.
    if (res.status !== 401) throw new Error(`/me 실패: ${res.status}`);
  }

  const pending = readSignupPending();
  if (pending) return { status: "no_profile", email: pending.email };

  return { status: "signed_out" };
}

// /me 는 아직 성별·생일·아바타를 주지 않는다 — 화면이 필요해지면 MeView 에 더한다(PF-01 때).
function toUser(me: MeResponse): User {
  return { id: me.id, email: me.email, nickname: me.nickname, gender: null, birth: null, avatarUrl: null };
}
