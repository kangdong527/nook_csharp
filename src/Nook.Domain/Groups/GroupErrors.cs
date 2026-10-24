using Nook.SharedKernel;

namespace Nook.Domain.Groups;

public static class GroupErrors
{
    public static readonly Error InviteCodeInvalidFormat = new(
    "Group.InviteCodeInvalidFormat",
    $"초대 코드는 영문과 숫자로 된 {InviteCode.Length}자리입니다.",
    ErrorType.Validation);

    public static readonly Error GroupNameLengthOutOfRange = new(
    "Group.GroupNameLengthOutOfRange",
    $"그룹명은 앞뒤 공백을 제외하고 {GroupName.MinLength}~{GroupName.MaxLength}자여야 합니다.",
    ErrorType.Validation);

    public static readonly Error GroupNameInvalidCharacters = new(
    "Group.GroupNameInvalidCharacters",
    "그룹명에는 한글, 영문, 숫자, 공백만 쓸 수 있습니다.",
    ErrorType.Validation);

    public static readonly Error AlreadyMember = new(
    "Group.AlreadyMember",
    "이미 이 그룹의 멤버입니다.",
    ErrorType.Conflict);

    public static readonly Error GroupFull = new(
    "Group.GroupFull",
    $"그룹 정원({Group.MaxGroupMembers}명)이 가득 찼습니다.",
    ErrorType.Conflict);
}
