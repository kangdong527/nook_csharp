# ERD · DB 스키마

> D3 산출물. 도메인 모델(`domain.md`)을 PostgreSQL 테이블로 옮긴다.
> 테이블마다 "이 테이블을 읽는 쿼리"를 먼저 적고, 그 쿼리가 PK·인덱스를 결정하게 한다.
>
> - **타입**: PostgreSQL 타입 (`uuid`, `text`, `varchar(n)`, `timestamptz`, `date`, ...)
> - **ON DELETE**: `RESTRICT` / `CASCADE` / `SET NULL` / `NO ACTION` 중 하나와 이유
> - **근거**: 규칙 ID(`R-영역-두자리`) 또는 ADR 번호

## 1. 다이어그램

```mermaid
erDiagram
```

## 2. 테이블

### users

관련 규칙: R-US-01 · R-US-02 · R-US-03 · R-GR-04

[이 테이블을 읽는 쿼리]

| # | 쿼리 | 쓰는 곳 |
|---|---|---|
| Q1 | `WHERE id = ?` | 인증 토큰의 사용자 ID 확인(GR-01 3a), 프로필 조회 |
| Q2 | `WHERE id = ? FOR UPDATE` (잠금 강도는 D6) | 그룹 합류·생성 시 사용자별 직렬화(R-GR-04) |
| Q3 | `WHERE lower(nickname) = lower(?)` | 가입·닉네임 변경 시 중복 미리 검사(R-US-02) |
| Q4 | `WHERE lower(email) = lower(?)` | 가입·로그인 시 이메일로 회원 찾기(R-US-03) |
| Q5 | 자식 테이블 FK 검사 `WHERE id = ?` | bookmarks·reviews·likes·group_members INSERT 시 |


| 컬럼 | 타입 | NULL | 기본값 | 비고 |
|---|---|---|---|---|
| id | `uuid` | NOT NULL | 없음 | 앱이 `UserId.New()`(UUID v7)로 발급(ADR-006). DB 기본값을 두면 발급 주체가 둘이 된다 |
| nickname | `varchar(12)` | NOT NULL | 없음 | 입력 표기 그대로 저장(R-US-02). PostgreSQL `varchar(n)`은 글자(코드 포인트) 단위라 `Nickname.MaxLength`(rune 수)와 단위가 같다 |
| email | `varchar(254)` | NOT NULL | 없음 | 입력 표기 그대로 저장(R-US-03). `Email.MaxLength`와 같은 상한 |

- 시각 컬럼(`created_at`)은 두지 않는다. 가입일을 쓰는 규칙·화면이 없다(ADR-007 "결과", `User` 주석)
- 인증 관련 컬럼(비밀번호 해시 등)은 AU-01 인증 방식이 D5에서 정해진 뒤 추가한다


| 제약·인덱스 | 정의 | 받는 쿼리 / 막는 것 | 근거 |
|---|---|---|---|
| PK | `pk_users (id)` | Q1·Q2·Q5. 자식 테이블 FK가 가리키는 대상 | ADR-005, ADR-006 |
| UNIQUE (식) | `ux_users_nickname_lower ON users (lower(nickname))` | Q3. 동시 가입·변경으로 `Nook`/`nook`이 공존하는 것을 막는다(앱 미리 검사는 TOCTOU라 최종 보장은 이 인덱스) | R-US-02 |
| UNIQUE (식) | `ux_users_email_lower ON users (lower(email))` | Q4. 대소문자만 다른 이메일 중복 가입을 막는다 | R-US-03 |
| CHECK | `ck_users_nickname`: `char_length(nickname) >= 2 AND nickname ~ '^[가-힣A-Za-z0-9]+$' AND nickname IS NFC NORMALIZED` | 앱을 거치지 않은 쓰기(SQL 직접 수정, 시드 스크립트)로 형식이 깨진 닉네임. 공백을 허용하지 않으므로 trim도 함께 보장된다. 상한 12는 `varchar(12)`가 맡는다 | R-US-01, CLAUDE.md "DB CHECK로 이중화" |
| CHECK | `ck_users_email`: `email ~ '^[^@[:space:]]+@[^@[:space:]]+$'` | 빈 문자열, `@` 0개·2개 이상, `@` 앞뒤가 빔, 공백 포함 | R-US-03 |
| FK (나가는 것) | 없음 | users는 다른 테이블을 참조하지 않는다 | |
| FK (들어오는 것) | bookmarks·reviews·likes·group_members `.user_id → users` — `ON DELETE`는 **각 자식 테이블에서** 정한다 | 탈퇴(PF-02) 때 users 행 DELETE가 자식마다 FK 검사를 한다. PostgreSQL은 FK 쪽 인덱스를 자동으로 만들지 않으므로 자식 테이블마다 `user_id`로 시작하는 인덱스(PK 포함)가 있는지 확인한다 | ADR-005, PF-02 |

메모
- R-GR-04 잠금은 PK(Q2)로 행 하나만 잠그므로 추가 인덱스가 필요 없다. 잠금 강도(`FOR UPDATE` vs `FOR NO KEY UPDATE`)와 잠금 순서(사용자 → 그룹)는 D6
- 중복 판단을 앱에서 미리 할 때 쿼리를 반드시 `lower(nickname) = lower(?)` 모양으로 써야 인덱스를 탄다. `nickname = ?`는 이 인덱스를 못 쓴다
  => 예상: PostgreSQL 16 EXPLAIN: 전자는 `Index Cond`, 후자는 `Seq Scan` + `Filter`
- `btrim(x)`은 기본적으로 ASCII 공백(`' '`)만 지운다. 탭·전각 공백 등 Unicode 공백만으로 된 값은 places CHECK를 통과한다 — 앱 우회 쓰기에서 빈 값만 막는 최소 이중화로 두고, Unicode 공백 처리는 Domain(`string.Trim()`)이 맡는다
- `lower()`는 DB의 `LC_CTYPE`·collation을 따르고 C# `ToLowerInvariant()`는 Unicode 전체를 바꾼다. 닉네임은 화이트리스트(한글·ASCII)라 결과가 같지만, 이메일은 비ASCII 문자에서 다를 수 있다 → DB를 띄운 뒤 `SELECT lower('ÄÖ')`와 C# 결과를 비교해 확인(미측정)


### places

관련 규칙: R-PL-01 · R-PL-02

[이 테이블을 읽는 쿼리]

| # | 쿼리 | 쓰는 곳 |
|---|---|---|
| Q1 | `WHERE kakao_place_id = ?` | 검색 장소의 카카오 장소 ID 확인(PL-01 2) |
| Q2 | 자식 테이블 FK 검사 `WHERE id = ?` | 장소 상세(PL-02), 장소 존재 확인(BM-01 2b·BM-02 2b·RV-01 2a), 자식 테이블 FK 검사 |
| Q3 | `INSERT ... ON CONFLICT (kakao_place_id) DO NOTHING` → 0행이면 Q1 | 동시 등록 시 1건만 생성(PL-01 3a). 방식 확정은 D5 |

| 컬럼 | 타입 | NULL | 기본값 | 비고 |
|---|---|---|---|---|
| id | `uuid` | NOT NULL | 없음 | 앱이 `PlaceId.New()`(UUID v7)로 발급(ADR-006). DB 기본값을 두면 발급 주체가 둘이 된다 |
| kakao_place_id | `varchar(64)` | NOT NULL | 없음 | 카카오 지도 검색 후 받아오는 카카오 쪽 고유 장소 ID |
| name | `varchar(100)` | NOT NULL | 없음 | 카카오 지도 검색 후 받아오는 카카오 장소 표시명 |
| address | `varchar(200)` | NOT NULL | 없음 | 카카오 지도 검색 후 받아오는 카카오 장소 주소 |
| latitude | `double precision` | NOT NULL | 없음 | 카카오 지도 검색 후 받아오는 카카오 장소 위도 |
| longitude | `double precision` | NOT NULL | 없음 | 카카오 지도 검색 후 받아오는 카카오 장소 경도 |

