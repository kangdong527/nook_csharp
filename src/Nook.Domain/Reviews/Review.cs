using Nook.Domain.Common;
using Nook.SharedKernel;
using Nook.Domain.Places;
using Nook.Domain.Users;

namespace Nook.Domain.Reviews;

// 리뷰 애그리거트 루트. ReviewContent(값)와 ReviewImage(내부 엔티티)를 포함하고 User·Place는 ID로만 참조한다.
// 지키는 규칙: R-RV-01(본문, ReviewContent가 검증), R-RV-02(이미지 최대 3장, 루트가 검증),
// R-RV-03(같은 장소 하루 1개 — 다른 인스턴스와 비교하므로 DB가 최종 보장, 기준 날짜 계산은 Domain).
public sealed class Review
{
    public const int MaxImages = 3;

    private readonly List<ReviewImage> _images = [];

    public ReviewId Id { get; }
    public UserId UserId { get; }
    public PlaceId PlaceId { get; }
    public ReviewContent Content { get; }

    // 순서(R-RV-02)는 Review가 지킨다. EF Core 복원 시 _images 순서는 보장되지 않으므로 읽을 때마다 SortOrder 순으로 정렬한 새 목록을 반환한다(erd.md §3).
    // 복사본이라 원본 _images는 밖에서 바뀌지 않는다. AsReadOnly는 복사본에 Add하는 실수가 조용히 사라지지 않고 예외로 드러나게 한다.
    // 추가·교체는 Review 메서드로만. 매 호출 정렬·할당이 생기므로 반복 접근 시 지역 변수에 담아 쓴다(최대 3장이라 비용은 작음).
    public IReadOnlyList<ReviewImage> Images => _images.OrderBy(i => i.SortOrder).ToList().AsReadOnly();

    public DateTimeOffset CreatedAt { get; }

    // R-RV-03 기준 날짜(UTC). DB의 (user_id, place_id, review_date) UNIQUE가 하루 1개를 최종 보장한다.
    public DateOnly ReviewDate { get; }

    private Review(ReviewId id, UserId userId, PlaceId placeId, ReviewContent content, DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        PlaceId = placeId;
        Content = content;
        CreatedAt = createdAt;
        ReviewDate = DateOnly.FromDateTime(createdAt.UtcDateTime);
    }

    public static Result<Review> Create(
        UserId userId,
        PlaceId placeId,
        ReviewContent content,
        IReadOnlyList<string> storageKeys,
        DateTimeOffset createdAt)
    {
        // 1) 호출 코드의 실수 → 예외. 사용자 입력 검사보다 먼저 해서, 버그가 검증 실패로 위장되지 않게 한다.
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(placeId);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(storageKeys);
        var utcCreatedAt = TimeGuard.Utc(createdAt);

        // 2) 회원이 고른 장수 → 예상 가능한 실패이므로 Result.
        if (storageKeys.Count > MaxImages)
            return ReviewErrors.TooManyImages;

        // 3) 생성. sortOrder = 목록 인덱스. 개별 키의 null·공백은 ReviewImage.Create가 예외로 거부한다.
        //    "예외 먼저" 원칙의 의도된 예외: 키 검사가 장수 검사 뒤에 있어, 4장 이상이면서 null 키가 섞인 버그 호출은
        //    TooManyImages로 보고된다. 키 검사를 앞에 한 번 더 두는 중복보다, 3장 이하에서는 반드시 드러나는 쪽을 택했다.
        //    같은 키 중복은 컬렉션 규칙이지만 서버가 만든 키라 버그일 때만 생기므로 DB UNIQUE(storage_key)에 맡긴다(erd.md review_images).
        var review = new Review(ReviewId.New(), userId, placeId, content, utcCreatedAt);
        for (var i = 0; i < storageKeys.Count; i++)
            review._images.Add(ReviewImage.Create(storageKeys[i], i));

        return review;
    }
}
