namespace Nook.Domain.Reviews;

// R-RV-03 "하루"의 기준 — KST 00:00(FE-2 사용자 결정, D2-2의 UTC에서 변경). 서비스 지역이 한국으로 한정되고(PL-01)
// 원본 명세가 "00시 기준"이라 사용자가 느끼는 하루와 맞춘다. KST는 UTC+9 고정(1988년 이후 서머타임 없음)이라
// 시간대 데이터베이스 없이 오프셋으로 정확하다. 작성일 계산은 이 한 곳 — Review 생성자와 RV-01 사전 확인이 함께 쓴다.
public static class ReviewDay
{
    public static readonly TimeSpan KstOffset = TimeSpan.FromHours(9);

    public static DateOnly Of(DateTimeOffset at) => DateOnly.FromDateTime(at.ToOffset(KstOffset).DateTime);
}
