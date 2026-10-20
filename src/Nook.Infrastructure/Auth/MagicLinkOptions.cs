using System.ComponentModel.DataAnnotations;

namespace Nook.Infrastructure.Auth;

// appsettings "MagicLink" 섹션. ValidateOnStart로 묶어 값이 없으면 앱 시작에서 실패한다.
public sealed class MagicLinkOptions
{
    public const string SectionName = "MagicLink";

    // 메일 링크가 여는 프론트 화면 주소. 설정값으로만 만든다 — Request.Host로 만들면 Host 헤더 주입으로
    // 공격자 도메인을 가리키는 링크가 회원에게 발송된다(erd.md 메모).
    [Required, Url]
    public string LinkBaseUrl { get; init; } = string.Empty;
}