| 제약·인덱스 | 정의 | 받는 쿼리 / 막는 것 | 근거 |
|---|---|---|---|
| PK | `pk_places (id)` | Q2. 자식 테이블 FK가 가리키는 대상 | PL-02, ADR-006 |
| UNIQUE | `ux_places_kakao_place_id ON places (kakao_place_id)` | Q1. 카카오 장소 ID는 장소마다 유일하다 | R-PL-01 |
| CHECK | `ck_places_kakao_place_id`: `btrim(kakao_place_id) <> ''` | 빈 값·공백뿐인 카카오 장소 ID. `KakaoPlaceId` 주석의 "DB 제약과 같은 조건"을 실제로 만든다 | R-PL-01, `KakaoPlaceId.Create` |
| CHECK (식) | `ck_places_name`: `btrim(name) <> ''` | ''는 0자라서 그대로 통과하기 때문에 name의 "필수"(하한 1자)인지 검증 | `Place.Create`(`PlaceErrors.NameLength`) |
| CHECK | `ck_places_latitude`: `latitude >= -90 AND latitude <= 90` | 물리적 위도 범위(지구) 안인지 검증 | R-PL-02 | 
| CHECK | `ck_places_longitude`: `longitude >= -180 AND longitude <= 180` | 물리적 경도 범위(지구) 안인지 검증 | R-PL-02 | 
| FK (나가는 것) | 없음 | places는 다른 테이블을 참조하지 않는다 | |
| FK (들어오는 것) | bookmarks·reviews `.place_id → places` — `ON DELETE`는 **각 자식 테이블에서** 정한다 | 둘 다 `RESTRICT`. 장소를 삭제하는 유스케이스가 없으므로 장소 삭제는 실수로 보고 거부한다(bookmarks·reviews FK 행) | ADR-005 |


### bookmarks

관련 규칙: R-BM-01 · R-BM-02 · R-STAR

이 테이블을 읽는 쿼리

| # | 쿼리 | 쓰는 곳 |
|---|---|---|
| Q1 | `WHERE user_id = ? AND place_id = ?` | 이 장소를 북마크했는가: BM-01·BM-02 2단계, 장소 하나의 별 상태(R-STAR) |
| Q2 | `INSERT ... ON CONFLICT (user_id, place_id) DO NOTHING` / `DELETE ... WHERE user_id = ? AND place_id = ?` | 저장·해제(BM-01·BM-02), 동시 요청(각 1b·1c, R-BM-02) |
| Q3 | `WHERE user_id = ?` | 내 북마크 목록: BM-03 내 지도, PF-03 내 별 개수, PF-02 탈퇴(CASCADE 삭제) |
| Q4 | `WHERE place_id = ?` | 장소 삭제 시 FK 검사뿐. 장소를 삭제하는 유스케이스는 없다 |

| 컬럼 | 타입 | NULL | 기본값 | 비고 |
|---|---|---|---|---|
| user_id | `uuid` | NOT NULL | 없음 | 북마크한 회원. `Bookmark.UserId` |
| place_id | `uuid` | NOT NULL | 없음 | 북마크한 장소. `Bookmark.PlaceId` |
| created_at | `timestamptz` | NOT NULL | 없음 | 앱이 `TimeProvider`로 정해 넣는다(ADR-007). DB 기본값을 두면 시각의 출처가 둘이 된다 |

- "북마크 여부" 컬럼은 두지 않는다. **행이 있으면 북마크한 것, 없으면 안 한 것**이다
  - 여부 컬럼(`is_bookmarked boolean`)을 두면 "행 없음 / false / true" 세 상태가 생기고, 앞의 둘이 같은 뜻이 된다. 해제가 DELETE가 아니라 UPDATE가 되어 false 행이 계속 쌓인다
  - R-STAR는 `EXISTS (Q1)`로 계산한다. 별 상태 자체를 저장할지는 별도 미결 결정(§3)이다

| 제약·인덱스 | 정의 | 받는 쿼리 / 막는 것 | 근거 |
|---|---|---|---|
| PK | `pk_bookmarks (user_id, place_id)` | Q1·Q2·Q3. 같은 (회원, 장소) 두 번째 행을 막는다. 동시 저장은 이 인덱스가 직렬화한다 | R-BM-01 · R-BM-02, ADR-005(복합 PK) |
| FK | `fk_bookmarks_users`: `user_id → users(id) ON DELETE CASCADE` | 탈퇴 시 users 행 DELETE 한 문장으로 북마크까지 함께 지워진다. 앱이 따로 지우다 일부만 커밋되는 일(원본 부분 커밋 결함)이 없다. CASCADE가 실행하는 `WHERE user_id = ?`는 PK 앞 컬럼이라 인덱스를 탄다 | PF-02 "모든 데이터 삭제", ADR-005 |
| FK | `fk_bookmarks_places`: `place_id → places(id) ON DELETE RESTRICT` | 북마크가 남은 장소의 삭제를 거부한다. 장소는 여러 회원이 공유하는 참조 데이터이고 삭제 유스케이스가 없으므로, 장소 삭제는 실수로 보고 회원 데이터가 조용히 사라지는 것을 막는다 | ADR-005 |
| 보조 인덱스 | 없음 (`place_id` 단독) | Q4는 장소 삭제 때만 실행되는데 그 유스케이스가 없다. 인덱스는 INSERT마다 갱신 비용만 든다. 장소별 북마크 수 같은 조회가 생기면 그때 `(place_id)`를 추가한다 | D8에서 측정 후 재검토 |
| CHECK | 없음 | 두 ID는 FK가, 중복은 PK가 막는다. `created_at`의 UTC는 Domain `TimeGuard`와 Npgsql이 보장한다 | ADR-007 |


### reviews

관련 규칙: R-RV-01 · R-RV-02 · R-RV-03 · R-STAR

[이 테이블을 읽는 쿼리]

| # | 쿼리 | 쓰는 곳 |
|---|---|---|
| Q1 | `WHERE id = ?` | 리뷰 수정·삭제(RV-02·RV-03, 작성자 확인 포함), 좋아요 대상 확인(LK-01), 자식 테이블 FK 검사 |
| Q2 | `WHERE place_id = ? ORDER BY created_at DESC` | 장소 상세의 리뷰 목록, 최신순(PL-02) |
| Q3 | `WHERE user_id = ? ORDER BY created_at DESC` (Keyset은 D8) | 내 리뷰 피드(PF-04) |
| Q4 | `WHERE user_id = ? AND place_id = ?` (`EXISTS`) | 별 상태 `fill` 판정(R-STAR), 리뷰 삭제 후 내 리뷰가 0개인지(RV-03), 내 지도·별 개수(BM-03·PF-03) |
| Q5 | `INSERT` → `(user_id, place_id, review_date)` UNIQUE 위반이면 거절 | 같은 장소 하루 1개(R-RV-03), 동시 작성 포함 |
| Q6 | `WHERE user_id = ?` (CASCADE 삭제) | 회원 탈퇴(PF-02) |

