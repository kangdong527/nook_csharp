// POST /api/auth/magic-link — 로그인·가입 링크 메일 요청을 C# AU-01 로 중계한다.
// 가입 여부와 무관하게 같은 응답(202)이다(D5) — 화면은 "메일을 보냈어요"만 안내한다(FE-2 결정 (a)).

import { NextResponse } from "next/server";
import { apiFetch, readProblem } from "@/lib/api/server";

export async function POST(request: Request) {
  const { email } = (await request.json()) as { email?: string };

  const res = await apiFetch("/auth/magic-link", { method: "POST", body: JSON.stringify({ email }) });
  if (res.ok) return new NextResponse(null, { status: 202 });

  const problem = await readProblem(res);
  return NextResponse.json(problem, { status: problem.status });
}
