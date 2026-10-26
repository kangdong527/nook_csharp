"use client";

// =========================================================
// 🚧 dev 전용 진단 화면 — 프로덕션에서는 ./page.tsx 가 404 로 막는다
//
// 용도 ①: signInWithOtp 의 **원시 응답** 확인. sendMagicLink() 는 매핑되지 않은
//         에러를 throw 해 정보를 가리므로, 여기서는 직접 호출해 error.code 를 본다.
// 용도 ②: Phase 1-6 RLS 검증. 계정 2개의 세션을 SDK 로 만들어 격리를 확인한다
//         (CLAUDE.md 🚫 #3 — RLS 를 SQL Editor 에서 검증하지 않는다).
//         → 이 용도 때문에 **삭제하지 않는다.**
//
// 접근: http://localhost:3300/dev/auth-probe
// (middleware 의 PROTECTED_PREFIXES 에 /dev 가 없으므로 미인증으로도 열린다)
// =========================================================

import { useState } from "react";
import { createClient } from "@/lib/adapters/supabase/client";

type Dump = { label: string; body: string };

export function AuthProbe() {
  const [email, setEmail] = useState("");
  const [dumps, setDumps] = useState<Dump[]>([]);
  const [busy, setBusy] = useState(false);

  const push = (label: string, value: unknown) =>
    setDumps((prev) => [
      { label, body: JSON.stringify(value, null, 2) },
      ...prev,
    ]);

  // public.users 에 이메일이 있는지 — 가입 모드의 사전 조회와 같은 쿼리
  async function checkExists() {
    setBusy(true);
    try {
      const supabase = createClient();
      const res = await supabase
        .from("users")
        .select("id, email, nickname")
        .eq("email", email.trim().toLowerCase())
        .maybeSingle();
      push("public.users 조회", { data: res.data, error: res.error });
    } catch (e) {
      push("public.users 조회 — throw", String(e));
    } finally {
      setBusy(false);
    }
  }

  // 원시 signInWithOtp 응답. error.code / error.status 가 확인 대상이다.
  async function send(mode: "login" | "signup") {
    setBusy(true);
    try {
      const supabase = createClient();
      const { data, error } = await supabase.auth.signInWithOtp({
        email: email.trim().toLowerCase(),
        options: {
          shouldCreateUser: mode === "signup",
          emailRedirectTo:
            mode === "signup"
              ? `${window.location.origin}/signup/profile`
              : `${window.location.origin}/map`,
        },
      });
      push(`signInWithOtp (${mode}, shouldCreateUser=${mode === "signup"})`, {
        data,
        error: error
          ? {
              code: error.code,
              status: error.status,
              name: error.name,
              message: error.message,
            }
          : null,
      });
    } catch (e) {
      push(`signInWithOtp (${mode}) — throw`, String(e));
    } finally {
      setBusy(false);
    }
  }

  // 쿠키 세션이 실제로 심어졌는지. 링크 클릭 후 눌러 확인한다.
  async function whoAmI() {
    setBusy(true);
    try {
      const supabase = createClient();
      const { data, error } = await supabase.auth.getUser();
      push("auth.getUser()", {
        userId: data.user?.id ?? null,
        email: data.user?.email ?? null,
        error: error ? { code: error.code, message: error.message } : null,
      });
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex flex-1 flex-col gap-4 p-6 text-sm">
      <h1 className="text-base font-bold">🚧 auth probe (임시)</h1>
      <p className="text-neutral-500">
        Resend 무료 티어는 <b>가입한 본인 주소로만</b> 발송된다. 그 주소를 넣을 것.
      </p>

      <input
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        placeholder="you@example.com"
        className="rounded border border-neutral-300 px-3 py-2"
      />

      <div className="flex flex-wrap gap-2">
        <button
          onClick={checkExists}
          disabled={busy || !email}
          className="rounded bg-neutral-200 px-3 py-2 disabled:opacity-40"
        >
          public.users 존재 확인
        </button>
        <button
          onClick={() => send("login")}
          disabled={busy || !email}
          className="rounded bg-neutral-800 px-3 py-2 text-white disabled:opacity-40"
        >
          login 모드 발송
        </button>
        <button
          onClick={() => send("signup")}
          disabled={busy || !email}
          className="rounded bg-neutral-800 px-3 py-2 text-white disabled:opacity-40"
        >
          signup 모드 발송
        </button>
        <button
          onClick={whoAmI}
          disabled={busy}
          className="rounded bg-blue-600 px-3 py-2 text-white disabled:opacity-40"
        >
          현재 세션 확인
        </button>
      </div>

      {dumps.map((d, i) => (
        <pre
          key={i}
          className="overflow-x-auto rounded bg-neutral-100 p-3 text-xs"
        >
          <b>{d.label}</b>
          {"\n"}
          {d.body}
        </pre>
      ))}
    </div>
  );
}
