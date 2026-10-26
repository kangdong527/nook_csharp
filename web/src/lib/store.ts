"use client";

import { supabase, STORAGE_BUCKET } from "./supabase";
import {
  GROUP_COLORS,
  GROUP_MAX_COUNT,
  GROUP_MEMBER_MAX,
  type Gender,
  type Group,
  type GroupSavedPlace,
  type KakaoPlace,
  type Place,
  type Review,
  type SavedPlace,
  type User,
} from "./types";

// =========================================================
// 🗑️ 폐기된 것 (Phase 1-5, 2026-08-04)
//
//   getSession / setSession / clearSession  → 쿠키 세션으로 대체됨
//       읽기: lib/data/user.server.ts getCurrentUser()  (서버)
//       종료: lib/data/auth.ts signOut()                (브라우저)
//   signIn / emailExists  → lib/data/auth.ts sendMagicLink() (실제 인증)
//   signUp                → lib/data/user.ts createProfile()
//
// signUp 을 남겨두지 않은 이유: insert 시 id 를 주지 않아 gen_random_uuid() 가
// 새 id 를 만들었다 → auth.users 와 어긋난 프로필 행이 생긴다. 호출되는 순간
// 정리 대상 데이터가 되므로 "미사용 코드"가 아니라 **함정**이었다.
//
// ⚠️ 이 파일의 나머지 함수는 여전히 lib/supabase.ts(`persistSession: false`)를
//    쓴다 → 쿠키 세션을 태우지 않으므로 요청이 anon 으로 나간다. RLS 를 거는
//    Phase 1-6 에서 어댑터를 adapters/supabase/client.ts 로 교체해야 한다.
// =========================================================

// =========================================================
// 사용자 매핑
// =========================================================
type UserRow = {
  id: string;
  email: string;
  nickname: string;
  gender: string | null;
  birth: string | null;
  avatar_url: string | null;
};

const mapUser = (r: UserRow): User => ({
  id: r.id,
  email: r.email,
  nickname: r.nickname,
  gender: (r.gender as Gender | null) ?? null,
  birth: r.birth,
  avatarUrl: r.avatar_url,
});

const USER_SELECT = "id, email, nickname, gender, birth, avatar_url";

export async function nicknameExists(
  nickname: string,
  excludeUserId?: string
): Promise<boolean> {
  let query = supabase.from("users").select("id").eq("nickname", nickname.trim());
  if (excludeUserId) query = query.neq("id", excludeUserId);
  const { data } = await query.maybeSingle();
  return !!data;
}

export async function updateProfile(
  userId: string,
  input: { nickname: string; avatarUrl: string | null }
): Promise<{ ok: true; user: User } | { ok: false; reason: "nickname" | "error" }> {
  if (await nicknameExists(input.nickname, userId)) {
    return { ok: false, reason: "nickname" };
  }
  const { data, error } = await supabase
    .from("users")
    .update({ nickname: input.nickname.trim(), avatar_url: input.avatarUrl })
    .eq("id", userId)
    .select(USER_SELECT)
    .single();
  if (error || !data) return { ok: false, reason: "error" };
  return { ok: true, user: mapUser(data as UserRow) };
}

/**
 * ⚠️ `public.users` 만 지운다 → `auth.users` 계정은 남는다(Phase 1-8 과제).
 *    호출한 화면이 이어서 signOut() 을 해야 "탈퇴했는데 로그인 상태"가 되지
 *    않는다(app/profile/settings/SettingsScreen.tsx 참조).
 */
export async function deleteAccount(userId: string): Promise<void> {
  await supabase.from("users").delete().eq("id", userId);
}

// =========================================================
// 장소 매핑 / upsert
// =========================================================
type PlaceRow = {
  id: string;
  kakao_id: string;
  name: string;
  category: string | null;
  address: string | null;
  road_address: string | null;
  phone: string | null;
  place_url: string | null;
  lat: number;
  lng: number;
};

const mapPlace = (r: PlaceRow): Place => ({
  id: r.id,
  kakaoId: r.kakao_id,
  name: r.name,
  category: r.category ?? "",
  address: r.address ?? "",
  roadAddress: r.road_address ?? "",
  phone: r.phone ?? "",
  placeUrl: r.place_url ?? "",
  lat: r.lat,
  lng: r.lng,
});

