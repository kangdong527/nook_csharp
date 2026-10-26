"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { getReview, updateReview } from "@/lib/store";
import type { Review, User } from "@/lib/types";
import { useToast } from "@/components/Toast";
import { FullSpinner, Spinner } from "@/components/Spinner";
import { ReviewEditor } from "@/components/ReviewEditor";

export function EditReviewScreen({
  user,
  reviewId,
}: {
  user: User;
  reviewId: string;
}) {
  const router = useRouter();
  const toast = useToast();

  const [review, setReview] = useState<Review | null>(null);
  const [loading, setLoading] = useState(true);

  const userId = user.id;

  useEffect(() => {
    getReview(reviewId, userId)
      .then((r) => {
        if (!r) {
          toast("기록을 찾을 수 없어요.");
          router.replace("/profile");
          return;
        }
        // 소유권 확인. ⚠️ 클라이언트 검사이므로 최종 방어선이 아니다 —
        // 실제 차단은 Phase 1-6 의 RLS(`user_id = auth.uid()`)가 한다.
        if (r.userId !== userId) {
          toast("내 기록만 수정할 수 있어요.");
          router.replace("/profile");
          return;
        }
        setReview(r);
      })
      .finally(() => setLoading(false));
  }, [userId, reviewId, router, toast]);

  if (loading) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner className="text-key" />
      </div>
    );
  }

  if (!review) return <FullSpinner />;

  return (
    <ReviewEditor
      mode="edit"
      user={user}
      placeName={review.placeName}
      initialContent={review.content}
      initialImages={review.images}
      onSubmit={async (content, images) => {
        await updateReview({ reviewId: review.id, content, images });
        toast("기록을 수정했어요.");
        router.back();
      }}
    />
  );
}
