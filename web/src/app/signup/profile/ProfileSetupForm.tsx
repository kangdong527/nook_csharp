"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { createProfile, type CreateProfileFail } from "@/lib/data/user";
import { exhaustive } from "@/lib/result";
import {
  GENDER_LABEL,
  isValidNickname,
  nicknameError,
  formatBirthInput,
  birthError,
  type Gender,
} from "@/lib/types";
import { useToast } from "@/components/Toast";
import { Spinner } from "@/components/Spinner";
import { Avatar } from "@/components/Avatar";
import { ChevronLeftIcon } from "@/components/icons";

const GENDERS: Gender[] = ["female", "male", "none"];
const ONBOARDING_HIDE_KEY = "nook.onboarding.hide.v2";

/**
 * reason → 문구 매핑. 데이터 계층은 reason 만 넘긴다(CLAUDE.md 계층 규칙).
 * default 의 exhaustive() 가 CreateProfileFail 추가 시 컴파일 에러를 낸다.
 */
function messageOf(reason: CreateProfileFail): string {
  switch (reason) {
    case "no_session":
      return "인증 정보가 만료됐어요. 처음부터 다시 진행해 주세요.";
    case "invalid_input":
      return "입력값을 다시 확인해 주세요.";
    case "nickname_taken":
      return "이미 사용 중인 닉네임이에요.";
    default:
      return exhaustive(reason);
  }
}

/**
 * `email` 은 폼 입력이 아니라 **서버가 가입 대기 쿠키에서 읽어 내려준 값**이다(표시용).
 * 저장되는 이메일은 매직 링크의 것이다 — 폼이 준 값을 신뢰하지 않는다.
 */
export function ProfileSetupForm({ email }: { email: string }) {
  const router = useRouter();
  const toast = useToast();

  const [nickname, setNickname] = useState("");
  const [gender, setGender] = useState<Gender | null>(null);
  const [birth, setBirth] = useState("");
  const [genderOpen, setGenderOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const nicknameOk = isValidNickname(nickname);
  const nickError = nicknameError(nickname);
  const birthErr = birthError(birth);
  const canSubmit = nicknameOk && gender !== null && !birthErr && !submitting;

  const submit = async () => {
    if (!nicknameOk) {
      toast("닉네임은 2~12자까지 입력할 수 있어요.");
      return;
    }
    if (!gender) {
      toast("성별을 선택해 주세요.");
      return;
    }
    if (birthErr) {
      toast(birthErr);
      return;
    }
    setSubmitting(true);
    try {
      const res = await createProfile({
        nickname,
        gender,
        birth: birth.trim() || null,
      });
      if (!res.ok) {
        toast(messageOf(res.reason));
        // 세션이 없으면 폼에 머물러 있어도 할 수 있는 게 없다.
        if (res.reason === "no_session") router.replace("/signup/email");
        return;
      }
      // 회원가입 직후에는 온보딩을 한번 보여준다.
      window.localStorage.removeItem(ONBOARDING_HIDE_KEY);
      router.replace("/onboarding?from=signup");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="flex flex-1 flex-col">
      <header className="flex items-center px-2 py-3">
        <button
          onClick={() => router.back()}
          className="flex h-10 w-10 items-center justify-center text-ink"
          aria-label="뒤로"
        >
          <ChevronLeftIcon className="h-6 w-6" />
        </button>
      </header>

      <div className="flex flex-1 flex-col px-6 pb-8">
        {/* 프로필 사진 — 가입 땐 기본 이미지. 업로드는 가입 직후 프로필 수정(PF-01)에서(FE-2 사용자 결정) */}
        <div className="flex flex-col items-center pt-2">
          <div className="relative">
            <Avatar url={null} size={88} />
          </div>
          <p className="mt-3 text-xs text-sub">{email}</p>
        </div>

        {/* 닉네임 */}
        <div className="mt-8">
          <label className="text-xs font-medium text-sub">닉네임</label>
          <input
            autoFocus
            value={nickname}
            onChange={(e) => setNickname(e.target.value.slice(0, 12))}
            placeholder="닉네임을 입력해 주세요 (2~12자)"
            className={`mt-2 w-full border-b bg-transparent pb-2 text-lg outline-none placeholder:text-disabled ${
              nickError ? "border-like focus:border-like" : "border-line focus:border-key"
            }`}
          />
          {nickError && <p className="mt-2 text-xs text-like">{nickError}</p>}
        </div>

        {/* 성별 */}
        <div className="mt-7">
          <label className="text-xs font-medium text-sub">성별</label>
          <button
            onClick={() => setGenderOpen(true)}
            className={`mt-2 flex w-full items-center justify-between border-b border-line pb-2 text-lg ${
              gender ? "text-ink" : "text-disabled"
            }`}
          >
            {gender ? GENDER_LABEL[gender] : "선택"}
            <span className="text-sub">▾</span>
          </button>
        </div>

        {/* 생년월일 (선택) */}
        <div className="mt-7">
          <label className="text-xs font-medium text-sub">생년월일 (선택)</label>
          <input
            value={birth}
            inputMode="numeric"
            onChange={(e) => setBirth(formatBirthInput(e.target.value))}
            placeholder="YY-MM-DD"
            className={`mt-2 w-full border-b bg-transparent pb-2 text-lg outline-none placeholder:text-disabled ${
              birthErr ? "border-like focus:border-like" : "border-line focus:border-key"
            }`}
          />
          {birthErr && <p className="mt-2 text-xs text-like">{birthErr}</p>}
        </div>

        <div className="mt-auto pt-8">
          <button
            onClick={submit}
            disabled={!canSubmit}
            className="flex w-full items-center justify-center gap-2 rounded-2xl bg-key py-4 text-base font-semibold text-white transition active:scale-[0.99] disabled:bg-disabled"
          >
            {submitting && <Spinner />}
            가입하기
          </button>
        </div>
      </div>

      {/* 성별 선택 바텀시트 */}
      {genderOpen && (
        <div className="fixed inset-0 z-50 mx-auto flex max-w-[420px] flex-col justify-end">
          <button
            className="absolute inset-0 bg-black/40"
            onClick={() => setGenderOpen(false)}
            aria-label="닫기"
          />
          <div className="animate-sheet-up relative rounded-t-3xl bg-white p-2 pb-[max(env(safe-area-inset-bottom),12px)]">
            {GENDERS.map((g) => (
              <button
                key={g}
                onClick={() => {
                  setGender(g);
                  setGenderOpen(false);
                }}
                className="flex w-full items-center justify-center py-4 text-lg font-medium hover:bg-field"
              >
                {GENDER_LABEL[g]}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
