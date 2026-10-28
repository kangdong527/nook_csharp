using Nook.Domain.Users;

namespace Nook.Application.Groups;

// GR-06 내 그룹 목록(원본 getMyGroups — 지도 첫 화면). 합류한 순서. 최대 3개(R-GR-02).
// 원본은 그룹마다 멤버 수를 따로 셌다(N+1). 여기선 쿼리 한 번(FE-2 사용자 결정) — 비교 측정은 D9.
public sealed record GetMyGroupsQuery(Guid UserId);

// MyColorSlot: 이 그룹에서 내 색 슬롯(R-GR-12). IsOwner: 이름 변경 버튼을 보일지(R-GR-11) — 서버가 판정한다.
public sealed record MyGroupView(
    Guid Id,
    string Name,
    string InviteCode,
    int MemberCount,
    int MyColorSlot,
    bool IsOwner);

public sealed class GetMyGroupsHandler(IGroupRepository groups)
{
    public async Task<IReadOnlyList<MyGroupView>> HandleAsync(GetMyGroupsQuery query, CancellationToken ct) =>
        await groups.ListMineAsync(UserId.From(query.UserId), ct);
}
