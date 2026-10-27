"use client";

import { nookFetch } from "./api/client";
import {
  slotColor,
  type Group,
  type GroupSavedPlace,
  type KakaoPlace,
  type Place,
  type Review,
  type ReviewImageRef,
  type SavedPlace,
  type StarState,
} from "./types";

// =========================================================
// 화면이 부르는 데이터 함수 — 모두 C# API(BFF 중계 /api/nook, lib/api/client.ts)를 부른다(FE-2).
// 원본은 Supabase 를 브라우저에서 직접 불렀다. 함수 이름·모양은 화면을 덜 고치려고 대부분 그대로 두었다.
// =========================================================

// PF-01 닉네임 — 미리 확인 없이 저장 때 서버가 판정한다(409 User.NicknameTaken, 동시 변경 포함).
export async function updateNickname(
  nickname: string
): Promise<{ ok: true; user: MeUser } | { ok: false; reason: "nickname_taken" | "invalid" }> {
  const res = await nookFetch("/me", { method: "PATCH", body: JSON.stringify({ nickname: nickname.trim() }) });
  if (res.status === 409) return { ok: false, reason: "nickname_taken" };
  if (res.status === 400) return { ok: false, reason: "invalid" };
  if (!res.ok) throw new ApiError(res.status, await problemTitle(res));
  return { ok: true, user: (await res.json()) as MeUser };
}

// PF-01 프로필 사진 교체 — 파일 한 장(multipart). 이전 사진은 서버가 커밋 뒤 지운다.
export async function changeAvatar(file: File): Promise<MeUser> {
  const form = new FormData();
  form.append("Image", file);
  const res = await nookFetch("/me/avatar", { method: "PUT", body: form });
  if (!res.ok) throw new ApiError(res.status, await problemTitle(res));
  return (await res.json()) as MeUser;
}

/** C# MeView — avatarUrl null = 기본 이미지. */
export type MeUser = { id: string; email: string; nickname: string; avatarUrl: string | null };

/**
 * ⚠️ `public.users` 만 지운다 → `auth.users` 계정은 남는다(Phase 1-8 과제).
 *    호출한 화면이 이어서 signOut() 을 해야 "탈퇴했는데 로그인 상태"가 되지
 *    않는다(app/profile/settings/SettingsScreen.tsx 참조).
 */
// PF-02 회원 탈퇴 — BFF 가 C# DELETE /me 를 부르고 쿠키를 지운다. 리뷰·좋아요는 남는다(PM), 복구 없음.
export async function deleteAccount(): Promise<void> {
  const res = await fetch("/api/auth/withdraw", { method: "POST" });
  if (!res.ok) throw new ApiError(res.status, await problemTitle(res));
}

// =========================================================
// 장소 매핑 / upsert
// =========================================================
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

/** C# API 실패. title = 오류 코드(예: "Review.AlreadyReviewedToday") — 화면이 문구를 고른다. */
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly title: string | null
  ) {
    super(`API ${status}${title ? ` ${title}` : ""}`);
  }
}

async function problemTitle(res: Response): Promise<string | null> {
  try {
    return ((await res.json()) as { title?: string }).title ?? null;
  } catch {
    return null;
  }
}

// PL-02 리뷰 항목(C# PlaceReviewView).
type PlaceReviewApiView = {
  id: string;
  author: { id: string; nickname: string; avatarUrl: string | null } | null;
  content: string;
  images: { id: string; url: string }[];
  createdAt: string;
  version: number;
  likeCount: number;
  likedByMe: boolean;
  mine: boolean;
};

// 탈퇴한 작성자는 author 가 null(PF-02) — "탈퇴한 사용자", 기본 이미지. 서버가 ID·닉네임·사진을 주지 않아 리뷰끼리 이어지지 않는다.
const WITHDRAWN_AUTHOR = "탈퇴한 사용자";
const fromPlaceReview = (v: PlaceReviewApiView, placeId: string): Review => ({
  id: v.id,
  userId: v.author?.id ?? "",
  authorNickname: v.author?.nickname ?? WITHDRAWN_AUTHOR,
  authorAvatarUrl: v.author?.avatarUrl ?? null,
  placeId,
  placeName: "",
  content: v.content,
  images: v.images.map(({ id, url }) => ({ id, url })),
  likeCount: v.likeCount,
  likedByMe: v.likedByMe,
  createdAt: v.createdAt,
  version: v.version,
  mine: v.mine,
});

