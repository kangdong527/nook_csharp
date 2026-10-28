namespace Nook.SharedKernel;

// 도메인의 "예상 가능한 실패"를 표현한다. HTTP 상태 코드는 모른다 — Api 층이 Type을 보고 번역한다.

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    // 토큰 검증(미들웨어)은 통과했지만 그 주체를 인정할 수 없을 때 — 예: 회원 행 없음(AU-04).
    // D2-1에선 "401은 미들웨어 몫"이라 뺐다가 FE-1에서 추가. ErrorOr 등 널리 쓰는 Result 라이브러리와 같은 분류다.
    Unauthorized
}

public sealed record Error(string Code, string Description, ErrorType Type)
{
    public string Code { get; } = string.IsNullOrWhiteSpace(Code)
        ? throw new ArgumentException("오류 식별자는 비어 있을 수 없습니다.", nameof(Code))
        : Code;
}
