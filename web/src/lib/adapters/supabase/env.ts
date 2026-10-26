// =========================================================
// Supabase 접속 정보 — client.ts · server.ts · middleware.ts 가 공유한다
//
// 세 곳이 같은 값을 읽어야 하는데 서로를 import 할 수는 없다:
//   client.ts     브라우저 전용
//   server.ts     next/headers 사용 → 서버 컴포넌트·Route Handler 전용
//   middleware.ts middleware 런타임에서는 next/headers 가 동작하지 않는다
// 그래서 env 만 이 파일로 분리한다. 외부 의존 0.
//
// placeholder: env 미설정 상태에서도 빌드가 throw 되지 않게 한다.
// CI 에 Supabase 시크릿을 넣지 않는 근거(진행내역 Phase 0-2).
// =========================================================

export const SUPABASE_URL =
  process.env.NEXT_PUBLIC_SUPABASE_URL || "https://placeholder.supabase.co";

export const SUPABASE_ANON_KEY =
  process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY || "placeholder-anon-key";
