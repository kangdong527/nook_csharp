"use client";

// =========================================================
// 메일 링크가 도착하는 자리 — http://localhost:3300/auth/magic-link#token=…
//
// 왜 route handler 가 아니라 클라이언트 페이지인가:
//   토큰은 URL 프래그먼트(#)에 있다(D5 — 서버 접근 로그·프록시 로그에 남지 않게).
//   프래그먼트는 브라우저가 서버로 보내지 않으므로, 브라우저 JS 가 한 번 읽어
//   같은 출처의 /api/auth/consume 로 넘긴다. 쿠키는 그 route handler 가 심는다.
// =========================================================

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { Spinner } from "@/components/Spinner";

export default function MagicLinkPage() {
  const router = useRouter();
  const [failed, setFailed] = useState(false);
  // React 18 개발 모드의 effect 두 번 실행 방지 — 같은 토큰을 두 번 보내면 두 번째는 InvalidLink 다.
  const sent = useRef(false);

  useEffect(() => {
    if (sent.current) return;
    sent.current = true;

    const token = new URLSearchParams(window.location.hash.slice(1)).get("token");
    // 주소창·방문 기록에서 토큰을 지운다(뒤로 가기·화면 공유로 새지 않게).
    window.history.replaceState(null, "", window.location.pathname);

    // 토큰이 없으면 요청 없이 실패 — setState 는 effect 본문이 아니라 비동기 콜백에서(react-hooks/set-state-in-effect)
    if (!token) {
      void Promise.resolve().then(() => setFailed(true));
      return;
    }

    fetch("/api/auth/consume", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ token }),
    })
      .then(async (res) => {
        if (!res.ok) {
          setFailed(true);
          return;
        }
        const { next } = (await res.json()) as { next: string };
        router.replace(next);
      })
      .catch(() => setFailed(true));
  }, [router]);

  if (!failed) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner className="text-key" />
      </div>
    );
  }

  // 없음·만료·이미 사용을 구분하지 않는다(D5-3 — 구분하면 토큰 존재 여부가 드러난다).
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-4 px-6 text-center">
      <p className="text-ink">로그인 링크가 유효하지 않아요.</p>
      <p className="text-sm text-sub">링크는 15분 동안 한 번만 쓸 수 있어요. 새 링크를 받아 주세요.</p>
      <Link href="/login" className="mt-2 rounded-full bg-key px-6 py-3 text-sm font-medium text-white">
        로그인 화면으로
      </Link>
    </div>
  );
}
