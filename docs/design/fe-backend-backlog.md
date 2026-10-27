# FE 전체 범위 백엔드 할 일 (초안)

> 2026-10-27 PM 결정 "프론트 연결 범위 = 원본 화면 전체"에 맞춰 다시 짠 목록. **초안 — 순서·일정은 사용자 결정.**
> 기준: 원본 `C:\Projects\nook\src\lib`의 데이터 함수 30개(store.ts 24 + data/ 4 + 이미지 표시·토큰 저장 2) ↔ 현재 API, 그리고 각 함수를 부르는 화면.
> *(FE-1 Claude 작성, 사용자 요청)*

## 1. 요약

| 구분 | 개수 | 내용 |
|---|---|---|
| ✅ 이미 있음 | 17 | 인증·장소·북마크·리뷰 CRUD·좋아요·그룹 생성/합류/탈퇴 — FE 교체만 |
| 🟡 있지만 응답 보강 필요 | 2 | `isBookmarked`(장소 상세에 북마크 여부 없음), `createProfile`(성별·생일·아바타) |
| 🆕 새 API | 9 | 프로필 5(`getStarCounts`는 A안이면 FE만) · 탈퇴 1 · 그룹 3 |
| 🔧 API 밖 결정 | 2 | 이미지 표시(선행 과제 3), 토큰 저장 위치(선행 과제 4) |
| ✅ 범위 질문(답 받음) | 3 | 성별·생일(가입 시 입력), 아바타(직접 설정·없으면 기본), 그룹 색(랜덤 배정) → 스키마 컬럼 추가 |

## 2. 함수별 대조

크기: S(반나절 이하) · M(하루 안팎) · L(이틀 이상, 스키마 변경 포함) — 감으로 잡은 상대 크기, 측정값 아님.

### ✅ 이미 있음 — FE 교체만

| 원본 함수 | 부르는 화면 | 현재 API | 메모 |
|---|---|---|---|
| `sendMagicLink` | `EmailAuthFlow` | `POST /auth/magic-link` (AU-01·02) | |
| `signOut` | 설정 | 없음(클라이언트가 토큰 버림, AU-03) | |
| `getCurrentUser` | 가드·첫 화면 | `GET /me` (AU-04) | 아바타가 들어오면 응답 필드 추가 |
| `upsertPlace` | 장소 선택 | `POST /places` (PL-01) | |
| `getPlace` | `PlaceSheet` | `GET /places/{id}` (PL-02) | |
| `toggleBookmark` | `PlaceSheet` | `PUT`·`DELETE /places/{id}/bookmark` (BM-01·02) | 토글 → 명시적 두 동작 |
| `getMySavedPlaces` | 지도 | `GET /me/places` (BM-03) | |
| `getReviewsByPlace` | `PlaceSheet` | `GET /places/{id}/reviews` (PL-02) | 원본은 전부, 우리는 커서 페이지 |
| `getReview` | 리뷰 수정 | `GET /reviews/{id}` (RV-04) | 작성자만 |
| `hasReviewedPlaceToday` | 리뷰 작성 | 없음 — `POST /reviews` 409 `Review.AlreadyReviewedToday`로 대체 | FE가 사전 조회 대신 409 처리 |
| `createReview` | 리뷰 작성 | `POST /reviews` (RV-01, multipart) | 원본은 `uploadImage` 후 URL 저장 → 우리는 한 요청 |
| `updateReview` | 리뷰 수정 | `PUT /reviews/{id}` (RV-02) | `version` 필수 |
| `deleteReview` | `ReviewCard` | `DELETE /reviews/{id}` (RV-03) | |
| `toggleLike` | `ReviewCard` | `PUT`·`DELETE /reviews/{id}/like` (LK-01) | |
| `createGroup` | `GroupControl` | `POST /groups` (GR-02) | |
| `joinByInviteCode` | `GroupControl` | `POST /groups/join` (GR-01) | |
| `leaveGroup` | `GroupControl` | `DELETE /groups/{id}/membership` (GR-04) | |

### 🟡 있지만 보강 필요

