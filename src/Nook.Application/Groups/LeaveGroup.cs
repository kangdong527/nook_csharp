using Nook.Application.Common;
using Nook.Domain.Groups;
using Nook.Domain.Users;
using Nook.SharedKernel;
using System.Diagnostics;

namespace Nook.Application.Groups;

// GR-04 그룹에서 나간다. UserId는 토큰의 sub, GroupId는 경로 값.
public sealed record LeaveGroupCommand(Guid UserId, Guid GroupId);

// R-GR-09: 마지막 멤버가 나가면 같은 트랜잭션에서 그룹을 지운다. 남은 두 멤버의 동시 탈퇴(Write Skew)는 그룹 행 잠금으로 줄 세운다.
// users 행은 잠그지 않는다 — 탈퇴는 참여 수를 줄이기만 해서 R-GR-02를 깨지 못한다(D6 사용자 판단). 잠그는 행이 하나라 잠금 순서와 무관하다.
// 멱등: 멤버가 아니거나 그룹이 이미 없어도 성공(usecases.md R-GR-09 메모, D6 사용자 결정).
public sealed class LeaveGroupHandler(IGroupRepository groups, IUnitOfWork uow)
{
    public async Task<Result> HandleAsync(LeaveGroupCommand command, CancellationToken ct)
    {
        var userId = UserId.From(command.UserId);
        var groupId = GroupId.From(command.GroupId);

        await using var tx = await uow.BeginAsync(ct);

        var group = await groups.LockByIdAsync(groupId, ct);
        if (group == null)
            return Result.Success();

        var memberRemoval = group.RemoveMember(userId);

        

        switch (memberRemoval) 
        {
            case MemberRemoval.NotMember:
                break;

            case MemberRemoval.Removed:
                await groups.SaveAsync(group, ct);
                break;

            case MemberRemoval.GroupEmptied:
                await groups.DeleteAsync(group, ct);
                break;

            default:
                throw new UnreachableException($"처리하지 않은 {nameof(MemberRemoval)}: {memberRemoval}");
        }
        
        await tx.CommitAsync(ct);
        return Result.Success();
    }
}
