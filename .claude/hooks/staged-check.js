// PreToolUse(Bash) 훅 — `git commit`이 들어간 명령 직전에 "이미 스테이징된 파일"을 경고로 보여 준다(막지 않음).
// 배경(2026-11-07): 앞서 스테이징해 둔 이동·삭제가 다른 커밋(lint 수정)에 섞여 들어간 사고.
// 이 훅은 명령 실행 "전"에 돈다 → 같은 명령 안의 `git add`보다 먼저 남아 있던 것이 보인다(사고 원인이 바로 그것).
const { execSync } = require("child_process");

let input = "";
process.stdin.on("data", (d) => (input += d));
process.stdin.on("end", () => {
  let command = "";
  try {
    command = JSON.parse(input).tool_input?.command ?? "";
  } catch {
    return;
  }
  if (!/\bgit\s+commit\b/.test(command)) return;

  let staged;
  try {
    staged = execSync("git diff --cached --stat", { encoding: "utf8", cwd: process.env.CLAUDE_PROJECT_DIR || process.cwd() }).trim();
  } catch {
    return; // git 저장소가 아니면 조용히 통과
  }
  const msg = staged
    ? `⚠ git commit 전 이미 스테이징된 파일(이 명령의 git add 이전):\n${staged}\n의도한 커밋 범위가 맞는지 확인. 범위를 고정하려면 git commit -- <경로>`
    : "git commit 전 스테이징된 파일 없음(이 명령의 git add 가 범위를 정함)";
  process.stdout.write(
    JSON.stringify({ systemMessage: msg, hookSpecificOutput: { hookEventName: "PreToolUse", additionalContext: msg } })
  );
});
