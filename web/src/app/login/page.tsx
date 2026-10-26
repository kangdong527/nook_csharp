"use client";

import Link from "next/link";
import { Suspense } from "react";
import { useSearchParams } from "next/navigation";
import { NookLogo } from "@/components/NookLogo";

/**
 * /auth/confirm 이 실패하면 ?error=link_invalid 로 여기 보낸다.
 * 라우트는 reason 만 넘기고, 문구는 화면이 만든다(계층 규칙).
 *
 * useSearchParams 를 쓰는 부분만 Suspense 로 감싼다 —
 * 감싸지 않으면 정적 렌더링 대상 페이지에서 빌드 경고가 난다.
 */
function LinkErrorNotice() {
  const error = useSearchParams().get("error");
  if (error !== "link_invalid") return null;
  return (
    <p className="mb-3 rounded-xl bg-field px-4 py-3 text-center text-xs leading-relaxed text-sub">
      링크가 만료되었거나 이미 사용되었어요.
      <br />
      다시 시도해 주세요.
    </p>
  );
}

export default function LoginEntryPage() {
  return (
    <div className="flex flex-1 flex-col px-6 pb-10 pt-6">
      {/* status bar 여백 */}
      <div className="flex flex-1 flex-col items-center justify-center">
        <NookLogo className="h-12" />
        <p className="mt-4 text-sm text-sub">가고 싶은 곳, 다녀온 곳을 지도에 기록해요</p>
      </div>

      <Suspense fallback={null}>
        <LinkErrorNotice />
      </Suspense>

      <div className="flex flex-col gap-3">
        <Link
          href="/signup/email"
          className="flex w-full items-center justify-center rounded-2xl bg-key py-4 text-base font-semibold text-white transition active:scale-[0.99]"
        >
          이메일로 시작
        </Link>
        <Link
          href="/login/email"
          className="flex w-full items-center justify-center rounded-2xl border border-line bg-white py-4 text-base font-semibold text-ink transition active:scale-[0.99]"
        >
          로그인
        </Link>
      </div>
    </div>
  );
}
