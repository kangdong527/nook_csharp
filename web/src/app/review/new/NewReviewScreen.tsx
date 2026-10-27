"use client";

import { Suspense, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import {
  ApiError,
  createReview,
  getPlace,
  hasReviewedPlaceToday,
  upsertPlace,
} from "@/lib/store";
import { searchPlaces } from "@/lib/kakao";
import type { KakaoPlace, Place, User } from "@/lib/types";
import { useToast } from "@/components/Toast";
import { FullSpinner, Spinner } from "@/components/Spinner";
import { ReviewEditor, type EditorImage } from "@/components/ReviewEditor";
import { ChevronLeftIcon, SearchIcon } from "@/components/icons";

function NewReview({ user }: { user: User }) {
  const router = useRouter();
  const toast = useToast();
  const params = useSearchParams();
  const placeIdParam = params.get("placeId");

  const [place, setPlace] = useState<Place | null>(null);
  const [loadingPlace, setLoadingPlace] = useState(!!placeIdParam);

  useEffect(() => {
    if (!placeIdParam) return;
    getPlace(placeIdParam)
      .then((p) => setPlace(p))
      .finally(() => setLoadingPlace(false));
  }, [placeIdParam]);

  if (loadingPlace) return <FullSpinner />;

  // 장소 미선택 → 장소 검색
  if (!place) {
    return (
      <PlacePicker
        onPick={async (p) => {
          if (await hasReviewedPlaceToday(p.id)) {
            toast("같은 장소에 대한 기록은 1일 1회만 가능해요. (00시 기준)");
            return;
          }
          setPlace(p);
        }}
      />
    );
  }

  return (
    <ReviewEditor
      mode="new"
      user={user}
      placeName={place.name}
      onCancel={
        placeIdParam
          ? () => router.back() // 장소 시트에서 진입 → 이전 화면으로
          : () => setPlace(null) // 검색으로 선택 → '장소 검색'으로 복귀
      }
      onSubmit={async (content, images) => {
        let reviewId: string;
        try {
          reviewId = await createReview({ placeId: place.id, content, files: newFiles(images) });
        } catch (e) {
          // 미리 확인을 통과해도 서버 UNIQUE 가 최종 판정한다(R-RV-03, 동시 작성 포함).
          if (e instanceof ApiError && e.title === "Review.AlreadyReviewedToday") {
            toast("같은 장소에 대한 기록은 1일 1회만 가능해요. (00시 기준)");
            return;
          }
          throw e;
        }
        toast("기록을 발행했어요.");
        router.replace(
          `/map?placeId=${place.id}&reviewId=${reviewId}`
        );
      }}
    />
  );
}

// 새 리뷰엔 기존 사진이 없다 — 고른 파일만.
const newFiles = (images: EditorImage[]): File[] =>
  images.flatMap((i) => (i.kind === "new" ? [i.file] : []));

function PlacePicker({
  onPick,
}: {
  onPick: (p: Place) => void | Promise<void>;
}) {
  const router = useRouter();
  const toast = useToast();
  const [keyword, setKeyword] = useState("");
  const [results, setResults] = useState<KakaoPlace[] | null>(null);
  const [searching, setSearching] = useState(false);
  const [picking, setPicking] = useState(false);

  const run = async () => {
    const q = keyword.trim();
    if (!q) return;
    setSearching(true);
    try {
      const found = await searchPlaces(q);
      setResults(found);
      if (found.length === 0) toast("검색 결과가 없어요");
    } catch {
      toast("일치하는 장소가 없어요. 다른 키워드로 검색해보세요!");
    } finally {
      setSearching(false);
    }
  };

  const pick = async (p: KakaoPlace) => {
    setPicking(true);
    try {
      await onPick(await upsertPlace(p));
    } catch {
      toast("일시적인 오류가 발생했어요. 잠시 후 다시 시도해 주세요.");
    } finally {
      setPicking(false);
    }
  };

  return (
    <div className="flex flex-1 flex-col">
      <header className="flex items-center gap-1 border-b border-line px-2 py-3">
        <button
          onClick={() => router.back()}
          className="flex h-10 w-10 items-center justify-center text-ink"
          aria-label="뒤로"
        >
          <ChevronLeftIcon className="h-6 w-6" />
        </button>
        <h1 className="text-base font-semibold">장소 검색</h1>
      </header>

      <div className="px-4 pt-3">
        <div className="flex items-center gap-2 rounded-2xl border border-line bg-white px-4 py-2.5">
          <SearchIcon className="h-5 w-5 text-sub" />
          <input
            autoFocus
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && run()}
            placeholder="주소, 상호명 검색"
            className="flex-1 bg-transparent text-sm outline-none placeholder:text-sub"
          />
          {(searching || picking) && <Spinner className="text-key" />}
        </div>
      </div>

      <div className="flex-1 overflow-y-auto px-4 py-3">
        {results === null ? (
          <div className="px-2 pt-8 text-sm leading-relaxed text-sub">
            <p className="font-semibold text-ink">리뷰를 작성할 장소를 찾아보세요</p>
            <ul className="mt-3 list-disc space-y-1 pl-5">
              <li>최근에 다녀온 카페, 맛집, 어디든</li>
              <li>특별했던 날의 그 장소</li>
              <li>자주 가는 동네 단골집</li>
            </ul>
          </div>
        ) : (
          results.map((p) => (
            <button
              key={p.kakaoId}
              onClick={() => pick(p)}
              disabled={picking}
              className="flex w-full flex-col items-start border-b border-line px-1 py-3 text-left last:border-0"
            >
              <span className="text-sm font-semibold">{p.name}</span>
              <span className="mt-0.5 text-xs text-sub">{p.roadAddress || p.address}</span>
              {p.category && <span className="mt-0.5 text-xs text-disabled">{p.category}</span>}
            </button>
          ))
        )}
      </div>
    </div>
  );
}

export function NewReviewScreen({ user }: { user: User }) {
  // Suspense 는 useSearchParams() 때문에 필요하다(?placeId= 로 진입하는 경로).
  return (
    <Suspense fallback={<FullSpinner />}>
      <NewReview user={user} />
    </Suspense>
  );
}
