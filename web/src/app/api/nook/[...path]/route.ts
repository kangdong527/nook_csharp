// =========================================================
// BFF 중계 — 브라우저 → /api/nook/<C# 경로> → C# API (FE-2 사용자 결정)
//
// 브라우저는 C# API 를 직접 부르지 않는다. 쿠키의 Access 토큰을 Bearer 로 바꿔 넘기는
// 일을 여기 한 곳에서 한다(토큰이 브라우저 JS 에 닿지 않는다).
//
// 🔒 열린 중계(open proxy)가 되지 않게 하는 장치 4개:
//   1. 허용 목록 — 화면이 실제로 쓰는 (메서드, 경로)만. 없으면 404
//   2. 헤더 정리 — 브라우저의 Authorization·Cookie 는 버리고 서버가 쿠키에서 꺼낸 토큰만 붙인다.
//      API 의 Set-Cookie 도 브라우저로 넘기지 않는다
//   3. CSRF — 쿠키가 SameSite=Lax 라 다른 사이트의 POST·PUT·DELETE 에는 쿠키가 안 붙는다.
//      그 위에 GET 이 아니면 Origin 이 우리 출처인지 한 번 더 본다(Server Actions 가 하는 검사와 같다)
//   4. 경로 조작 — "."·".."·슬래시가 섞인 세그먼트는 거절(인코딩된 %2F 로 허용 목록을 빠져나가는 것 방지)
//
// 권한 판단(남의 리뷰 등)은 여기서 하지 않는다 — C# API 가 한다. 여기는 통로만.
// =========================================================

import { NextResponse, type NextRequest } from "next/server";
import { apiFetch } from "@/lib/api/server";
import { isAllowedOrigin } from "@/lib/api/origin";

const GUID = "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";

/** 허용 목록. 화면을 연결할 때마다 여기에 한 줄 — 곧 "프론트가 쓰는 API 목록"이다. */
const ALLOWED: ReadonlyArray<{ method: string; path: RegExp }> = [
  // 지도·별 상태 (BM-03, PL-01, PL-02 장소 상세, BM-01·02)
  { method: "GET", path: /^\/me\/places$/ },

  // 프로필 (PF-04 내 리뷰 피드, PF-01 닉네임·사진)
  { method: "GET", path: /^\/me\/reviews$/ },
  { method: "PATCH", path: /^\/me$/ },
  { method: "PUT", path: /^\/me\/avatar$/ },
  { method: "POST", path: /^\/places$/ },
  { method: "GET", path: new RegExp(`^/places/${GUID}$`) },
  { method: "PUT", path: new RegExp(`^/places/${GUID}/bookmark$`) },
  { method: "DELETE", path: new RegExp(`^/places/${GUID}/bookmark$`) },

  // 리뷰 (PL-02 목록, RV-01~04, LK-01)
  { method: "GET", path: new RegExp(`^/places/${GUID}/reviews$`) },
  { method: "POST", path: /^\/reviews$/ },
  { method: "GET", path: new RegExp(`^/reviews/${GUID}$`) },
  { method: "PUT", path: new RegExp(`^/reviews/${GUID}$`) },
  { method: "DELETE", path: new RegExp(`^/reviews/${GUID}$`) },
  { method: "PUT", path: new RegExp(`^/reviews/${GUID}/like$`) },
  { method: "DELETE", path: new RegExp(`^/reviews/${GUID}/like$`) },

  // 그룹 (GR-01~06)
  { method: "GET", path: /^\/me\/groups$/ },
  { method: "POST", path: /^\/groups$/ },
  { method: "POST", path: /^\/groups\/join$/ },
  { method: "PATCH", path: new RegExp(`^/groups/${GUID}$`) },
  { method: "DELETE", path: new RegExp(`^/groups/${GUID}/membership$`) },
  { method: "GET", path: new RegExp(`^/groups/${GUID}/places$`) },

  // 리뷰 이미지·프로필 사진(<img src>). C# 은 익명 허용이고 키 모양을 다시 검사한다 — 여기서도 같은 모양만 넘긴다.
  { method: "GET", path: /^\/images\/(reviews|avatars)\/[0-9]{4}\/[0-9]{2}\/[0-9]{2}\/[0-9a-f]{32}\.(jpg|png|webp)$/ },
];

type Context = { params: Promise<{ path: string[] }> }; // Next 15+: Promise

async function relay(request: NextRequest, { params }: Context): Promise<NextResponse> {
  const segments = (await params).path;
  if (segments.some((s) => s === "" || s === "." || s === ".." || /[/\\]/.test(s))) {
    return NextResponse.json({ status: 404 }, { status: 404 });
  }

  const path = `/${segments.join("/")}`;
  const method = request.method;
  if (!ALLOWED.some((a) => a.method === method && a.path.test(path))) {
    return NextResponse.json({ status: 404 }, { status: 404 });
  }

  if (method !== "GET" && !isAllowedOrigin(request)) {
    return NextResponse.json({ status: 403, title: "Bff.CrossOrigin" }, { status: 403 });
  }

  const headers = new Headers();
  const contentType = request.headers.get("content-type");
  if (contentType) headers.set("Content-Type", contentType); // multipart 경계(boundary) 포함 그대로

  const res = await apiFetch(`${path}${request.nextUrl.search}`, {
    method,
    headers,
    body: method === "GET" || method === "DELETE" ? undefined : await request.arrayBuffer(),
  });

  // 넘기는 헤더는 이것만(Set-Cookie 등 API 헤더는 넘기지 않는다): 본문 해석·이미지 캐시(immutable)·nosniff.
  const out = new Headers();
  for (const name of ["content-type", "cache-control", "x-content-type-options"]) {
    const value = res.headers.get(name);
    if (value) out.set(name, value);
  }
  const location = res.headers.get("location");
  if (location) out.set("Location", location.replace(/^https?:\/\/[^/]+/, "/api/nook"));
  return new NextResponse(res.status === 204 ? null : res.body, { status: res.status, headers: out });
}

export const GET = relay;
export const POST = relay;
export const PUT = relay;
export const PATCH = relay;
export const DELETE = relay;
