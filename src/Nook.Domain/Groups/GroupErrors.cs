using Nook.Domain.Common;

namespace Nook.Domain.Groups;

public static class GroupErrors
{
    public static readonly Error InviteCodeInvalidFormat = new(
    "Group.InviteCodeInvalidFormat",
    $"초대 코드는 영문과 숫자로 된 {InviteCode.Length}자리입니다.",
    ErrorType.Validation);
}
