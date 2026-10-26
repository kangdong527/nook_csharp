using Nook.Domain.Groups;

namespace Nook.Application.Groups;

// 새 초대 코드 발급(R-GR-05). 운영은 암호학적 난수, 테스트는 정해 둔 순서로 충돌(R-GR-06)을 재현한다.
public interface IInviteCodeGenerator
{
    InviteCode Next();
}