// 카카오 장소를 DB 에 저장(있으면 가져오기)
export async function upsertPlace(p: KakaoPlace): Promise<Place> {
  const existing = await supabase
    .from("places")
    .select("*")
    .eq("kakao_id", p.kakaoId)
    .maybeSingle();
  if (existing.data) return mapPlace(existing.data as PlaceRow);

  const { data, error } = await supabase
    .from("places")
    .insert({
      kakao_id: p.kakaoId,
      name: p.name,
      category: p.category,
      address: p.address,
      road_address: p.roadAddress,
      phone: p.phone,
      place_url: p.placeUrl,
      lat: p.lat,
      lng: p.lng,
    })
    .select("*")
    .single();
  if (error || !data) {
    // 동시 insert 충돌 시 재조회
    const retry = await supabase
      .from("places")
      .select("*")
      .eq("kakao_id", p.kakaoId)
      .maybeSingle();
    if (retry.data) return mapPlace(retry.data as PlaceRow);
    throw error ?? new Error("upsert place failed");
  }
  return mapPlace(data as PlaceRow);
}

export async function getPlace(placeId: string): Promise<Place | null> {
  const { data } = await supabase
    .from("places")
    .select("*")
    .eq("id", placeId)
    .maybeSingle();
  return data ? mapPlace(data as PlaceRow) : null;
}

// =========================================================
// 북마크 (회색 별)
// =========================================================
export async function isBookmarked(
  userId: string,
  placeId: string
): Promise<boolean> {
  const { data } = await supabase
    .from("bookmarks")
    .select("id")
    .eq("user_id", userId)
    .eq("place_id", placeId)
    .maybeSingle();
  return !!data;
}

// 북마크 토글 → 최종 북마크 여부 반환
export async function toggleBookmark(
  userId: string,
  placeId: string
): Promise<boolean> {
  if (await isBookmarked(userId, placeId)) {
    await supabase
      .from("bookmarks")
      .delete()
      .eq("user_id", userId)
      .eq("place_id", placeId);
    return false;
  }
  await supabase.from("bookmarks").insert({ user_id: userId, place_id: placeId });
  return true;
}

// =========================================================
// 내가 저장한 장소 (지도 마커)
// =========================================================
export async function getMySavedPlaces(userId: string): Promise<SavedPlace[]> {
  const [bm, rv] = await Promise.all([
    supabase
      .from("bookmarks")
      .select("places(*)")
      .eq("user_id", userId),
    supabase
      .from("reviews")
      .select("places(*)")
      .eq("user_id", userId),
  ]);

  const byId = new Map<string, SavedPlace>();

  const addRows = (
    rows: { places: PlaceRow | null }[] | null,
    star: "gray" | "fill"
  ) => {
    (rows ?? []).forEach((row) => {
      const pr = row.places;
      if (!pr) return;
      const existing = byId.get(pr.id);
      // 리뷰(fill)가 북마크(gray)보다 우선
      if (existing && existing.star === "fill") return;
      byId.set(pr.id, { ...mapPlace(pr), star });
    });
  };

  addRows(bm.data as unknown as { places: PlaceRow | null }[], "gray");
  addRows(rv.data as unknown as { places: PlaceRow | null }[], "fill");

  return Array.from(byId.values());
}

// 프로필 별 개수: 빈별(=북마크만) / 채운별(=리뷰 작성 장소)
// 리뷰를 작성한 장소는 채운 별로 집계하고 빈 별에서는 제외한다.
export async function getStarCounts(
  userId: string
): Promise<{ bookmark: number; review: number }> {
  const [bm, rv] = await Promise.all([
    supabase.from("bookmarks").select("place_id").eq("user_id", userId),
    supabase.from("reviews").select("place_id").eq("user_id", userId),
  ]);
  const reviewPlaces = new Set(
    ((rv.data ?? []) as { place_id: string }[]).map((r) => r.place_id)
  );
  const bookmarkOnly = new Set(
    ((bm.data ?? []) as { place_id: string }[])
      .map((b) => b.place_id)
      .filter((pid) => !reviewPlaces.has(pid))
  );
  return { bookmark: bookmarkOnly.size, review: reviewPlaces.size };
}

// =========================================================
// 리뷰
// =========================================================
type ReviewRow = {
  id: string;
  user_id: string;
  place_id: string;
  content: string;
  created_at: string;
  users: { nickname: string; avatar_url: string | null } | null;
  places: { name: string } | null;
  review_images: { url: string; order_index: number }[] | null;
  likes: { user_id: string }[] | null;
};

const REVIEW_SELECT = `
  id, user_id, place_id, content, created_at,
  users:user_id ( nickname, avatar_url ),
  places:place_id ( name ),
  review_images ( url, order_index ),
  likes ( user_id )
`;

