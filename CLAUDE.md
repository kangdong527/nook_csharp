# CLAUDE.md

nook_csharp — 장소 기록 서비스 nook(원본: `C:\Projects\nook`, Next.js + Supabase)의 백엔드를 C# .NET으로 다시 만드는 **2주 학습 프로젝트**다.
결과물보다 **사용자의 체득**이 우선이다.

## 반드시 지킬 것

1. **모든 작업 전에 `nook-coach` 스킬을 적용한다.** 소유권 표(사용자가 직접 쓰는 영역과 Claude가 써도 되는 영역)와 힌트 사다리를 따른다
2. 세션 시작 시 `docs/progress.md`를 읽고, 끝날 때 갱신한다
3. 설계 리뷰는 `design-review`, 자료구조는 `dsa-lab`, 버그는 `systematic-debugging` → `incident-log`
4. 외부 스킬(TDD, EF Core, 성능 등)은 사용자 소유 영역에서 **리뷰 체크리스트**로만 쓴다. 대신 구현하지 않는다
5. 성능 주장은 측정값(`EXPLAIN ANALYZE`, BenchmarkDotNet, k6)으로만 한다
6. 완료를 말하기 전에 `dotnet build`와 `dotnet test` 출력을 확인한다

## 문서

| 문서 | 내용 |
|---|---|
| `docs/개발_프로세스.md` | 목표, 기술 스택, 구조, D1~D14 일정과 게이트 |
| `docs/progress.md` | 현재 Day, 다음 할 일, 미결 결정 |
| `docs/learning-log.md` | 일일 학습 기록(사용자 작성) |
| `docs/design/` · `docs/adr/` · `docs/dsa/` · `docs/incidents/` | 사용자 작성 산출물 |

## 도메인 SSoT (원본 nook에서 확정된 값)

리뷰 10~50자(공백 포함) · 리뷰 이미지 최대 3장 · 닉네임 2~12자 · 참여 그룹 최대 3개 · 그룹 정원 4명 · 그룹명 2~6자 · 초대코드 6자(0/O·1/I 제외) · 별 상태 `empty`/`gray`/`fill`
→ 코드에서는 `Nook.Domain`의 상수 한 곳에서만 정의하고, DB CHECK 제약으로 이중화한다.

## 실행 (D1 이후 채운다)

```bash
dotnet build
dotnet test
```

## 하지 말 것

- 트랜잭션 안에서 외부 I/O(이미지 업로드, 외부 API)를 호출하지 않는다
- 비밀값(연결 문자열, JWT 키)을 커밋하지 않는다. `dotnet user-secrets` 또는 환경변수를 쓴다
- `main`에 직접 커밋하지 않는다. `feat/*`·`fix/*` 브랜치에 Conventional Commits로 커밋한다
