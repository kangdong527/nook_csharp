using Nook.SharedKernel;

namespace Nook.Integration.Tests.Infrastructure;

// Arrange 단계 전용. 테스트 입력은 유효하다고 정한 값이므로 실패하면 테스트 작성 실수 → 즉시 예외로 드러낸다.
public static class TestResult
{
    public static T Unwrap<T>(this Result<T> result) =>
        result.TryGetValue(out var value, out var error)
            ? value
            : throw new InvalidOperationException($"Arrange 입력이 도메인 검증에 실패했습니다: {error}");
}
