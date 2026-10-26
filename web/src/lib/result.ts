// =========================================================
// Result 패턴 — 예상 가능한 실패를 타입으로 표현한다
//
// 규칙(docs/아키텍처_패턴.md §3):
//   예상 가능(사용자에게 문구 필요)  → Result 반환
//   예상 불가능(사용자가 할 게 없음) → throw  (→ app/error.tsx + Sentry)
//
// 이 파일은 계층 ①(도메인)이다. 외부 의존 0을 유지한다.
// (네트워크·DOM·Supabase·React import 금지)
// =========================================================

// ---------------------------------------------------------
// TODO 1 — Result 타입을 판별 유니온(discriminated union)으로 선언한다.
//
//   판별자(discriminant)는 `ok` 필드다. `ok`를 검사하면 TS가 나머지 필드를
//   자동으로 좁혀 준다(narrowing) — 성공 분기에서는 `value`만, 실패 분기에서는
//   `reason`만 접근 가능해진다.
//
//   시그니처 의도:
//     T                  성공 시 실어 보낼 값의 타입 (제네릭)
//     E extends string   실패 이유. 문자열 **리터럴 유니온**으로만 받는다.
//
//   왜 `E extends string`인가:
//     제약이 없으면 `E`에 `string`이 들어와 문구 매핑 switch의 never 체크가
//     무력화된다(모든 문자열이 가능해지므로 "누락"을 판정할 수 없다).
//     리터럴 유니온으로 좁혀야 케이스 누락이 컴파일 에러가 된다.
// ---------------------------------------------------------
export type Result<T, E extends string> = 
  | { ok: true; value: T }
  | { ok: false; reason: E };

// ---------------------------------------------------------
// TODO 2 — 생성 헬퍼 두 개.
//
//   `as const`를 붙이는 이유를 확인하며 작성할 것:
//     as const 없이 { ok: true, value } 를 반환하면 `ok`의 타입이 `boolean`으로
//     넓어진다(widening). 그러면 판별 유니온이 성립하지 않아 호출부에서
//     `if (res.ok)` 로 좁히기가 동작하지 않는다.
//
//   힌트: `ok(1)` 은 `{ readonly ok: true; readonly value: number }` 가 되어야 한다.
//   확인법: 아래 줄을 임시로 넣고 에디터에서 타입을 확인 → 확인 후 지운다.
//     const _probe = ok(1);   // 마우스를 올려 `ok: true` 인지 `ok: boolean` 인지 본다
// ---------------------------------------------------------
export const ok  = <T>(value: T) => ({ ok: true, value }) as const;
export const err = <E extends string>(reason: E) => ({ ok: false, reason }) as const;

// ---------------------------------------------------------
// TODO 3 — 케이스 누락을 컴파일 에러로 만드는 헬퍼.
//
//   문구 매핑 switch 의 default 에서 호출한다. 인자 타입이 `never` 이므로,
//   처리하지 않은 reason 이 하나라도 남아 있으면 그 값이 never 에 할당되지
//   않아 **빌드가 실패**한다.
//
//   사용 예 (화면 계층 — reason → 문구 매핑은 화면의 책임이다):
//     switch (reason) {
//       case "not_found": return "존재하지 않는 초대 코드예요.";
//       case "full":      return "그룹 정원(4명)이 가득 찼어요.";
//       default: return exhaustive(reason);   // ← "already" 를 빼먹으면 여기서 에러
//     }
//
//   왜 반환 타입이 `never` 인가: 이 함수는 정상 반환하지 않는다(항상 throw).
//   `never` 로 선언하면 위 예시처럼 `return exhaustive(...)` 를 써도
//   함수의 반환 타입 추론을 오염시키지 않는다.
// ---------------------------------------------------------
export function exhaustive(value: never): never {
  // TODO: 도달 불가 지점이다. 값을 담아 throw 한다.
  //       (런타임에 여기 오면 타입이 실제와 어긋났다는 뜻이므로 조용히 넘기지 않는다)
  throw new Error(`unhandled case: ${String(value)}`);
}
