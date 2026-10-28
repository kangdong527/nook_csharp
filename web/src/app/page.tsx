// 진입 화면 — 어디로 보낼지만 정한다.
//
// 인증 판정이 **서버로 올라온 것**이 이 파일의 변화다. 예전에는
// localStorage 의 세션 JSON 을 보고 판단해서, 쿠키 세션이 살아 있어도
// 로그인 화면으로 보내는 불일치가 있었다.

import { redirect } from "next/navigation";
import { getCurrentUser } from "@/lib/data/user.server";
import { EntryRedirect } from "./EntryRedirect";

export default async function RootPage() {
  const current = await getCurrentUser();

  if (current.status === "ready") redirect("/map");
  // 인증은 됐지만 프로필이 없다 → 남은 절차로 보낸다(로그인 화면이 아니다).
  if (current.status === "no_profile") redirect("/signup/profile");

  // 미인증: 온보딩을 봤는지가 localStorage 에 있어 브라우저만 판단할 수 있다.
  return <EntryRedirect />;
}