| 컬럼 | 타입 | NULL | 기본값 | 비고 |
|---|---|---|---|---|
| id | `uuid` | NOT NULL | 없음 | 앱이 `ReviewId.New()`(UUID v7)로 발급(ADR-006) |
| user_id | `uuid` | NOT NULL | 없음 | 작성자. `Review.UserId` |
| place_id | `uuid` | NOT NULL | 없음 | 리뷰 대상 장소. `Review.PlaceId`. 수정으로 바뀌지 않는다(RV-02) |
| content | `varchar(50)` | NOT NULL | 없음 | trim된 본문(`ReviewContent.Value`). 상한은 `ReviewContent.MaxLength`와 같다 |
| created_at | `timestamptz` | NOT NULL | 없음 | 앱이 `TimeProvider`로 정해 넣는다(ADR-007) |
| review_date | `date` | NOT NULL | 없음 | R-RV-03 기준 날짜. Domain이 `created_at`의 UTC 날짜로 계산한 `Review.ReviewDate`를 그대로 저장한다 |

- 수정 시각(`updated_at`)은 두지 않는다. `Review`에 없고, 수정 시각을 쓰는 규칙·화면이 없다(ADR-007 기준)
- 좋아요 수 컬럼은 두지 않는다. 카운터를 저장할지 likes에서 셀지는 D9(LK-01 "좋아요 수 갱신 유실")
- 이미지 장수(R-RV-02)는 행 하나로 판단할 수 없어 이 테이블에 제약이 없다 → review_images 표

| 제약·인덱스 | 정의 | 받는 쿼리 / 막는 것 | 근거 |
|---|---|---|---|
| PK | `pk_reviews (id)` | Q1. review_images·likes FK가 가리키는 대상 | ADR-006 |
| UNIQUE | `ux_reviews_user_place_date (user_id, place_id, review_date)` | Q5. 같은 회원·장소·날짜의 두 번째 리뷰를 막는다(동시 작성도 이 인덱스가 직렬화). 앞 컬럼이 `user_id`, `(user_id, place_id)`라서 Q4·Q6과 Q3의 필터도 받는다 | R-RV-03 |
| 보조 인덱스 | `ix_reviews_place_created (place_id, created_at DESC)` | Q2. 장소 상세마다 실행되는 조회라 정렬까지 인덱스로 받는다. places 삭제 시 RESTRICT 검사(`WHERE place_id = ?`)도 받는다 | PL-02 |
| FK | `fk_reviews_users`: `user_id → users(id) ON DELETE CASCADE` | 탈퇴 시 users 행 DELETE 한 문장으로 리뷰까지 지워진다(bookmarks와 같은 근거). 리뷰가 지워지면 review_images·likes도 아래 CASCADE로 따라 지워진다. 스토리지 이미지는 트랜잭션 밖 정리 배치가 맡는다 | PF-02, ADR-005 |
| FK | `fk_reviews_places`: `place_id → places(id) ON DELETE RESTRICT` | 리뷰가 남은 장소의 삭제를 거부한다(bookmarks와 같은 근거) | ADR-005 |
| CHECK | `ck_reviews_content`: `char_length(btrim(content)) >= 10` | 앱을 거치지 않은 쓰기로 10자 미만 본문이 들어오는 것. 상한 50은 `varchar(50)`이 맡는다. `btrim`은 ASCII 공백만 지우는 최소 이중화(users 메모와 같음) | R-RV-01 |

메모
- `review_date`와 `created_at`이 어긋나지 않게 하는 CHECK(`review_date = (created_at AT TIME ZONE 'UTC')::date`)는 두지 않는다. 표현식 인덱스를 기각한 이유와 같다: 같은 계산이 SQL에도 생겨 규칙이 두 곳이 된다(progress D2 결정)
- Q3의 정렬·Keyset 페이지네이션용 인덱스(`(user_id, created_at DESC, id)` 등)는 D8에서 `EXPLAIN (ANALYZE, BUFFERS)`로 Before를 재고 정한다


### review_images

관련 규칙: R-RV-02 (루트가 지킴)

[이 테이블을 읽는 쿼리]

| # | 쿼리 | 쓰는 곳 |
|---|---|---|
| Q1 | `WHERE review_id = ?` / `WHERE review_id IN (...)` | Review 애그리거트 로딩(RV-02·RV-03), 리뷰 목록의 이미지(PL-02·PF-04) |
| Q2 | `INSERT` / `UPDATE sort_order` / `DELETE WHERE id = ?` | 리뷰 작성(RV-01), 순서 변경·교체(RV-02) |
| Q3 | `WHERE review_id = ?` (CASCADE 삭제) | 리뷰 삭제(RV-03), 회원 탈퇴(PF-02 → reviews CASCADE) |
| Q4 | `WHERE storage_key = ANY(@keys)` | 고아 이미지 정리 배치: 스토리지 목록에서 읽은 키 묶음 중 DB가 참조하는 것을 한 번에 확인(키마다 따로 묻지 않음). 판별 기준은 §3 |

| 컬럼 | 타입 | NULL | 기본값 | 비고 |
|---|---|---|---|---|
| id | `uuid` | NOT NULL | 없음 | 앱이 `ReviewImageId.New()`(UUID v7)로 발급(ADR-006). 순서 변경은 같은 id, 교체는 새 id |
| review_id | `uuid` | NOT NULL | 없음 | 부모 리뷰. Domain에는 없는 섀도 키(내부 엔티티는 부모 ID를 두지 않음, D2 결정) |
| storage_key | `varchar(56)` | NOT NULL | 없음 | URL이 아니라 스토리지 키. 형식 `reviews/{yyyy}/{MM}/{dd}/{32자 hex}.{jpg\|png\|webp}`, 상한 56은 Domain 상수 `ReviewImage.StorageKeyMaxLength`를 `HasMaxLength`로 옮긴 값(§3.1) |
| sort_order | `smallint` | NOT NULL | 없음 | 0부터 시작하는 표시 순서. `Review.Create`가 목록 인덱스로 넣는다 |

- 생성 시각 컬럼은 두지 않는다. `ReviewImage`에 없고, 고아 판별의 "생성 후 N시간"은 DB 행이 아니라 스토리지 객체의 생성 시각이다

| 제약·인덱스 | 정의 | 받는 쿼리 / 막는 것 | 근거 |
|---|---|---|---|
| PK | `pk_review_images (id)` | Q2의 순서 변경·삭제 대상 식별 | ADR-006, D2 "ReviewImage는 순서·교체 때문에 식별자 유지" |
| UNIQUE | `ux_review_images_storage_key (storage_key)` | Q4. 같은 키를 가진 두 행(같은 리뷰든 다른 리뷰든). 다른 리뷰끼리 키를 공유하면 한 리뷰의 즉시 삭제(§3 A안)가 남의 이미지를 지운다. 키는 서버가 v4로 만들므로 위반은 서버 버그일 때만 생긴다 | `Review.Create` 주석, §3.1 |
| UNIQUE | `ux_review_images_review_sort_order (review_id, sort_order) DEFERRABLE INITIALLY DEFERRED` | 앞 컬럼 `review_id`로 Q1·Q3도 받는다고 **예상**(미확인 — 확인 항목). 한 리뷰 안에서 같은 순서 번호 중복. 아래 CHECK와 합쳐 **한 리뷰의 이미지는 최대 3행**이 DB에서도 보장된다. 순서 맞바꾸기(0↔1)는 행 단위 UPDATE 중간에 잠깐 중복이 생기므로 커밋 시점에 검사한다 | R-RV-02 |
| CHECK | `ck_review_images_sort_order`: `sort_order BETWEEN 0 AND 2` | 음수 순서, 4번째 이상 이미지. 2는 `Review.MaxImages - 1` | R-RV-02, `ReviewImage.Create` |
| CHECK | `ck_review_images_storage_key`: `btrim(storage_key) <> ''` | 빈 키. `ReviewImage.Create`의 `ThrowIfNullOrWhiteSpace`와 같은 조건. 형식 검사 CHECK는 두지 않는다 — 키는 서버만 만들고 Domain이 검증한다(§3.1) | `ReviewImage.Create` |
| FK | `fk_review_images_reviews`: `review_id → reviews(id) ON DELETE CASCADE` | 애그리거트 내부 엔티티라 루트와 생애가 같다. 리뷰 삭제(RV-03)·회원 탈퇴의 리뷰 CASCADE가 이미지 행까지 한 번에 지운다 | RV-03, PF-02 |

