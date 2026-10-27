"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, getReview, updateReview, type EditableReview } from "@/lib/store";
import type { User } from "@/lib/types";
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

  const [review, setReview] = useState<EditableReview | null>(null);
  const [loading, setLoading] = useState(true);

  // 본인 확인은 서버가 한다(RV-04 — 작성자만, 남의 리뷰 403). 원본은 클라이언트가 userId 를 비교했다.
  useEffect(() => {
    getReview(reviewId)
      .then((r) => {
        if (r === "not_found") {
          toast("기록을 찾을 수 없어요.");
          router.replace("/profile");
          return;
        }
        if (r === "forbidden") {
          toast("내 기록만 수정할 수 있어요.");
          router.replace("/profile");
          return;
        }
        setReview(r);
      })
      .finally(() => setLoading(false));
  }, [reviewId, router, toast]);

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
        try {
          await updateReview({
            reviewId: review.id,
            version: review.version,
            content,
            keepImageIds: images.flatMap((i) => (i.kind === "existing" ? [i.id] : [])),
            files: images.flatMap((i) => (i.kind === "new" ? [i.file] : [])),
          });
        } catch (e) {
          // 화면을 연 사이 다른 기기에서 먼저 수정했다(RV-02 6c) — 최신 내용을 다시 열게 한다.
          if (e instanceof ApiError && e.title === "Review.EditConflict") {
            toast("다른 곳에서 먼저 수정된 기록이에요. 다시 불러올게요.");
            window.location.reload();
            return;
          }
          if (e instanceof ApiError && e.title === "Review.NotFound") {
            toast("기록을 찾을 수 없어요.");
            router.replace("/profile");
            return;
          }
          throw e;
        }
        toast("기록을 수정했어요.");
        router.back();
      }}
    />
  );
}
