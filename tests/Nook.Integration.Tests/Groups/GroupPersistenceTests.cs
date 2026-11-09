using Microsoft.EntityFrameworkCore;
using Nook.Domain.Groups;
using Nook.Domain.Users;
using Nook.Integration.Tests.Infrastructure;

namespace Nook.Integration.Tests.Groups;

// groups + group_members 저장·복원 통합 테스트. 실제 PostgreSQL(마이그레이션 적용 스키마)에서 확인한다.
public sealed class GroupPersistenceTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    // 테스트 목록
    //
    // 그룹 생성(R-GR-08) — erd.md §1 보장 주체 "앱 절차" 행: DB는 멤버 0명 그룹을 막지 않는다
    // - Create_WhenSaved_ReloadsCreatorAsOnlyMember

    // timestamptz는 마이크로초까지만 저장한다. UtcNow(100ns 단위)를 쓰면 라운드트립 비교가 우연히 깨진다.
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_WhenSaved_ReloadsCreatorAsOnlyMember()
    {
        // Arrange — 테스트 입력은 이미 유효한 값이므로 Result를 풀지 않고 From으로 복원한다.
        var creator = User.Create(Nickname.From("누크"), Email.From("nook@example.com"), TestConsent.Required);
        var group = Group.Create(GroupName.From("산책모임"), creator.Id, InviteCode.From("ABCDEF"), CreatedAt);

        // 저장용 컨텍스트. User와 Group을 한 번에 넣어도 EF가 FK(fk_group_members_users)를 보고
        // users → groups → group_members 순으로 INSERT한다.
        await using (var db = Db.CreateContext())
        {
            db.Users.Add(creator);
            db.Groups.Add(group);
            await db.SaveChangesAsync();
        }

        // Act — 검증용 컨텍스트를 새로 만든다. 저장용으로 읽으면 식별 맵이 메모리 객체를 그대로 돌려줘 DB를 거치지 않는다.
        await using var verify = Db.CreateContext();
        var reloaded = await verify.Groups.SingleAsync(g => g.Id == group.Id);

        // Assert — Members는 OwnsMany라 Include 없이 함께 로딩된다.
        var member = Assert.Single(reloaded.Members);
        Assert.Equal(creator.Id, member.UserId);
        Assert.Equal(CreatedAt, member.JoinedAt);
    }
}
