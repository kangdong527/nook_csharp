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
}
