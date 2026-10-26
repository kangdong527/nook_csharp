"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { deleteReview, toggleLike } from "@/lib/store";
import { formatDate, type Review } from "@/lib/types";
import { useToast } from "./Toast";
import { Avatar } from "./Avatar";
import { HeartIcon, MoreIcon } from "./icons";

export function ReviewCard({
  review,
  myUserId,
  highlight = false,
  showPlaceName = false,
  onChanged,
}: {
  review: Review;
  myUserId: string | null;
  highlight?: boolean;
  showPlaceName?: boolean;
  onChanged?: () => void;
}) {
  const router = useRouter();
  const toast = useToast();
  const [liked, setLiked] = useState(review.likedByMe);
  const [likeCount, setLikeCount] = useState(review.likeCount);
  const [menuOpen, setMenuOpen] = useState(false);
  const [deleted, setDeleted] = useState(false);

  const isMine = myUserId === review.userId;
  if (deleted) return null;

  const onLike = async () => {
    if (!myUserId) return;
    const prevLiked = liked;
    const prevCount = likeCount;
    setLiked(!prevLiked);
    setLikeCount(prevCount + (prevLiked ? -1 : 1));
    try {
      await toggleLike(myUserId, review.id);
    } catch {
      setLiked(prevLiked);
      setLikeCount(prevCount);
      toast("일시적인 오류가 발생했어요. 잠시 후 다시 시도해 주세요");
    }
  };

  const onDelete = async () => {
    setMenuOpen(false);
    try {
      await deleteReview(review.id);
      setDeleted(true);
      toast("기록을 삭제했어요.");
      onChanged?.();
    } catch {
      toast("일시적인 오류가 발생했어요. 잠시 후 다시 시도해 주세요.");
    }
  };

  return (
    <article
      className={`rounded-2xl p-1 ${highlight ? "bg-field/70 ring-1 ring-line" : ""}`}
    >
      <div className="flex items-center gap-2.5">
        <Avatar url={review.authorAvatarUrl} size={36} />
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-semibold">{review.authorNickname}</p>
          <p className="text-xs text-sub">
            {showPlaceName && review.placeName ? `${review.placeName} · ` : ""}
            {formatDate(review.createdAt)}
          </p>
        </div>
        {isMine && (
          <div className="relative">
            <button
              onClick={() => setMenuOpen((v) => !v)}
              className="flex h-8 w-8 items-center justify-center text-sub"
              aria-label="메뉴"
            >
              <MoreIcon className="h-5 w-5" />
            </button>
            {menuOpen && (
              <>
                <button
                  className="fixed inset-0 z-10 cursor-default"
                  onClick={() => setMenuOpen(false)}
                  aria-label="닫기"
                />
                <div className="absolute right-0 top-9 z-20 w-28 overflow-hidden rounded-xl border border-line bg-white shadow-card">
                  <button
                    onClick={() => router.push(`/review/edit/${review.id}`)}
                    className="block w-full px-4 py-2.5 text-left text-sm hover:bg-field"
                  >
                    수정
                  </button>
                  <button
                    onClick={onDelete}
                    className="block w-full px-4 py-2.5 text-left text-sm text-like hover:bg-field"
                  >
                    삭제
                  </button>
                </div>
              </>
            )}
          </div>
        )}
      </div>

      <p className="mt-2 whitespace-pre-wrap break-words text-sm leading-relaxed">
        {review.content}
      </p>

      {review.images.length > 0 && (
        <div className="no-scrollbar mt-2.5 flex gap-2 overflow-x-auto">
          {review.images.map((url, i) => (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              key={i}
              src={url}
              alt={`사진 ${i + 1}`}
              className="h-40 w-40 shrink-0 rounded-xl object-cover"
            />
          ))}
        </div>
      )}

      <div className="mt-2 flex items-center gap-1">
        <button onClick={onLike} className="flex items-center gap-1 py-1" aria-label="좋아요">
          <HeartIcon filled={liked} className="h-5 w-5" />
          <span className="text-xs text-sub">{likeCount}</span>
        </button>
      </div>
    </article>
  );
}
