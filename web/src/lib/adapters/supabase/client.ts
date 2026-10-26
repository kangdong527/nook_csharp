// =========================================================
// 계층 ④ 어댑터 — 브라우저용 Supabase 클라이언트
//
// 이 파일은 **클라이언트 컴포넌트에서만** import 한다.
// 짝이 되는 파일: ./server.ts (서버 컴포넌트·Route Handler 전용)
//
// 왜 두 파일로 나눠야 하는가 🚨
//   server.ts 는 `next/headers` 의 cookies() 를 쓴다. 한 파일에 합치면
//   클라이언트 컴포넌트가 이 모듈을 import 하는 순간 `next/headers` 까지
//   클라이언트 번들에 끌려 들어가 빌드가 깨진다.
//   (docs/아키텍처_패턴.md §4 "use client" 전파)
// =========================================================

import { createBrowserClient } from "@supabase/ssr";
import { SUPABASE_ANON_KEY, SUPABASE_URL } from "./env";

// 이 코드는 브라우저에서 실행되므로 값이 번들에 인라인되어야 한다 → NEXT_PUBLIC_.
// anon 키는 애초에 공개 전제이며, 실제 접근 차단은 RLS(Phase 1-6)가 한다.
// 🚫 service_role 키에는 NEXT_PUBLIC_ 을 절대 붙이지 않는다 (CLAUDE.md 🚫 #2).

/**
 * 브라우저용 Supabase 클라이언트를 반환한다.
 *
 * 쿠키 옵션을 넘기지 않는 것이 정답이다 — 라이브러리가 document.cookie 로
 * 알아서 읽고 쓴다. 타입 정의에도 그렇게 적혀 있다:
 *   "In most cases you should not configure the `options.cookies` object,
 *    as this is automatically handled for you."
 *
 * 상수가 아니라 **함수**인 이유:
 *   ① server.ts 와 시그니처가 대칭이 된다(그쪽은 요청마다 새로 만들어야 하므로
 *      반드시 함수여야 한다).
 *   ② import 만으로 클라이언트가 생성되지 않는다 — 생성 시점이 호출자에게 있다.
 *
 * 중복 호출은 문제되지 않는다: createBrowserClient 는 브라우저에서
 * `isSingleton` 이 기본 활성이라 캐시된 같은 인스턴스를 돌려준다.
 */
export function createClient() {
  return createBrowserClient(SUPABASE_URL, SUPABASE_ANON_KEY);
}
