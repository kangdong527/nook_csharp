// =========================================================
// 브라우저 → BFF 중계(/api/nook/*) 호출
//
// 경로는 C# API 경로 그대로 쓴다(예: "/me/places"). 허용 목록은 app/api/nook/[...path]/route.ts.
// 401(토큰 만료·탈퇴 후 토큰)은 어느 화면에서 나든 로그인으로 보낸다(AU-04 결정: 모든 401 = 다시 로그인).
// =========================================================

export async function nookFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const headers = new Headers(init.headers);
  if (typeof init.body === "string" && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const res = await fetch(`/api/nook${path}`, { ...init, headers });
  if (res.status === 401) {
    window.location.assign("/login");
    throw new Error("로그인이 필요합니다.");
  }
  return res;
}
