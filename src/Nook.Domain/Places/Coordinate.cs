using Nook.SharedKernel;

namespace Nook.Domain.Places;

// 좌표 값 객체. 물리적 범위(지구)만 검증한다. 서비스 지역(한국 영역) 판단은 정책이므로 여기서 하지 않는다.
public sealed record Coordinate
{
    public const int MinLatitude = -90;
    public const int MaxLatitude = 90;
    public const int MinLongitude = -180;
    public const int MaxLongitude = 180;

    private Coordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }
    public double Longitude { get; }

    public static Result<Coordinate> Create(double latitude, double longitude)
    {
        if (latitude is not (>= MinLatitude and <= MaxLatitude))
            return PlaceErrors.CoordinateLatitudeOutOfRange;

        if (longitude is not (>= MinLongitude and <= MaxLongitude))
            return PlaceErrors.CoordinateLongitudeOutOfRange;

        return new Coordinate(latitude, longitude);
    }
}
