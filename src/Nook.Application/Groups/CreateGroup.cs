using Nook.Domain.Groups;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Groups;

// GR-02 그룹을 만든다. UserId는 토큰의 sub, Name은 본문 값.
public sealed record CreateGroupCommand(Guid UserId, string? Name);

// 그룹 + 만든 회원(R-GR-08)은 TryAddAsync의 SaveChanges 한 번으로 함께 저장된다 — 원본의 "그룹만 생기고 멤버 등록 실패"가 구조적으로 없다.
// 초대 코드 충돌(R-GR-06)은 새 코드로 다시 만든다(사용자 결정: 최대 3회, 그래도 실패하면 예외).
// 아직 없는 것: R-GR-02(참여 그룹 3개) + users 행 잠금(R-GR-04) — D6 다음 단계에서 합류와 함께.
public sealed class CreateGroupHandler(IGroupRepository groups, IInviteCodeGenerator codes, TimeProvider time)
{
    public const int MaxInviteCodeAttempts = 3;

    public async Task<Result<GroupView>> HandleAsync(CreateGroupCommand command, CancellationToken ct)
    {
        if (!GroupName.Create(command.Name).TryGetValue(out var name, out var nameError))
            return nameError;

        var creatorId = UserId.From(command.UserId);
        var now = time.GetUtcNow();

        // 시도마다 새 Group(새 ID·새 코드) — 실패한 그룹은 TryAddAsync가 추적기에서 떼어 냈다.
        // 재시도는 코드 충돌(false)만. FK 등 다른 DB 오류는 다시 해도 같으므로 예외 그대로 올라간다.
        for (var attempt = 1; attempt <= MaxInviteCodeAttempts; attempt++)
        {
            var group = Group.Create(name, creatorId, codes.Next(), now);
            if (await groups.TryAddAsync(group, ct))
                return GroupView.From(group);
        }

        // 10.7억 칸에서 연속 충돌 = 사실상 생성기 고장. 회원 입력 문제가 아니므로 Result가 아니라 예외.
        throw new InvalidOperationException($"초대 코드가 {MaxInviteCodeAttempts}번 연속 충돌했습니다(생성기 점검 필요).");
    }
}
