using Microsoft.Extensions.Logging;
using Nook.Application.Auth;
using Nook.Application.Images;
using Nook.Application.Reviews;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Users;

// PF-01 프로필 사진 교체(PUT /me/avatar)·삭제(DELETE /me/avatar). 저장소·크기·형식 규칙은 리뷰 이미지와 같다(FE-2 사용자 결정).
// 순서는 RV-02와 같은 원칙: 싼 검증 → 회원 확인 → 업로드(트랜잭션 밖) → 저장 → 커밋 뒤 이전 사진 삭제(실패해도 응답은 그대로, 배치가 정리).
public sealed record ChangeAvatarCommand(Guid UserId, ReviewImageFile Image);

public sealed class ChangeAvatarHandler(
    IUserRepository users,
    IImageStorage storage,
    IReviewImageUrls urls,
    TimeProvider time,
    ILogger<ChangeAvatarHandler> logger)
{
    public async Task<Result<MeView>> HandleAsync(ChangeAvatarCommand command, CancellationToken ct)
    {
        if (command.Image.Length > ReviewImageFile.MaxBytes)
            return AvatarErrors.TooLarge;
        if (await ReviewImageFormat.DetectAsync(command.Image.Content, ct) is not { } format)
            return AvatarErrors.UnsupportedFormat;

        // 회원 확인을 업로드 전에 — 탈퇴 후 남은 토큰이 사진을 남기지 않게.
        var user = await users.FindForUpdateAsync(UserId.From(command.UserId), ct);
        if (user is null)
            return AuthErrors.UnknownUser;

        var key = ReviewImageKeys.NewAvatar(time.GetUtcNow(), format);
        await storage.PutAsync(key, command.Image.Content, format.ContentType, ct);

        // 동시에 두 번 바꾸면 나중 저장이 이긴다 — 진 쪽 사진은 어느 행도 가리키지 않는 고아(배치 정리). 사진은 한 장이라 잠그지 않는다.
        var previous = user.ReplaceAvatar(key);
        if ((await users.SaveAsync(user, ct)).IsFailure(out var saveError))
            return saveError;

        if (previous is not null)
            await ReviewImageCleanup.DeleteAfterCommitAsync(storage, [previous], logger);

        return MeView.From(user, urls);
    }
}

public sealed class RemoveAvatarHandler(
    IUserRepository users,
    IImageStorage storage,
    IReviewImageUrls urls,
    ILogger<RemoveAvatarHandler> logger)
{
    // 멱등 — 사진이 없어도 200(기본 이미지 그대로).
    public async Task<Result<MeView>> HandleAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.FindForUpdateAsync(UserId.From(userId), ct);
        if (user is null)
            return AuthErrors.UnknownUser;

        var previous = user.ReplaceAvatar(null);
        if (previous is null)
            return MeView.From(user, urls);

        if ((await users.SaveAsync(user, ct)).IsFailure(out var saveError))
            return saveError;

        await ReviewImageCleanup.DeleteAfterCommitAsync(storage, [previous], logger);
        return MeView.From(user, urls);
    }
}

public static class AvatarErrors
{
    public static readonly Error TooLarge = new(
        "User.AvatarTooLarge",
        $"프로필 사진은 {ReviewImageFile.MaxBytes / (1024 * 1024)}MB 이하여야 합니다.",
        ErrorType.Validation);

    public static readonly Error UnsupportedFormat = new(
        "User.AvatarUnsupportedFormat",
        "프로필 사진은 JPG, PNG, WEBP만 올릴 수 있습니다.",
        ErrorType.Validation);
}