메모
- 스토리지 객체 삭제는 외부 I/O라 트랜잭션 안에서 하지 않는다(CLAUDE.md). 삭제 전에 키를 읽어 두고 DB 삭제 커밋 뒤 지우거나, 정리 배치가 Q4로 미참조 키를 찾아 지운다
- UNIQUE를 `(review_id, storage_key)`에서 `(storage_key)`로 바꿨다(D3-2). 앞의 것은 같은 리뷰 안 중복만 막아 리뷰끼리 키 공유를 허용한다. 바꾸면서 Q1·Q3(`review_id`)은 `ux_review_images_review_sort_order`의 앞 컬럼이 받는다고 예상한다
- `DEFERRABLE` 제약은 `ON CONFLICT`의 대상이 될 수 없다. 이 테이블은 ON CONFLICT를 쓰지 않으므로 문제없다
- **확인 항목 (D3 마이그레이션 · D4 통합 테스트)** — `ux_review_images_review_sort_order`
  - EF Core 마이그레이션 API는 `DEFERRABLE`을 표현하지 못한다 → `migrationBuilder.Sql("ALTER TABLE ... ADD CONSTRAINT ... UNIQUE (...) DEFERRABLE INITIALLY DEFERRED")`로 작성. `CREATE UNIQUE INDEX`로는 DEFERRABLE을 줄 수 없다
  - EF 모델에 이 인덱스를 `IsUnique()`로 알릴지: 알리면 EF가 순서 맞바꾸기(0↔1)의 UPDATE 순서를 정할 때 순환으로 보고 실패할 수 있다(미확인)
  - 통합 테스트: 이미지 2장 리뷰의 순서를 맞바꿔 `SaveChanges` 한 번으로 저장 → 성공하는지, 커밋 시점에 중복이면 위반이 나는지 확인
  - `EXPLAIN`: 이 제약이 만든 인덱스가 `WHERE review_id = ?`(Q1·Q3)를 `Index Cond`로 받는지


### likes

관련 규칙: R-LK-01

[이 테이블을 읽는 쿼리]

| # | 쿼리 | 쓰는 곳 |
|---|---|---|
| Q1 | `WHERE review_id = ? AND user_id = ?` / `WHERE user_id = ? AND review_id IN (...)` | 좋아요 토글 전 확인(LK-01), 리뷰 목록의 "내 좋아요 여부"(PL-02) |
| Q2 | `INSERT ... ON CONFLICT (review_id, user_id) DO NOTHING` / `DELETE WHERE review_id = ? AND user_id = ?` | 좋아요·취소, 같은 회원 동시 토글(LK-01) |
| Q3 | `WHERE review_id IN (...) GROUP BY review_id` (COUNT) | 리뷰 목록의 좋아요 수(PL-02). 카운터 전략은 D9 |
| Q4 | `WHERE review_id = ?` (CASCADE 삭제) | 리뷰 삭제(RV-03) |
| Q5 | `WHERE user_id = ?` (CASCADE 삭제) | 회원 탈퇴(PF-02) |

| 컬럼 | 타입 | NULL | 기본값 | 비고 |
|---|---|---|---|---|
| review_id | `uuid` | NOT NULL | 없음 | 좋아요 대상 리뷰. `Like.ReviewId` |
| user_id | `uuid` | NOT NULL | 없음 | 좋아요한 회원. `Like.UserId` |
| created_at | `timestamptz` | NOT NULL | 없음 | 앱이 `TimeProvider`로 정해 넣는다(ADR-007) |

- "좋아요 여부" 컬럼은 두지 않는다. 행이 있으면 좋아요한 것이다(bookmarks와 같은 근거)

| 제약·인덱스 | 정의 | 받는 쿼리 / 막는 것 | 근거 |
|---|---|---|---|
| PK | `pk_likes (review_id, user_id)` | Q1·Q2·Q3·Q4. 같은 (회원, 리뷰) 두 번째 행을 막는다. `review_id`를 앞에 둔 이유: 리뷰 단독 조회(Q3 좋아요 수)는 장소 상세마다 실행되고, 회원 단독 조회(Q5)는 탈퇴 때만 실행된다 | R-LK-01, ADR-005(복합 PK) |
| 보조 인덱스 | `ix_likes_user_id (user_id)` | Q5. 탈퇴 시 users DELETE의 CASCADE·FK 검사가 likes 전체를 읽지 않게 한다(users 표 "자식 테이블마다 `user_id`로 시작하는 인덱스" 확인 항목) | PF-02 |
| FK | `fk_likes_users`: `user_id → users(id) ON DELETE CASCADE` | 탈퇴 시 그 회원의 좋아요도 함께 지워진다 | PF-02, ADR-005 |
| FK | `fk_likes_reviews`: `review_id → reviews(id) ON DELETE CASCADE` | 리뷰가 지워지면 그 리뷰의 좋아요는 의미가 없다. places와 달리 리뷰 삭제는 정상 유스케이스(RV-03)라 RESTRICT를 걸면 삭제 자체가 막힌다 | RV-03 "좋아요는?", ADR-005 |
| CHECK | 없음 | 두 ID는 FK가, 중복은 PK가 막는다. `created_at`의 UTC는 Domain `TimeGuard`와 Npgsql이 보장한다 | ADR-007 |


### groups

관련 규칙: R-GR-01 · R-GR-03 · R-GR-05 · R-GR-06 · R-GR-08 · R-GR-09 · R-GR-10 · ADR-004

[이 테이블을 읽는 쿼리]

| # | 쿼리 | 쓰는 곳 |
|---|---|---|
| Q1 | `WHERE invite_code = ?` | 초대 코드로 그룹 식별(GR-01 4) |
| Q2 | `WHERE id = ? FOR UPDATE` (잠금 강도·순서는 D6) | 인원을 세기 전 그룹 행 잠금: 합류(R-GR-03), 탈퇴(R-GR-09 Write Skew) |
| Q3 | `WHERE id = ?` | 그룹맵·이름 변경(GR-05·GR-03), group_members FK 검사 |
| Q4 | `INSERT` → `invite_code` UNIQUE 위반이면 새 코드로 재시도 | 그룹 생성(GR-02, R-GR-06) |
| Q5 | `DELETE WHERE id = ?` | 마지막 멤버 탈퇴 시 같은 트랜잭션에서 삭제(R-GR-09) |

