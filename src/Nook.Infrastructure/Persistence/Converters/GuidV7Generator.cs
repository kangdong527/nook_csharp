using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Nook.Infrastructure.Persistence.Converters;

// 도메인이 모르는 대리 키(likes.id, PF-02)를 앱이 UUIDv7로 만든다 — 강타입 ID들과 같은 발급 규칙(ADR-006). DB 기본값을 두지 않는다.
internal sealed class GuidV7Generator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Guid Next(EntityEntry entry) => Guid.CreateVersion7();
}
