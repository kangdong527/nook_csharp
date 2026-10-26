"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { NookLogo } from "@/components/NookLogo";

const ONBOARDING_HIDE_KEY = "nook.onboarding.hide.v2";

/**
 * 미인증 방문자의 첫 목적지를 정한다 — `/onboarding` 또는 `/login`.
 *
 * 🚧 **이 부분만 클라이언트에 남았다.** 온보딩 숨김 여부가 localStorage 에
 *    있어서 서버가 알 수 없기 때문이다. 인증 판정은 이미 page.tsx(서버)가
 *    끝냈고, 여기 남은 것은 "이 기기에서 온보딩을 봤는가" 하나다.
 *
 *    서버로 옮기려면 쿠키로 바꿔야 한다(같은 값을 서버도 읽을 수 있다).
 *    지금 바꾸지 않는 이유: 기존 사용자의 플래그가 초기화되어 온보딩을
 *    다시 보게 되고, "기기별 상태"라는 성격상 인증과 함께 다룰 값이 아니다.
 *    → Phase 3 기능 사이클에서 판단한다.
 */
export function EntryRedirect() {
  const router = useRouter();

  useEffect(() => {
    const hidden = window.localStorage.getItem(ONBOARDING_HIDE_KEY) === "1";
    router.replace(hidden ? "/login" : "/onboarding");
  }, [router]);

  return (
    <div className="flex flex-1 items-center justify-center">
      <NookLogo className="h-11 opacity-90" />
    </div>
  );
}
