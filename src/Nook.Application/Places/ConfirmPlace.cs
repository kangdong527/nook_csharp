using Nook.Domain.Places;
using Nook.SharedKernel;

namespace Nook.Application.Places;

// PL-01 선택한 장소를 확정한다. 프론트가 카카오 검색 결과 1건을 보낸다 — 입력은 불신(값 객체가 검증).
public sealed record ConfirmPlaceCommand(
    string? KakaoPlaceId,
    string? Name,
    string? Address,
    double Latitude,
    double Longitude);

// Api는 Domain을 볼 수 없어(ADR-001 결정 3) 원시 값으로 넘긴다.
public sealed record PlaceView(Guid Id, string KakaoPlaceId, string Name, string Address, double Latitude, double Longitude)
{
    internal static PlaceView From(Place place) => new(
        place.Id.Value,
        place.KakaoPlaceId.Value,
        place.Name,
        place.Address,
        place.Coordinate.Latitude,
        place.Coordinate.Longitude);
}

// 트랜잭션 없음: 문장 하나하나가 자동 커밋이고, 문장 사이에 함께 되돌려야 할 쓰기가 없다.
public sealed class ConfirmPlaceHandler(IPlaceRepository places)
{
    public async Task<Result<PlaceView>> HandleAsync(ConfirmPlaceCommand command, CancellationToken ct)
    {
        if (!KakaoPlaceId.Create(command.KakaoPlaceId).TryGetValue(out var kakaoPlaceId, out var error))
            return error;

        // 1) 대부분 이미 있는 장소를 다시 고른다 → 1왕복으로 끝
        if (await places.FindByKakaoIdAsync(kakaoPlaceId, ct) is { } existing)
            return PlaceView.From(existing);

        if (!CreatePlace(kakaoPlaceId, command).TryGetValue(out var newPlace, out error))
            return error;

        // 2) 동시 등록에서 이기면 1행
        if (await places.TryAddAsync(newPlace, ct) is { } added)
            return PlaceView.From(added);

        // 3) 0행 = 같은 카카오 ID를 상대가 먼저 커밋했다 → 그 행을 읽어 돌려준다(두 회원이 같은 장소를 받는다, PL-01 3a)
        //    반드시 있는 근거: ON CONFLICT는 미커밋 상대를 기다려 커밋된 경우에만 0행, Read Committed는 문장마다 새 스냅샷, 장소 삭제 없음
        //    셋 중 하나가 깨진 것이므로 회원 탓이 아니라 서버 결함 → Result가 아니라 예외(500)
        return PlaceView.From(await places.FindByKakaoIdAsync(kakaoPlaceId, ct)
            ?? throw new InvalidOperationException("ON CONFLICT 0행 -> 같은 카카오 장소 ID의 행을 찾지 못했습니다."));
    }

    private static Result<Place> CreatePlace(KakaoPlaceId kakaoPlaceId, ConfirmPlaceCommand command)
    {
        if (!Coordinate.Create(command.Latitude, command.Longitude).TryGetValue(out var coordinate, out var error))
            return error;

        return Place.Create(kakaoPlaceId, command.Name, command.Address, coordinate);
    }
}
