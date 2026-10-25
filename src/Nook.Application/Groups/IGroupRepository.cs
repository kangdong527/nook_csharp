using Nook.Domain.Groups;

namespace Nook.Application.Groups;

// 그룹 저장 담당(erd.md groups·group_members). 동시 합류(R-GR-03)는 그룹 행 잠금(D6 (가) FOR UPDATE, 사용자 결정)으로 직렬화한다.
public interface IGroupRepository
{
    // erd.md groups Q1·Q2 + group_members Q1. 초대 코드로 그룹 행을 잠그고, 멤버까지 추적 상태로 돌려준다.
    // 잠금은 트랜잭션 끝까지 유지된다 — 호출한 쪽이 트랜잭션 안에서 불러야 의미가 있다(밖이면 문장 끝에 바로 풀린다).
    Task<Group?> LockByInviteCodeAsync(InviteCode inviteCode, CancellationToken ct);

    // erd.md groups Q4 + group_members Q3. 그룹과 첫 멤버(R-GR-08)를 SaveChanges 한 번으로 INSERT한다 — 한 트랜잭션이라 둘 중 하나만 남지 않는다.
    // 초대 코드가 이미 쓰이고 있으면(ux_groups_invite_code) false — 호출한 쪽이 새 코드로 다시 만든다(R-GR-06). 다른 위반은 예외.
    Task<bool> TryAddAsync(Group group, CancellationToken ct);

    // 추적 중인 그룹의 변경(멤버 추가·제거)을 저장한다. erd.md group_members Q3.
    Task SaveAsync(Group group, CancellationToken ct);
}
