// 서버 컴포넌트 — 가드 + 프로필 전달.

import { requireProfile } from "@/lib/guard.server";
import { SettingsScreen } from "./SettingsScreen";

export default async function SettingsPage() {
  // 가드만(로그인 필요) — 화면이 회원 값을 쓰지 않는다. 탈퇴는 서버가 토큰으로 회원을 정한다.
  await requireProfile();
  return <SettingsScreen />;
}
