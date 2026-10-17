using Nook.Domain.Common;

namespace Nook.Domain.Places;

// 장소 애그리거트 루트. 프론트가 카카오 SDK로 검색해 고른 장소를 회원이 보낸다(PL-01) — 입력은 불신.
// 지키는 규칙: R-PL-01(카카오 장소 ID 유일 — 다른 인스턴스와 비교하므로 DB가 최종 보장, Domain은 형식만),
// R-PL-02(좌표 범위, Coordinate가 검증).
// name·address는 화면 표시용 문자열이라 값 객체로 감싸지 않는다.
// 시각 없음: 장소는 여러 회원이 공유하는 참조 데이터이고, 최초 등록 시각을 쓰는 규칙·화면이 없다.
public sealed class Place
{
    public const int NameMaxLength = 100;
    public const int AddressMaxLength = 200;

    public PlaceId Id { get; }
    public KakaoPlaceId KakaoPlaceId { get; }
    public string Name { get; }
    public string Address { get; }
    public Coordinate Coordinate { get; }

    private Place(PlaceId id, KakaoPlaceId kakaoPlaceId, string name, string address, Coordinate coordinate)
    {
        Id = id;
        KakaoPlaceId = kakaoPlaceId;
        Name = name;
        Address = address;
        Coordinate = coordinate;
    }

    // EF Core 복원 전용. EF는 복합 프로퍼티(Coordinate)를 생성자 매개변수로 넘기지 못하므로, 이 생성자로 만든 직후 Coordinate를 채운다.
    // 앱 코드는 이 생성자를 쓰지 않는다 — Coordinate가 null인 Place는 정상 흐름에 없다.
    private Place(PlaceId id, KakaoPlaceId kakaoPlaceId, string name, string address)
    {
        Id = id;
        KakaoPlaceId = kakaoPlaceId;
        Name = name;
        Address = address;
        Coordinate = null!;
    }

    public static Result<Place> Create(
        KakaoPlaceId kakaoPlaceId,
        string? name,
        string? address,
        Coordinate coordinate)
    {
        // 1) 이미 검증된 값 객체가 null이면 호출 코드의 실수 → 예외
        ArgumentNullException.ThrowIfNull(kakaoPlaceId);
        ArgumentNullException.ThrowIfNull(coordinate);

        // 2) name: 회원 입력 → Result. 비면 화면에 보여 줄 것이 없으므로 필수.
        //    길이는 코드 포인트로 센다(PostgreSQL varchar(n)과 같은 기준).
        var normalizedName = name?.Trim() ?? string.Empty;
        var nameLength = normalizedName.EnumerateRunes().Count();
        if (nameLength == 0 || nameLength > NameMaxLength)
            return PlaceErrors.NameLength;

        // 3) address: 카카오 응답에 빈 주소가 올 수 있다고 보고 느슨하게 빈 값 허용, 상한만 검사.
        var normalizedAddress = address?.Trim() ?? string.Empty;
        if (normalizedAddress.EnumerateRunes().Count() > AddressMaxLength)
            return PlaceErrors.AddressTooLong;

        return new Place(PlaceId.New(), kakaoPlaceId, normalizedName, normalizedAddress, coordinate);
    }
}
