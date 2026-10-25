using Nook.Application.Common;
using Nook.Application.Users;
using Nook.Domain.Groups;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Groups;

// GR-01 초대 코드로 그룹에 합류한다. UserId는 토큰의 sub, InviteCode는 본문 값.
public sealed record JoinGroupCommand(Guid UserId, string? InviteCode);

// D6 (가) FOR UPDATE(사용자 결정): 그룹 행을 잠근 뒤 멤버를 읽어 AddMember가 정원을 판단한다(R-GR-03).
// 3b·5a(R-GR-02 참여 그룹 3개)는 users 행 잠금(R-GR-04) 뒤에 센다. 잠금 순서는 항상 users → groups(D6 사용자 결정) — 반대 순서 경로가 생기면 교착.
public sealed class JoinGroupHandler(IUserRepository users, IGroupRepository groups, IUnitOfWork uow, TimeProvider time)
{
    public async Task<Result> HandleAsync(JoinGroupCommand command, CancellationToken ct)
    {
        // 2단계(2a). DB 없는 검사는 트랜잭션 밖에서 — 잠금을 쥐는 시간을 줄인다.
        if (!InviteCode.Create(command.InviteCode).TryGetValue(out var inviteCode, out var codeError))
            return codeError;

        var userId = UserId.From(command.UserId);

        // 4·5단계 — 그룹 행 잠금부터 COMMIT까지 한 트랜잭션.
        await using var tx = await uow.BeginAsync(ct);

        await users.LockAsync(userId, ct);
        if (await groups.CountMembershipsAsync(userId, ct) >= Group.MaxGroupsPerUser)
            return GroupErrors.TooManyGroups;

        var group = await groups.LockByInviteCodeAsync(inviteCode, ct);

        if (group == null)
            return JoinGroupErrors.GroupNotFound;

        if (group.AddMember(userId, time.GetUtcNow()).IsFailure(out var error))
            return error;

        await groups.SaveAsync(group, ct);
        await tx.CommitAsync(ct);

        return Result.Success();
    }
}