| 원본 함수 | 부르는 화면 | 빈 곳 | 할 일 | 크기 |
|---|---|---|---|---|
| `isBookmarked` | `PlaceSheet`(북마크 버튼 상태) | `GET /places/{id}`의 `Star`는 리뷰가 있으면 `fill` — 북마크 여부가 가려진다 | `PlaceDetailView`에 `IsBookmarked` 추가 | S |
| `createProfile` | 가입 프로필 폼 | 원본 폼은 닉네임 + **성별(필수)·생일·아바타**. 우리 consume은 닉네임만 | consume에 성별·생일 추가 + `users` 컬럼. 아바타는 가입 직후 PF-01로 따로(2026-10-27 결정) — consume은 JSON 그대로 | S |

### 🆕 새 API

| 원본 함수 | 부르는 화면 | 제안 API | 유스케이스 | 결정할 것 | 크기 |
|---|---|---|---|---|---|
| `getMyReviews` | 프로필 피드 | `GET /me/reviews?cursor=` | PF-04 | 페이지 방식(PL-02와 같은 키셋 재사용 여부) | M |
| `getStarCounts` | 프로필 | (A) API 없음 — FE가 `GET /me/places`의 `star`로 센다 / (B) `GET /me/star-counts` | PF-03 | A면 API 0개. B는 데이터가 커질 때 | S |
| `nicknameExists` | 가입·프로필 수정 | `GET /users/nickname-availability?nickname=` | (새) | 필요 여부 — 저장 시 409 `User.NicknameTaken`이 최종 판정. 미리 확인은 UX용이고, 닉네임 존재 여부를 누구에게 노출할지 | S |
| `updateProfile` | `ProfileEditModal` | `PATCH /me` (닉네임·아바타) | PF-01 | 동시 변경은 UNIQUE가 판정(R-US-02) | M |
| `uploadImage`(아바타) | 가입·`ProfileEditModal` | `PATCH /me` multipart 또는 `PUT /me/avatar` | PF-01 | 저장소(🔧-1), 교체된 옛 아바타 정리(리뷰 이미지 고아 정리와 같은 문제) | M |
| `deleteAccount` | 설정 | `DELETE /me` | PF-02 | ⓪ (가) 확정. ①잠금·②남은 토큰·좋아요 삭제 여부(PM 미답)·PL-02 작성자 NULL·RV-02/03 권한·마이그레이션 | **L** |
| `getMyGroups` | 지도(첫 로드) | `GET /me/groups` | (새, GR-06?) | `memberCount` N+1 그대로 vs 한 쿼리, 초대 코드 노출 | M |
| `renameGroup` | `GroupControl` | `PATCH /groups/{id}` | GR-03 | 방장 없음(ADR-004 결정 6) → **멤버 누구나** 바꿀 수 있나 | S |
| `getGroupSavedPlaces` | 지도(그룹 모드) | `GET /groups/{id}/places` | GR-05 | 멤버만 조회(403/404). 장소별로 묶은 사실(리뷰 수·첫 리뷰어 슬롯·북마크 수·단독 북마커 슬롯)을 주고 그리기 규칙은 FE — 아래 "그룹맵 표시 정책" | M |

### 🔧 API 밖 결정 (기존 선행 과제)

| 항목 | 막는 것 | 결정 | 크기 |
|---|---|---|---|
| 이미지 표시(선행 과제 3) | 리뷰 이미지 표시, 아바타 | **A 확정(2026-10-27 사용자 결정)**: 로컬 디스크 `IReviewImageStorage` 구현 + `GET /images/{key}`(익명 허용 — BFF라 `<img>`에 Bearer 없음, 키는 128비트 랜덤). 키는 불신 입력 → 형식 재검증·경로 조작 방어. 응답에 서버가 만든 `Url`(키→URL 변환 한 곳, erd.md §3) → DEP에서 S3로 구현체만 교체 | M |
| 토큰 저장 위치(선행 과제 4) | FE 인증 교체 전체 | **③ Next.js BFF 확정(2026-10-27 사용자 결정)** — 브라우저↔Next는 httpOnly 쿠키, Next→C# API는 Bearer 그대로(백엔드 변경 없음), 원본 서버 컴포넌트 가드 유지. 결과: `<img>`는 Bearer를 못 보내므로 이미지 엔드포인트는 익명 허용 | FE |

