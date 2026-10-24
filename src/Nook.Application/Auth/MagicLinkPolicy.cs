namespace Nook.Application.Auth;

// 매직 링크 정책값(progress.md 확정된 결정 D5). 바꾸면 erd.md magic_link_tokens "정책값"도 함께 고친다.
public static class MagicLinkPolicy
{
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

    // 이메일별 RateLimitWindow 안에 MaxIssuesPerWindow개까지 발급한다.
    public static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);
    public const int MaxIssuesPerWindow = 3;
}
