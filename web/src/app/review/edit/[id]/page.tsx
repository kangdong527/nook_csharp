// 서버 컴포넌트 — 가드 + 프로필 전달.
// reviewId 는 서버가 params 에서 읽어 내려준다(클라이언트의 useParams 대체).

import { requireProfile } from "@/lib/guard.server";
import { EditReviewScreen } from "./EditReviewScreen";

export default async function EditReviewPage({
  params,
}: {
  params: { id: string };
}) {
  const user = await requireProfile();
  return <EditReviewScreen user={user} reviewId={params.id} />;
}
