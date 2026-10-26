// 서버 컴포넌트 — 가드 + 프로필 전달.

import { requireProfile } from "@/lib/guard.server";
import { SettingsScreen } from "./SettingsScreen";

export default async function SettingsPage() {
  const user = await requireProfile();
  return <SettingsScreen user={user} />;
}
