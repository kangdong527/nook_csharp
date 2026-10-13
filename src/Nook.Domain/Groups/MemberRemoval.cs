namespace Nook.Domain.Groups;

// Group.RemoveMember의 결과. 실패가 아니라 "무슨 일이 일어났는가"를 알린다(멱등이라 오류가 없다).
// GroupEmptied: 방금 마지막 멤버가 나갔다 → Application이 같은 트랜잭션에서 그룹을 삭제한다(R-GR-09).
// NotMember를 0(기본값)으로 둔다: 초기화되지 않은 값이 "아무 일도 없음"으로 읽혀야 실수로 그룹을 지우지 않는다.
public enum MemberRemoval
{
    NotMember,
    Removed,
    GroupEmptied,
}