| 컬럼 | 타입 | NULL | 기본값 | 비고 |
|---|---|---|---|---|
| id | `uuid` | NOT NULL | 없음 | 앱이 `GroupId.New()`(UUID v7)로 발급(ADR-006) |
| name | `varchar(6)` | NOT NULL | 없음 | NFC·trim된 그룹명(`GroupName.Value`). 상한은 `GroupName.MaxLength`와 같다. 유일하지 않다 |
| invite_code | `varchar(6)` | NOT NULL | 없음 | 대문자로 정규화된 초대 코드(`InviteCode.Value`). 길이는 `InviteCode.Length` |
| created_at | `timestamptz` | NOT NULL | 없음 | 앱이 `TimeProvider`로 정해 넣는다(ADR-007). 첫 멤버의 `joined_at`과 같은 값(R-GR-08) |

- 방장(`owner_id`) 컬럼은 두지 않는다. 읽는 유스케이스가 없고, 원본의 `owner_id ... on delete cascade`는 방장 탈퇴 시 다른 멤버가 남은 그룹까지 지웠다(ADR-004 결정 6)
- 인원 수 컬럼은 두지 않는다. 인원은 group_members 행 수이고, Group 로딩(group_members Q1) 뒤 메모리에서 센다

| 제약·인덱스 | 정의 | 받는 쿼리 / 막는 것 | 근거 |
|---|---|---|---|
| PK | `pk_groups (id)` | Q2·Q3·Q5. group_members FK가 가리키는 대상 | ADR-006 |
| UNIQUE | `ux_groups_invite_code (invite_code)` | Q1·Q4. 두 그룹이 같은 코드를 갖는 것을 막는다. 생성 경합에서 앱 미리 검사는 TOCTOU라 최종 보장은 이 인덱스, 위반 시 재생성 | R-GR-06 |
| CHECK | `ck_groups_invite_code`: `invite_code ~ '^[A-HJ-NP-Z2-9]{6}$'` | 소문자, I·O·0·1, 6자가 아닌 코드. `A-H J-N P-Z`가 I·O를 뺀 24자, `2-9`가 8자 | R-GR-05, `InviteCode.Alphabet` |
| CHECK | `ck_groups_name`: `char_length(name) >= 2 AND name = btrim(name) AND name ~ '^[가-힣A-Za-z0-9 ]+$' AND name IS NFC NORMALIZED` | 하한 2자 미만, 앞뒤 공백, 허용 밖 문자(전각 공백·U+3164 등). 허용 공백이 ASCII 공백뿐이라 `btrim`이 Domain `Trim()`과 같은 결과를 내므로 users·places와 달리 trim까지 완전히 이중화된다. 상한 6은 `varchar(6)`이 맡는다 | R-GR-10 |
| FK (나가는 것) | 없음 | groups는 다른 테이블을 참조하지 않는다(방장 없음) | ADR-004 |
| FK (들어오는 것) | group_members `.group_id → groups` — `ON DELETE CASCADE` (group_members FK 행·메모 참고) | 정상 경로(R-GR-09)는 멤버 0명일 때만 삭제하므로 따라 지워지는 행이 없다. 버그로 멤버가 남은 그룹을 지우면 멤버십이 조용히 지워진다 — 애그리거트 경계 표현을 우선하고 앱 테스트(D4)로 막는다 | ADR-004, R-GR-09 |

메모
- 정원 4명(R-GR-01)은 여러 행에 걸친 규칙이라 CHECK로 막을 수 없다. 동시 합류는 Q2(그룹 행 잠금) 뒤 인원을 세는 것으로 막는다(D6)
- 합류는 users 행(R-GR-04)과 groups 행(R-GR-03)을 둘 다 잠근다. 데드락을 피할 잠금 순서(예: 항상 users → groups)는 D6


### group_members

관련 규칙: R-GR-01 · R-GR-02 · R-GR-07 · R-GR-09

[이 테이블을 읽는 쿼리]

| # | 쿼리 | 쓰는 곳 |
|---|---|---|
| Q1 | `WHERE group_id = ?` (행 전체) | Group 로딩 시 Members(OwnsMany) 채우기 → 정원(R-GR-01)·중복(R-GR-07)·마지막 멤버(R-GR-09) 판정, 그룹맵(GR-05) |
| Q2 | `WHERE user_id = ?` (COUNT) | 회원의 참여 그룹 수(R-GR-02), users 행 잠금(R-GR-04) 뒤 실행 |
| Q3 | `INSERT` / `DELETE WHERE group_id = ? AND user_id = ?` | 합류(GR-01), 그룹 생성 시 첫 멤버(R-GR-08), 탈퇴(GR-04) |
| Q4 | `WHERE user_id = ?` (FK 검사) | 회원 탈퇴(PF-02) 시 users DELETE의 RESTRICT 검사 |

| 컬럼 | 타입 | NULL | 기본값 | 비고 |
|---|---|---|---|---|
| group_id | `uuid` | NOT NULL | 없음 | 부모 그룹. Domain에는 없는 OwnsMany 섀도 키(D2 결정) |
| user_id | `uuid` | NOT NULL | 없음 | 멤버 회원. `GroupMember.UserId` |
| joined_at | `timestamptz` | NOT NULL | 없음 | 앱이 `TimeProvider`로 정해 넣는다(ADR-007). 첫 멤버는 `groups.created_at`과 같다 |

| 제약·인덱스 | 정의 | 받는 쿼리 / 막는 것 | 근거 |
|---|---|---|---|
| PK | `pk_group_members (group_id, user_id)` | Q1·Q3. 같은 (그룹, 회원) 두 번째 행을 막는다. Group 로딩(Q1)이 이 테이블의 주 접근 경로라 `group_id`가 앞이다 | R-GR-07, ADR-005(복합 PK) |
| 보조 인덱스 | `ix_group_members_user_id (user_id)` | Q2·Q4. PK 순서로는 `user_id` 단독 조건을 범위로 좁힐 수 없다(leftmost prefix) | R-GR-02, PF-02 |
| FK | `fk_group_members_groups`: `group_id → groups(id) ON DELETE CASCADE` | 내부 엔티티라 루트와 생애가 같다(review_images와 같은 근거). R-GR-09로 그룹이 지워질 때는 이미 멤버가 0명이라 실제로 지워지는 행은 없고, 애그리거트 경계를 스키마에 드러내는 의미다 | ADR-004 |
| FK | `fk_group_members_users`: `user_id → users(id) ON DELETE RESTRICT` | 멤버십이 남은 회원의 삭제를 거부한다. CASCADE로 두면 탈퇴한 회원이 마지막 멤버였던 그룹이 **멤버 0명으로 남는다**(R-GR-09 위반, 원본의 고아 그룹 결함). 탈퇴(PF-02)는 먼저 참여 그룹마다 `Group.RemoveMember`를 거쳐 `GroupEmptied`면 그룹을 지우고, 그 다음 users 행을 지워야 한다. 이 순서를 빠뜨리면 RESTRICT가 오류로 드러낸다 | R-GR-09, ADR-004, PF-02 |
| CHECK | 없음 | 두 ID는 FK가, 중복은 PK가 막는다. 정원 4명(R-GR-01)·참여 3개(R-GR-02)는 여러 행에 걸친 규칙이라 CHECK로 표현할 수 없다 → 잠금(D6) | ADR-007 |

메모
- users 쪽 FK 중 group_members만 RESTRICT다. bookmarks·reviews·likes는 지워도 깨지는 불변식이 없지만, 멤버십 삭제는 R-GR-09 판정을 거쳐야 하기 때문이다
- groups 쪽 FK 중 group_members는 CASCADE다. 버그로 멤버가 남은 그룹을 지우면 멤버십이 조용히 지워진다. 애그리거트 경계 표현을 우선하고, 이 경로는 앱 테스트로 막는다
- 인덱스는 동시 합류로 정원을 넘는 것(메모리 COUNT의 스냅샷 경합)을 막지 못한다. 막는 것은 groups Q2의 행 잠금이다(D6)