const mapReview = (r: ReviewRow, myUserId: string | null): Review => {
  const images = (r.review_images ?? [])
    .slice()
    .sort((a, b) => a.order_index - b.order_index)
    .map((i) => i.url);
  const likes = r.likes ?? [];
  return {
    id: r.id,
    userId: r.user_id,
    authorNickname: r.users?.nickname ?? "user",
    authorAvatarUrl: r.users?.avatar_url ?? null,
    placeId: r.place_id,
    placeName: r.places?.name ?? "",
    content: r.content,
    images,
    likeCount: likes.length,
    likedByMe: myUserId ? likes.some((l) => l.user_id === myUserId) : false,
    createdAt: r.created_at,
  };
};

export async function getReviewsByPlace(
  placeId: string,
  myUserId: string | null
): Promise<Review[]> {
  const { data, error } = await supabase
    .from("reviews")
    .select(REVIEW_SELECT)
    .eq("place_id", placeId)
    .order("created_at", { ascending: false });
  if (error) throw error;
  return ((data ?? []) as unknown as ReviewRow[]).map((r) => mapReview(r, myUserId));
}

export async function getMyReviews(userId: string): Promise<Review[]> {
  const { data, error } = await supabase
    .from("reviews")
    .select(REVIEW_SELECT)
    .eq("user_id", userId)
    .order("created_at", { ascending: false });
  if (error) throw error;
  return ((data ?? []) as unknown as ReviewRow[]).map((r) => mapReview(r, userId));
}

export async function getReview(
  reviewId: string,
  myUserId: string | null
): Promise<Review | null> {
  const { data, error } = await supabase
    .from("reviews")
    .select(REVIEW_SELECT)
    .eq("id", reviewId)
    .maybeSingle();
  if (error) throw error;
  if (!data) return null;
  return mapReview(data as unknown as ReviewRow, myUserId);
}

// 같은 장소 기록 1일 1회 제한 (00시 기준) → 오늘 이미 작성했는지
export async function hasReviewedPlaceToday(
  userId: string,
  placeId: string
): Promise<boolean> {
  const start = new Date();
  start.setHours(0, 0, 0, 0);
  const { data } = await supabase
    .from("reviews")
    .select("id")
    .eq("user_id", userId)
    .eq("place_id", placeId)
    .gte("created_at", start.toISOString())
    .limit(1);
  return !!(data && data.length > 0);
}

export async function createReview(input: {
  userId: string;
  placeId: string;
  content: string;
  images: string[];
}): Promise<string> {
  const { data, error } = await supabase
    .from("reviews")
    .insert({
      user_id: input.userId,
      place_id: input.placeId,
      content: input.content,
    })
    .select("id")
    .single();
  if (error || !data) throw error ?? new Error("create review failed");
  const reviewId = data.id as string;
  await saveReviewImages(reviewId, input.images);
  return reviewId;
}

export async function updateReview(input: {
  reviewId: string;
  content: string;
  images: string[];
}): Promise<void> {
  const { error } = await supabase
    .from("reviews")
    .update({ content: input.content, updated_at: new Date().toISOString() })
    .eq("id", input.reviewId);
  if (error) throw error;
  // 이미지 전체 교체
  await supabase.from("review_images").delete().eq("review_id", input.reviewId);
  await saveReviewImages(input.reviewId, input.images);
}

async function saveReviewImages(reviewId: string, images: string[]) {
  if (images.length === 0) return;
  const rows = images.slice(0, 3).map((url, i) => ({
    review_id: reviewId,
    url,
    order_index: i,
  }));
  const { error } = await supabase.from("review_images").insert(rows);
  if (error) throw error;
}

export async function deleteReview(reviewId: string): Promise<void> {
  await supabase.from("reviews").delete().eq("id", reviewId);
}

// =========================================================
// 좋아요
// =========================================================
// 토글 → 최종 좋아요 여부 반환
export async function toggleLike(
  userId: string,
  reviewId: string
): Promise<boolean> {
  const { data } = await supabase
    .from("likes")
    .select("id")
    .eq("user_id", userId)
    .eq("review_id", reviewId)
    .maybeSingle();
  if (data) {
    await supabase
      .from("likes")
      .delete()
      .eq("user_id", userId)
      .eq("review_id", reviewId);
    return false;
  }
  await supabase.from("likes").insert({ user_id: userId, review_id: reviewId });
  return true;
}

