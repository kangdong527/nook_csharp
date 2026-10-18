using Microsoft.EntityFrameworkCore;
using Nook.Domain.Groups;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Integration.Tests.Infrastructure;
using Npgsql;

namespace Nook.Integration.Tests.Constraints;

// DB CHECK 제약 통합 테스트 — CLAUDE.md "SSoT 상수를 DB CHECK로 이중화"가 실제로 막는지 확인한다.
// 도메인을 거치면 위반 값을 만들 수 없으므로, 앱을 우회한 쓰기(raw SQL)를 흉내 낸다:
// 정상 행을 EF로 저장 → 컬럼 하나만 UPDATE로 위반 값으로 바꿈 → 23514 + 기대한 제약 이름.
public sealed class CheckConstraintTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    // 테스트 목록
    //
    // - Update_WhenValueViolatesCheck_ThrowsCheckViolation   (CHECK 14개, 제약마다 위반 방식 1~3개)

    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);

    // (테이블, SET 식, 기대 제약). 위반 값은 기대한 조건 하나만 어기도록 고른다 — 다른 CHECK·UNIQUE·varchar 길이에 걸리면 안 된다.
    // 각 테이블은 Arrange에서 정확히 1행이므로 WHERE 없이 UPDATE한다.
    public static TheoryData<string, string, string> Violations => new()
    {
        { "users", "nickname = 'a'", "ck_users_nickname" },                             // 2자 미만
        { "users", "nickname = 'nook!'", "ck_users_nickname" },                         // 허용 문자 밖
        { "users", "email = 'nook.example.com'", "ck_users_email" },                    // @ 없음
        { "places", "kakao_place_id = '   '", "ck_places_kakao_place_id" },              // 공백만
        { "places", "name = '   '", "ck_places_name" },                                  // 공백만
        { "places", "latitude = 90.5", "ck_places_latitude" },                           // 범위 밖
        { "places", "longitude = -180.5", "ck_places_longitude" },                       // 범위 밖
        { "reviews", "content = '가가가가가가가가가'", "ck_reviews_content" },             // 9자
        { "reviews", "content = '  가가가가가가가가가  '", "ck_reviews_content" },         // 13자지만 btrim 뒤 9자
        { "review_images", "sort_order = 3", "ck_review_images_sort_order" },            // 4번째 이미지
        { "review_images", "sort_order = -1", "ck_review_images_sort_order" },           // 음수
        { "review_images", "storage_key = '   '", "ck_review_images_storage_key" },      // 공백만
        { "groups", "invite_code = 'ABCDE0'", "ck_groups_invite_code" },                 // 제외 문자 0
        { "groups", "invite_code = 'abcdef'", "ck_groups_invite_code" },                 // 소문자
        { "groups", "name = 'a'", "ck_groups_name" },                                    // 2자 미만
        { "groups", "name = ' 산책 '", "ck_groups_name" },                               // trim 안 된 값
        { "magic_link_tokens", "token_hash = decode('ab', 'hex')", "ck_magic_link_tokens_token_hash" },  // 1바이트(해시 아님)
        { "magic_link_tokens", "expires_at = created_at", "ck_magic_link_tokens_expires" },               // 처음부터 만료
        { "magic_link_tokens", "consumed_at = created_at - interval '1 second'", "ck_magic_link_tokens_consumed" }, // 발급 전 소모
    };

    [Theory]
    [MemberData(nameof(Violations))]
    public async Task Update_WhenValueViolatesCheck_ThrowsCheckViolation(string table, string set, string constraint)
    {
        await SeedOneRowPerTableAsync();

        // 테이블·식은 위 고정 목록에서만 온다(외부 입력 아님).
        var sql = $"UPDATE {table} SET {set}";
        await using var db = Db.CreateContext();

        var ex = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(sql));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);   // 23514
        Assert.Equal(constraint, ex.ConstraintName);                     // 다른 CHECK에 우연히 걸린 것이 아닌가
    }

    // 도메인 경로로 만든 정상 행 — CHECK를 전부 통과하는 기준선. 여기서 실패하면 Arrange 문제다.
    private async Task SeedOneRowPerTableAsync()
    {
        var user = User.Create(Nickname.From("누크"), Email.From("nook@example.com"));
        var place = Place.Create(
            KakaoPlaceId.From("12345"), "누크 카페", "서울 중구", Coordinate.Create(37.5665, 126.9780).Unwrap()).Unwrap();
        var review = Review.Create(
            user.Id, place.Id, ReviewContent.From("조용하고 커피가 맛있는 곳"), ["img/a.jpg"], CreatedAt).Unwrap();
        var group = Group.Create(GroupName.Create("주말 산책").Unwrap(), user.Id, InviteCode.Generate(_ => 0), CreatedAt);

        await using var db = Db.CreateContext();
        db.AddRange(user, place, review, group);
        await db.SaveChangesAsync();

        // MagicLinkToken은 Infrastructure internal이라 SQL로 넣는다. 32바이트 해시, 15분 유효, 미사용.
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO magic_link_tokens (token_hash, email, created_at, expires_at)
            VALUES (decode(repeat('ab', 32), 'hex'), 'nook@example.com',
                    '2026-10-18 09:00:00+00', '2026-10-18 09:15:00+00')
            """);
    }
}
