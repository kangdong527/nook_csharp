using Nook.Domain.Places;

namespace Nook.Application.Places;

// 장소 저장 담당(erd.md places). 카카오 장소 ID 유일성(R-PL-01)은 ux_places_kakao_place_id가 최종 보장한다.
// 장소는 삭제하지 않는다 — PL-01 흐름(TryAdd 0행 → Find는 반드시 1행)이 이 전제에 기댄다.
public interface IPlaceRepository
{
    // erd.md places Q1.
    Task<Place?> FindByKakaoIdAsync(KakaoPlaceId kakaoPlaceId, CancellationToken ct);

    // erd.md places Q3. INSERT ... ON CONFLICT (kakao_place_id) DO NOTHING RETURNING.
    // 같은 카카오 ID가 이미 있으면(동시 등록에서 진 쪽 포함) 예외가 아니라 null. 그 밖의 오류는 예외 그대로.
    Task<Place?> TryAddAsync(Place place, CancellationToken ct);
}
