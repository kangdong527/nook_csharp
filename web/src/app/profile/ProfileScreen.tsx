"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { getMyReviews, getStarCounts } from "@/lib/store";
import type { Review, User } from "@/lib/types";
import { BottomNav } from "@/components/BottomNav";
import { Avatar } from "@/components/Avatar";
import { ReviewCard } from "@/components/ReviewCard";
import { ProfileEditModal } from "@/components/ProfileEditModal";
import { FullSpinner } from "@/components/Spinner";
import { SettingsIcon, StarIcon } from "@/components/icons";

const PAGE = 5;

export function ProfileScreen({ initialUser }: { initialUser: User }) {
  // 서버가 내려준 값이 시작점이고, 프로필 수정 결과를 즉시 반영하기 위해
  // 로컬 상태로 들고 있는다(수정 후 서버 재요청 없이 화면이 갱신된다).
  const [user, setUser] = useState<User>(initialUser);
  const [reviews, setReviews] = useState<Review[]>([]);
  const [counts, setCounts] = useState({ bookmark: 0, review: 0 });
  const [visible, setVisible] = useState(PAGE);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);

  const userId = user.id;

  const load = useCallback(async () => {
    const [rv, c] = await Promise.all([getMyReviews(userId), getStarCounts(userId)]);
    setReviews(rv);
    setCounts(c);
    setLoading(false);
  }, [userId]);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <div className="flex flex-1 flex-col">
      <header className="flex items-center justify-end gap-1 px-4 py-3">
        <Link
          href="/profile/settings"
          className="flex h-9 w-9 items-center justify-center text-ink"
          aria-label="설정"
        >
          <SettingsIcon className="h-6 w-6" />
        </Link>
      </header>

      <div className="flex-1 overflow-y-auto pb-6">
        {/* 사용자 정보 */}
        <div className="flex flex-col items-center px-6">
          <Avatar url={user.avatarUrl} size={84} />
          <p className="mt-3 text-lg font-bold">{user.nickname}</p>

          <div className="mt-5 flex w-full max-w-[260px] items-center justify-around">
            <Stat state="gray" label="회색 별" value={counts.bookmark} />
            <span className="h-8 w-px bg-line" />
            <Stat state="fill" label="채운 별" value={counts.review} />
          </div>

          <button
            onClick={() => setEditing(true)}
            className="mt-5 w-full rounded-2xl border border-line py-3 text-sm font-semibold transition active:scale-[0.99]"
          >
            프로필 수정
          </button>
        </div>

        {/* 내 리뷰 피드 */}
        <div className="mt-6 px-5">
          <h2 className="text-sm font-bold">내 리뷰</h2>
          {loading ? (
            <FullSpinner />
          ) : reviews.length === 0 ? (
            <p className="py-12 text-center text-sm text-sub">아직 작성한 리뷰가 없어요</p>
          ) : (
            <>
              <div className="mt-3 space-y-4">
                {reviews.slice(0, visible).map((r) => (
                  <ReviewCard
                    key={r.id}
                    review={r}
                    myUserId={userId}
                    showPlaceName
                    onChanged={load}
                  />
                ))}
              </div>
              {visible < reviews.length && (
                <button
                  onClick={() => setVisible((v) => v + PAGE)}
                  className="mt-4 w-full rounded-2xl bg-field py-3 text-sm font-semibold text-sub"
                >
                  리뷰 더 보기
                </button>
              )}
            </>
          )}
        </div>
      </div>

      <BottomNav />

      {editing && (
        <ProfileEditModal
          user={user}
          onClose={() => setEditing(false)}
          onSaved={(u) => {
            setUser(u);
            setEditing(false);
          }}
        />
      )}
    </div>
  );
}

function Stat({
  state,
  label,
  value,
}: {
  state: "gray" | "fill";
  label: string;
  value: number;
}) {
  return (
    <div className="flex flex-col items-center gap-1">
      <StarIcon state={state} className="h-5 w-5" />
      <span className="text-lg font-bold">{value}</span>
      <span className="text-xs text-sub">{label}</span>
    </div>
  );
}
