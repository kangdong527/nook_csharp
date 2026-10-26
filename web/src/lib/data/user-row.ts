// =========================================================
// 계층 ② 데이터 접근 — users 행 ↔ 도메인 User 매핑
//
// 왜 별도 파일인가: 같은 매핑을 브라우저(user.ts)와 서버(user.server.ts)가
// 모두 쓰는데, 두 파일은 서로 import 할 수 없다(server.ts 가 next/headers 를
// 쓴다 → 클라이언트 번들에 섞이면 빌드가 깨진다).
// adapters/supabase/env.ts 를 분리한 것과 같은 이유다.
//
// ⚠️ store.ts 에도 같은 매핑이 있으나 그 파일은 "use client" 이므로
//    서버 컴포넌트에서 호출할 수 없다. store.ts 를 lib/data/* 로 분할할 때
//    (Phase 1-8) 이 파일로 합친다.
// =========================================================

import type { Gender, User } from "../types";

export const USER_SELECT = "id, email, nickname, gender, birth, avatar_url";

export type UserRow = {
  id: string;
  email: string;
  nickname: string;
  gender: string | null;
  birth: string | null;
  avatar_url: string | null;
};

export const mapUser = (r: UserRow): User => ({
  id: r.id,
  email: r.email,
  nickname: r.nickname,
  gender: (r.gender as Gender | null) ?? null,
  birth: r.birth,
  avatarUrl: r.avatar_url,
});
