using Microsoft.EntityFrameworkCore;
using Nook.Application.Places;
using Nook.Domain.Places;
using Nook.Infrastructure.Persistence;

namespace Nook.Infrastructure.Places;

internal sealed class PlaceRepository(NookDbContext db) : IPlaceRepository
{
    public Task<Place?> FindByKakaoIdAsync(KakaoPlaceId kakaoPlaceId, CancellationToken ct) =>
        // erd.md places Q1. 받는 인덱스: ux_places_kakao_place_id. 값 변환기로 문자열 그대로 비교(대소문자 구분 — 카카오 ID 원형).
        db.Places.AsNoTracking().SingleOrDefaultAsync(p => p.KakaoPlaceId == kakaoPlaceId, ct);

    public async Task<Place?> TryAddAsync(Place place, CancellationToken ct)
    {
        // erd.md places Q3. 충돌 판정: ux_places_kakao_place_id. 0행 = 같은 카카오 ID가 이미 커밋됨(진 쪽).
        // 엔티티가 아니라 id만 받는다: FromSql은 ComplexProperty(Coordinate)의 HasColumnName을 무시하고
        // 'Coordinate_Latitude'를 찾아 실패한다(LINQ는 정상). D5-5 탐침으로 확인.
        // 1행이면 넘겨받은 place를 그대로 돌려준다 — 저장된 값과 같다(값 객체가 검증, DB가 채우거나 바꾸는 열 없음).
        // places에 DEFAULT·트리거로 채우는 열이 생기면 이 전제가 깨진다 → 그때는 다시 읽는다.
        // ToList인 이유: SingleOrDefaultAsync는 SQL을 서브쿼리로 감싸는데, PostgreSQL은 서브쿼리 안의 INSERT를 허용하지 않는다.
        var inserted = await db.Database.SqlQuery<Guid>($"""
            INSERT INTO places (id, kakao_place_id, name, address, latitude, longitude)
            VALUES ({place.Id.Value}, {place.KakaoPlaceId.Value}, {place.Name}, {place.Address},
                    {place.Coordinate.Latitude}, {place.Coordinate.Longitude})
            ON CONFLICT (kakao_place_id) DO NOTHING
            RETURNING id AS "Value"
            """).ToListAsync(ct);

        return inserted is [_] ? place : null;
    }
}
