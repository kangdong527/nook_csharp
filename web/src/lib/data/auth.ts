// =========================================================
// 계층 ② 데이터 접근 — 인증 (브라우저)
//
// FE-2: Supabase Auth → C# API(BFF 경유). 브라우저는 같은 출처의 /api/auth/* 만 부른다.
// 규칙(CLAUDE.md):
//   - UI 문구를 만들지 않는다. reason 만 반환하고 문구 매핑은 화면의 책임.
//   - 예상 가능한 실패는 Result, 예상 불가능한 실패는 throw.
// =========================================================

import { err, ok, type Result } from "../result";

/**
 * 매직링크 발송 실패 이유.
 *
 * 원본의 "not_registered"·"already_registered" 는 없다 — C# API 는 가입 여부와
 * 무관하게 같은 응답을 준다(D5, 가입 여부 노출 방지). 화면은 "메일을 보냈어요"만
 * 안내하고, 새 회원인지는 링크를 연 뒤 consume 이 판단한다(FE-2 사용자 결정 (a)).
 */
export type SendLinkFail =
  | "invalid_email" // 400 User.EmailFormat — 화면 검사를 통과했는데 서버가 거절
  | "rate_limited"; // 429 — DEP 에서 RateLimiter 가 붙으면 생긴다

/** 로그인·가입 공통 — 같은 요청이다. */
export async function sendMagicLink(email: string): Promise<Result<null, SendLinkFail>> {
  const res = await fetch("/api/auth/magic-link", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email: email.trim() }),
  });

  if (res.ok) return ok(null);
  if (res.status === 400) return err("invalid_email");
  if (res.status === 429) return err("rate_limited");

  // 매핑되지 않은 실패는 삼키지 않는다.
  throw new Error(`magic-link 요청 실패: ${res.status}`);
}

/**
 * 로그아웃(AU-03). 쿠키만 지운다 — 무상태 JWT 라 서버가 할 일이 없다.
 * 실패해도 사용자에게 제시할 선택지가 없으므로 throw.
 */
export async function signOut(): Promise<void> {
  const res = await fetch("/api/auth/logout", { method: "POST" });
  if (!res.ok) throw new Error(`logout 실패: ${res.status}`);
}
