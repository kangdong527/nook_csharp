"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import {
  getMyPlaceState,
  getReviewsByPlace,
  isSameUtcDay,
  setBookmarked as saveBookmark,
  upsertPlace,
} from "@/lib/store";
import type { KakaoPlace, Place, Review, StarState } from "@/lib/types";
import { useToast } from "./Toast";
import { FullSpinner, Spinner } from "./Spinner";
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
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [loadingMore, setLoadingMore] = useState(false);
  const [hasReview, setHasReview] = useState(false);
  const [bookmarked, setBookmarked] = useState(false);
  const [loading, setLoading] = useState(true);
  const sentinelRef = useRef<HTMLDivElement>(null);
  const [limitOpen, setLimitOpen] = useState(false);
  const highlightRef = useRef<HTMLDivElement>(null);

  // 별은 서버 판정(R-STAR)에서 시작해 북마크 토글만 화면에서 반영한다. 리뷰가 있으면 북마크와 무관하게 fill.
  // 리뷰 목록은 페이지라 "내 리뷰가 있나"를 목록으로 판정할 수 없다(첫 페이지에 없을 수 있다).
  const star: StarState = hasReview ? "fill" : bookmarked ? "gray" : "empty";

  // 같은 장소 기록은 1일 1회 — 미리 확인(문구용). 기준은 서버와 같은 UTC 날짜, 최종 판정은 서버 409(R-RV-03).
  const reviewedToday = reviews.some((r) => r.mine && isSameUtcDay(r.createdAt, new Date()));

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
        const [page, state] = await Promise.all([getReviewsByPlace(p.id), getMyPlaceState(p.id)]);
        if (!alive) return;
        setReviews(page.items);
        setNextCursor(page.nextCursor);
        setHasReview(state.star === "fill");
        setBookmarked(state.isBookmarked);
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

  // 무한 스크롤(PL-02 키셋, FE-2 사용자 결정 (b)): 목록 끝의 표지가 보이면 다음 페이지를 이어 붙인다.
  // 키셋이라 스크롤 중 새 리뷰가 생겨도 앞 페이지 항목이 다시 나오지 않는다 — 그래도 id 로 한 번 더 거른다.
  useEffect(() => {
    const el = sentinelRef.current;
    if (!el || !place || !nextCursor) return;
    const observer = new IntersectionObserver(async ([entry]) => {
      if (!entry.isIntersecting || loadingMore) return;
      setLoadingMore(true);
      try {
        const page = await getReviewsByPlace(place.id, nextCursor);
        setReviews((prev) => {
          const seen = new Set(prev.map((r) => r.id));
          return [...prev, ...page.items.filter((r) => !seen.has(r.id))];
        });
        setNextCursor(page.nextCursor);
      } catch {
        toast("일시적인 오류가 발생했어요. 잠시 후 다시 시도해 주세요.");
      } finally {
        setLoadingMore(false);
      }
    });
    observer.observe(el);
    return () => observer.disconnect();
  }, [place, nextCursor, loadingMore, toast]);

  // 내 리뷰 삭제 뒤: 첫 페이지와 별을 서버에서 다시 받는다(RV-03 — 남은 리뷰·북마크에 따라 fill/gray/empty).
  const reload = async () => {
    if (!place) return;
    const [page, state] = await Promise.all([getReviewsByPlace(place.id), getMyPlaceState(place.id)]);
    setReviews(page.items);
    setNextCursor(page.nextCursor);
    setHasReview(state.star === "fill");
    setBookmarked(state.isBookmarked);
  };

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
    if (hasReview) {
      toast("리뷰를 작성한 장소예요.");
      return;
    }
    const prev = bookmarked;
    setBookmarked(!prev);
    try {
      await saveBookmark(place.id, !prev);
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
                        reload();
                        onChanged?.();
                      }}
                    />
                  </div>
                ))}
                {/* 다음 페이지 표지 — 보이면 이어 불러온다 */}
                {nextCursor && (
                  <div ref={sentinelRef} className="flex justify-center py-4">
                    {loadingMore && <Spinner className="text-key" />}
                  </div>
                )}
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
