using Nook.Domain.Common;
using Nook.Domain.Users;

namespace Nook.Domain.Groups;

// 그룹 멤버. Group 애그리거트 내부 엔티티 — Group을 거쳐서만 만들어지고 사라진다.
// 그룹 안에서의 식별자는 UserId다(R-GR-07: (회원, 그룹) 0/1건). 부모 GroupId는 두지 않는다 — DB 매핑(D3) 관심사.
// R-GR-02(회원당 N개)는 여러 그룹에 걸친 규칙이라 GroupMember도 Group도 확인할 수 없다 → Application + User 행 잠금(R-GR-04).
// 별도 Id 타입 없음: 그룹 안에서 UserId가 이미 유일한 자연 키이고, 애그리거트 밖에서 멤버 하나를 따로 가리키는 곳이 없다.
// 바뀌는 값 없음: 탈퇴는 수정이 아니라 Group 컬렉션에서의 제거이므로 setter를 두지 않는다.
public sealed class GroupMember
{
    public UserId UserId { get; }
    public DateTimeOffset JoinedAt { get; }

    private GroupMember(UserId userId, DateTimeOffset joinedAt)
    {
        UserId = userId;
        JoinedAt = joinedAt;
    }

    // 합류·그룹 생성(R-GR-08) 때 Group만 부른다.
    internal static GroupMember Create(UserId userId, DateTimeOffset joinedAt)
    {
        // null·default·비UTC는 회원 입력이 아니라 호출 코드(Group)의 실수이므로 예외로 알린다.
        ArgumentNullException.ThrowIfNull(userId);

        return new GroupMember(userId, TimeGuard.Utc(joinedAt));
    }
}
