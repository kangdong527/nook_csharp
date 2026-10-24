using Microsoft.EntityFrameworkCore;

namespace Nook.Integration.Tests.Infrastructure;

// "500ms 안에 안 끝났다" 대신 PostgreSQL에 직접 묻는다 — blocker의 잠금에서 기다리는 세션이 있는가.
// 시간 기준은 첫 요청 준비(JIT·EF 모델·첫 연결)가 느려 쿼리가 DB에 닿기도 전인 경우를 대기로 착각한다.
// 단독 실행 시 LK-01 catch 삭제 변이를 놓친 원인이다.
public static class LockWait
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(20);

    // blocker는 잠금을 쥔 채 열린 트랜잭션의 컨텍스트, waiting은 그 잠금에 막혀야 하는 작업.
    public static async Task AssertBlockedByAsync(PostgresFixture db, DbContext blocker, Task waiting)
    {
        var blockerPid = await blocker.Database
            .SqlQuery<int>($"""SELECT pg_backend_pid() AS "Value" """)
            .SingleAsync();

        await using var observer = db.CreateContext();
        var deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            Assert.False(waiting.IsCompleted, "잠금 대기가 관찰되기 전에 작업이 끝났다 — 기다리지 않은 것.");

            var blocked = await observer.Database.SqlQuery<bool>($"""
                SELECT EXISTS (
                    SELECT 1 FROM pg_stat_activity
                    WHERE wait_event_type = 'Lock' AND {blockerPid} = ANY (pg_blocking_pids(pid))
                ) AS "Value"
                """).SingleAsync();
            if (blocked)
                return;

            await Task.Delay(PollInterval);
        }

        Assert.Fail($"{Timeout.TotalSeconds}초 안에 blocker(pid {blockerPid})의 잠금을 기다리는 세션이 없었다.");
    }
}
