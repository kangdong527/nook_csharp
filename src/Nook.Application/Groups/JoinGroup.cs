using Nook.Application.Common;
using Nook.Application.Users;
using Nook.Domain.Groups;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Groups;

// GR-01 초대 코드로 그룹에 합류한다. UserId는 토큰의 sub, InviteCode는 본문 값.
// 성공하면 GroupView — 클라이언트는 코드만 알고 들어오므로 그룹 화면으로 갈 ID·이름을 돌려준다(D6-2 사용자 결정).
public sealed record JoinGroupCommand(Guid UserId, string? InviteCode);

// D6 (가) FOR UPDATE(사용자 결정): 그룹 행을 잠근 뒤 멤버를 읽어 AddMember가 정원을 판단한다(R-GR-03).
// 3b·5a(R-GR-02 참여 그룹 3개)는 users 행 잠금(R-GR-04) 뒤에 센다. 잠금 순서는 항상 users → groups(D6 사용자 결정) — 반대 순서 경로가 생기면 교착.
public sealed class JoinGroupHandler(IUserRepository users, IGroupRepository groups, IUnitOfWork uow, TimeProvider time)
{
    public async Task<Result<GroupView>> HandleAsync(JoinGroupCommand command, CancellationToken ct)
    {
        // 2단계(2a). DB 없는 검사는 트랜잭션 밖에서 — 잠금을 쥐는 시간을 줄인다.
        if (!InviteCode.Create(command.InviteCode).TryGetValue(out var inviteCode, out var codeError))
            return codeError;

        var userId = UserId.From(command.UserId);

        // 4·5단계 — 그룹 행 잠금부터 COMMIT까지 한 트랜잭션.
        await using var tx = await uow.BeginAsync(ct);

        await users.LockAsync(userId, ct);

        var group = await groups.LockByInviteCodeAsync(inviteCode, ct);

        if (group == null)
            return JoinGroupErrors.GroupNotFound;

        if (group.AddMember(userId, time.GetUtcNow()).IsFailure(out var error))
            return error;

        // 3b. 참여 수는 AddMember 뒤에 센다 — 이미 멤버인 그룹에 다시 들어오면 TooManyGroups보다 AlreadyMember가 정확하다(D6 사용자 결정).
        // 그래서 GroupFull도 TooManyGroups보다 먼저다. users 잠금은 위에서 이미 쥐었으므로 센 값은 커밋까지 유지된다.
        // 여기서 돌아가면 메모리의 AddMember 변경은 저장되지 않고 버려진다(SaveAsync 전, Dispose = 롤백).
        if (await groups.CountMembershipsAsync(userId, ct) >= Group.MaxGroupsPerUser)
            return GroupErrors.TooManyGroups;

        await groups.SaveAsync(group, ct);
        await tx.CommitAsync(ct);

        return GroupView.From(group);
    }
}
