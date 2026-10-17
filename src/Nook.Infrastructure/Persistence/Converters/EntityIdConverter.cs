using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nook.Domain.Common;

namespace Nook.Infrastructure.Persistence.Converters;

// 강타입 ID ↔ uuid. IEntityId<TSelf>의 Value·From 계약 하나로 ID 5개를 모두 변환한다(ADR-006).
public sealed class EntityIdConverter<TId> : ValueConverter<TId, Guid>
    where TId : IEntityId<TId>
{
    public EntityIdConverter() : base(id => id.Value, value => FromGuid(value)) { }

    // 식 트리는 정적 추상 인터페이스 멤버(TId.From)를 직접 담을 수 없다(CS8927). 일반 정적 메서드로 한 번 감싼다.
    private static TId FromGuid(Guid value) => TId.From(value);
}
