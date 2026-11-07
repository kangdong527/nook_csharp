// 온보딩은 **공개 화면**이다(proxy 보호 대상이 아니다).
// 서버가 하는 일은 하나뿐: 이미 로그인한 사용자를 지도로 돌려보낸다.
//
// `?from=signup` 은 가입 직후 1회 노출 경로이므로 그때는 돌려보내지 않는다.

import { redirect } from "next/navigation";
import { getCurrentUser } from "@/lib/data/user.server";
import { OnboardingSlides } from "./OnboardingSlides";

export default async function OnboardingPage({
  searchParams,
}: {
  searchParams: Promise<{ from?: string }>; // Next 15+: Promise
}) {
  const fromSignup = (await searchParams).from === "signup";

  if (!fromSignup) {
    const current = await getCurrentUser();
    if (current.status === "ready") redirect("/map");
  }

  return <OnboardingSlides fromSignup={fromSignup} />;
}
