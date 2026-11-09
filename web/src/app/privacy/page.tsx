// =========================================================
// /privacy — 개인정보 처리방침 (R-US-06, DEP 7단계)
//
// 원문 SSoT 는 docs/design/privacy-policy.md 다. 개정하면 세 곳을 함께 바꾼다:
//   그 문서 · 이 페이지(VERSION·본문) · C# PrivacyPolicy.CurrentVersion
// 로그인 없이 열린다(proxy 보호 경로 아님) — 가입 폼의 "보기" 링크가 새 탭으로 연다.
// ⚠️ 초안: PM 확정 전. [확정 필요] 표시는 공개 전에 채운다.
// =========================================================

import type { Metadata } from "next";

export const metadata: Metadata = { title: "개인정보 처리방침 · nook" };

const VERSION = "2026-11-09-draft"; // = PrivacyPolicy.CurrentVersion (C#)

const COLLECTED: ReadonlyArray<[string, string, string, string]> = [
  ["필수", "이메일", "로그인 링크 발송, 계정 식별", "탈퇴 시 삭제"],
  ["필수", "닉네임", "리뷰·그룹에서 회원 표시", "탈퇴 시 삭제"],
  ["필수", "동의 기록(시각·방침 버전)", "동의 증명", "탈퇴 시 삭제"],
  ["선택", "성별, 생년월일", "[확정 필요]", "탈퇴 시 삭제"],
  ["이용 중", "프로필 사진", "회원 표시", "탈퇴 시 삭제"],
  ["이용 중", "저장한 장소, 그룹 참여", "지도·그룹 기능", "탈퇴 시 삭제"],
  ["이용 중", "리뷰(글·사진), 좋아요", "장소 기록·공유", "탈퇴 후에도 남음(작성자 정보 없이)"],
  ["자동", "접속 IP, 요청 경로·시각", "보안, 과도한 요청 차단", "30일"],
];

export default function PrivacyPage() {
  return (
    <main className="mx-auto flex w-full max-w-[420px] flex-col gap-6 px-6 py-8 text-[15px] leading-relaxed text-ink">
      <header className="flex flex-col gap-2">
        <h1 className="text-xl font-bold">개인정보 처리방침</h1>
        <p className="text-xs text-sub">버전 {VERSION} · 시행일 [확정 필요]</p>
        <p className="rounded-xl bg-field px-3 py-2 text-xs text-sub">
          초안입니다. 서비스 공개 전에 확정됩니다.
        </p>
      </header>

      <Section title="1. 수집하는 개인정보와 목적">
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-left text-xs">
            <thead>
              <tr className="border-b border-line text-sub">
                <th className="py-2 pr-2 font-medium">구분</th>
                <th className="py-2 pr-2 font-medium">항목</th>
                <th className="py-2 pr-2 font-medium">목적</th>
                <th className="py-2 font-medium">보관</th>
              </tr>
            </thead>
            <tbody>
              {COLLECTED.map(([kind, item, purpose, keep]) => (
                <tr key={item} className="border-b border-line align-top">
                  <td className="py-2 pr-2 whitespace-nowrap">{kind}</td>
                  <td className="py-2 pr-2">{item}</td>
                  <td className="py-2 pr-2">{purpose}</td>
                  <td className="py-2">{keep}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <p>선택 항목에 동의하지 않아도 가입과 모든 기능을 이용할 수 있어요.</p>
      </Section>

      <Section title="2. 만 14세 미만">
        <p>
          만 14세 미만은 가입할 수 없어요. 가입할 때 만 14세 이상임을 확인받고, 생년월일을 입력했다면 만 14세 미만인 경우 가입을
          거절해요.
        </p>
      </Section>

      <Section title="3. 보관 기간과 파기">
        <ul className="list-disc space-y-1 pl-5">
          <li>탈퇴하면 위 표에서 &ldquo;탈퇴 시 삭제&rdquo;인 정보를 바로 삭제해요.</li>
          <li>
            리뷰 글·사진과 좋아요는 다른 회원이 보는 장소 기록이라 남고, 작성자는 &ldquo;탈퇴한 사용자&rdquo;로 표시돼요. 지우고
            싶은 리뷰는 탈퇴 전에 직접 삭제해 주세요.
          </li>
          <li>장애 복구용 데이터베이스 백업에는 탈퇴 후 최대 약 5주 동안 남아 있다가 자동으로 삭제돼요.</li>
          <li>접속 기록은 30일 뒤 삭제해요.</li>
        </ul>
      </Section>

      <Section title="4. 처리 위탁">
        <p>
          Amazon Web Services(서울 리전)에 서버·데이터베이스 운영, 사진 저장, 로그인 메일 발송을 맡겨요. [확정 필요: 위탁 고지
          문구]
        </p>
      </Section>

      <Section title="5. 외부 서비스">
        <p>
          지도와 장소 검색은 브라우저가 카카오 지도에 직접 요청해요. 이때 검색어, 지도 위치, 접속 IP가 카카오에 전달될 수 있어요.
          글꼴은 jsDelivr에서 내려받아요.
        </p>
      </Section>

      <Section title="6. 이용자의 권리">
        <ul className="list-disc space-y-1 pl-5">
          <li>닉네임과 프로필 사진은 프로필 화면에서 언제든 바꿀 수 있어요.</li>
          <li>설정의 회원탈퇴에서 언제든 탈퇴할 수 있어요.</li>
          <li>열람·정정·삭제·처리 정지 요청: [확정 필요]</li>
        </ul>
      </Section>

      <Section title="7. 개인정보 보호책임자">
        <p>[확정 필요]</p>
      </Section>
    </main>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="flex flex-col gap-2">
      <h2 className="text-base font-semibold">{title}</h2>
      {children}
    </section>
  );
}
