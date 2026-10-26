"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { sendMagicLink, type SendLinkFail } from "@/lib/data/auth";
import { exhaustive } from "@/lib/result";
import { isValidEmail } from "@/lib/types";
import { useToast } from "./Toast";
import { Spinner } from "./Spinner";
import { ChevronLeftIcon } from "./icons";

type Step = "input" | "sent";

/**
 * reason → 문구 매핑. **화면 계층의 책임**이다(CLAUDE.md 계층 규칙).
 * 데이터 계층(lib/data/auth.ts)은 reason 만 넘기고 문구를 만들지 않는다.
 *
 * 📌 default 의 exhaustive(reason) 가 이 함수의 핵심이다.
 *    SendLinkFail 에 reason 이 하나 추가되면 여기서 **컴파일 에러**가 난다.
 *    이전 코드는 삼항 연산자 체인이어서 케이스를 빼먹어도 통과했고,
 *    그 결과는 "아무 안내도 뜨지 않는 무반응 버그"였다.
 */
function messageOf(reason: SendLinkFail): string {
  switch (reason) {
    case "not_registered":
      return "가입되지 않은 이메일이에요. 회원가입을 진행해 주세요.";
    case "already_registered":
      return "이미 가입된 이메일이에요. 로그인해 주세요.";
    case "rate_limited":
      return "메일을 너무 자주 보냈어요. 잠시 후 다시 시도해 주세요.";
    default:
      return exhaustive(reason);
  }
}

export function EmailAuthFlow({ mode }: { mode: "login" | "signup" }) {
  const router = useRouter();
  const toast = useToast();
  const [step, setStep] = useState<Step>("input");
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [cooldown, setCooldown] = useState(0);
  const timerRef = useRef<number | null>(null);

  const valid = isValidEmail(email);
  const showEmailError = email.trim().length > 0 && !valid;

  useEffect(() => {
    return () => {
      if (timerRef.current) window.clearInterval(timerRef.current);
    };
  }, []);

  const startCooldown = () => {
    setCooldown(60);
    if (timerRef.current) window.clearInterval(timerRef.current);
    timerRef.current = window.setInterval(() => {
      setCooldown((c) => {
        if (c <= 1) {
          if (timerRef.current) window.clearInterval(timerRef.current);
          return 0;
        }
        return c - 1;
      });
    }, 1000);
  };

  // 매직링크 발송. 가입 여부 판정은 데이터 계층이 한다(login 모드는 서버가 판정).
  const sendMail = async () => {
    if (!valid) {
      toast("올바른 이메일 형식으로 입력해 주세요.");
      return;
    }
    setLoading(true);
    try {
      const res = await sendMagicLink(email, mode);
      if (!res.ok) {
        toast(messageOf(res.reason));
        return;
      }
      setStep("sent");
      startCooldown();
      toast("인증 메일을 보냈어요. 메일함을 확인해 주세요.");
    } catch (e) {
      // 예상 불가능한 실패(네트워크 단절 등). 규칙상 throw 는 error.tsx + Sentry 로
      // 가야 하지만, 폼 제출 중 일시적 네트워크 오류로 화면 전체를 날리는 것은
      // UX 손실이 크다. 대신 삼키지 않도록 콘솔에 남긴다.
      // → Phase 2-11 에서 Sentry.captureException 으로 교체한다.
      console.error("[EmailAuthFlow] sendMagicLink 실패", e);
      toast("메일 발송에 실패했어요. 잠시 후 다시 시도해 주세요.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex flex-1 flex-col">
      <header className="flex items-center px-2 py-3">
        <button
          onClick={() => (step === "sent" ? setStep("input") : router.back())}
          className="flex h-10 w-10 items-center justify-center text-ink"
          aria-label="뒤로"
        >
          <ChevronLeftIcon className="h-6 w-6" />
        </button>
      </header>

      {step === "input" ? (
        <div className="flex flex-1 flex-col px-6 pb-8">
          <h1 className="mt-2 text-2xl font-bold">이메일을 입력해주세요</h1>
          <div className="mt-8">
            <label className="text-xs font-medium text-sub">이메일</label>
            <input
              type="email"
              inputMode="email"
              autoFocus
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && valid && !loading && sendMail()}
              placeholder="jam@gmail.com"
              className={`mt-2 w-full border-b bg-transparent pb-2 text-lg outline-none placeholder:text-disabled ${
                showEmailError ? "border-like focus:border-like" : "border-line focus:border-key"
              }`}
            />
            {showEmailError && (
              <p className="mt-2 text-xs text-like">올바른 이메일 형식으로 입력해 주세요.</p>
            )}
          </div>

          <div className="mt-auto">
            <button
              onClick={sendMail}
              disabled={!valid || loading}
              className="flex w-full items-center justify-center gap-2 rounded-2xl bg-key py-4 text-base font-semibold text-white transition active:scale-[0.99] disabled:bg-disabled"
            >
              {loading && <Spinner />}
              인증 메일 전송
            </button>
          </div>
        </div>
      ) : (
        <div className="flex flex-1 flex-col items-center px-6 pb-8 text-center">
          <div className="mt-10 flex h-14 w-14 items-center justify-center rounded-full bg-field">
            <span className="text-2xl">📩</span>
          </div>
          <p className="mt-6 text-lg font-bold">{email}</p>
          <p className="mt-3 text-sm leading-relaxed text-sub">
            인증 메일을 보냈어요.
            <br />
            메일함에서 인증하기 버튼을 눌러 주세요.
          </p>
          <p className="mt-1 text-xs text-disabled">
            메일이 보이지 않으면 스팸함도 확인해 주세요.
          </p>

          {/*
            "인증 완료 (데모)" 버튼을 제거했다.
            이제 인증은 **메일의 링크**가 /auth/confirm 에서 완료한다.
            버튼을 남겨두면 실제 인증 없이 다음 화면으로 넘어가는 우회로가 된다.
          */}
          <div className="mt-auto w-full">
            <button
              onClick={sendMail}
              disabled={cooldown > 0 || loading}
              className="mt-3 text-sm font-medium text-sub disabled:text-disabled"
            >
              {cooldown > 0 ? `재전송 ${cooldown}s` : "인증 메일 재전송"}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
