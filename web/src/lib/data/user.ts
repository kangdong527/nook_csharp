// =========================================================
// 계층 ② 데이터 접근 — 프로필 생성 (브라우저)
//
// store.ts 의 signUp() 을 대체한다. 차이는 하나이며 그것이 이 파일의 존재 이유다:
//
//   store.signUp : insert 시 id 를 주지 않아 gen_random_uuid() 가 새 id 를 만들었다
//                  → public.users 행과 auth.users 계정이 **남남**이 된다
//   createProfile: id 를 auth.users.id 로 **고정**한다
//                  → auth.uid() = users.id 가 성립 → Phase 1-6 RLS 의 전제
//
// email 도 클라이언트가 넘긴 값이 아니라 **세션에서 읽는다.** 폼이 보낸 이메일을
// 그대로 믿으면 타인의 이메일로 프로필을 만들 수 있다.
//
// 🚫 이 파일은 브라우저 클라이언트를 쓴다. 서버에서는 user.server.ts 를 쓴다.
// =========================================================

import { createClient } from "../adapters/supabase/client";
import { err, ok, type Result } from "../result";
import type { Gender, User } from "../types";
import { mapUser, USER_SELECT, type UserRow } from "./user-row";

/**
 * 프로필 생성 실패 이유.
 * 문구 매핑은 화면의 책임이다(CLAUDE.md 계층 규칙).
 */
export type CreateProfileFail =
  | "no_session" // 인증 세션이 없다 → 매직링크부터 다시
  | "already_exists" // 이 계정의 프로필이 이미 있다
  | "nickname_taken";

/**
 * 닉네임 중복 확인.
 *
 * ⚠️ **DB 에 unique 제약이 없다**(`supabase/schema.sql:16`). 즉 이 조회는
 *    유일성을 보장하지 못하는 TOCTOU 검사이고, 동시 가입 시 중복이 통과한다.
 *    제약 추가는 마이그레이션 체계(Phase 2-9) 이후 과제다.
 */
async function nicknameTaken(nickname: string): Promise<boolean> {
  const supabase = createClient();
  const { data, error } = await supabase
    .from("users")
    .select("id")
    .eq("nickname", nickname.trim())
    .maybeSingle();

  if (error) throw error; // 조회 실패는 예상 불가능한 실패다
  return !!data;
}

/**
 * 현재 로그인한 계정의 프로필을 만든다. `/signup/profile` 에서만 호출한다.
 *
 * ⚠️ 원자적이지 않다 — 아바타 업로드가 먼저 끝나고 insert 가 실패하면 고아
 *    Storage 객체가 남는다(개발_운영_전략.md §4 P0-4). RPC 원자화는 Phase 1-8.
 *    순서 자체는 규칙을 지킨다: 업로드 → URL 확보 → insert (CLAUDE.md 🚫 #4).
 */
export async function createProfile(input: {
  nickname: string;
  gender: Gender;
  birth: string | null;
  avatarUrl: string | null;
}): Promise<Result<User, CreateProfileFail>> {
  const supabase = createClient();

  // getSession() 이 아니라 getUser() — 전자는 JWT 서명을 검증하지 않는다.
  const {
    data: { user: authUser },
    error: authError,
  } = await supabase.auth.getUser();

  if (authError) throw authError;
  if (!authUser?.email) return err("no_session");

  if (await nicknameTaken(input.nickname)) return err("nickname_taken");

  const { data, error } = await supabase
    .from("users")
    .insert({
      id: authUser.id, // 🔑 이 한 줄이 auth.users 와 public.users 를 잇는다
      email: authUser.email, // 폼이 준 값이 아니라 세션의 값
      nickname: input.nickname.trim(),
      gender: input.gender,
      birth: input.birth,
      avatar_url: input.avatarUrl,
    })
    .select(USER_SELECT)
    .single();

  if (error) {
    // 23505 = unique_violation. id(PK) 또는 email 중복이므로 프로필이 이미 있다.
    // 새로고침·이중 클릭으로 실제로 발생하는 경로이니 사용자에게 알려준다.
    if (error.code === "23505") return err("already_exists");
    throw error; // 그 외는 정체불명 → Sentry 에 남겨야 한다
  }

  return ok(mapUser(data as UserRow));
}