/** RV-04 수정 화면이 받는 리뷰. */
export type EditableReview = {
  id: string;
  placeId: string;
  placeName: string;
  content: string;
  images: ReviewImageRef[];
  version: number;
};

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
// PL-02 장소 상세의 내 상태 — 별(R-STAR, 서버 판정)과 북마크 여부.
// 별은 리뷰가 있으면 fill 이라 북마크 여부를 따로 받는다. 리뷰 목록은 페이지라 "내 리뷰가 있나"를 목록으로 판정할 수 없다.
// 회원은 토큰이 정한다 — userId 인자를 받지 않는다(남의 상태를 물을 길이 없다).
export async function getMyPlaceState(
  placeId: string
): Promise<{ star: StarState; isBookmarked: boolean }> {
  const detail = await getPlaceDetail(placeId);
  return { star: detail?.star ?? "empty", isBookmarked: detail?.isBookmarked ?? false };
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

// PF-03 내 별 개수 — 새 API 없이 내 지도(BM-03)의 별을 센다(FE-2 사용자 결정). 별 판정은 서버(R-STAR).
export async function getStarCounts(): Promise<{ bookmark: number; review: number }> {
  const places = await getMySavedPlaces();
  return {
    bookmark: places.filter((p) => p.star === "gray").length,
    review: places.filter((p) => p.star === "fill").length,
  };
}

// =========================================================
// 리뷰
// =========================================================
// PL-02 리뷰 목록 — 최신순 키셋 페이지(FE-2 사용자 결정 (b) 무한 스크롤). 첫 요청은 cursor 없이, 다음은 받은 nextCursor 그대로.
export async function getReviewsByPlace(
  placeId: string,
  cursor: string | null = null
): Promise<{ items: Review[]; nextCursor: string | null }> {
  const query = cursor ? `?cursor=${encodeURIComponent(cursor)}` : "";
  const res = await nookFetch(`/places/${placeId}/reviews${query}`);
  if (!res.ok) throw new ApiError(res.status, await problemTitle(res));
  const page = (await res.json()) as { items: PlaceReviewApiView[]; nextCursor: string | null };
  return { items: page.items.map((v) => fromPlaceReview(v, placeId)), nextCursor: page.nextCursor };
}

// PF-04 내 리뷰 피드 — 최신순 키셋, 기본 5개("더 보기"). 첫 요청은 cursor 없이.
export async function getMyReviews(
  cursor: string | null = null
): Promise<{ items: Review[]; nextCursor: string | null }> {
  const query = cursor ? `?cursor=${encodeURIComponent(cursor)}` : "";
  const res = await nookFetch(`/me/reviews${query}`);
  if (!res.ok) throw new ApiError(res.status, await problemTitle(res));
  const page = (await res.json()) as {
    items: {
      id: string;
      placeId: string;
      placeName: string;
      content: string;
      images: { id: string; url: string }[];
      createdAt: string;
      version: number;
      likeCount: number;
      likedByMe: boolean;
    }[];
    nextCursor: string | null;
  };
  return {
    // 작성자는 나 — 닉네임·사진은 화면이 가진 user 로 충분해 비워 둔다(카드는 showPlaceName 으로 장소를 보인다).
    items: page.items.map((v) => ({
      id: v.id,
      userId: "",
      authorNickname: "",
      authorAvatarUrl: null,
      placeId: v.placeId,
      placeName: v.placeName,
      content: v.content,
      images: v.images.map(({ id, url }) => ({ id, url })),
      likeCount: v.likeCount,
      likedByMe: v.likedByMe,
      createdAt: v.createdAt,
      version: v.version,
      mine: true,
    })),
    nextCursor: page.nextCursor,
  };
}

// RV-04 수정 화면용 단건 — 작성자만. 없으면 "not_found"(404), 남의 것이면 "forbidden"(403).
export async function getReview(
  reviewId: string
): Promise<EditableReview | "not_found" | "forbidden"> {
  const res = await nookFetch(`/reviews/${reviewId}`);
  if (res.status === 404) return "not_found";
  if (res.status === 403) return "forbidden";
  if (!res.ok) throw new ApiError(res.status, await problemTitle(res));
  const v = (await res.json()) as {
    id: string;
    placeId: string;
    placeName: string;
    content: string;
    images: { id: string; url: string }[];
    version: number;
  };
  return { ...v, images: v.images.map(({ id, url }) => ({ id, url })) };
}

// R-RV-03 미리 확인(문구용). 최종 판정은 서버 409. 기준은 서버와 같은 KST 날짜(00시, ReviewDay).
// 내 리뷰는 최신순 첫 페이지에 없을 수도 있다(남의 리뷰가 많을 때) → 미리 확인은 "있으면 막기"만, 없다고 보장하지 않는다.
export async function hasReviewedPlaceToday(placeId: string): Promise<boolean> {
  const { items } = await getReviewsByPlace(placeId);
  return items.some((r) => r.mine && isSameKstDay(r.createdAt, new Date()));
}

// KST = UTC+9 고정(서머타임 없음) — 브라우저 시간대와 무관하게 서버(C# ReviewDay)와 같은 날을 본다.
const KST_OFFSET_MS = 9 * 60 * 60 * 1000;
const kstDate = (d: Date) => new Date(d.getTime() + KST_OFFSET_MS).toISOString().slice(0, 10);

export function isSameKstDay(iso: string, now: Date): boolean {
  return kstDate(new Date(iso)) === kstDate(now);
}

// RV-01. 본문과 이미지 파일을 한 요청으로(multipart) — 서버가 전부 검증한 뒤 올리고 저장한다.
// 원본은 이미지를 먼저 올려 URL 을 받았다 → 저장이 실패하면 고아 이미지가 남았다.
// 실패: 409 Review.AlreadyReviewedToday(같은 장소 하루 1개, KST 00시 기준) 등은 ApiError 로.
export async function createReview(input: {
  placeId: string;
  content: string;
  files: File[];
}): Promise<string> {
  const form = new FormData();
  form.append("PlaceId", input.placeId);
  form.append("Content", input.content);
  input.files.forEach((f) => form.append("Images", f));

  const res = await nookFetch("/reviews", { method: "POST", body: form });
  if (!res.ok) throw new ApiError(res.status, await problemTitle(res));
  return ((await res.json()) as { id: string }).id;
}

// RV-02. 최종 상태를 통째로: 남길 기존 이미지 ID(순서대로) + 새 파일 + 화면이 받은 version.
// 409 Review.EditConflict = 화면을 연 사이 다른 수정이 먼저 저장됨 → 다시 불러와야 한다.
export async function updateReview(input: {
  reviewId: string;
  version: number;
  content: string;
  keepImageIds: string[];
  files: File[];
}): Promise<void> {
  const form = new FormData();
  form.append("Version", String(input.version));
  form.append("Content", input.content);
  input.keepImageIds.forEach((id) => form.append("KeepImageIds", id));
  input.files.forEach((f) => form.append("Images", f));

  const res = await nookFetch(`/reviews/${input.reviewId}`, { method: "PUT", body: form });
  if (!res.ok) throw new ApiError(res.status, await problemTitle(res));
}

// RV-03. 멱등 — 이미 지운 리뷰도 성공. 남의 리뷰는 403.
export async function deleteReview(reviewId: string): Promise<void> {
  const res = await nookFetch(`/reviews/${reviewId}`, { method: "DELETE" });
  if (!res.ok) throw new ApiError(res.status, await problemTitle(res));
}

// LK-01. 토글 대신 목표 상태(PUT = 좋아요, DELETE = 취소, 둘 다 멱등) — 북마크와 같은 이유.
export async function setLiked(reviewId: string, on: boolean): Promise<void> {
  const res = await nookFetch(`/reviews/${reviewId}/like`, { method: on ? "PUT" : "DELETE" });
  if (!res.ok) throw new ApiError(res.status, await problemTitle(res));
}

// =========================================================
// 이미지 업로드 (Supabase Storage)
// =========================================================
export const MAX_IMAGE_BYTES = 5 * 1024 * 1024; // 5MB


// =========================================================
// 그룹 (공유 지도)
// =========================================================
// GR-06 내 그룹 목록 — 합류 순, 멤버 수·내 슬롯·방장 여부를 서버가 한 쿼리로(원본 N+1 해소).
export async function getMyGroups(): Promise<Group[]> {
  const res = await nookFetch("/me/groups");
  if (!res.ok) throw new ApiError(res.status, await problemTitle(res));
  const views = (await res.json()) as {
    id: string;
    name: string;
    inviteCode: string;
    memberCount: number;
    myColorSlot: number;
    isOwner: boolean;
  }[];
  return views.map((v) => ({
    id: v.id,
    name: v.name,
    inviteCode: v.inviteCode,
    isOwner: v.isOwner,
    memberCount: v.memberCount,
    myColor: slotColor(v.myColorSlot),
  }));
}

// 생성·합류 응답(GroupView)엔 멤버 수·내 슬롯이 없다 → 목록을 다시 받아 그 그룹을 꺼낸다(최대 3개라 싸다).
async function myGroup(groupId: string): Promise<Group> {
  const group = (await getMyGroups()).find((g) => g.id === groupId);
  if (!group) throw new Error(`방금 들어간 그룹이 목록에 없습니다: ${groupId}`);
  return group;
}

// GR-02. 그룹명 2~6자(SSoT — 원본 화면의 1~16자와 다르다). 409 Group.TooManyGroups(R-GR-02).
export async function createGroup(
  name: string
): Promise<{ ok: true; group: Group } | { ok: false; reason: "invalid_name" | "too_many_groups" }> {
  const res = await nookFetch("/groups", { method: "POST", body: JSON.stringify({ name: name.trim() }) });
  if (res.status === 400) return { ok: false, reason: "invalid_name" };
  if (res.status === 409) return { ok: false, reason: "too_many_groups" };
  if (!res.ok) throw new ApiError(res.status, await problemTitle(res));
  return { ok: true, group: await myGroup(((await res.json()) as { id: string }).id) };
}

// GR-01. 실패는 오류 코드(title)로 가른다 — 409 가 세 가지라 상태 코드만으로는 모른다.
export async function joinByInviteCode(
  code: string
): Promise<
  { ok: true; group: Group } | { ok: false; reason: "not_found" | "already" | "too_many_groups" | "full" }
> {
  const res = await nookFetch("/groups/join", { method: "POST", body: JSON.stringify({ inviteCode: code.trim() }) });
  if (res.ok) return { ok: true, group: await myGroup(((await res.json()) as { id: string }).id) };

  const title = await problemTitle(res);
  if (res.status === 400 || title === "Group.NotFound") return { ok: false, reason: "not_found" };
  if (title === "Group.AlreadyMember") return { ok: false, reason: "already" };
  if (title === "Group.TooManyGroups") return { ok: false, reason: "too_many_groups" };
  if (title === "Group.GroupFull") return { ok: false, reason: "full" };
  throw new ApiError(res.status, title);
}

// GR-03. 방장만(403 Group.NotOwner). 그룹명 2~6자.
export async function renameGroup(
  groupId: string,
  name: string
): Promise<{ ok: true } | { ok: false; reason: "invalid_name" | "not_owner" }> {
  const res = await nookFetch(`/groups/${groupId}`, { method: "PATCH", body: JSON.stringify({ name: name.trim() }) });
  if (res.ok) return { ok: true };
  if (res.status === 400) return { ok: false, reason: "invalid_name" };
  if (res.status === 403) return { ok: false, reason: "not_owner" };
  throw new ApiError(res.status, await problemTitle(res));
}

// GR-04. 멱등(204). 방장이 나가면 서버가 가장 먼저 합류한 멤버에게 넘긴다(R-GR-11).
export async function leaveGroup(groupId: string): Promise<void> {
  const res = await nookFetch(`/groups/${groupId}/membership`, { method: "DELETE" });
  if (!res.ok) throw new ApiError(res.status, await problemTitle(res));
}

// GR-05 그룹 지도 — 장소마다 한 행(사실). 멤버 아님 403 은 nookFetch 가 401 이 아니므로 그대로 ApiError.
export async function getGroupSavedPlaces(groupId: string): Promise<GroupSavedPlace[]> {
  const res = await nookFetch(`/groups/${groupId}/places`);
  if (!res.ok) throw new ApiError(res.status, await problemTitle(res));
  const views = (await res.json()) as (PlaceApiView & {
    placeId: string;
    reviewMemberCount: number;
    firstReviewerSlot: number | null;
    bookmarkCount: number;
    soleBookmarkerSlot: number | null;
  })[];
  return views.map((v) => ({
    ...toPlace({ ...v, id: v.placeId }),
    reviewMemberCount: v.reviewMemberCount,
    firstReviewerSlot: v.firstReviewerSlot,
    bookmarkCount: v.bookmarkCount,
    soleBookmarkerSlot: v.soleBookmarkerSlot,
  }));
}
