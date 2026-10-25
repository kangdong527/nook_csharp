using Nook.SharedKernel;

namespace Nook.Application.Groups;

public static class JoinGroupErrors
{
    // GR-01 4c. 형식은 맞지만 그 코드를 가진 그룹이 없다.
    public static readonly Error GroupNotFound = new(
        "Group.NotFound",
        "초대 코드에 해당하는 그룹을 찾을 수 없습니다.",
        ErrorType.NotFound);
}
