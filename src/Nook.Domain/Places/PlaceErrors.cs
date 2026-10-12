using Nook.Domain.Common;

namespace Nook.Domain.Places;

public static class PlaceErrors
{
    public static readonly Error CoordinateLatitudeOutOfRange = new(
    "Place.CoordinateLatitudeOutOfRange",
    $"위도가 지구 범위({Coordinate.MinLatitude}~{Coordinate.MaxLatitude})를 벗어났습니다",
    ErrorType.Validation);

    public static readonly Error CoordinateLongitudeOutOfRange = new(
    "Place.CoordinateLongitudeOutOfRange",
    $"경도가 지구 범위({Coordinate.MinLongitude}~{Coordinate.MaxLongitude})를 벗어났습니다",
    ErrorType.Validation);

    public static readonly Error KakaoPlaceIdEmpty = new(
    "Place.KakaoPlaceIdEmpty",
    "카카오 장소 ID가 비어 있습니다.",
    ErrorType.Validation);

    public static readonly Error KakaoPlaceIdTooLong = new(
    "Place.KakaoPlaceIdTooLong",
    $"카카오 장소 ID는 최대 {KakaoPlaceId.MaxLength}자여야 합니다.",
    ErrorType.Validation);

    public static readonly Error NameLength = new(
    "Place.NameLength",
    $"장소 이름은 앞뒤 공백을 제외하고 1~{Place.NameMaxLength}자여야 합니다.",
    ErrorType.Validation);

    public static readonly Error AddressTooLong = new(
    "Place.AddressTooLong",
    $"장소 주소는 최대 {Place.AddressMaxLength}자여야 합니다.",
    ErrorType.Validation);
}
