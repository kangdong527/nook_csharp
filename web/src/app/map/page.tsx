// 서버 컴포넌트 — 인증·프로필 가드만 하고 화면은 클라이언트 잎에 맡긴다.
// requireProfile() 이 signed_out → /login, no_profile → /signup/profile 로 보낸다.

import { requireProfile } from "@/lib/guard.server";
import { MapScreen } from "./MapScreen";

export default async function MapPage() {
  const user = await requireProfile();
  return <MapScreen user={user} />;
}
