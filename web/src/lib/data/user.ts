// =========================================================
// 계층 ② 데이터 접근 — 프로필 생성 (브라우저)
//
// FE-2: 가입은 C# consume 한 번으로 끝난다(매직 링크 토큰 + 닉네임·성별·생일).
// 토큰은 브라우저가 모른다 — /api/auth/signup 이 가입 대기 쿠키에서 꺼내 쓴다.
// 이메일도 폼이 아니라 링크의 것이 쓰인다(폼이 준 값을 신뢰하지 않는다).
// 아바타는 가입 직후 PF-01 로 따로 올린다(FE-2 사용자 결정).
// =========================================================

import { err, ok, type Result } from "../result";
import type { Gender } from "../types";

/** 프로필 생성 실패 이유. 문구 매핑은 화면의 책임이다. */
export type CreateProfileFail =
  | "no_session" // 가입 대기(15분)가 끝났다 → 매직링크부터 다시
  | "nickname_taken" // 409 User.NicknameTaken — 동시 가입까지 DB UNIQUE 가 판정
  | "invalid_input" // 400 — 화면 검사를 통과했는데 서버가 거절(규칙 불일치)
  | "rate_limited"; // 429 — 같은 IP 의 링크 사용(consume)이 10분 5회를 넘었다(DEP 7단계). 가입 대기 쿠키는 남아 다시 제출 가능

/**
 * 원본 화면의 생년월일 입력(YY-MM-DD)을 API 형식(YYYY-MM-DD)으로 바꾼다.
 * 세기 규칙(FE-2 사용자 결정 — 변환은 FE 한 곳): YY 가 올해 끝 두 자리 이하면 2000년대, 크면 1900년대.
 * 형식 검사는 types.ts birthError 가 먼저 한다. 빈 값은 null(선택 입력).
 */
export function toIsoBirthdate(yyMmDd: string, today: Date = new Date()): string | null {
  const t = yyMmDd.trim();
  if (t.length === 0) return null;
  const [yy, mm, dd] = t.split("-");
  const currentYy = today.getFullYear() % 100;
  const century = Number(yy) <= currentYy ? 2000 : 1900;
  return `${century + Number(yy)}-${mm}-${dd}`;
}

export async function createProfile(input: {
  nickname: string;
  gender: Gender;
  birth: string | null;
}): Promise<Result<null, CreateProfileFail>> {
  const res = await fetch("/api/auth/signup", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      nickname: input.nickname.trim(),
      gender: input.gender,
      birthdate: input.birth ? toIsoBirthdate(input.birth) : null,
    }),
  });

  if (res.ok) return ok(null);
  if (res.status === 401) return err("no_session");
  if (res.status === 409) return err("nickname_taken");
  if (res.status === 400) return err("invalid_input");
  if (res.status === 429) return err("rate_limited");

  throw new Error(`signup 실패: ${res.status}`);
}