## 3. D3에서 정할 미결 결정

| 결정 | 선택 | 이유 |
|---|---|---|
| 닉네임·이메일 대소문자 무시 UNIQUE: `lower()` 표현식 인덱스 vs `citext` | `lower()` 표현식 인덱스 (`ux_users_nickname_lower`·`ux_users_email_lower`) | 확장 설치 없이 표준 `text` 컬럼 유지, 대소문자 무시 비교가 쿼리에 드러난다. 대가: 쿼리를 `lower(col) = lower(?)` 모양으로 써야 인덱스를 탄다(users 메모). 이메일의 C#·DB 소문자 변환 일치는 미확인 |
| 별 상태: 저장 컬럼 vs 조회 시 계산 | **저장하지 않는다. 조회 시 계산**한다 — (회원, 장소)마다 `EXISTS` bookmarks Q1 · reviews Q4 조합(R-STAR). 목록(BM-03·PF-03·근처 검색)은 장소마다 따로 묻지 않고 `place_id = ANY(@ids)` 또는 `user_id` 단위로 한 번에 계산한다(N+1 금지) | **계산 비용(예상, D8 실측)**: 두 쿼리 모두 `user_id`가 앞 컬럼인 인덱스(`pk_bookmarks`, `ux_reviews_user_place_date`)를 타므로 비용은 log N(전체 행) + k(그 회원의 행)이다. 전체 사용자 수보다 한 회원의 행 수와 heap 방문이 좌우한다. 별 계산은 `place_id`만 필요해 Index Only Scan 가능성이 있다<br>**저장의 매일 비용**: 서로 다른 두 애그리거트(Bookmark·Review)의 쓰기마다 같은 파생 행을 갱신해야 한다. 동시 북마크·리뷰 작성 시 전이 계산 방식은 lost update(Read Committed에서 대기한 UPDATE는 SET 값을 다시 계산하지 않음), 원천 재계산 방식은 write skew(서로 상대의 미커밋 쓰기를 못 봄)가 생긴다. 막으려면 모든 쓰기 경로에서 별 행을 먼저 잠가야(`FOR UPDATE`) 하고 하나라도 빠뜨리면 조용히 틀어진다. 계산하면 두 트랜잭션이 공유하는 행이 없어 이 문제가 구조적으로 없다(사실만 저장, 파생값은 읽을 때)<br>**전환 비용 비대칭**: 저장→계산은 컬럼 DROP과 읽기 쿼리 교체로 싸다. 계산→저장은 이중 쓰기 배포 + 운영 중 백필 + 검증 + 읽기 전환이라 비싸다. 그래도 계산을 고른 이유: 비싼 전환은 "한 번 × 실제로 필요해질 확률"이고, 저장의 동기화·정합성 비용은 매일 낸다<br>**재검토 시점**: D8 대용량 시드에서 BM-03·PF-03을 `EXPLAIN (ANALYZE, BUFFERS)`로 측정(일반 회원 vs 헤비 회원). 느리면 전체 저장 전에 D9 카운터(별 개수만 비정규화)나 D11 캐시를 먼저 검토 |
| 북마크 여부 컬럼 필요 여부 (행 존재 = 북마크?) | 두지 않는다. 행 존재 = 북마크 | 여부 컬럼은 "행 없음 / false"가 같은 뜻인 상태 중복을 만들고, 해제가 UPDATE가 되어 false 행이 쌓인다 → bookmarks 메모 |
| `review_images.storage_key` 규칙 | 형식: `reviews/{yyyy}/{MM}/{dd}/{식별자}.{확장자} 예: reviews/2026/10/16/3f2a9c0e7b1d4e8fa6c5d2b1e0f9a8c7.webp`, 길이: `varchar(56)`, 상한은 Domain 상수 `ReviewImage.StorageKeyMaxLength = 56`에서 가져온다 | 키는 서버가 고정 규칙으로 만드므로 최대 길이를 계산할 수 있다(§3.1 표 2). 계산 가능한 상한은 Domain 상수 한 곳에 두고 EF Core HasMaxLength로 컬럼에 옮긴다(CLAUDE.md SSoT). 규칙이 바뀌어 길어지면 varchar 확장은 카탈로그만 바꾸는 변경이라 싸다(**확인 예정**: 마이그레이션에서 `ALTER COLUMN ... TYPE varchar(60)` 전후 `pg_relation_filenode('review_images')` 비교 — 값이 바뀌면 테이블 재작성) |
| 고아 이미지 판별 기준과 정리 배치 | **판별**: DB에서 미참조(Q4) **그리고** 객체 나이(스토리지 Last-Modified 기준) > N. N = **6시간**<br>**배치(B안)**: 매일 06:00 최근 30시간(주기 24시간 + N) prefix + 주 1회 전체 스캔<br>**즉시 삭제(A안)**: 리뷰 삭제·이미지 교체·탈퇴는 DB 커밋 후 스토리지 객체를 지운다. 실패분은 배치가 잡는다<br>**삭제 대기 목록(C안, transactional outbox)**: 지금은 두지 않는다. 도입 조건: 주 1회 전체 스캔이 너무 오래 걸릴 때(D8 소요 시간 측정) | **나이 조건**: 업로드 완료 → DB 커밋 사이에 배치가 끼어들면 커밋될 이미지를 지운다. N의 하한 = 서버 요청 타임아웃(DB 호출까지 취소 토큰 전달) + 여유(시계 차이). DB 타임아웃은 겹치는 안전망이라 더하지 않는다. 실제 N은 손해 비대칭(아래 메모) 때문에 하한보다 넉넉하게 잡는다<br>**B안**: 업로드 실패 고아는 DB가 키를 모르므로 스토리지에서 출발하는 스캔만 찾을 수 있고, 항상 최근 폴더에 있다 → 매일 최근 폴더. 삭제 실패 고아는 작성일 폴더(오래된 폴더)에 있어 최근 스캔이 못 본다 → 주 1회 전체 스캔. 매일 비용이 이미지 총량과 무관하게 일정하다. PF-02 최대 노출 기간 = 약 7일 + N(**B안**)<br>**C안 보류**: B→C 전환 비용이 작다 — 새 테이블 추가뿐이고, 과거 고아는 B의 전체 스캔이 계속 잡아 백필이 없다. 전제: A안의 "키 읽기 → DB 삭제 → 커밋 → 스토리지 삭제" 흐름을 한 곳에 모은다(D5). 그러면 전환은 "키를 읽어 두는 바로 그 자리에 INSERT 한 줄 추가"다 |
| 자식 컬렉션 매핑: `IReadOnlyList`/`IReadOnlyCollection` + backing field vs `ImmutableArray` | 해당 리스트의 객체 의미대로 공개 타입 지정(`ReviewImage -> IReadOnlyList`, `GroupMember -> IReadOnlyCollection`), **순서 보장 책임은 도메인(Review)**에 두기로 결정, 따라서 정렬 방식도 도메인 내부에서 SortOrder 기준 정렬하여 반환하도록 구현, ImmutableArray는 매핑 때 확인, D4 통합 테스트 시나리오(순서 맞바꾸기 → 다시 불러오기 → Images[i].SortOrder == i) 테스트 진행 예정 | 순서 보장 책임은 **EF 복원 시 순서가 보장되지 않는다(ORDER BY 없음, UPDATE마다 행 버전 위치가 바뀜)**는 문제로 인하여 도메인에 두기로 한다. 캐스팅 우회를 막는 것과 순서를 지키는 것 모두 엔티티가 보장한다. 공개 타입을 객체 의미대로 지정하는 이유는, 하나로 통일해서 일관성 및 가독성을 높이는 것 보다, 객체의 특성과 사용 용도(예: 유저가 의도하여 업로드한 이미지 순서를 항상 지켜서 보게 한다)에 따르는게 좋다고 판단. |

