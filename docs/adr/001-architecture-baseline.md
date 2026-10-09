# ADR-001 아키텍처 기준선: API 스타일, 계층 경계, 영속 모델

- 상태: 채택됨
- 날짜: 2026-10-09
- 결정자: 강동진

## 맥락

nook 백엔드를 계층형 솔루션(Api, Application, Domain, Infrastructure, Algorithms)으로 다시 만든다. 무엇을 정해야 했고, 왜 지금 정해야 하나?
  => 프로젝트별 역할의 이해와 컴포넌트(프로젝트) 간 참조 설계, 유스케이스, ADR. 해당 설계를 지금 하지 않고 개발을 진행하면 일관적인 구조 유지가 어렵고 추후 잘못 수정 시 유지보수 비용이 많아지기 때문

## 결정

### 1. API 스타일: Controllers
- 결정: ASP.NET Core **Controllers**를 쓴다 (Minimal API 아님)
- 이유: API 단 서비스, 리포지토리 계층을 참조하여 비즈니스 로직 직접 작성하는 경험 목적
- 고려한 대안: Minimal API — (작성: 비즈니스 로직 작성 경험을 Controller만큼 쌓을 수 없음)

### 2. 리포지토리 인터페이스 위치: Application
- 결정: 리포지토리 인터페이스는 **Application**에, 구현은 Infrastructure에 둔다. 연결(DI 등록)은 Api의 `Program.cs`에서 한다
- 이유: 참조 역전(DIP)을 반영하여 클린 아키텍처를 깨지 않음
- 고려한 대안: Domain에 두기 — *(Claude)* 엔티티와 저장 계약이 한곳에 있어 DDD 전통 배치와 같다는 장점이 있다. 그러나 리포지토리 메서드는 유스케이스가 필요로 하는 형태(페이지 조회, 개수 조회 등)를 따라가므로 Application의 관심사에 가깝고, Domain에는 규칙과 불변식만 두기 위해 고르지 않았다

### 3. 전이 참조 차단과 Infrastructure→Domain 직접 참조
- 결정: (A) 전이 참조 차단
- *(Claude)* 정리: 이 결정은 **서로 독립된 두 개의 스위치**로 나뉜다. 아래 A/B 장단점 서술에는 스위치 ②를 끈 경우("Infrastructure가 Domain을 모른다")의 내용이 섞여 있다

  | 스위치 | 이 프로젝트의 선택 | 이유 |
  |---|---|---|
  | ① 전이 참조 차단 (`DisableTransitiveProjectReferences`) | **켬** | Api가 Application을 거쳐 Domain 엔티티를 몰래 쓰지 못하게 컴파일러가 막는다 |
  | ② Infrastructure → Domain 직접 참조 | **유지** | 결정 4(겸용)에 따라 Infrastructure가 Domain 엔티티를 Fluent API로 직접 매핑한다. 끄면 별도 영속 모델이 필요해져 결정 4와 모순된다 |

  - 그래서 A의 단점 중 "매핑 코드 증가", "Owned Type 등 사용 어려움"은 스위치 ②를 껐을 때의 이야기이고, 이 프로젝트에는 해당하지 않는다
  - A의 단점 "DDD 리포지토리 배치 불가"도 결정 2(Application에 둠)로 이미 해소되었다
  - A의 단점 "Application이 Domain 타입을 노출하면 Api에서 컴파일 에러"는 **그대로 유효하며, 이것이 바로 원하는 효과**다. Application의 공개 메서드는 DTO만 주고받는다
- 판단 질문: 컨트롤러가 Domain 엔티티를 직접 쓰거나 반환하는 것을 컴파일러가 막게 할 것인가, 코드 리뷰로 막을 것인가?
- 선택지
  [간단 비교]
  | **관점** | **전이 참조 차단** | **Infra → Domain 직접 참조** |
  | 경계 강제 |	컴파일러가 보장 | 규율·아키텍처 테스트 필요 |
  | 매핑 코드 | 많음 | 적음 | 
  | DDD 리포지토리 | 배치	인터페이스를 Application으로 이동 | 정석 그대로 가능 | 
  | 도메인 순수성 | 지키기 쉬움 | 오염 위험 | 
  | 생산성 | 낮음 | 높음 | 

  - A. `DisableTransitiveProjectReferences` 적용, 필요한 참조는 모두 명시
    [장점]
    **경계가 컴파일 시점에 강제됨**. Infrastructure가 Application의 포트(인터페이스)와 DTO만 볼 수 있음. 도메인 엔티티를 직접 조작하거나 도메인 로직을 우회하는 코드는 아예 컴파일되지 않음.
    **의존성 그래프가 정직해짐** 각 모듈이 실제로 무엇에 의존하는지 빌드 파일에 그대로 드러남.
    **도메인 오염을 막는 압력** ORM이 도메인 엔티티를 직접 다루지 않음. 따라서 기본 생성자, virtual 프로퍼티, public setter 같은 영속성 요구사항이 도메인으로 새어 들어오는 상황 감소.
    **Domain 변경의 파급이 줄어듦** Domain이 바뀌어도 Application이 그 변화를 흡수하면 Infrastructure는 영향을 받지 않음.

    [단점]
    **DDD(도메인 주도 설계)식 리포지토리 패턴과 충돌** 리포지토리 인터페이스를 Domain에 두고 구현을 Infrastructure에 두는 전통적 배치가 불가능. 인터페이스를 Application으로 옮겨야 함.
    **매핑 코드가 크게 증가** 애그리거트를 저장하려면 별도의 영속성 모델(Persistence Model)을 만들고 도메인 ↔ 영속성 모델 매핑을 직접 작성해야 함. 보일러플레이트와 매핑 버그 증가.
    **실제로는 쉽게 새어 나감** Application의 public API가 도메인 타입을 반환하거나 받으면, 소비자 쪽에도 Domain이 필요해 컴파일 에러 발생. 결국 api로 노출하거나 Application에서 Domain 타입을 감싸야 합니다.(DTO) 차단이 형식적으로만 남기 쉽습니다.
    **ORM의 강점을 포가** EF Core의 Owned Type이나 private 필드 매핑처럼, 도메인 모델을 직접 매핑하도록 설계된 기능을 사용하기 어려움.

  - B. 전이 참조 유지
    [장점]
    **Clean Architecture의 의존성 규칙 준수** "바깥에서 안쪽으로만 의존하라" 에 따라, 가장 바깥인 Infrastructure가 가장 안쪽인 Domain을 참조하는 것은 정당.
    **DDD 리포지토리 배치가 자연스럽다** Domain에 IOrderRepository를 두고 Infrastructure에서 구현하는 정석적인 구조를 그대로 쓸 수 있음.
    **매핑 비용 감소** ORM이 도메인 엔티티를 직접 매핑하므로 별도 영속성 모델과 변환 코드가 필요 없고, 작은 팀이나 CRUD 비중이 큰 서비스에서 생산성 차이 큼.
    **전이 참조에 기대지 않는 명시적 의존** “Application을 통해 우연히 보이는 Domain”이 아니라, 빌드 파일에 의도가 드러나는, 의식적으로 선언한 의존.

    [단점]
    **도메인 로직 우회 가능해짐** Infrastructure 코드가 애그리거트를 임의로 생성·수정하거나 도메인 메서드를 직접 호출 가능. 컴파일러가 막아주지 않으므로 ArchUnit, NetArchTest 같은 아키텍처 테스트나 코드 리뷰로 보완 필요.
    **도메인이 ORM 요구사항에 오염 쉬움** 직접 매핑하다 보면 “EF가 필요하니까” 기본 생성자나 setter를 열어주는 일이 생김. 도메인 순수성이 조금씩 무너짐.
    **결합도가 올라감** 도메인 모델 변경이 곧바로 DB 매핑과 마이그레이션으로 이어짐. 도메인 리팩터링의 비용이 커짐

