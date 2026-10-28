using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Users;

// 회원 저장 담당(erd.md users). 유일성(R-US-02 닉네임·R-US-03 이메일)은 DB UNIQUE가 최종 보장한다.
public interface IUserRepository
{
    // erd.md users Q4. 대소문자 무시.
    Task<User?> FindByEmailAsync(Email email, CancellationToken ct);

    // erd.md users Q1(PK). 읽기 전용 — 추적하지 않는다(AU-04).
    Task<User?> FindByIdAsync(UserId userId, CancellationToken ct);

    // PF-01. 바꿀 회원을 추적 상태로 읽는다. 없으면 null. 받는 인덱스: pk_users.
    Task<User?> FindForUpdateAsync(UserId userId, CancellationToken ct);

    // FindForUpdateAsync로 읽어 바꾼 회원을 저장한다. 닉네임 UNIQUE 위반은 실패(UserErrors.NicknameTaken) — AddAsync와 같다.
    Task<Result> SaveAsync(User user, CancellationToken ct);

    // PF-02. 회원 행을 FOR UPDATE로 잠그고 추적 상태로 읽는다(합류·생성의 FOR NO KEY UPDATE와 충돌 → 줄 선다). 없으면 null.
    // 트랜잭션 안에서만 의미가 있다.
    Task<User?> LockForDeleteAsync(UserId userId, CancellationToken ct);

    // PF-02. 잠근 회원을 지운다(SaveChanges). 자식 FK: bookmarks CASCADE, reviews·likes SET NULL, group_members·groups.owner RESTRICT.
    Task DeleteAsync(User user, CancellationToken ct);

    // INSERT 후 저장까지 한다. 닉네임 UNIQUE 위반은 실패(UserErrors.NicknameTaken)로 돌려준다 — 예상 가능한 실패.
    // 위반이 나면 PostgreSQL 트랜잭션은 중단 상태가 된다. 호출한 쪽은 더 쓰지 말고 돌아가야 한다(Dispose = 롤백).
    Task<Result> AddAsync(User user, CancellationToken ct);

    // erd.md users Q2. 회원별 직렬화 지점(R-GR-04) — 그룹 합류·생성이 참여 수를 세기 전에 부른다. 트랜잭션 안에서만 의미가 있다.
    // 잠금 순서는 항상 users → groups(D6 사용자 결정). 행을 고치지 않는 자리 잡기 잠금이라 FOR NO KEY UPDATE(D6 사용자 결정):
    // 같은 회원의 리뷰·북마크 INSERT(FK 검사 = FOR KEY SHARE)는 막지 않고, 같은 잠금끼리는 줄 세운다.
    // 없는 회원이면 잠글 행이 없을 뿐 오류가 아니다 — UserId는 토큰에서 오고, 이후 FK가 최종 보장한다.
    Task LockAsync(UserId userId, CancellationToken ct);
}