### ✅ 범위 질문 — 답 받음(2026-10-27). D3-2 "MVP 밖"으로 뺀 컬럼 3개를 다시 넣는다 → ADR-002 "원본과 다른 점" 갱신 대상

1. **성별·생일 = 회원가입 때 사용자가 입력.** 원본: `gender` `'female'|'male'|'none'`(폼에서 필수), `birth` `'YY-MM-DD'`(선택). **생일은 `date`로 저장(2026-10-27 결정)** — 원본 문자열 `YY`는 세기가 모호하다. **FE가 `YYYY-MM-DD`로 바꿔 보낸다**(2026-10-27 사용자 결정 — 세기 판단 규칙은 FE 한 곳, 서버는 표준 날짜만 받는다). 남은 것: 성별 필수를 DB `NOT NULL`·CHECK로 이중화할지. 영향: AU-01 consume 요청 필드, `User`·`users` 스키마
2. **아바타 = 프로필 사진. 사용자가 직접 설정, 안 하면 기본 이미지.** 출처: 원본 `users.avatar_url`(`schema.sql:19`), 업로드 2곳(가입 폼·`ProfileEditModal`), 표시 3곳(프로필·`ReviewEditor`·**`ReviewCard` 작성자**). 참고: 원본 기능명세서 121행은 "MVP에서는 기본 이미지" — 이번 답으로 업로드까지 범위. 영향: `users.avatar_key` NULL 허용(NULL = 기본 이미지는 FE 몫), 이미지 저장소(🔧-1) 키 규칙에 `avatars/...` prefix, PL-02 응답에 작성자 아바타 — **탈퇴 회원 리뷰는 아바타도 없음**(연결 불가, PM 3번)
3. **그룹 색 — PM 정의(2026-10-27):** 그룹맵 = 그룹 멤버들의 리뷰 작성 장소(별)를 함께 보는 지도. 정원 4명에 **랜덤 색 4개**, **멤버마다 그룹 안에서 고유한 색**, 모든 멤버에게 같은 색으로 보인다(유저 1의 리뷰 = 빨간 별).
   - 해석: 색은 (그룹, 회원)마다 하나 → `group_members.color`에 저장(새로고침·다른 멤버 화면에서도 같아야 함). 같은 회원이 다른 그룹에서는 다른 색일 수 있다
   - 그룹 안 중복 금지: 합류(GR-01)는 이미 `groups` 행을 잠그고 인원을 센다(ADR-010) → 같은 잠금 안에서 "안 쓰인 색 중 하나"를 고르면 동시 합류도 안전. `(group_id, color)` UNIQUE로 이중화 가능. 탈퇴하면 그 색이 비고 다음 합류자가 쓸 수 있다
   - ~~PM 확인 ① 색 출처~~ → **색은 디자인이 확정되면 정한다(2026-10-27 PM).** 그래서 DB에는 색 값(hex)이 아니라 **슬롯 번호**(`color_slot smallint`, 0~3 = 정원 4)를 저장하고, 슬롯 → 색 변환은 FE 한 곳에서(제안). 색이 바뀌어도 마이그레이션이 없다. "랜덤" = 그룹 안에서 안 쓰인 슬롯 중 랜덤
   - ~~PM 확인 ② 그룹맵에 보이는 것~~ → 리뷰와 북마크 모두, 아래 정책(2026-10-27 PM)

#### 그룹맵 표시 정책 (PM, 2026-10-27) → GR-05 응답 설계

