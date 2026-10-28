using Nook.Domain.Groups;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Groups;

// GR-05 그룹 지도 — 현재 멤버 전원의 리뷰·북마크 장소. 멤버만 볼 수 있다(403, 없는 그룹 404).
// 장소마다 한 행(FE-2 사용자 결정 A): 별 하나를 그리는 데 필요한 "사실"만 서버가 집계하고, 그리는 규칙(색·숫자·외곽선)은 FE가 정한다
// — 디자인(색)이 확정되기 전이라 표시 규칙이 바뀌어도 API는 그대로. PM 표시 정책은 fe-backend-backlog.md "그룹맵 표시 정책".
public sealed record GetGroupPlacesQuery(Guid UserId, Guid GroupId);

// ReviewMemberCount: 이 장소에 리뷰를 쓴 멤버 수(한 멤버가 여러 날 써도 1, 사용자 결정). 0이면 북마크만 있는 장소.
// FirstReviewerSlot: 가장 먼저 리뷰한 멤버의 색 슬롯(현재 멤버 중 — 그 멤버가 나가면 다음 사람으로 바뀐다, 저장하지 않고 계산).
// BookmarkCount: 북마크한 멤버 수. SoleBookmarkerSlot: 북마크가 정확히 1개일 때 그 멤버의 슬롯(외곽선 색).
public sealed record GroupPlaceView(
    Guid PlaceId,
    string KakaoPlaceId,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    int ReviewMemberCount,
    int? FirstReviewerSlot,
    int BookmarkCount,
    int? SoleBookmarkerSlot);

public sealed class GetGroupPlacesHandler(IGroupRepository groups)
{
    public async Task<Result<IReadOnlyList<GroupPlaceView>>> HandleAsync(GetGroupPlacesQuery query, CancellationToken ct)
    {
        var groupId = GroupId.From(query.GroupId);
        switch (await groups.IsMemberAsync(groupId, UserId.From(query.UserId), ct))
        {
            case null:
                return GroupQueryErrors.GroupNotFound;
            case false:
                return GroupQueryErrors.NotMember;
        }

        return Result<IReadOnlyList<GroupPlaceView>>.Success(await groups.ListPlacesAsync(groupId, ct));
    }
}
