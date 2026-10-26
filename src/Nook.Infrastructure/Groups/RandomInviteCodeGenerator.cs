using System.Security.Cryptography;
using Nook.Application.Groups;
using Nook.Domain.Groups;

namespace Nook.Infrastructure.Groups;

// 초대 코드는 그룹에 들어오는 열쇠라 추측할 수 없어야 한다 → Random이 아니라 RandomNumberGenerator.
internal sealed class RandomInviteCodeGenerator : IInviteCodeGenerator
{
    public InviteCode Next() => InviteCode.Generate(RandomNumberGenerator.GetInt32);
}
