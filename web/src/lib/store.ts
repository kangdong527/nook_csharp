"use client";

import { supabase, STORAGE_BUCKET } from "./supabase";
import { nookFetch } from "./api/client";
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
  type StarState,
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

// C# API 장소 응답(PlaceView·MyPlaceView·PlaceDetailView 공통 필드). FE-2.
type PlaceApiView = {
  id: string;
  kakaoPlaceId: string;
  name: string;
  address: string;
  latitude: number;
  longitude: number;
};

type PlaceDetailApiView = PlaceApiView & { star: StarState; isBookmarked: boolean };

// 우리 places 에 없는 카카오 부가 정보는 빈 값 — 화면은 이미 빈 값이면 숨긴다(`p.category &&`, `roadAddress || address`).
const toPlace = (v: PlaceApiView): Place => ({
  id: v.id,
  kakaoId: v.kakaoPlaceId,
  name: v.name,
  category: "",
  address: v.address,
  roadAddress: "",
  phone: "",
  placeUrl: "",
  lat: v.latitude,
  lng: v.longitude,
});

async function getPlaceDetail(placeId: string): Promise<PlaceDetailApiView | null> {
  const res = await nookFetch(`/places/${placeId}`);
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`장소 조회 실패: ${res.status}`);
  return (await res.json()) as PlaceDetailApiView;
}

// 카카오 장소를 DB 에 저장(있으면 가져오기)
// PL-01. 카카오 검색 결과를 확정한다 — 새로 만들든 기존 것이든 같은 응답(멱등, 동시 등록은 서버 UNIQUE가 판정).
// 우리 places 에는 카테고리·전화·URL 이 없다(D3-2) — 화면 표시용으로 카카오 값을 그대로 붙여 돌려준다.
export async function upsertPlace(p: KakaoPlace): Promise<Place> {
  const res = await nookFetch("/places", {
    method: "POST",
    body: JSON.stringify({
      kakaoPlaceId: p.kakaoId,
      name: p.name,
      address: p.roadAddress || p.address,
      latitude: p.lat,
      longitude: p.lng,
    }),
  });
  if (!res.ok) throw new Error(`장소 확정 실패: ${res.status}`);
  const view = (await res.json()) as PlaceApiView;
  return { ...p, id: view.id };
}

// PL-02 장소 상세. 없으면 null(404).
export async function getPlace(placeId: string): Promise<Place | null> {
  const detail = await getPlaceDetail(placeId);
  return detail ? toPlace(detail) : null;
}

// =========================================================
// 북마크 (회색 별)
// =========================================================
// PL-02 장소 상세의 IsBookmarked — 별(star)은 리뷰가 있으면 fill 이라 북마크 여부를 따로 받는다.
// 회원은 토큰이 정한다 — userId 인자를 받지 않는다(남의 북마크를 물을 길이 없다).
export async function isBookmarked(placeId: string): Promise<boolean> {
  return (await getPlaceDetail(placeId))?.isBookmarked ?? false;
}

// 북마크 토글 → 최종 북마크 여부 반환
// BM-01·02. 토글 대신 원하는 상태를 명시한다(PUT = 저장, DELETE = 해제, 둘 다 멱등).
// 원본 toggle 은 "조회 → 반대로"라 두 번 누르면 경합했다. 화면이 지금 상태를 알므로 목표 상태를 보낸다.
export async function setBookmarked(placeId: string, on: boolean): Promise<void> {
  const res = await nookFetch(`/places/${placeId}/bookmark`, { method: on ? "PUT" : "DELETE" });
  if (!res.ok) throw new Error(`북마크 ${on ? "저장" : "해제"} 실패: ${res.status}`);
}

// =========================================================
// 내가 저장한 장소 (지도 마커)
// =========================================================
// BM-03 내 지도. 별 판정(gray/fill)은 서버가 한다(R-STAR 한 곳).
export async function getMySavedPlaces(): Promise<SavedPlace[]> {
  const res = await nookFetch("/me/places");
  if (!res.ok) throw new Error(`내 지도 조회 실패: ${res.status}`);
  const views = (await res.json()) as (PlaceApiView & { star: "gray" | "fill" })[];
  return views.map((v) => ({ ...toPlace(v), star: v.star }));
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
