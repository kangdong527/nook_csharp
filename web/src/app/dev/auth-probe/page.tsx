// =========================================================
// dev 전용 라우트 가드
//
// 이 화면은 임의 이메일로 매직링크를 발송하고 세션 정보를 노출한다.
// 프로덕션 빌드(Vercel Preview 포함)에서는 **반드시 막혀야 한다.**
//
// 🚨 가드를 서버 컴포넌트에 두는 이유:
//    클라이언트에서 판정하면 화면이 이미 브라우저로 내려간 뒤다. 번들에 코드가
//    들어 있으므로 "안 보이게" 하는 것만으로는 막은 것이 아니다.
//    여기서 notFound() 를 호출하면 라우트 자체가 404 가 되어 내려가지 않는다.
//
// 📌 이 파일이 Phase 1-5 의 남은 작업(화면 7곳 전환)에서 쓸 패턴이다:
//      page.tsx        서버 — 가드 + 초기 데이터
//        └─ *.tsx      "use client" — 상호작용만
//    (docs/아키텍처_패턴.md §4)
// =========================================================

import { notFound } from "next/navigation";
import { AuthProbe } from "./AuthProbe";

export default function AuthProbePage() {
  if (process.env.NODE_ENV === "production") notFound();
  return <AuthProbe />;
}
