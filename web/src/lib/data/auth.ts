// =========================================================
// 계층 ② 데이터 접근 — 인증
//
// store.ts 의 signIn / clearSession(localStorage 데모)을 대체한다.
// 규칙(CLAUDE.md):
//   - UI 문구를 만들지 않는다. reason 만 반환하고 문구 매핑은 화면의 책임.
//   - 예상 가능한 실패는 Result, 예상 불가능한 실패는 throw.
//
// 브라우저 클라이언트를 쓴다(로그인 화면에서 호출).
// 화면이 supabase 를 직접 import 하지 않게 하는 것이 이 계층의 목적이다.
// =========================================================

import { createClient } from "../adapters/supabase/client";
import { err, ok, type Result } from "../result";

/**
 * 매직링크 발송 실패 이유.
 *
 * 🚫 "error" 를 넣지 않는다 — 정체불명 실패는 throw 해서 Sentry 에 남긴다
 *    (docs/아키텍처_패턴.md §187). store.ts:96 이 현재 그 안티패턴이다.
 */
export type SendLinkFail =
  | "not_registered" // 로그인 화면: 미가입 이메일 (명세서 :148)
  | "already_registered" // 가입 화면: 이미 가입된 이메일 (명세서 :81)
  | "rate_limited"; // 연속 발송 제한 — 알려주지 않으면 "눌렀는데 아무 일도 없다"가 된다

export type SendLinkMode = "login" | "signup";

/**
 * 이메일이 이미 가입되어 있는지 확인한다.
 *
 * 가입 화면에서만 필요하다. signInWithOtp 는 **기존 사용자에게 shouldCreateUser:
 * true 로 호출하면 에러 없이 로그인 링크를 보내고 성공으로 끝난다** — 즉 "이미
 * 가입됨"을 에러로 알려주지 않는다. 명세서 :81 이 그 안내를 요구하므로
 * 사전 조회가 불가피하다.
 *
 * ⚠️ 한계 두 가지를 알고 쓴다:
 *   ① TOCTOU — 조회와 발송 사이에 상태가 바뀔 수 있다. 정합성의 최종 방어선은
 *      DB 제약이고, 이 조회는 **문구를 위한 것**이다(개발_운영_전략.md §4).
 *   ② 이메일 존재 여부가 외부에 노출된다(enumeration). 명세서가 양쪽 화면 모두
 *      "미가입/이미 가입" 안내를 요구하므로 기획이 이미 감수한 트레이드오프다.
 *      RLS 를 거는 Phase 1-6 에서 이 조회 경로를 다시 검토한다.
 */
async function emailExists(email: string): Promise<boolean> {
  const supabase = createClient();
  const { data, error } = await supabase
    .from("users")
    .select("id")
    .eq("email", email)
    .maybeSingle();

  if (error) throw error; // 조회 자체가 실패하는 건 예상 불가능한 실패다
  return !!data;
}

/**
 * 매직링크(로그인 링크 / 가입 인증 링크)를 발송한다.
 *
 * mode 가 shouldCreateUser 를 가르는 스위치다:
 *   login  → false: 미가입 이메일이면 Supabase 가 거부한다(서버가 판정)
 *   signup → true : 없으면 새로 만든다
 *
 * emailRedirectTo 는 이메일 템플릿의 {{ .RedirectTo }} 로 들어가 /auth/confirm 의
 * next 파라미터가 된다. 즉 **인증 후 최종 목적지**다.
 * ⚠️ 이 URL 이 Supabase 대시보드의 Redirect URLs 허용 목록에 없으면 조용히
 *    무시되고 Site URL 로 간다. `http://localhost:3300/**` 등록이 선행 조건이다.
 */
export async function sendMagicLink(
  email: string,
  mode: SendLinkMode
): Promise<Result<null, SendLinkFail>> {
  const normalized = email.trim().toLowerCase();

  if (mode === "signup" && (await emailExists(normalized))) {
    return err("already_registered");
  }

  const supabase = createClient();
  const { error } = await supabase.auth.signInWithOtp({
    email: normalized,
    options: {
      shouldCreateUser: mode === "signup",
      // 가입은 프로필 입력이 남아 있고(명세서 :34), 로그인은 지도로 바로 간다.
      emailRedirectTo:
        mode === "signup"
          ? `${window.location.origin}/signup/profile`
          : `${window.location.origin}/map`,
    },
  });

  if (!error) return ok(null);

  if (error.code === "over_email_send_rate_limit") return err("rate_limited");

  // 🔍 실측으로 확정한 매핑 (2026-07-31, /dev/auth-probe):
  //    미가입 이메일 + shouldCreateUser: false
  //      → code "otp_disabled" / status 422 / "Signups not allowed for otp"
  //
  // ⚠️ 같은 코드가 두 상황에서 나오므로 mode 로 갈라야 한다:
  //    ① login 모드  : 미가입 이메일 (위 실측)          → not_registered
  //    ② signup 모드 : shouldCreateUser 가 true 이므로 ①이 성립할 수 없다.
  //                    여기서 otp_disabled 가 나오면 **대시보드에서 이메일
  //                    로그인이 비활성된 설정 문제**다 → throw.
  //
  //    mode 로 가르지 않고 무조건 not_registered 로 매핑하면, 누가 Auth 설정을
  //    끈 순간 모든 사용자에게 "가입되지 않은 이메일입니다"가 떠서
  //    **서비스 전체 장애가 개별 안내 문구로 위장된다.** 아무도 알아채지 못한다.
  if (error.code === "otp_disabled" && mode === "login") {
    return err("not_registered");
  }

  // 매핑되지 않은 실패는 삼키지 않는다. throw 해야 원인이 남는다.
  throw error;
}

/**
 * 로그아웃. store.ts:38 clearSession() 을 대체한다.
 *
 * Result 를 쓰지 않는 이유: 실패해도 사용자에게 제시할 선택지가 없다
 * → 예상 불가능한 실패이므로 throw 가 맞다.
 */
export async function signOut(): Promise<void> {
  const supabase = createClient();
  const { error } = await supabase.auth.signOut();
  if (error) throw error;
}
