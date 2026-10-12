namespace Nook.Domain.Places;

// 장소 식별자. Guid를 감싸 UserId·ReviewId 등과 섞어 넘기는 실수를 컴파일 시점에 막는다.
// record class: default/배열 원소가 null이 되어 nullable 경고로 드러난다(struct면 Guid.Empty가 조용히 통과).
public sealed record PlaceId
{
    public Guid Value { get; }

    private PlaceId(Guid value)
    {
        Value = value;
    }

    // 새 장소용 발급. v7: 시간 순으로 커져서 PK B-tree 삽입이 인덱스 끝에 모여 페이지 분할이 적다(v4는 무작위 위치에 흩어짐).
    public static PlaceId New() => new(Guid.CreateVersion7());

    // DB 값 복원(EF Core 값 변환기 입력). Guid.Empty는 정상 흐름에서 나올 수 없으므로 개발자 실수·데이터 손상으로 보고 예외.
    public static PlaceId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PlaceId에 Guid.Empty를 쓸 수 없습니다.", nameof(value));

        return new PlaceId(value);
    }
}
