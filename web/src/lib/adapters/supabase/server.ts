// =========================================================
// 계층 ④ 어댑터 — 서버용 Supabase 클라이언트
//
// 쓰는 곳: 서버 컴포넌트(page.tsx) · Route Handler(app/api/*) · Server Action
// 🚫 클라이언트 컴포넌트에서 이 파일을 import 하지 않는다.
//    `next/headers` 가 클라이언트 번들에 끌려 들어가 빌드가 깨진다.
//    (짝: ./client.ts)
// =========================================================

import { createServerClient } from "@supabase/ssr";
import { cookies } from "next/headers";
import { SUPABASE_ANON_KEY, SUPABASE_URL } from "./env";

// 서버에서 실행되지만 여기서도 **anon 키**를 쓴다.
// 목적이 "이 요청을 보낸 사용자로서 접근"이기 때문이다 — 사용자의 쿠키에서
// 꺼낸 JWT 를 anon 키와 함께 보내면 RLS 가 auth.uid() 로 그 사용자를 식별한다.
// service_role 키를 쓰면 RLS 를 **우회**해 버려서 목적이 달라진다
// (그건 Phase 1-8 의 관리자성 연산에서만 쓰고, 그때도 별도 파일로 분리한다).

/**
 * 서버용 Supabase 클라이언트를 반환한다. **요청마다 호출한다.**
 *
 * ℹ️ setAll 의 두 번째 인자 headers 는 여기서 받지 않는다 — 응답 헤더를
 *    세울 수 없는 자리이므로 쓸 데가 없다. 실제로 적용하는 곳은 middleware.ts 다.
 *
 * ⚠️ Next 14 에서 cookies() 는 동기다. Next 15+ 에서 async 로 바뀌므로
 *    D-2(Next 16 업그레이드)의 직접 영향권이다.
 */
export function createClient() {
  // 🚨 cookies() 는 반드시 이 함수 **안에서** 호출한다.
  //    모듈 최상단에 두면 import 시점(요청 밖)에 한 번만 평가되어,
  //    첫 요청의 쿠키가 프로세스 전체에 고정된다 → 사용자 간 세션 혼입.
  const cookieStore = cookies();

  return createServerClient(SUPABASE_URL, SUPABASE_ANON_KEY, {
    cookies: {
      getAll() {
        return cookieStore.getAll();
      },
      setAll(cookiesToSet) {
        try {
          cookiesToSet.forEach(({ name, value, options }) => {
            cookieStore.set(name, value, options);
          });
        } catch (e) {
          // 서버 컴포넌트에서는 응답 헤더가 이미 확정되어 set() 이 throw 한다.
          // 실패가 아니라 정상 경로다 — 토큰 갱신은 middleware.ts 가 담당한다.
          // (에러를 숨기는 빈 catch 가 아니므로 지우지 말 것)
          //
          // 🚧 단, **Route Handler 에서는 쿠키 쓰기가 되어야 한다.** 거기서 실패하면
          //    세션이 심어지지 않고도 조용히 성공처럼 보인다 → dev 에서는 드러낸다.
          if (process.env.NODE_ENV !== "production") {
            console.warn(
              "[supabase/server] 쿠키 쓰기 실패 (서버 컴포넌트라면 정상):",
              cookiesToSet.map((c) => c.name).join(", "),
              e instanceof Error ? e.message : e
            );
          }
        }
      },
    },
  });
}

// ---------------------------------------------------------
// 📌 Step 5 에서 쓸 때 미리 알아둘 것 — getUser() vs getSession()
//
//   서버에서 **인증 판단**을 할 때는 getSession() 을 쓰지 않는다.
//   getSession() 은 쿠키 값을 그대로 읽어 돌려줄 뿐 JWT 서명을 검증하지 않으므로,
//   쿠키를 조작하면 통과한다.
//   getUser() 는 Supabase 서버에 물어 토큰을 검증한다 → 가드에는 이쪽을 쓴다.
// ---------------------------------------------------------
