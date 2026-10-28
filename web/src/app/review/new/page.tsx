// 서버 컴포넌트 — 가드 + 프로필 전달.
// 리뷰 작성은 user.id 를 그대로 review.user_id 로 쓴다. 그 id 가 이제
// auth.users.id 와 같으므로 Phase 1-6 에서 `user_id = auth.uid()` 정책이 성립한다.

import { requireProfile } from "@/lib/guard.server";
import { NewReviewScreen } from "./NewReviewScreen";

export default async function NewReviewPage() {
  const user = await requireProfile();
  return <NewReviewScreen user={user} />;
}