메모
- 고아 이미지 객체 나이 하한 설정 시 손해 비대칭: 
  - N이 너무 작으면: 커밋된 리뷰의 이미지가 지워짐 → 데이터 손실, 되돌릴 수 없음
  - N이 너무 크면: 고아가 조금 더 오래 남음 → 스토리지 비용 다소 증가


### 3.1 `storage_key` 규칙 상세

1) 구성

| 부분 | 선택 | 이유 |
|---|---|---|
| 날짜 prefix | 업로드 시각의 UTC 날짜, `TimeProvider`가 정한 값 | 시간 기준 prefix(결정됨). 시각의 기준은 ADR-007과 같다. 정리 배치가 스토리지 목록을 훑을 **범위**를 prefix로 좁힐 수 있다(예: 최근 며칠치 폴더만). 단, prefix는 범위를 좁히는 용도일 뿐 **나이 판정에 쓰지 않는다** — 23:59:59에 업로드된 객체는 prefix로는 "어제"지만 실제 나이는 1초다. "생성 후 N시간"은 스토리지 객체의 생성 시각(Last-Modified)으로 판정한다(고아 판별 기준, §3) |
| 식별자 | 무작위 UUID v4, 하이픈 없는 32자 소문자 hex (`Guid.NewGuid().ToString("N")`) | 리뷰 ID·이미지 ID를 넣지 않기로 했다(키로 리뷰를 찾을 일이 없음). 그래서 `Review.Create`가 ID를 미리 받는 구조 변경도 필요 없다. v7이 아니라 v4인 이유: 시각은 이미 prefix에 있고, 식별자까지 시간순일 이유가 없으며 추측하기 어려울수록 좋다(무작위 비트 v4 122 / v7 74). 하이픈을 빼서 4자를 줄인다 |
| 확장자 | `jpg` · `png` · `webp` 중 하나. 업로드 파일 이름이 아니라 서버가 판별한 형식으로 정한다 | 회원이 보낸 파일명은 불신 입력이다(경로 조작, 개인정보 노출, 길이 예측 불가). 형식을 서버가 정하면 키에 들어가는 문자가 고정된다. `jpeg`가 아니라 `jpg`로 통일한다 |
| 회원 ID | 넣지 않는다 | 넣으면 회원 ID(v7, 가입 시각 포함)가 이미지 URL로 노출된다(ADR-006). 탈퇴 시 이미지 정리 방식은 고아 판별 기준(§3)에서 정한다 — 최근 prefix만 훑는 배치는 오래된 폴더의 탈퇴 회원 이미지를 찾지 못하므로 PF-02 "모든 데이터 삭제"를 만족하는지 거기서 확인 |

2) 최대 길이

| 부분 | 글자 수 |
|---|---|
| `reviews/` | 8 |
| `yyyy/` | 5 |
| `MM/` | 3 |
| `dd/` | 3 |
| 식별자(32자 hex) | 32 |
| `.` | 1 |
| 확장자(가장 긴 `webp`) | 4 |
| **합계** | **56** |

- 날짜가 0으로 채운 고정 폭(MM, dd)이라 모든 키는 확장자 3자(jpg·png)면 55자, webp면 56자다
- 전부 ASCII라 글자 수 = 바이트 수다

3) 검증 위치

| 질문 | 결정 | 이유 |
|---|---|---|
| 길이 검사 | Domain `ReviewImage.Create`가 `StorageKeyMaxLength` 초과 시 **예외** | 키는 서버가 만든다. 초과는 회원 입력이 아니라 서버 실수이므로 Result가 아니라 예외로 터뜨린다. DB `varchar(56)`은 이중 방어 |
| 형식 CHECK | 두지 않는다 | 키는 서버만 만들고 Domain이 검증한다. 상한은 `varchar(56)`이 막는다 |
| 상수와 생성기 불일치 | 키 생성기 단위 테스트가 실패해야 한다 | 형식을 바꾸면서 상수를 고치지 않으면 생성된 키가 Domain 검증을 통과하지 못해야 한다 |

## 4. 원본 스키마와 비교 (설계를 마친 뒤 작성)

> `C:\Projects\nook\supabase\schema.sql`과 비교해 다른 점을 적는다 → ADR-002 근거

> 작성: Claude(사용자 요청, 2026-10-16). 판단 근거는 D3-2 세션의 사용자 답변(중복 인덱스 3개, `review_images` 무제약, places CASCADE)과 위 §2·§3 결정. "이유"는 원본의 의도를 추측하지 않고 우리 기준을 적용한 결과를 쓴다. 원본에서 같은 것은 표 끝 "같은 점"에만 적는다

**1. PK 모양과 발급 주체**

| 항목 | 원본 | nook_csharp | 이유 |
|---|---|---|---|
| ID 발급 | 8개 테이블 모두 `id uuid default gen_random_uuid()` — DB가 v4 발급, `pgcrypto` 확장 | 엔티티 테이블은 앱이 `XxxId.New()`(v7)로 발급, DB 기본값 없음 | 발급 주체를 하나로(앱). 저장 전에 ID를 알아야 Domain이 완결된다 — ADR-006 |
| 관계 테이블 PK | bookmarks·likes·group_members도 대리 키 `id` + 별도 `unique (...)` | 대리 키 없이 복합 PK: `(user_id, place_id)`·`(review_id, user_id)`·`(group_id, user_id)` | 유일성 규칙이 PK 자체가 되어 인덱스가 하나 준다. 원본 bookmarks는 `pk(id)`·`unique`·`idx` 3개, 우리는 PK 1개로 같은 쿼리를 받는다 — ADR-005 |

**2. 컬럼 유무와 타입**

| 항목 | 원본 | nook_csharp | 이유 |
|---|---|---|---|
| 문자열 타입 | 전부 `text`(상한 없음) | `varchar(n)`, n은 Domain 상수(`Nickname.MaxLength` 등)와 같다 | 상한을 DB에서도 막는다(CLAUDE.md "DB CHECK로 이중화") |
| 시각 기본값 | `created_at ... default now()` | 기본값 없음, 앱이 `TimeProvider`로 넣는다 | 시각의 출처를 하나로 — ADR-007 |
| `users` 시각·프로필 | `created_at`, `gender`, `birth`(`text` 'YY-MM-DD'), `avatar_url` | 없음(`id`·`nickname`·`email`만) | `created_at`: 쓰는 규칙·화면이 없음(ADR-007). 프로필 3개: MVP 범위 밖(D3-2 사용자 확인) |
| `places` 부가 정보 | `category`, `road_address`, `phone`, `place_url`, `created_at`. `address`는 NULL 허용 | 없음. `address`는 NOT NULL | MVP 범위 밖(D3-2 사용자 확인) |
| `reviews.updated_at` | 있음 | 없음 | 쓰는 규칙·화면이 없음 — ADR-007 |
| `reviews.review_date` | 없음 | `date` NOT NULL | R-RV-03 UNIQUE의 기준 날짜(UTC). 표현식 인덱스 대신 컬럼 |
| 리뷰 이미지 값 | `url text` | `storage_key varchar(56)` | URL은 도메인·CDN이 바뀌면 전부 고쳐야 한다. 키만 저장하고 URL 변환은 한 곳(D2). 형식·길이는 §3.1 |
| 리뷰 이미지 순서 | `order_index int default 0` | `sort_order smallint`, 기본값 없음 | `Review.Create`가 목록 인덱스로 항상 넣는다. 기본값 0이 있으면 값을 빠뜨린 INSERT가 조용히 0번이 된다 |
| `groups.owner_id` | `owner_id ... on delete cascade` | 없음 | 읽는 유스케이스가 없고, CASCADE가 방장 탈퇴 시 다른 멤버가 남은 그룹까지 지웠다 — ADR-004 결정 6 |
| `group_members.color` | `color text not null`(멤버별 마커 색) | 없음 | MVP 범위 밖(D3-2 사용자 확인) |

