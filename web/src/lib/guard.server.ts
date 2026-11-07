// =========================================================
// 화면 가드 (서버 컴포넌트 전용)
//
// getCurrentUser() 의 3-상태를 "어디로 보낼지"로 바꾸는 곳이다.
// 데이터 계층(data/user.server.ts)은 상태만 알려주고 라우팅은 하지 않는다
// — 목적지 결정은 화면 계층의 책임이기 때문이다(CLAUDE.md 계층 규칙).
//
// 🚫 클라이언트 컴포넌트에서 import 하지 않는다(next/headers 가 딸려 온다).
//
// ⚠️ proxy 가 이미 /map·/review·/profile 을 막지만 여기서 또 확인한다.
//    matcher 에서 빠지거나 접두사가 어긋난 경로는 proxy 를 통과하므로,
//    화면마다의 가드가 최종 방어선이다(검증 이중화 — 아키텍처_패턴.md §4).
// =========================================================

import { redirect } from "next/navigation";
import { getCurrentUser } from "./data/user.server";
import type { User } from "./types";

/**
 * 로그인 + 프로필이 모두 있어야 하는 화면에서 쓴다.
 * 반환값이 곧 화면에 내릴 `user` 다.
 *
 *   signed_out → /login
 *   no_profile → /signup/profile   ← "프로필 없는 계정" 가드
 *
 * 🔴 no_profile 을 /login 으로 보내면 **로그인은 되어 있으니 다시 /map 으로
 *    돌아오고, 또 튕기는 무한 루프**가 된다. 세션을 끊는 것이 아니라
 *    남은 절차(프로필 입력)로 보내는 것이 맞다.
 */
export async function requireProfile(): Promise<User> {
  const current = await getCurrentUser();

  if (current.status === "signed_out") redirect("/login");
  if (current.status === "no_profile") redirect("/signup/profile");
  return current.user;
}

/**
 * `/signup/profile` 전용 가드 — 여기만 요구 조건이 반대다.
 * **프로필이 아직 없는** 인증된 계정만 들어올 수 있다.
 *
 *   signed_out → /login  (매직링크부터 다시)
 *   ready      → /map    (이미 프로필이 있다 → 폼을 다시 보여줄 이유가 없다)
 */
export async function requireProfileSetup(): Promise<{ email: string }> {
  const current = await getCurrentUser();

  if (current.status === "signed_out") redirect("/login");
  if (current.status === "ready") redirect("/map");
  return { email: current.email };
}
