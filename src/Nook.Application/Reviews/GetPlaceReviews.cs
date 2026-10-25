using System.Buffers.Binary;
using System.Buffers.Text;
using Nook.Application.Places;
using Nook.Domain.Places;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Reviews;

// PL-02(리뷰 부분) 장소의 리뷰 목록을 최신순으로, 무한 스크롤로 조회한다(2026-10-25 사용자 결정).
// Cursor: 직전 응답의 NextCursor(첫 페이지는 null). Limit: 한 번에 받을 개수.
public sealed record GetPlaceReviewsQuery(Guid UserId, Guid PlaceId, string? Cursor, int? Limit);

// NextCursor가 null이면 마지막 페이지. 클라이언트는 커서의 내용을 해석하지 않는다(불투명) — 서버가 정렬·방식을 바꿔도 계약이 그대로다.
public sealed record ReviewPageView(IReadOnlyList<PlaceReviewView> Items, string? NextCursor);

// Mine: 수정·삭제 버튼을 보일지. 클라이언트가 자기 회원 ID를 몰라도 되게 서버가 판정한다.
// Version: 수정 화면이 RV-02에 그대로 돌려보낼 불투명 값(ReviewView.Version과 같다).
public sealed record PlaceReviewView(
    Guid Id,
    ReviewAuthorView Author,
    string Content,
    IReadOnlyList<ReviewImageView> Images,
    DateTimeOffset CreatedAt,
    int Version,
    int LikeCount,
    bool LikedByMe,
    bool Mine);

public sealed record ReviewAuthorView(Guid Id, string Nickname);

public static class ReviewQueryErrors
{
    public static readonly Error InvalidCursor = new(
        "Review.InvalidCursor",
        "목록 위치 정보가 올바르지 않습니다. 처음부터 다시 불러와 주세요.",
        ErrorType.Validation);

    public static readonly Error InvalidLimit = new(
        "Review.InvalidLimit",
        $"한 번에 1~{GetPlaceReviewsHandler.MaxLimit}개까지 불러올 수 있습니다.",
        ErrorType.Validation);
}

// 키셋 위치: 정렬 키(created_at DESC, id DESC) 그대로. 다음 페이지 = 이 위치보다 "뒤"(더 오래된) 행.
// id가 동점 깨기 — 같은 시각 리뷰가 페이지 경계에 걸려도 빠지거나 겹치지 않는다.
public sealed record ReviewCursor(DateTimeOffset CreatedAt, Guid Id)
{
    // 형식: UTC ticks 8바이트 + Guid 16바이트 → base64url. 형식은 서버만 안다(위 ReviewPageView 주석).
    public string Encode()
    {
        Span<byte> bytes = stackalloc byte[24];
        BinaryPrimitives.WriteInt64BigEndian(bytes, CreatedAt.UtcTicks);
        Id.TryWriteBytes(bytes[8..]);
        return Base64Url.EncodeToString(bytes);
    }

    // 클라이언트가 보낸 값이라 불신: 형식이 틀리면 예외가 아니라 false(→ 400).
    public static bool TryDecode(string value, out ReviewCursor? cursor)
    {
        cursor = null;
        Span<byte> bytes = stackalloc byte[24];
        if (!Base64Url.TryDecodeFromChars(value, bytes, out var written) || written != 24)
            return false;

        var ticks = BinaryPrimitives.ReadInt64BigEndian(bytes);
        if (ticks < DateTimeOffset.MinValue.UtcTicks || ticks > DateTimeOffset.MaxValue.UtcTicks)
            return false;

        cursor = new ReviewCursor(new DateTimeOffset(ticks, TimeSpan.Zero), new Guid(bytes[8..]));
        return true;
    }
}

public sealed class GetPlaceReviewsHandler(IPlaceRepository places, IReviewRepository reviews)
{
    public const int DefaultLimit = 10;
    public const int MaxLimit = 30;

    public async Task<Result<ReviewPageView>> HandleAsync(GetPlaceReviewsQuery query, CancellationToken ct)
    {
        var limit = query.Limit ?? DefaultLimit;
        if (limit is < 1 or > MaxLimit)
            return ReviewQueryErrors.InvalidLimit;

        ReviewCursor? after = null;
        if (query.Cursor is not null && !ReviewCursor.TryDecode(query.Cursor, out after))
            return ReviewQueryErrors.InvalidCursor;

        // 경로의 Guid.Empty는 From이 예외 → 500(SaveBookmark와 같은 사용자 결정).
        var placeId = PlaceId.From(query.PlaceId);
        if (!await places.ExistsAsync(placeId, ct))
            return PlaceQueryErrors.PlaceNotFound;

        // 하나 더 읽어 다음 페이지가 있는지 안다 — COUNT 없이.
        var me = UserId.From(query.UserId);
        var rows = await reviews.ListByPlaceAsync(placeId, me, after, limit + 1, ct);
        var page = rows.Take(limit).ToList();

        var nextCursor = rows.Count > limit
            ? new ReviewCursor(page[^1].Review.CreatedAt, page[^1].Review.Id.Value).Encode()
            : null;

        return new ReviewPageView([.. page.Select(row => ToView(row, me))], nextCursor);
    }

    private static PlaceReviewView ToView(PlaceReview row, UserId me) => new(
        row.Review.Id.Value,
        new ReviewAuthorView(row.Review.UserId.Value, row.AuthorNickname),
        row.Review.Content.Value,
        [.. row.Review.Images.Select(i => new ReviewImageView(i.Id.Value, i.StorageKey))],
        row.Review.CreatedAt,
        row.Review.Version,
        row.LikeCount,
        row.LikedByMe,
        row.Review.UserId == me);
}
