// ---------- 도메인 타입 ----------

export type Gender = "female" | "male" | "none";

export interface User {
  id: string;
  email: string;
  nickname: string; // 2~12자
  gender: Gender | null;
  birth: string | null; // YY-MM-DD
  avatarUrl: string | null;
}

// 카카오 키워드 검색 결과 (저장 전)
export interface KakaoPlace {
  kakaoId: string;
  name: string;
  category: string;
  address: string;
  roadAddress: string;
  phone: string;
  placeUrl: string;
  lat: number;
  lng: number;
}

// DB 에 저장된 장소
export interface Place extends KakaoPlace {
  id: string;
}

// 장소의 내 별 상태
export type StarState = "empty" | "gray" | "fill";
// empty: 저장 안 함 / gray: 북마크만 / fill: 리뷰 작성

// 지도 위 마커용 (내가 저장한 장소)
export interface SavedPlace extends Place {
  star: "gray" | "fill"; // 지도엔 저장된 것만 → empty 제외
}

// ---------- 그룹 (공유 지도) ----------
export const GROUP_MAX_COUNT = 3;
export const GROUP_MEMBER_MAX = 4;

export interface Group {
  id: string;
  name: string;
  inviteCode: string;
  ownerId: string;
  memberCount: number;
  myColor: string;
}

// 그룹 멤버의 저장 장소 (멤버 색상으로 표시)
export interface GroupSavedPlace extends Place {
  star: "gray" | "fill";
  color: string; // 멤버 색상
}

// 그룹 멤버에게 순서대로 배정되는 마커 색상 팔레트
export const GROUP_COLORS = [
  "#000000", // 나(오너) 닉네임
  "#E99938", // 멤버1
  "#38AE50", // 멤버2
  "#388AAE", // 멤버3
];

export interface ReviewImage {
  url: string;
  orderIndex: number;
}

export interface Review {
  id: string;
  userId: string;
  authorNickname: string;
  authorAvatarUrl: string | null;
  placeId: string;
  placeName: string;
  content: string;
  images: string[]; // 최대 3장
  likeCount: number;
  likedByMe: boolean;
  createdAt: string; // ISO
}

// ---------- 유틸 ----------

// YYYY.MM.DD
export function formatDate(iso: string): string {
  const d = new Date(iso);
  const p = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}.${p(d.getMonth() + 1)}.${p(d.getDate())}`;
}

// 닉네임 규칙: 한글/영문 포함 2~12자
export function isValidNickname(v: string): boolean {
  const t = v.trim();
  return t.length >= 2 && t.length <= 12;
}

// 닉네임 에러 문구 (입력값이 있을 때만 노출)
export function nicknameError(v: string): string | null {
  const t = v.trim();
  if (t.length === 0) return null;
  if (t.length < 2 || t.length > 12) return "닉네임은 2~12자까지 입력할 수 있어요.";
  return null;
}

// 생년월일 입력값을 YY-MM-DD 형태로 자동 포맷 (숫자만 추출)
export function formatBirthInput(v: string): string {
  const digits = v.replace(/\D/g, "").slice(0, 6);
  const yy = digits.slice(0, 2);
  const mm = digits.slice(2, 4);
  const dd = digits.slice(4, 6);
  let out = yy;
  if (digits.length > 2) out += `-${mm}`;
  if (digits.length > 4) out += `-${dd}`;
  return out;
}

// 생년월일 형식/범위 검증 (선택 입력 → 값이 있을 때만)
export function birthError(v: string): string | null {
  const t = v.trim();
  if (t.length === 0) return null;
  const m = /^(\d{2})-(\d{2})-(\d{2})$/.exec(t);
  if (!m) return "생년월일을 YY-MM-DD 형식으로 입력해 주세요.";
  const mm = Number(m[2]);
  const dd = Number(m[3]);
  if (mm < 1 || mm > 12 || dd < 1 || dd > 31) {
    return "생년월일을 YY-MM-DD 형식으로 입력해 주세요.";
  }
  return null;
}

// 리뷰 본문: 공백 포함 10~50자
export const REVIEW_MIN = 10;
export const REVIEW_MAX = 50;
export function reviewLengthError(v: string): string | null {
  // 공백만 입력한 경우 발행 불가
  if (v.trim().length === 0) return `${REVIEW_MIN}자 이상으로 입력해 주세요.`;
  const len = v.length;
  if (len < REVIEW_MIN) return `${REVIEW_MIN}자 이상으로 입력해 주세요.`;
  if (len > REVIEW_MAX) return `${REVIEW_MAX}자 이내로 입력해주세요.`;
  return null;
}

// 이메일 형식 (로컬@도메인.TLD, TLD 2자 이상)
export function isValidEmail(v: string): boolean {
  return /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)*\.[a-zA-Z]{2,}$/.test(
    v.trim()
  );
}

export const GENDER_LABEL: Record<Gender, string> = {
  female: "여성",
  male: "남성",
  none: "선택안함",
};
