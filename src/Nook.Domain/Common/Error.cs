namespace Nook.Domain.Common;

// 도메인의 "예상 가능한 실패"를 표현한다. HTTP 상태 코드는 모른다 — Api 층이 Type을 보고 번역한다.

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden
}

public sealed record Error(string Code, string Description, ErrorType Type)
{
    public string Code { get; } = string.IsNullOrWhiteSpace(Code)
        ? throw new ArgumentException("오류 식별자는 비어 있을 수 없습니다.", nameof(Code))
        : Code;
}
