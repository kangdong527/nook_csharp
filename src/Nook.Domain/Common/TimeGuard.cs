using System.Runtime.CompilerServices;

namespace Nook.Domain.Common;

// 엔티티 시각(CreatedAt·JoinedAt 등) 검증. 시각은 TimeProvider.GetUtcNow()에서만 와야 하므로,
// 어긋나면 사용자 실패가 아니라 호출 코드의 실수로 보고 예외를 던진다.

/*
 정적 가드를 고른 이유

- 빠뜨릴 위험이 낮음. 엔티티는 private 생성자와 Create 하나로만 만들어지므로, 시각이 들어오는 길은 엔티티마다 정확히 한 곳입니다. 검사를 빠뜨려도 Create 한 줄만 보면 드러남
- 값 객체의 비용이 이득보다 큼. UtcTimestamp를 만들면 D3에서 시각 컬럼마다 값 변환기가 필요하고, GetUtcNow()의 결과를 매번 감싸야 함
- 호출이 한 줄로 끝남. TimeGuard.Utc(createdAt)을 생성자 인자 자리에 바로 넣으면 검증과 전달이 함께 끝나고, 검증하지 않은 값이 엔티티에 들어갈 틈이 생기지 않습니다.
 */
public static class TimeGuard
{
    // 검증을 통과한 값을 그대로 돌려줘서 호출 한 줄로 검증과 대입이 함께 끝나게 한다.
    // paramName: 호출부의 인자 식(예: "createdAt")이 자동으로 들어가 예외 메시지가 원래 인자 이름을 가리킨다.
    public static DateTimeOffset Utc(
        DateTimeOffset value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == default)
            throw new ArgumentException("시각이 지정되지 않았습니다.", paramName);
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("시각은 UTC(오프셋 0)여야 합니다.", paramName);

        return value;
    }
}
