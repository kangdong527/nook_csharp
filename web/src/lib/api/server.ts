// =========================================================
// C# API 호출 (서버 전용 — 서버 컴포넌트·route handler)
//
// 🚫 클라이언트 컴포넌트에서 import 하지 않는다(next/headers 가 딸려 온다).
//    브라우저는 C# API 를 직접 부르지 않는다 — 항상 Next 를 거친다(BFF).
// =========================================================

import { cookies, headers as requestHeaders } from "next/headers";
import { AUTH_COOKIE, SIGNUP_COOKIE, type SignupPending } from "./cookies";

/** 서버 환경변수. NEXT_PUBLIC_ 이 아니다 — API 주소를 브라우저에 알릴 이유가 없다. */
export const API_URL = process.env.NOOK_API_URL ?? "http://localhost:5153";

/** C# API 의 실패 응답(ProblemDetails). title = 오류 코드(예: "User.NicknameTaken"). */
export type Problem = { status: number; title?: string; detail?: string };

/** 쿠키의 Access 토큰을 Bearer 로 붙여 C# API 를 부른다. 응답 해석은 호출한 쪽 몫. */
export async function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const token = (await cookies()).get(AUTH_COOKIE)?.value;
  const headers = new Headers(init.headers);
  if (token) headers.set("Authorization", `Bearer ${token}`);
  if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");

  // 클라이언트 IP를 API로 넘긴다(7단계 IP RateLimiter). 받은 XFF의 맨 오른쪽 = 바로 앞 프록시(Caddy)가 적은 값만 쓴다 —
  // 왼쪽 값은 누구나 적을 수 있다. Caddy는 trusted_proxies가 없으면 들어온 XFF를 버리고 실제 접속 IP로 덮어쓴다.
  // API는 web 서브넷에서 온 XFF만 믿는다(ForwardedHeaders:KnownNetworks) — 이 줄만으로는 위조를 막지 못한다.
  const clientIp = (await requestHeaders()).get("x-forwarded-for")?.split(",").at(-1)?.trim();
  if (clientIp) headers.set("X-Forwarded-For", clientIp);

  return fetch(`${API_URL}${path}`, { ...init, headers, cache: "no-store" });
}

/** 실패 응답 본문을 Problem 으로. 본문이 비었거나 JSON 이 아니어도(미들웨어 401) status 는 남긴다. */
export async function readProblem(res: Response): Promise<Problem> {
  try {
    const body = (await res.json()) as { title?: string; detail?: string };
    return { status: res.status, title: body.title, detail: body.detail };
  } catch {
    return { status: res.status };
  }
}

// Next 15+: cookies() 는 Promise 다(요청 API 비동기화) → 이 함수도 async.
export async function readSignupPending(): Promise<SignupPending | null> {
  const raw = (await cookies()).get(SIGNUP_COOKIE)?.value;
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as Partial<SignupPending>;
    return parsed.token && parsed.email ? { token: parsed.token, email: parsed.email } : null;
  } catch {
    return null;
  }
}
