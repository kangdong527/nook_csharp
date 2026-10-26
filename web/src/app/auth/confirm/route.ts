// =========================================================
// Route Handler — 이메일 링크를 눌렀을 때 도착하는 자리
//
// 2주차 로드맵의 "Route Handler (app/api/*/route.ts)" 항목이 여기서 실물이 된다.
// 페이지가 아니라 **HTTP 요청을 직접 받아 응답하는 함수**다.
// 화면이 없으므로 export default 컴포넌트가 아니라 GET 을 export 한다.
//
// 흐름:
//   1. 사용자가 이메일의 링크를 클릭
//   2. 여기로 GET (?token_hash=...&type=...&next=...)
//   3. verifyOtp 로 토큰 검증 → **쿠키에 세션이 심어진다** ← 인증의 실제 시작점
//   4. next 로 리다이렉트
//
// 📌 Route Handler 에서는 쿠키 쓰기가 **가능**하다. server.ts 의 setAll 을
//    try/catch 로 감쌌던 제약(서버 컴포넌트)이 여기서는 적용되지 않아
//    실제로 Set-Cookie 가 나간다. 그래서 인증의 시작점이 페이지가 아니라 라우트다.
//
// 🚨 Supabase 대시보드 선행 조건 (코드만으로는 동작하지 않는다):
//    ① Authentication → Email Templates 에서 두 템플릿의 링크를 아래로 교체
//        Magic Link     : {{ .SiteURL }}/auth/confirm?token_hash={{ .TokenHash }}&type=magiclink&next={{ .RedirectTo }}
//        Confirm signup : {{ .SiteURL }}/auth/confirm?token_hash={{ .TokenHash }}&type=signup&next={{ .RedirectTo }}
//    ② URL Configuration → Redirect URLs 에 http://localhost:3300/** 등록
//        (검사 대상은 emailRedirectTo 에 담긴 **최종 목적지**다)
//    기본 템플릿을 두면 증상은 "메일은 오는데 링크를 눌러도 로그인되지 않음"이다.
// =========================================================

import { type EmailOtpType } from "@supabase/supabase-js";
import { redirect } from "next/navigation";
import { type NextRequest } from "next/server";
import { createClient } from "@/lib/adapters/supabase/server";

const DEFAULT_NEXT = "/map";

/**
 * next 파라미터를 **같은 출처의 경로로만** 허용한다.
 *
 * 🚨 검증 없이 redirect(next) 하면 오픈 리다이렉트가 된다 —
 *    공격자가 /auth/confirm?next=https://evil.example 링크를 만들어
 *    "우리 도메인에서 시작하는 신뢰할 수 있는 링크"로 위장할 수 있다.
 *    쿼리스트링은 사용자가 조작할 수 있는 입력이라는 점을 잊지 않는다.
 */
function safeNext(raw: string | null, origin: string): string {
  if (!raw) return DEFAULT_NEXT;
  try {
    // 상대 경로도 절대 URL 도 같은 방식으로 다루기 위해 origin 을 기준으로 해석한다.
    const target = new URL(raw, origin);
    if (target.origin !== origin) return DEFAULT_NEXT;
    return `${target.pathname}${target.search}`;
  } catch {
    return DEFAULT_NEXT;
  }
}

export async function GET(request: NextRequest) {
  const { searchParams, origin } = new URL(request.url);

  const token_hash = searchParams.get("token_hash");
  // searchParams.get() 의 반환은 string | null 이지만 verifyOtp 는 리터럴 유니온만
  // 받는다. 쿼리스트링은 런타임 입력이라 TS 가 검증할 수 없어 단언이 필요하다.
  // 단언은 "믿는다"는 선언일 뿐 검사가 아니다 — 잘못된 값은 verifyOtp 가 거부하고
  // 아래 실패 경로로 간다. 그게 실제 방어선이다.
  const type = searchParams.get("type") as EmailOtpType | null;
  const next = safeNext(searchParams.get("next"), origin);

  // 🚧 Step 4 진단용 로그 — 매직링크 흐름이 확인되면 제거한다.
  //    실패 원인이 "템플릿 미교체(파라미터 없음)"인지 "토큰 검증 실패"인지
  //    구분하려면 받은 파라미터를 그대로 봐야 한다.
  if (process.env.NODE_ENV !== "production") {
    console.log("[auth/confirm] 수신 파라미터", {
      token_hash: token_hash ? `${token_hash.slice(0, 12)}…` : null,
      type,
      rawNext: searchParams.get("next"),
      resolvedNext: next,
      allParams: Object.fromEntries(searchParams.entries()),
    });
  }

  if (token_hash && type) {
    const supabase = createClient();
    const { error } = await supabase.auth.verifyOtp({ type, token_hash });

    if (process.env.NODE_ENV !== "production") {
      console.log("[auth/confirm] verifyOtp 결과", {
        ok: !error,
        code: error?.code,
        status: error?.status,
        message: error?.message,
      });
    }

    // ⚠️ redirect() 를 try/catch 로 감싸지 않는다. Next 의 redirect() 는
    //    내부적으로 예외를 throw 해서 동작하므로 catch 가 삼키면 조용히 사라진다.
    if (!error) redirect(next);
  }

  // 링크가 만료됐거나 이미 사용됐거나 위조된 경우.
  // 문구는 만들지 않고 신호만 남긴다 — reason → 문구 매핑은 화면의 책임이다.
  redirect("/login?error=link_invalid");
}
