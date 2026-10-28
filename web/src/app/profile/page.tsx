// 서버 컴포넌트 — 가드 + 프로필 전달. 화면은 클라이언트 잎이 그린다.
// 닉네임·아바타가 첫 렌더부터 채워져 있다(예전에는 localStorage 를 읽는
// useEffect 가 끝나기 전까지 스피너였다).

import { requireProfile } from "@/lib/guard.server";
import { ProfileScreen } from "./ProfileScreen";

export default async function ProfilePage() {
  const user = await requireProfile();
  return <ProfileScreen initialUser={user} />;
}
