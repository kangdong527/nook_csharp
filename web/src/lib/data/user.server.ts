// =========================================================
// 계층 ② 데이터 접근 — 현재 사용자 (서버)
//
// 🚫 클라이언트 컴포넌트에서 import 하지 않는다.
//    adapters/supabase/server.ts → next/headers 가 딸려 와 빌드가 깨진다.
//    (짝: ./user.ts — 브라우저용 프로필 생성)
//
// 이 파일이 useSession.ts(localStorage) 를 대체한다. 판정 주체가
// **브라우저 → 서버**로 바뀌는 것이 Phase 1-5 화면 전환의 핵심이다.
// =========================================================

import { createClient } from "../adapters/supabase/server";
import type { User } from "../types";
import { mapUser, USER_SELECT, type UserRow } from "./user-row";

/**
 * 현재 요청의 사용자 상태.
 *
 * **3-상태인 것이 요점이다.** "인증됨 / 아님"의 2-상태로 다루면
 * `no_profile`(auth.users 는 있는데 public.users 행이 없는 계정)이
 * `ready` 로 뭉개져서 화면이 닉네임·아바타 없는 사용자를 받는다.
 * 이 상태는 실제로 발생한다:
 *   ① 매직링크 인증 직후 ~ 프로필 폼 제출 전 (정상 경로)
 *   ② 프로필 생성이 중간에 실패 (개발_운영_전략.md §4 P0-4 "프로필 없는 계정")
 */
export type CurrentUser =
  | { status: "signed_out" }
  | { status: "no_profile"; authId: string; email: string }
  | { status: "ready"; user: User };

/**
 * 쿠키 세션 → auth 계정 → public.users 프로필까지 확인해 돌려준다.
 *
 * ⚠️ 요청마다 Supabase 왕복 1~2회다. 한 요청에서 여러 번 부르지 말고
 *    page.tsx 에서 한 번 받아 props 로 내린다.
 */
export async function getCurrentUser(): Promise<CurrentUser> {
  const supabase = createClient();

  // getUser() 는 Supabase 서버에 물어 JWT 를 검증한다. 가드에는 반드시 이쪽.
  const {
    data: { user: authUser },
  } = await supabase.auth.getUser();

  if (!authUser?.email) return { status: "signed_out" };

  // 🔑 id 로 찾는다. email 로 찾으면 인수 시점의 데모 행(auth 계정 없는 고아
  //    프로필, D-6)과 이메일이 같을 때 남의 프로필을 집어올 수 있다.
  const { data, error } = await supabase
    .from("users")
    .select(USER_SELECT)
    .eq("id", authUser.id)
    .maybeSingle();

  if (error) throw error; // 조회 실패는 예상 불가능한 실패다

  if (!data) {
    return { status: "no_profile", authId: authUser.id, email: authUser.email };
  }

  return { status: "ready", user: mapUser(data as UserRow) };
}
