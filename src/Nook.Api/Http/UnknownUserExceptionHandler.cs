using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Nook.Application.Auth;
using Npgsql;

namespace Nook.Api.Http;

// 탈퇴 직후 남은 토큰(최대 60분)으로 쓰기를 하면 회원 FK가 위반된다(PF-02 미결 ②). 이걸 요청마다 회원을 조회해 막지 않고,
// DB 제약을 최종 판정자로 두고 그 위반을 여기 한 곳에서 401 Auth.UnknownUser로 번역한다 — /me의 "회원 없음 401"(AU-04)과 같은 응답.
// (닉네임 UNIQUE 위반을 409로 바꾸는 것과 같은 모양. 다른 예외는 처리하지 않고 다음 처리기·기본 500으로 넘긴다.)
internal sealed class UnknownUserExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    // 회원을 가리키는 FK들. 회원 행이 없을 때만 위반된다.
    private static readonly HashSet<string> UserForeignKeys =
        ["fk_bookmarks_users", "fk_likes_users", "fk_reviews_users", "fk_group_members_users", "fk_groups_owner"];

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var postgres = exception as PostgresException ?? (exception as DbUpdateException)?.InnerException as PostgresException;
        if (postgres is not { SqlState: PostgresErrorCodes.ForeignKeyViolation } || !UserForeignKeys.Contains(postgres.ConstraintName ?? ""))
            return false;

        // 같은 FK라도 부모 쪽 위반("update or delete on table users ...")은 탈퇴 절차의 버그(RESTRICT를 건너뜀)다 — 401로 가리면 안 된다.
        // 자식 쪽 위반("insert or update on table <child> ...") = 가리킨 회원이 없음 = 남은 토큰만 401.
        // FE-2 변이로 확인: 그룹 나가기를 빼면 이 검사가 없을 때 401로 가려졌다. Detail은 Npgsql이 가리므로 MessageText로 본다
        // (서버 lc_messages 영어 전제 — postgres 공식 이미지 기본값).
        if (!postgres.MessageText.StartsWith("insert or update", StringComparison.Ordinal))
            return false;

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails =
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = AuthErrors.UnknownUser.Code,
                Detail = AuthErrors.UnknownUser.Description,
            },
        });
    }
}
