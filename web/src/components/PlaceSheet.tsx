"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import {
  getReviewsByPlace,
  isBookmarked,
  toggleBookmark,
  upsertPlace,
} from "@/lib/store";
import type { KakaoPlace, Place, Review, StarState } from "@/lib/types";
import { useToast } from "./Toast";
import { FullSpinner } from "./Spinner";
import { ReviewCard } from "./ReviewCard";
import { CloseIcon, StarIcon } from "./icons";

export function PlaceSheet({
  kakaoPlace,
  userId,
  highlightReviewId,
  onClose,
  onChanged,
}: {
  kakaoPlace: KakaoPlace;
  userId: string;
  highlightReviewId?: string | null;
  onClose: () => void;
  onChanged?: () => void;
}) {
  const router = useRouter();
  const toast = useToast();
  const [place, setPlace] = useState<Place | null>(null);
  const [reviews, setReviews] = useState<Review[]>([]);
  const [bookmarked, setBookmarked] = useState(false);
  const [loading, setLoading] = useState(true);
  const [limitOpen, setLimitOpen] = useState(false);
  const highlightRef = useRef<HTMLDivElement>(null);

  const myReview = reviews.find((r) => r.userId === userId);
  const star: StarState = myReview ? "fill" : bookmarked ? "gray" : "empty";

  // 같은 장소 기록은 1일 1회 (00시 기준)
  const startOfToday = new Date();
  startOfToday.setHours(0, 0, 0, 0);
  const reviewedToday = reviews.some(
    (r) => r.userId === userId && new Date(r.createdAt) >= startOfToday
  );

  const onWrite = () => {
    if (!place) return;
    if (reviewedToday) {
      setLimitOpen(true);
      return;
    }
    router.push(`/review/new?placeId=${place.id}`);
  };

  useEffect(() => {
    let alive = true;
    (async () => {
      setLoading(true);
      try {
        const p = await upsertPlace(kakaoPlace);
        if (!alive) return;
        setPlace(p);
        const [rv, bm] = await Promise.all([
          getReviewsByPlace(p.id, userId),
          isBookmarked(userId, p.id),
        ]);
        if (!alive) return;
        setReviews(rv);
        setBookmarked(bm);
      } catch {
        if (alive) toast("일시적인 오류가 발생했어요. 잠시 후 다시 시도해 주세요.");
      } finally {
        if (alive) setLoading(false);
      }
    })();
    return () => {
      alive = false;
    };
  }, [kakaoPlace, userId, toast]);

  // 하이라이트된 리뷰(직전에 작성한 리뷰)를 화면 중앙으로 부드럽게 스크롤
  useEffect(() => {
    if (loading || !highlightReviewId || !highlightRef.current) return;
    // 바텀시트 등장 애니메이션이 끝난 뒤 스크롤
    const t = window.setTimeout(() => {
      highlightRef.current?.scrollIntoView({
        behavior: "smooth",
        block: "center",
      });
    }, 400);
    return () => window.clearTimeout(t);
  }, [loading, highlightReviewId, reviews]);

  const onToggleStar = async () => {
    if (!place) return;
    if (myReview) {
      toast("리뷰를 작성한 장소예요.");
      return;
    }
    const prev = bookmarked;
    setBookmarked(!prev);
    try {
      await toggleBookmark(userId, place.id);
      onChanged?.();
    } catch {
      setBookmarked(prev);
      toast("일시적인 오류가 발생했어요. 잠시 후 다시 시도해 주세요.");
    }
  };

  return (
    <div className="fixed inset-0 z-40 mx-auto flex max-w-[420px] flex-col justify-end">
      <button className="absolute inset-0 bg-black/30" onClick={onClose} aria-label="닫기" />
      <div className="animate-sheet-up relative flex max-h-[82%] flex-col rounded-t-3xl bg-white shadow-sheet">
        {/* 핸들 */}
        <div className="flex justify-center pt-2.5">
          <span className="h-1 w-10 rounded-full bg-line" />
        </div>

        {/* 헤더 */}
        <div className="flex items-start gap-2 px-5 pb-3 pt-2">
          <button onClick={onToggleStar} className="mt-0.5 shrink-0" aria-label="즐겨찾기">
            <StarIcon state={star} className="h-7 w-7" />
          </button>
          <div className="min-w-0 flex-1">
            <h2 className="truncate text-lg font-bold">{kakaoPlace.name}</h2>
            {kakaoPlace.category && (
              <p className="truncate text-xs text-sub">{kakaoPlace.category}</p>
            )}
          </div>
          <button
            onClick={onClose}
            className="flex h-8 w-8 items-center justify-center text-sub"
            aria-label="닫기"
          >
            <CloseIcon className="h-5 w-5" />
          </button>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto px-5 pb-[max(env(safe-area-inset-bottom),16px)]">
          {/* 상세 정보 */}
          <dl className="space-y-1.5 border-b border-line pb-4 text-sm">
            <Info label="주소" value={kakaoPlace.roadAddress || kakaoPlace.address} />
            {kakaoPlace.phone && <Info label="전화" value={kakaoPlace.phone} />}
          </dl>

          {/* 리뷰 작성 버튼 */}
          {place && (
            <button
              onClick={onWrite}
              className="mt-4 w-full rounded-2xl bg-key py-3 text-sm font-semibold text-white transition active:scale-[0.99]"
            >
              이 장소에 기록 남기기
            </button>
          )}

          {/* 리뷰 피드 */}
          <div className="py-4">
            {loading ? (
              <FullSpinner />
            ) : reviews.length === 0 ? (
              <p className="py-10 text-center text-sm text-sub">아직 남긴 기록이 없어요</p>
            ) : (
              <div className="space-y-4">
                {reviews.map((r) => (
                  <div
                    key={r.id}
                    ref={r.id === highlightReviewId ? highlightRef : undefined}
                  >
                    <ReviewCard
                      review={r}
                      myUserId={userId}
                      highlight={r.id === highlightReviewId}
                      onChanged={() => {
                        // 내 리뷰 삭제 시 목록 갱신
                        if (place) getReviewsByPlace(place.id, userId).then(setReviews);
                        onChanged?.();
                      }}
                    />
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* 1일 1회 제한 안내 팝업 */}
      {limitOpen && (
        <div className="fixed inset-0 z-50 mx-auto flex max-w-[420px] items-center justify-center px-8">
          <button
            className="absolute inset-0 bg-black/40"
            onClick={() => setLimitOpen(false)}
            aria-label="닫기"
          />
          <div className="animate-pop-in relative w-full rounded-3xl bg-key px-6 py-6 text-white shadow-sheet">
            <button
              onClick={() => setLimitOpen(false)}
              className="absolute right-4 top-4 text-white/80"
              aria-label="닫기"
            >
              <CloseIcon className="h-5 w-5" />
            </button>
            <p className="mt-2 text-center text-base font-semibold leading-relaxed">
              같은 장소에 대한 기록은
              <br />1일 1회만 가능해요
            </p>
            <p className="mt-2 text-center text-xs text-white/55">*00시 기준</p>
          </div>
        </div>
      )}
    </div>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex gap-2">
      <dt className="w-12 shrink-0 text-sub">{label}</dt>
      <dd className="flex-1 text-ink">{value || "-"}</dd>
    </div>
  );
}
