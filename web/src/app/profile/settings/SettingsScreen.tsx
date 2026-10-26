"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { signOut } from "@/lib/data/auth";
import { deleteAccount } from "@/lib/store";
import type { User } from "@/lib/types";
import { useToast } from "@/components/Toast";
import { Spinner } from "@/components/Spinner";
import { ChevronLeftIcon } from "@/components/icons";

export function SettingsScreen({ user }: { user: User }) {
  const router = useRouter();
  const toast = useToast();
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [working, setWorking] = useState(false);

  // 로그아웃은 이제 localStorage 삭제가 아니라 **쿠키 세션 종료**다.
  // router.refresh() 로 서버 컴포넌트 캐시까지 비워야 뒤로가기로 이전
  // 화면의 서버 렌더 결과가 되살아나지 않는다.
  const logout = async () => {
    setWorking(true);
    try {
      await signOut();
      router.replace("/login");
      router.refresh();
    } catch {
      toast("일시적인 오류가 발생했어요. 잠시 후 다시 시도해주세요");
      setWorking(false);
    }
  };

  const withdraw = async () => {
    setWorking(true);
    try {
      await deleteAccount(user.id);
      // ⚠️ deleteAccount 는 public.users 만 지운다 → auth.users 계정은 남는다.
      //    세션까지 끊지 않으면 "탈퇴했는데 로그인 상태"이고, 가드가
      //    /signup/profile 로 보내 프로필을 다시 만들라고 요구한다.
      //    auth.users 삭제(admin.deleteUser)는 Phase 1-8 과제다.
      await signOut();
      router.replace("/login");
      router.refresh();
    } catch {
      toast("일시적인 오류가 발생했어요. 잠시 후 다시 시도해주세요.");
      setWorking(false);
    }
  };

  return (
    <div className="flex flex-1 flex-col">
      <header className="flex items-center gap-1 border-b border-line px-2 py-3">
        <button
          onClick={() => router.back()}
          className="flex h-10 w-10 items-center justify-center text-ink"
          aria-label="뒤로"
        >
          <ChevronLeftIcon className="h-6 w-6" />
        </button>
        <h1 className="text-base font-semibold">설정</h1>
      </header>

      <div className="flex-1">
        <RowDisabled label="서비스 약관 및 방침" />
        <RowDisabled label="신고 / 문의하기" />
        <Row label="버전 정보" right={<span className="text-sm text-sub">0.1.0</span>} />
        <Row label="로그아웃" onClick={working ? undefined : logout} />
        <Row label="회원탈퇴" danger onClick={() => setConfirmOpen(true)} />
      </div>

      {confirmOpen && (
        <div className="fixed inset-0 z-50 mx-auto flex max-w-[420px] items-center justify-center px-8">
          <button
            className="absolute inset-0 bg-black/45"
            onClick={() => !working && setConfirmOpen(false)}
            aria-label="닫기"
          />
          <div className="animate-pop-in relative w-full rounded-3xl bg-white p-6 text-center">
            <p className="text-base font-bold">정말 탈퇴할까요?</p>
            <p className="mt-2 text-sm leading-relaxed text-sub">
              탈퇴 시 모든 데이터가 삭제되며 복구할 수 없어요
            </p>
            <div className="mt-6 flex flex-col gap-2">
              <button
                onClick={withdraw}
                disabled={working}
                className="flex w-full items-center justify-center gap-2 rounded-2xl bg-like py-3.5 text-base font-semibold text-white disabled:opacity-60"
              >
                {working && <Spinner />}
                탈퇴하기
              </button>
              <button
                onClick={() => setConfirmOpen(false)}
                disabled={working}
                className="w-full rounded-2xl border border-line py-3.5 text-base font-semibold"
              >
                취소
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function Row({
  label,
  right,
  danger = false,
  onClick,
}: {
  label: string;
  right?: React.ReactNode;
  danger?: boolean;
  onClick?: () => void;
}) {
  return (
    <button
      onClick={onClick}
      disabled={!onClick}
      className="flex w-full items-center justify-between border-b border-line px-5 py-4 text-left disabled:cursor-default"
    >
      <span className={`text-[15px] ${danger ? "text-like" : "text-ink"}`}>{label}</span>
      {right ?? (onClick && <span className="text-sub">›</span>)}
    </button>
  );
}

function RowDisabled({ label }: { label: string }) {
  return (
    <div className="flex w-full items-center justify-between border-b border-line px-5 py-4">
      <span className="text-[15px] text-disabled">{label}</span>
      <span className="text-disabled">›</span>
    </div>
  );
}
