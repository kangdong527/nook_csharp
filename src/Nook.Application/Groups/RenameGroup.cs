using Nook.Application.Common;
using Nook.Domain.Groups;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Groups;

// GR-03 그룹 이름을 바꾼다 — 방장만(R-GR-11). 나가기(방장 넘기기)와 같은 그룹 행 잠금 안에서 방장을 확인한다:
// 잠그지 않으면 "방장이 나가 넘어간 직후"에 옛 방장의 이름 변경이 끼어들 수 있다.
public sealed record RenameGroupCommand(Guid UserId, Guid GroupId, string? Name);

public sealed class RenameGroupHandler(IUnitOfWork uow, IGroupRepository groups)
{
    public async Task<Result<GroupView>> HandleAsync(RenameGroupCommand command, CancellationToken ct)
    {
        if (!GroupName.Create(command.Name).TryGetValue(out var name, out var nameError))
            return nameError;

        await using var tx = await uow.BeginAsync(ct);

        var group = await groups.LockByIdAsync(GroupId.From(command.GroupId), ct);
        if (group is null)
            return GroupQueryErrors.GroupNotFound;

        // 멤버가 아닌 회원도 방장이 아니므로 403 — 그룹 ID는 추측할 수 없는 UUID라 존재를 숨길 이득이 작다(리뷰 403과 같은 판단).
        if (group.Rename(name, UserId.From(command.UserId)).IsFailure(out var renameError))
            return renameError;

        await groups.SaveAsync(group, ct);
        await tx.CommitAsync(ct);
        return GroupView.From(group);
    }
}

public static class GroupQueryErrors
{
    // ID로 찾은 그룹이 없다(지워졌거나 없는 ID). 초대 코드로 찾는 GR-01은 JoinGroupErrors.GroupNotFound(같은 코드, 다른 안내).
    public static readonly Error GroupNotFound = new(
        "Group.NotFound",
        "그룹을 찾을 수 없습니다.",
        ErrorType.NotFound);
}
