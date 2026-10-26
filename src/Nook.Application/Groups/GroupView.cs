using Nook.Domain.Groups;

namespace Nook.Application.Groups;

// GR-02 응답. 만든 회원이 곧바로 공유해야 하므로 초대 코드를 담는다(사용자 결정).
public sealed record GroupView(Guid Id, string Name, string InviteCode)
{
    public static GroupView From(Group group) =>
        new(group.Id.Value, group.Name.Value, group.InviteCode.Value);
}
