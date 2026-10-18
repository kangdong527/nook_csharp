using System.ComponentModel.DataAnnotations;

namespace Nook.Api.Auth;

// appsettings "Jwt" 섹션. 서명 키는 커밋하지 않는다: 개발은 user-secrets(Jwt:SigningKey), 배포는 환경변수 Jwt__SigningKey.
// ValidateOnStart로 묶으므로 값이 빠지면 첫 요청이 아니라 앱 시작에서 실패한다.
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    // HS256 키는 256비트 이상이어야 한다(RFC 7518 3.2). ASCII 기준 32자 = 32바이트.
    [Required, MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    // Access 토큰만 쓴다(D5 결정). 만료되면 매직 링크로 다시 로그인한다.
    [Range(1, 1440)]
    public int AccessTokenMinutes { get; init; } = 60;
}
