# ADR-002 원본 nook 스키마와 다른 점

- 상태: 채택됨 (2026-10-17 사용자 검토, 결정 3 문장 사용자 수정)
- 날짜: 2026-10-17
- 결정자: 강동진
- 작성: Claude (사용자 요청, 결정 내용은 D3-1·D3-2 세션의 사용자 결정. 차이 목록은 `docs/design/erd.md` §4)

## 맥락

원본 nook(`C:\Projects\nook\supabase\schema.sql`)의 스키마를 보지 않고 쿼리에서 출발해 D3 ERD를 설계했고, 설계를 마친 뒤 원본과 비교했다.
원본은 도메인 규칙을 DB에서 하나도 막지 않고(CHECK 0개, 리뷰 하루 1개·이미지 3장·닉네임 유일 제약 없음), 삭제는 FK 10개 모두 `CASCADE`로 지운다.
그래서 잘못된 행은 쉽게 들어가고, 지우면 안 되는 행은 함께 지워진다. 원본의 알려진 결함 중 고아 그룹(방장·마지막 멤버 탈퇴)과 경합으로 깨지는 유일성이 이 두 성질에서 나온다.

범위: 스키마 차이만 다룬다(D3-2 사용자 결정). 도메인 규칙 자체의 차이(리뷰 길이 trim 후 계산, 하루 기준 UTC, 닉네임 화이트리스트·NFC)는 다루지 않는다.

## 결정

1. **행 하나로 판단할 수 있는 규칙은 CHECK로 이중화한다.**
   Domain 검증은 Domain을 거치는 코드만 지킨다. SQL 직접 수정·시드 스크립트처럼 앱을 거치지 않은 쓰기는 CHECK만 막는다. 문자열 상한도 `text` 대신 `varchar(n)`(n = Domain 상수)으로 DB가 막는다
2. **여러 행에 걸친 유일성은 UNIQUE가 최종 보장한다.**
   앱의 미리 검사는 TOCTOU라서 동시 요청 둘이 모두 "없음"을 보고 INSERT한다. 이것은 Domain이 아예 막을 수 없는 규칙이라 이중화가 아니라 DB의 몫이다(닉네임·이메일 대소문자 무시 유일, 리뷰 하루 1개, 이미지 키 유일)
3. **행 개수 규칙은 DB 제약에서 셀 수 없으니, 행 값 규칙으로 바꿔 제약으로 막는다.**
   이미지 3장은 `sort_order BETWEEN 0 AND 2` CHECK + `(review_id, sort_order)` UNIQUE로 표현한다. 바꿀 수 없는 규칙(정원 4명, 참여 그룹 3개)은 잠금이 맡는다(D6)
4. **`ON DELETE`는 두 기준으로 정한다.**
   ① 정상 삭제 경로가 없으면 `RESTRICT`(`bookmarks`·`reviews`의 `place_id`) ② 지워도 깨지는 불변식이 있으면 `RESTRICT` + 앱 절차(`group_members.user_id` — R-GR-09). 둘 다 아니면 `CASCADE`
5. **인덱스마다 받는 쿼리를 적는다.**
   PK·UNIQUE가 만든 인덱스도 인덱스로 센다. 같은 쿼리를 받는 인덱스가 둘이면 쓰기마다 갱신할 인덱스만 늘어난다. 원본의 `idx_bookmarks_user`·`idx_group_members_group`·`idx_groups_invite`는 기존 UNIQUE와 겹쳐 두지 않는다
6. **읽는 유스케이스가 없는 컬럼은 두지 않는다.**
   `users`의 `gender`·`birth`·`avatar_url`, `places`의 `category`·`road_address`·`phone`·`place_url`, `group_members.color`는 MVP 범위 밖이다. 필요해지면 NULL 허용 컬럼 추가로 들인다
7. **보안 경계는 API 계층이다.**
   원본은 클라이언트가 DB에 직접 붙어 RLS가 유일한 방어선이었는데, 정책은 `using (true)`로 아무것도 막지 않았다. 우리는 API 서버만 DB에 접속하고 인가는 API에서 한다(D5). RLS는 두지 않는다
8. **스키마는 EF Core 마이그레이션으로 적용한다.**
   원본의 "SQL Editor에 붙여넣어 1회 실행 + `create ... if not exists`"는 이미 있는 테이블의 변경을 조용히 건너뛰고 이력이 없다. 적용 시점·방식은 ADR-003(D12)

이미 별도 ADR로 남긴 차이는 여기서 반복하지 않는다.

| 차이 | ADR |
|---|---|
| `groups.owner_id` 제거, 그룹 생애 | ADR-004 |
| FK 유지, 관계 테이블 복합 PK | ADR-005 |
| ID를 앱이 UUID v7로 발급(DB `gen_random_uuid()` 기본값 없음) | ADR-006 |
| 시각을 앱이 넣음(`default now()` 없음), `updated_at`·`users.created_at` 없음 | ADR-007 |

## 고려한 대안

- **원본처럼 규칙을 앱 코드에만 둔다**: 쓰기 경로가 Domain 하나뿐이면 동작한다. 그러나 유일성은 동시 요청에서 앱이 막을 수 없고, 앱 우회 쓰기는 조용히 잘못된 행을 남긴다 → 기각
- **원본처럼 모든 FK를 `CASCADE`로 둔다**: 삭제 코드가 단순해진다. 그러나 장소 하나의 삭제가 북마크·리뷰 → 이미지 행·좋아요까지 연쇄로 지우고 Storage 이미지는 고아로 남으며, 회원 탈퇴가 멤버 0명 그룹을 남긴다(원본 결함) → 기각
- **이미지 키에 형식 CHECK까지 둔다**: 키는 서버만 만들고 Domain이 검증하므로 상한(`varchar(56)`)과 빈 값 CHECK로 충분하다 → 기각(erd.md §3.1)

## 결과

- 규칙이 Domain 상수와 DB 제약 두 곳에 생긴다. 값을 바꾸면 Domain 상수와 마이그레이션을 함께 바꿔야 한다. 어긋남은 D4 통합 테스트(실제 PostgreSQL)가 잡는다
- `btrim`은 ASCII 공백만 지워 users·places·reviews의 trim은 최소 이중화다. Unicode 공백 처리는 Domain이 맡는다(erd.md users 메모)
- 장소 삭제는 자식 행이 남아 있으면 오류가 난다. 장소 삭제 유스케이스가 생기면 기준 ①을 다시 판단한다
- 회원 탈퇴(PF-02)는 users DELETE 전에 참여 그룹마다 `Group.RemoveMember`를 거쳐야 한다. 빠뜨리면 `RESTRICT`가 오류로 드러낸다
- DEFERRABLE UNIQUE는 EF Core 마이그레이션 API로 표현할 수 없어 `migrationBuilder.Sql`로 쓴다
- 측정하지 않은 항목(인덱스가 받는 쿼리의 `EXPLAIN`, 중복 인덱스 제거의 쓰기 비용)은 erd.md에 "예상"으로 남기고 D3 마이그레이션·D8에서 확인한다
- 세부 차이 목록: `docs/design/erd.md` §4
