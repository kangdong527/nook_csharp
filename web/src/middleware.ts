// =========================================================
// middleware — 모든 요청이 렌더링 **전에** 통과하는 지점
//
// 두 가지 책임을 진다:
//   ① 만료 직전 토큰 갱신 → 갱신된 쿠키를 응답에 실어 보낸다
//   ② 미인증 사용자를 보호 라우트에서 /login 으로 돌려보낸다
//
// ①이 middleware 의 존재 이유다. 서버 컴포넌트는 렌더링 시점에 응답 헤더가
// 이미 확정돼 쿠키를 쓸 수 없다(adapters/supabase/server.ts 의 빈 catch).
// 그래서 갱신을 여기서 대신 한다. 이게 없으면 세션이 만료되는 순간
// "로그인은 되어 있는데 조용히 끊기는" 상태가 된다.
//
// 왜 server.ts 를 재사용하지 않는가:
//   middleware 런타임에서는 next/headers 의 cookies() 가 동작하지 않는다.
//   쿠키는 request/response 객체에서 직접 다뤄야 하므로 별도 구성이 필요하다.
// =========================================================

import { createServerClient } from "@supabase/ssr";
import { NextResponse, type NextRequest } from "next/server";
import { SUPABASE_ANON_KEY, SUPABASE_URL } from "@/lib/adapters/supabase/env";

// 로그인이 필요한 라우트. 접두사로 판정한다(/review/edit/[id] 같은 동적 경로 포함).
const PROTECTED_PREFIXES = ["/map", "/review", "/profile"] as const;

export async function middleware(request: NextRequest) {
  // setAll 안에서 새 응답으로 **교체**하므로 const 가 아니라 let 이다.
  let response = NextResponse.next({ request });

  const supabase = createServerClient(SUPABASE_URL, SUPABASE_ANON_KEY, {
    cookies: {
      getAll() {
        // server.ts 는 cookieStore, 여기는 request.cookies — 출처만 다르다.
        return request.cookies.getAll();
      },
      setAll(cookiesToSet, headers) {
        // ① 이 요청의 이후 단계(서버 컴포넌트)가 갱신된 쿠키를 읽게 한다.
        //    빼면 middleware 는 새 토큰을 아는데 페이지는 낡은 걸 본다.
        cookiesToSet.forEach(({ name, value }) => {
          request.cookies.set(name, value);
        });

        // ② ①의 수정을 반영한 응답을 새로 만든다.
        response = NextResponse.next({ request });

        // ③ 브라우저에 갱신된 쿠키를 내려보낸다. options(만료·경로 등) 필수.
        //    ①은 서버 내부용, ③은 브라우저용이라 둘 다 필요하다.
        cookiesToSet.forEach(({ name, value, options }) => {
          response.cookies.set(name, value, options);
        });

        // ④ 🚨 Cache-Control: private, no-store 등을 응답에 적용한다.
        //    빠뜨리면 토큰이 실린 응답이 CDN(Vercel Edge)에 캐시되어
        //    한 사용자의 세션이 다른 사용자에게 전달될 수 있다.
        //    @supabase/ssr 0.12 에서 추가된 인자다(구버전 예제에는 없다).
        Object.entries(headers).forEach(([key, value]) => {
          response.headers.set(key, value);
        });
      },
    },
  });

  // ⚠️ getSession() 이 아니라 getUser() — 전자는 JWT 서명을 검증하지 않아
  //    쿠키를 조작하면 통과한다.
  // 📌 이 await 자체가 토큰 갱신을 유발한다(필요하면 내부에서 setAll 호출).
  //    호출하지 않으면 갱신이 아예 일어나지 않는다.
  const {
    data: { user },
  } = await supabase.auth.getUser();

  // 보호 라우트 가드 — 렌더링 전에 끊어 "화면이 그려진 뒤 리다이렉트"되는
  // 깜빡임을 없앤다(docs/아키텍처_패턴.md §4).
  // ℹ️ middleware 만 믿지 않는다. matcher 에서 빠진 경로는 통과하므로
  //    Step 5 에서 page.tsx 에도 가드를 둔다(검증 이중화).
  const path = request.nextUrl.pathname;
  const needsAuth = PROTECTED_PREFIXES.some((p) => path.startsWith(p));

  if (needsAuth && !user) {
    const redirectUrl = request.nextUrl.clone();
    redirectUrl.pathname = "/login";
    return NextResponse.redirect(redirectUrl);
  }

  // 🚨 위에서 만든 response 를 그대로 반환한다. NextResponse.next() 를 새로
  //    만들어 반환하면 setAll 이 심어둔 갱신 쿠키가 사라진다
  //    — 증상은 "며칠에 한 번씩 로그아웃"이다.
  return response;
}

// =========================================================
// matcher — middleware 를 **어떤 요청에** 돌릴지 정한다.
//
// 정적 파일·이미지까지 통과시키면 요청마다 불필요한 실행이 붙는다.
// 아래 패턴은 _next/static · _next/image · favicon · 이미지 확장자를 제외한
// 모든 경로를 대상으로 한다(Supabase 공식 예시와 동일한 형태).
//
// ⚠️ /auth/callback (Step 4 에서 신설)은 **반드시 통과해야 한다.**
//    그 자리에서 쿠키가 처음 심어지기 때문이다. 아래 패턴에 포함되어 있다.
// =========================================================
export const config = {
  matcher: [
    "/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)",
  ],
};