**3. 도메인 규칙의 DB 이중화**

| 항목 | 원본 | nook_csharp | 이유 |
|---|---|---|---|
| CHECK 제약 | 0개 — 모든 규칙을 앱 코드만 지킨다 | 닉네임·이메일·장소(ID·이름·위경도)·리뷰 본문·이미지 순서·이미지 키·초대 코드·그룹명 | 앱을 거치지 않은 쓰기(SQL 직접 수정, 시드)도 막는다 — CLAUDE.md SSoT |
| 닉네임 유일 | 제약 없음 | `ux_users_nickname_lower (lower(nickname))` | 앱 미리 검사는 TOCTOU라 동시 가입으로 중복이 생긴다 — R-US-02 |
| 이메일 유일 | `unique`(대소문자 구분 — `A@x.com`과 `a@x.com` 공존) | `ux_users_email_lower (lower(email))` | 대소문자만 다른 중복 가입 — R-US-03 |
| 리뷰 하루 1개 | 제약 없음 | `ux_reviews_user_place_date (user_id, place_id, review_date)` | 동시 작성은 앱 검사로 막을 수 없다. 최종 보장은 UNIQUE — R-RV-03 |
| 리뷰 이미지 장수·순서 | 제약 없음 — 같은 `order_index` 중복, 4장째, 음수 모두 저장된다 | `(review_id, sort_order)` DEFERRABLE UNIQUE + `CHECK (sort_order BETWEEN 0 AND 2)` → 리뷰당 최대 3행 | R-RV-02. 순서 맞바꾸기 중간의 일시 중복 때문에 DEFERRABLE |
| 이미지 키 중복 | `url` 제약 없음 | `ux_review_images_storage_key (storage_key)` | 키를 공유한 두 리뷰 중 하나를 지우면 남의 이미지가 지워진다(§3 A안) |

**4. `ON DELETE`** — 원본은 FK 10개가 모두 `CASCADE`

| 항목 | 원본 | nook_csharp | 이유 |
|---|---|---|---|
| `bookmarks.place_id`·`reviews.place_id` | `CASCADE` | `RESTRICT` | 기준 ①: 장소 삭제 유스케이스가 없다. CASCADE면 장소 하나의 삭제가 북마크·리뷰 → `review_images`·`likes`까지 연쇄로 지우고, DB 밖 Storage 이미지는 고아로 남는다 |
| `group_members.user_id` | `CASCADE` | `RESTRICT` | 기준 ②: 탈퇴한 회원이 마지막 멤버면 멤버 0명 그룹이 남는다(원본 고아 그룹 결함, R-GR-09). 탈퇴는 `Group.RemoveMember`를 거쳐야 한다 — ADR-004 |
| `groups.owner_id` | `CASCADE` | 컬럼 없음 | 2번 표 참고 — ADR-004 |

**5. 인덱스** — 원본 `idx_*` 9개 기준

| 항목 | 원본 | nook_csharp | 이유 |
|---|---|---|---|
| 중복 인덱스 | `idx_bookmarks_user`, `idx_group_members_group`, `idx_groups_invite` — 각각 `unique (user_id, place_id)`·`unique (group_id, user_id)`의 앞 컬럼, `invite_code unique`가 이미 받는다 | 없음 | 같은 쿼리를 받는 인덱스가 둘이면 쓰기마다 갱신할 인덱스만 는다. 인덱스마다 "받는 쿼리"를 적어 중복을 막는다(§2 형식) |
| `reviews` 인덱스 | `(user_id)`, `(place_id)`, `(created_at desc)` 단독 3개 | `ux_reviews_user_place_date`(앞 컬럼 `user_id`), `ix_reviews_place_created (place_id, created_at DESC)` | 장소 상세 최신순(Q2)은 필터와 정렬을 한 인덱스로 받는다(예상, D8 측정). `created_at` 단독을 쓰는 쿼리는 읽는 쿼리 표에 없다 |
| `review_images` 인덱스 | `(review_id)` | 별도 없음 — `(review_id, sort_order)` 제약 인덱스의 앞 컬럼 | Q1·Q3을 받는다고 예상(미확인, review_images 확인 항목) |
| `likes` 컬럼 순서 | `unique (user_id, review_id)` + `idx_likes_review (review_id)` | PK `(review_id, user_id)` + `ix_likes_user_id (user_id)` | 둘 다 양쪽 단독 조회를 받는다. 자주 도는 리뷰별 좋아요 수(Q3)를 PK 앞 컬럼으로, 탈퇴 때만 도는 회원 조회를 보조 인덱스로 |

**6. 보안·운영**

| 항목 | 원본 | nook_csharp | 이유 |
|---|---|---|---|
| RLS | 8개 테이블 모두 `anon`·`authenticated`에 전체 허용(`using (true) with check (true)`) — 클라이언트가 DB에 직접 쓴다 | RLS 없음. DB는 API 서버만 접속하고 인가는 API 계층에서 한다(D5) | 원본 정책은 RLS를 켜 두고 실제로는 아무것도 막지 않는다 |
| Storage 정책 | 버킷 `images` 공개 읽기, `anon` 업로드·**삭제** 허용 | 미정(D5 이미지 업로드) | 원본은 누구나 남의 이미지를 지울 수 있다 |
| 스키마 적용 | SQL Editor에 붙여넣어 1회 실행, `create ... if not exists`. 그룹은 별도 `groups.sql` | EF Core 마이그레이션(D3 매핑, 배포는 ADR-003·D12) | `if not exists`는 이미 있는 테이블의 컬럼·제약 변경을 조용히 건너뛴다. 이력·되돌리기가 없다 |

**같은 점**: FK 나머지 6개(`user_id` → bookmarks·reviews·likes, `review_images.review_id`, `likes.review_id`, `group_members.group_id`)는 둘 다 `CASCADE` · `places.kakao_place_id`·`groups.invite_code` UNIQUE · `group_members (user_id)` 보조 인덱스(원본 `idx_group_members_user`, 우리 `ix_group_members_user_id` — 같은 leftmost prefix 이유) · 시각은 `timestamptz` · 좌표는 `double precision`

메모
- 정원 4명(R-GR-01)·참여 그룹 3개(R-GR-02)는 둘 다 DB 제약이 없다. 차이는 앱의 잠금 전략(D6)이라 이 표(스키마 차이)에는 넣지 않는다
- ADR-002 범위는 스키마 차이만(사용자 결정, D3-2). 도메인 규칙 차이(trim, UTC 날짜 기준, 닉네임 화이트리스트·NFC)는 ADR-002에 넣지 않는다
