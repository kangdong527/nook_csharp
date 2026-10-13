namespace Nook.Domain.Common;

// 모든 ID 공통: DB에 저장된 값을 꺼내 다시 ID로 되돌리는 구현(From)을 한 곳에서 통일해 처리하기 위함
public interface IEntityId<TSelf> where TSelf : IEntityId<TSelf>
{
    Guid Value { get; }
    static abstract TSelf From(Guid value);
}
