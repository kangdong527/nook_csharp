// 서버 컴포넌트 — 가드만 하고 폼은 클라이언트 잎(ProfileSetupForm)에 맡긴다.
//
// 이 화면은 요구 조건이 다른 화면과 **반대**다: 인증은 되어 있어야 하지만
// 프로필은 아직 없어야 한다. requireProfileSetup() 이 그 판정을 한다.
//
// 📌 이메일이 더 이상 쿼리 파라미터(`?email=`)가 아니다. 매직링크 인증 후
//    세션이 있으므로 서버가 세션에서 읽는다 — URL 을 고쳐 남의 이메일로
//    프로필을 만드는 경로가 사라진다.

import { requireProfileSetup } from "@/lib/guard.server";
import { ProfileSetupForm } from "./ProfileSetupForm";

export default async function SignupProfilePage() {
  const { email } = await requireProfileSetup();
  return <ProfileSetupForm email={email} />;
}
