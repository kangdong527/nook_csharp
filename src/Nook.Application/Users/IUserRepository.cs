using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Users;

// 회원 저장 담당(erd.md users). 유일성(R-US-02 닉네임·R-US-03 이메일)은 DB UNIQUE가 최종 보장한다.
public interface IUserRepository
{
    // erd.md users Q4. 대소문자 무시.
    Task<User?> FindByEmailAsync(Email email, CancellationToken ct);

    // INSERT 후 저장까지 한다. 닉네임 UNIQUE 위반은 실패(UserErrors.NicknameTaken)로 돌려준다 — 예상 가능한 실패.
    // 위반이 나면 PostgreSQL 트랜잭션은 중단 상태가 된다. 호출한 쪽은 더 쓰지 말고 돌아가야 한다(Dispose = 롤백).
    Task<Result> AddAsync(User user, CancellationToken ct);
}
