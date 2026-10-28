# nook (pm6-mvp-team-6)

가고 싶은 곳·다녀온 곳을 **지도에 별로 기록**하고, 장소마다 리뷰를 남겨 공유하는 위치 기반 기록 SNS MVP.

## 기술 스택

- Next.js 14 (App Router) + TypeScript
- Tailwind CSS (Pretendard)
- Supabase (Postgres + Storage)
- 카카오 지도 JS SDK + 로컬 키워드 검색

## 화면 (기능명세서 기준)

| 화면 | 경로 |
| --- | --- |
| 로그인 진입 | `/login` |
| 이메일 로그인 | `/login/email` |
| 이메일 회원가입 | `/signup/email` |
| 프로필 설정 | `/signup/profile` |
| 지도(홈) | `/map` |
| 기록(리뷰) 작성 | `/review/new` |
| 기록 수정 | `/review/edit/[id]` |
| 프로필 | `/profile` |
| 설정 | `/profile/settings` |

## 핵심 개념: 별(star)

- **빈 별** — 저장 안 한 장소 (바텀시트 토글 기본값)
- **회색 별** — 북마크(단순 저장)한 장소
- **채운 별(노랑)** — 리뷰를 작성한 장소

지도에는 내가 저장한 장소(회색/채운 별)가 마커로 표시됩니다.

## 셋업

1. 의존성 설치

```bash
npm install
```

2. 환경변수 — `.env.local`

```
NEXT_PUBLIC_SUPABASE_URL=https://<project>.supabase.co
NEXT_PUBLIC_SUPABASE_ANON_KEY=<anon/publishable key>
NEXT_PUBLIC_KAKAO_MAP_KEY=<카카오 JavaScript 키>
```

> **카카오 키 발급**: [Kakao Developers](https://developers.kakao.com) → 애플리케이션 추가 →
> JavaScript 키 복사 → [앱 설정 > 플랫폼 > Web]에 사이트 도메인 등록
> (`http://localhost:3300`, 배포 후 Vercel 도메인). 지도 SDK + 로컬 검색에 사용됩니다.

3. Supabase SQL Editor 에서 순서대로 실행
   - `supabase/schema.sql` (테이블 + RLS + Storage 버킷 `images`)
   - `supabase/seed_content.sql` (데모 유저 3명 + 서울 장소 6곳 + 리뷰)

4. 개발 서버

```bash
npm run dev   # http://localhost:3300
```

## 데모 로그인

- 이메일: `nyam_demo@nyam.io`
- 로그인 화면 → `로그인` → 이메일 입력 → `인증 메일 전송` → `인증 완료 (데모)`

## MVP 구현 메모

- **인증(패스워드리스)**: 데모용 시뮬레이션 — 실제 메일 발송 대신 화면의 `인증 완료 (데모)` 버튼이 매직링크 클릭을 대신함. 세션은 localStorage. 운영 시 Supabase Auth(매직링크/OTP) 전환 권장.
- **지도/검색**: 카카오 로컬 키워드 검색 결과(상호명/카테고리/주소/전화/링크/좌표)를 `places` 테이블에 upsert 후 북마크·리뷰와 연결.
- **이미지**: 파일 선택 → Supabase Storage 업로드 → public URL 저장 (리뷰 최대 3장, 5MB 제한).
- **리뷰**: 공백 포함 10~50자. 작성/수정/삭제, 좋아요.
- **닉네임**: 2~12자, 중복 체크.
- **그룹맵**: 기능명세 v2 범위로 분리 (이번 MVP 미포함).
- RLS는 데모용 anon 전체 허용. 운영 시 사용자별 정책으로 강화 필요.

## Vercel 배포

Environment Variables 에 `NEXT_PUBLIC_SUPABASE_URL`, `NEXT_PUBLIC_SUPABASE_ANON_KEY`,
`NEXT_PUBLIC_KAKAO_MAP_KEY` 등록 + 카카오 플랫폼에 배포 도메인 추가 후 재배포.