// =========================================================
// 이미지 업로드 (Supabase Storage)
// =========================================================
export const MAX_IMAGE_BYTES = 5 * 1024 * 1024; // 5MB

export async function uploadImage(file: File): Promise<string> {
  const ext = file.name.split(".").pop() ?? "jpg";
  const path = `${Date.now()}-${Math.random().toString(36).slice(2)}.${ext}`;
  const { error } = await supabase.storage
    .from(STORAGE_BUCKET)
    .upload(path, file, { cacheControl: "3600", upsert: false });
  if (error) throw error;
  const { data } = supabase.storage.from(STORAGE_BUCKET).getPublicUrl(path);
  return data.publicUrl;
}

// =========================================================
// 그룹 (공유 지도)
// =========================================================
type GroupRow = {
  id: string;
  name: string;
  invite_code: string;
  owner_id: string;
};

const GROUP_SELECT = "id, name, invite_code, owner_id";

// 헷갈리기 쉬운 0/O, 1/I 는 제외한 초대 코드 (6자)
function genInviteCode(): string {
  const chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
  let s = "";
  for (let i = 0; i < 6; i++) {
    s += chars[Math.floor(Math.random() * chars.length)];
  }
  return s;
}

async function memberCount(groupId: string): Promise<number> {
  const { count } = await supabase
    .from("group_members")
    .select("id", { count: "exact", head: true })
    .eq("group_id", groupId);
  return count ?? 0;
}

// 그룹의 다음 멤버 색상 (이미 쓰인 색은 건너뜀)
async function nextGroupColor(groupId: string): Promise<string> {
  const { data } = await supabase
    .from("group_members")
    .select("color")
    .eq("group_id", groupId);
  const used = new Set(((data ?? []) as { color: string }[]).map((m) => m.color));
  const free = GROUP_COLORS.find((c) => !used.has(c));
  return free ?? GROUP_COLORS[(data?.length ?? 0) % GROUP_COLORS.length];
}

// 내가 속한 그룹 목록
export async function getMyGroups(userId: string): Promise<Group[]> {
  const { data, error } = await supabase
    .from("group_members")
    .select("color, joined_at, groups:group_id ( id, name, invite_code, owner_id )")
    .eq("user_id", userId)
    .order("joined_at", { ascending: true });
  if (error || !data) return [];

  const rows = (data as unknown as {
    color: string;
    groups: GroupRow | null;
  }[]).filter((r) => r.groups);

  return Promise.all(
    rows.map(async (r) => {
      const g = r.groups as GroupRow;
      return {
        id: g.id,
        name: g.name,
        inviteCode: g.invite_code,
        ownerId: g.owner_id,
        memberCount: await memberCount(g.id),
        myColor: r.color,
      } satisfies Group;
    })
  );
}

// 그룹 만들기 (생성자가 첫 멤버로 합류)
export async function createGroup(
  userId: string,
  name: string
): Promise<
  | { ok: true; group: Group }
  | { ok: false; reason: "setup" | "user_missing" | "too_many_groups" | "error" }
> {
  const trimmedName = name.trim();
  const { count: myGroupCount } = await supabase
    .from("group_members")
    .select("id", { count: "exact", head: true })
    .eq("user_id", userId);
  if ((myGroupCount ?? 0) >= GROUP_MAX_COUNT) {
    return { ok: false, reason: "too_many_groups" };
  }

  for (let attempt = 0; attempt < 5; attempt++) {
    const code = genInviteCode();
    const { data, error } = await supabase
      .from("groups")
      .insert({ name: trimmedName, invite_code: code, owner_id: userId })
      .select(GROUP_SELECT)
      .single();
    if (error || !data) {
      const code = (error as { code?: string } | null)?.code;
      if (code === "23505") continue; // 초대 코드 충돌 → 재시도
      if (code === "42P01") return { ok: false, reason: "setup" }; // 테이블 없음
      if (code === "23503") return { ok: false, reason: "user_missing" }; // FK 오류
      console.error("[createGroup] groups insert failed", error);
      return { ok: false, reason: "error" };
    }
    const g = data as GroupRow;
    const color = GROUP_COLORS[0];
    const { error: mErr } = await supabase
      .from("group_members")
      .insert({ group_id: g.id, user_id: userId, color });
    if (mErr) {
      const mCode = (mErr as { code?: string } | null)?.code;
      await supabase.from("groups").delete().eq("id", g.id);
      if (mCode === "42P01") return { ok: false, reason: "setup" };
      if (mCode === "23503") return { ok: false, reason: "user_missing" };
      console.error("[createGroup] group_members insert failed", mErr);
      return { ok: false, reason: "error" };
    }
    return {
      ok: true,
      group: {
        id: g.id,
        name: g.name,
        inviteCode: g.invite_code,
        ownerId: g.owner_id,
        memberCount: 1,
        myColor: color,
      },
    };
  }
  return { ok: false, reason: "error" };
}

