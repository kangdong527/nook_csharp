using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Nook.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    // 인증된 요청의 회원 ID(AccessTokenIssuer가 넣은 sub). MapInboundClaims = false라 이름이 "sub" 그대로다.
    // 401은 미들웨어가 이미 걸렀다 — 여기까지 와서 sub가 없거나 Guid가 아니면 발급 쪽 결함이므로 예외(500).
    // 메시지에 토큰·클레임 값을 넣지 않는다.
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("인증된 요청에 유효한 sub 클레임이 없습니다.");
}