| 장소에 있는 것(현재 멤버 기준) | 표시 | 서버가 줄 사실 |
|---|---|---|
| 리뷰한 멤버 1명 | 그 리뷰어의 색 별 | 리뷰 멤버 수 1, 첫 리뷰어 슬롯 |
| 리뷰한 멤버 2명 이상 | **가장 먼저 리뷰한 사람**의 색 별 + 안쪽에 리뷰한 멤버 수 | 리뷰 멤버 수, 첫 리뷰어 슬롯 |
| 리뷰 + 북마크 | 리뷰 규칙을 따른다(리뷰 우선) | 위와 같음(북마크 수는 줘도 FE가 안 씀) |
| 북마크 1개 | 기존 회색 별 + 그 멤버 색 **외곽선** | 북마크 수 1, 북마커 슬롯 |
| 북마크 2개 이상 | 진한 회색(#4A4A4A) 별 + 안쪽에 북마크 수 | 북마크 수 |

- 응답은 **장소마다 1행**(원본은 (장소, 회원)마다 1행 — 바뀜). 그리기 규칙(색·숫자 노출 조건)은 FE, 서버는 집계한 사실만 → 디자인이 바뀌어도 API 그대로
- 남은 세부(사용자 결정, GR-05 유스케이스 때):
  - ~~"중첩된 리뷰 수"~~ → **리뷰한 멤버 수**(2026-10-27 사용자 결정). 한 멤버가 같은 장소에 날짜를 달리해 리뷰를 여러 개 써도 1로 센다(`COUNT(DISTINCT user_id)`)
  - "가장 먼저 리뷰한 사람" = 현재 멤버 중 `created_at`이 가장 이른 리뷰의 작성자. 그 멤버가 그룹을 나가면 다음 사람으로 바뀐다(현재 멤버 기준이면 자연히 그렇게 됨). 같은 시각 동률은 리뷰 id로
  - ~~가입 전 리뷰 포함 여부~~ → **포함**(2026-10-27 사용자 결정, 원본과 같음) — 현재 멤버의 리뷰·북마크 전부
  - 탈퇴 회원 리뷰(작성자 NULL)는 멤버가 아니므로 제외 — 연결 불가 정책과도 맞음

## 3. 의존 관계와 제안 순서

```
토큰 저장(🔧-2) ─→ CORS ─→ FE 인증 교체 ─→ 모든 화면
이미지 표시(🔧-1) ─→ 리뷰 이미지 화면 · 아바타(PF-01·가입 폼·PL-02 작성자)
PL-02 작성자 NULL 처리 ─→ PF-02
```

| 순서 | 묶음 | 포함 | 이유 |
|---|---|---|---|
| 1 | 결정 묶음 | 🔧-1·🔧-2 결정, 범위 3개의 세부(생일 형식·색 해석·가입 아바타 경로) | 아래 전부의 전제. 코드 없이 결정만 |
| 2 | 지금 화면이 바로 쓰는 보강 | `IsBookmarked` | S, 지도·장소 시트 연결 직후 필요 |
| 3 | FE 인증·지도·리뷰 연결 | 기존 진행 순서 그대로 | ✅ 17개를 화면에 붙이는 일 |
| 4 | 이미지 표시 구현 | 🔧-1 | 리뷰 화면 완성 + 아바타 전제 |
| 5 | 프로필 | PF-04 · PF-03(A안이면 FE만) · PF-01(+아바타) · 닉네임 확인 | 같은 화면 묶음 |
| 6 | 그룹 | `group_members.color` + GR-01·02 색 배정 · `GET /me/groups` · GR-03 · GR-05 | 지도 그룹 모드 묶음 |
| 7 | 탈퇴 | PF-02 | 가장 크다(스키마·권한·응답). 남은 PM 질문 2개 답 필요 |

## 4. 일정 재검토용 메모

- 새 API 9개 + 보강 2개 + 이미지 표시 1개 = 백엔드 작업 12건. 크기 합: S×5 · M×6 · L×1
- 범위 답(2026-10-27)으로 늘어난 것: `users` 컬럼 3개(성별·생일·아바타 키) + `group_members.color` → 마이그레이션 1~2개, GR-01·02 색 배정, PL-02 응답에 작성자 아바타
- 원래 일정(`개발_프로세스.md` §5)의 FE 칸은 데모 범위 전제였다 — 이 표로 다시 잡는다