// 초대 코드로 그룹 합류
export async function joinByInviteCode(
  userId: string,
  code: string
): Promise<
  | { ok: true; group: Group }
  | { ok: false; reason: "not_found" | "already" | "too_many_groups" | "full" | "error" }
> {
  const c = code.trim().toUpperCase();
  if (!c) return { ok: false, reason: "not_found" };

  const { count: myGroupCount } = await supabase
    .from("group_members")
    .select("id", { count: "exact", head: true })
    .eq("user_id", userId);
  if ((myGroupCount ?? 0) >= GROUP_MAX_COUNT) {
    return { ok: false, reason: "too_many_groups" };
  }

  const { data: g } = await supabase
    .from("groups")
    .select(GROUP_SELECT)
    .eq("invite_code", c)
    .maybeSingle();
  if (!g) return { ok: false, reason: "not_found" };
  const group = g as GroupRow;

  const { data: existing } = await supabase
    .from("group_members")
    .select("id")
    .eq("group_id", group.id)
    .eq("user_id", userId)
    .maybeSingle();
  if (existing) return { ok: false, reason: "already" };
  if ((await memberCount(group.id)) >= GROUP_MEMBER_MAX) {
    return { ok: false, reason: "full" };
  }

  const color = await nextGroupColor(group.id);
  const { error } = await supabase
    .from("group_members")
    .insert({ group_id: group.id, user_id: userId, color });
  if (error) return { ok: false, reason: "error" };

  return {
    ok: true,
    group: {
      id: group.id,
      name: group.name,
      inviteCode: group.invite_code,
      ownerId: group.owner_id,
      memberCount: await memberCount(group.id),
      myColor: color,
    },
  };
}

export async function renameGroup(
  groupId: string,
  name: string
): Promise<boolean> {
  const { error } = await supabase
    .from("groups")
    .update({ name: name.trim() })
    .eq("id", groupId);
  return !error;
}

// 그룹 나가기 → 멤버가 0명이면 그룹 자체 삭제
export async function leaveGroup(
  groupId: string,
  userId: string
): Promise<void> {
  await supabase
    .from("group_members")
    .delete()
    .eq("group_id", groupId)
    .eq("user_id", userId);
  if ((await memberCount(groupId)) === 0) {
    await supabase.from("groups").delete().eq("id", groupId);
  }
}

// 그룹 전체 멤버가 저장한 장소 (멤버 색상으로)
export async function getGroupSavedPlaces(
  groupId: string
): Promise<GroupSavedPlace[]> {
  const { data: members } = await supabase
    .from("group_members")
    .select("user_id, color")
    .eq("group_id", groupId);
  if (!members || members.length === 0) return [];

  const colorByUser = new Map<string, string>();
  (members as { user_id: string; color: string }[]).forEach((m) =>
    colorByUser.set(m.user_id, m.color)
  );
  const userIds = Array.from(colorByUser.keys());

  const [bm, rv] = await Promise.all([
    supabase.from("bookmarks").select("user_id, places(*)").in("user_id", userIds),
    supabase.from("reviews").select("user_id, places(*)").in("user_id", userIds),
  ]);

  const byKey = new Map<string, GroupSavedPlace>();
  const addRows = (
    rows: { user_id: string; places: PlaceRow | null }[] | null,
    star: "gray" | "fill"
  ) => {
    (rows ?? []).forEach((row) => {
      const pr = row.places;
      if (!pr) return;
      const color = colorByUser.get(row.user_id) ?? GROUP_COLORS[0];
      const key = `${pr.id}|${row.user_id}`;
      const existing = byKey.get(key);
      if (existing && existing.star === "fill") return; // 리뷰 우선
      byKey.set(key, { ...mapPlace(pr), star, color });
    });
  };

  addRows(
    bm.data as unknown as { user_id: string; places: PlaceRow | null }[],
    "gray"
  );
  addRows(
    rv.data as unknown as { user_id: string; places: PlaceRow | null }[],
    "fill"
  );

  return Array.from(byKey.values());
}
