// =========================================================
// CSRF 2차 방어 — 상태를 바꾸는 요청(GET 아님)의 Origin 이 우리 출처인지 (DEP 2단계, Claude 작성)
//
// 왜 설정값인가: 프록시(Caddy) 뒤에서는 Next 가 보는 자기 주소(request.nextUrl.origin = http://web:3300 류)와
// 브라우저가 보내는 Origin(https://도메인)이 다르다 → 자기 주소와 비교하면 모든 POST 가 403.
// 그렇다고 검사를 끄면 CSRF 방어가 사라진다 → 공개 출처를 NOOK_PUBLIC_ORIGIN 으로 **명시**한다(dep-plan §4).
//
//   - 운영(NODE_ENV=production): NOOK_PUBLIC_ORIGIN 필수. 없으면 전부 거절(fail-closed — 설정 누락이 열린 문이 되지 않게)
//   - 개발(next dev): 없으면 지금처럼 자기 주소(http://localhost:3300)와 비교
//   - 쉼표로 여러 개 허용(예: https://nook.example,https://www.nook.example). 끝 슬래시는 무시 — Origin 헤더엔 경로가 없다
// =========================================================

import type { NextRequest } from "next/server";

function allowedOrigins(request: NextRequest): string[] {
  const configured = (process.env.NOOK_PUBLIC_ORIGIN ?? "")
    .split(",")
    .map((o) => o.trim().replace(/\/+$/, ""))
    .filter(Boolean);
  if (configured.length > 0) return configured;
  return process.env.NODE_ENV === "production" ? [] : [request.nextUrl.origin];
}

/** Origin 헤더가 허용 출처 중 하나와 정확히 같으면 true. Origin 이 없으면 false(같은 출처 fetch 도 POST 엔 Origin 을 붙인다). */
export function isAllowedOrigin(request: NextRequest): boolean {
  const origin = request.headers.get("origin");
  return origin !== null && allowedOrigins(request).includes(origin);
}