### 4. 영속 모델 전략: Domain 엔티티를 EF 엔티티로 겸용
- 결정: Domain 엔티티를 EF Core 엔티티로 **겸용**한다. 매핑은 Infrastructure의 **Fluent API**(`IEntityTypeConfiguration<T>`)로만 하고, **지연 로딩은 쓰지 않는다**
- 이유: *(Claude)* EF Core는 private 생성자·백킹 필드·Owned/Complex Type으로 풍부한 도메인 모델을 직접 매핑할 수 있고, Fluent API를 쓰면 Domain이 EF를 참조하지 않아도 된다. 2주 일정에서 매핑 코드를 두 벌 유지할 이유가 없다
- 고려한 대안: 별도 영속 모델 + 매핑 — *(Claude)* 도메인과 DB 스키마가 완전히 독립된다는 장점이 있다. 그러나 엔티티마다 영속 모델과 양방향 매핑이 필요해 코드와 매핑 버그가 늘고, 이 프로젝트의 도메인 크기(엔티티 6~8개)에서는 대가가 이득보다 크다
- 지킬 것
  - Domain 클래스에 EF 어노테이션을 붙이지 않는다. Domain은 EF Core를 참조하지 않는다
  - 쓰기 경로는 `Include`로 엔티티를 불러오고, 읽기 경로는 `Select`로 DTO에 프로젝션한다
  - API 응답에 엔티티를 그대로 쓰지 않는다

## 결과

*(Claude)*

### 한눈에 보기

```
Api ──────────► Application ──────────► Domain
 │  (DTO만)        ▲   (리포지토리 인터페이스)   ▲
 │                 │                         │
 └──► Infrastructure ─────────────────────────┘
      (리포지토리 구현, EF Fluent API 매핑)

✔ Api는 Domain을 볼 수 없다 (전이 참조 차단)
✔ Domain은 아무것도 모른다 (leaf, EF 참조 없음)
✔ 연결은 Api의 Program.cs 한 곳 (Composition Root)
```

### 결정별 득실

| # | 결정 | 얻는 것 | 치르는 것 |
|---|---|---|---|
| 1 | Controllers | 계층별 코드를 직접 쓰는 학습 경험, 익숙한 구조 | 보일러플레이트(클래스·특성·생성자 주입) |
| 2 | 리포지토리 인터페이스 → Application | 유스케이스가 필요한 형태로 계약을 정의, Domain은 규칙만 | DDD 교과서 배치와 다름 |
| 3 | 전이 참조 차단 | Api가 엔티티를 직접 쓰면 **컴파일 에러** | 참조를 모두 명시해야 함(테스트 프로젝트 포함), Application 경계마다 DTO 필요 |
| 4 | 엔티티 겸용 + Fluent API + 지연 로딩 없음 | 매핑 코드 한 벌, N+1이 코드에 드러남 | EF 요구사항(private 생성자 등)이 Domain에 조금 남음, Include를 잊으면 빈 컬렉션 |

### 다시 볼 조건

| 신호 | 다시 볼 결정 |
|---|---|
| EF 때문에 Domain에 public setter나 `virtual`이 필요해진다 | 4 |
| Application ↔ DTO 변환 코드가 유스케이스 로직보다 커진다 | 3 |
| 엔드포인트가 단순 CRUD뿐이라 Controller가 빈 껍데기가 된다 | 1 |
| DB 스키마를 도메인과 다르게 바꿔야 하는 요구(성능 튜닝 등)가 생긴다 | 4 (D8~D9에서 확인) |
