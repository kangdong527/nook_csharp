using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nook.Application.Auth;
using Nook.Application.Common;
using Nook.Application.Groups;
using Nook.Application.Reviews;
using Nook.Domain.Groups;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Users;

// PF-02 회원 탈퇴(DELETE /me). 정책(PM, 2026-10-27): 리뷰·리뷰 이미지·좋아요는 남기고(작성자·누른 사람만 끊긴다 — FK SET NULL),
// 복구는 없다. 한 트랜잭션:
//   1. 회원 행 잠금(FOR UPDATE) — 그룹 합류·생성이 같은 행을 FOR NO KEY UPDATE로 잡으므로 탈퇴 도중 끼어들지 못한다(미결 ①).
//      잠금 순서 users → groups(ADR-010)
//   2. 그룹마다 나가기와 같은 경로(Group.RemoveMember): 방장이면 넘기고(R-GR-11), 마지막 멤버면 그룹 삭제(R-GR-09).
//      group_members·groups.owner_id FK가 RESTRICT라 이 단계를 건너뛰면 4가 실패한다
//   3. 로그인 링크 기록 삭제(이메일 = 개인정보)
//   4. 회원 행 DELETE — 북마크 CASCADE, 리뷰·좋아요 SET NULL
// 커밋 뒤: 프로필 사진 파일 삭제(실패해도 응답은 그대로, 배치가 정리).
public sealed class WithdrawHandler(
    IUnitOfWork uow,
    IUserRepository users,
    IGroupRepository groups,
    IMagicLinkTokenStore tokens,
    IReviewImageStorage storage,
    ILogger<WithdrawHandler> logger)
{
    public async Task<Result> HandleAsync(Guid userIdValue, CancellationToken ct)
    {
        var userId = UserId.From(userIdValue);

        await using var tx = await uow.BeginAsync(ct);

        // 이미 탈퇴했으면(남은 토큰으로 다시 호출) 401 — 다른 "회원 없음"과 같다(AU-04). 프론트는 쿠키를 버린다.
        var user = await users.LockForDeleteAsync(userId, ct);
        if (user is null)
            return AuthErrors.UnknownUser;

        foreach (var groupId in await groups.ListMembershipGroupIdsAsync(userId, ct))
        {
            var group = await groups.LockByIdAsync(groupId, ct);
            if (group is null)
                continue;   // 기다리는 동안 다른 트랜잭션이 지웠다

            switch (group.RemoveMember(userId))
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
                    throw new UnreachableException();
            }
        }

        var avatarKey = user.AvatarKey;
        await tokens.DeleteAllAsync(user.Email, ct);
        await users.DeleteAsync(user, ct);
        await tx.CommitAsync(ct);

        if (avatarKey is not null)
            await ReviewImageCleanup.DeleteAfterCommitAsync(storage, [avatarKey], logger);

        return Result.Success();
    }
}
