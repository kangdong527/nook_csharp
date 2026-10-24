using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Nook.Api.Auth;

// 토큰 발급은 이곳 한 곳뿐이다. 매직 링크든 나중의 소셜 로그인이든 "본인 확인 완료 → UserId"까지가 앞단이고,
// 그 뒤는 모두 이 메서드를 부른다. 그래서 로그인 방식이 늘어도 다른 API가 받는 토큰은 한 가지다.
public sealed class AccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider time)
{
    public string Issue(Guid userId)
    {
        // Api는 Domain을 볼 수 없어(ADR-001 결정 3) UserId 대신 Application이 넘긴 Guid를 받는다.
        if (userId == Guid.Empty)
            throw new ArgumentException("빈 사용자 ID로는 토큰을 발급하지 않는다.", nameof(userId));

        var jwt = options.Value;
        var now = time.GetUtcNow().UtcDateTime;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(jwt.AccessTokenMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)), SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(BuildClaims(userId)),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    // sub 하나만 넣는다. iat·nbf·exp는 SecurityTokenDescriptor가 이미 채운다.
    // iat·exp를 직접 넣지 않는 이유: descriptor의 IssuedAt·Expires와 같은 클레임을 Subject에도 넣으면 같은 이름이 두 번 들어가거나 어느 쪽이 이기는지 헷갈리게 돼서 출처를 한 곳으로.
    // jti는 폐기 목록(AU-03 로그아웃 결정)이 생길 때 추가한다. 지금은 읽는 곳이 없다.
    // 검증 시 주의사항: 401을 잡는건 미들웨어에서 검증, Guid.TryParse 는 내부 문제로 발생하는 상황만 잡으므로 500에러 + 로그(토큰 원문 포함 X) 처리.
    private static IEnumerable<Claim> BuildClaims(Guid userId)
    {
        yield return new Claim(JwtRegisteredClaimNames.Sub, userId.ToString());
    }
}
