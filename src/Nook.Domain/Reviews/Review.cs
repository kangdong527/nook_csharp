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
    public ReviewContent Content { get; private set; }

    // 순서(R-RV-02)는 Review가 지킨다. EF Core 복원 시 _images 순서는 보장되지 않으므로 읽을 때마다 SortOrder 순으로 정렬한 새 목록을 반환한다(erd.md §3).
    // 복사본이라 원본 _images는 밖에서 바뀌지 않는다. AsReadOnly는 복사본에 Add하는 실수가 조용히 사라지지 않고 예외로 드러나게 한다.
    // 추가·교체는 Review 메서드로만. 매 호출 정렬·할당이 생기므로 반복 접근 시 지역 변수에 담아 쓴다(최대 3장이라 비용은 작음).
    public IReadOnlyList<ReviewImage> Images => _images.OrderBy(i => i.SortOrder).ToList().AsReadOnly();

    public DateTimeOffset CreatedAt { get; }

    // R-RV-03 기준 날짜(KST, ReviewDay). DB의 (user_id, place_id, review_date) UNIQUE가 하루 1개를 최종 보장한다.
    public DateOnly ReviewDate { get; }

    // 낙관적 동시성 토큰(incident 002). Edit가 성공할 때마다 1 오른다. 저장은 "읽은 Version과 같을 때만" 반영되므로,
    // 읽은 뒤 다른 수정이 먼저 커밋됐으면 바뀐 내용(본문·이미지 무엇이든)과 상관없이 늦은 쪽이 실패한다. 0부터 시작.
    public int Version { get; private set; }

    private Review(ReviewId id, UserId userId, PlaceId placeId, ReviewContent content, DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        PlaceId = placeId;
        Content = content;
        CreatedAt = createdAt;
        ReviewDate = ReviewDay.Of(createdAt);
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

    // RV-02 수정. 본문은 통째로 바꾸고, 이미지 최종 순서 = keepImageIds 순서 → 그 뒤에 newStorageKeys 순서(2026-10-23 사용자 결정).
    // keepImageIds에 없는 기존 이미지는 빠진다. 반환: 빠진 이미지의 스토리지 키 — 핸들러가 커밋 후 지운다(ADR-009 결정 1).
    // 실패(Result): 합계 장수 초과(R-RV-02, 3c), 이 리뷰의 이미지가 아닌 ID(4c). 본문 글자 수(R-RV-01)는 ReviewContent가 이미 검증했다.
    // null 인자·잘못된 키는 규칙 위반이 아니라 호출 코드의 버그 → 예외(Create와 같은 구분).
    public Result<IReadOnlyList<string>> Edit(
        ReviewContent content,
        IReadOnlyList<ReviewImageId> keepImageIds,
        IReadOnlyList<string> newStorageKeys)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(keepImageIds);
        ArgumentNullException.ThrowIfNull(newStorageKeys);

        if (keepImageIds.Count + newStorageKeys.Count > MaxImages)
            return ReviewErrors.TooManyImages;

        // 1) 검사 먼저: 하나라도 이 리뷰에 없는 ID면 아무것도 지우지 않고 거절한다.
        //    같은 ID 중복([A, A])도 4c — 존재 검사는 통과하지만 1장을 2장으로 세고, 한 이미지가 두 자리를 차지하게 된다.
        if (keepImageIds.Distinct().Count() != keepImageIds.Count
            || keepImageIds.Any(id => !_images.Exists(image => image.Id == id)))
            return ReviewErrors.ImageNotInReview;

        // 2) 새 이미지도 변경 전에 만든다. 키가 잘못되면(버그) ReviewImage.Create가 예외를 던지는데, 그때도 애그리거트는 그대로다.
        //    sortOrder = 최종 목록 인덱스 — 남길 이미지 뒤에 이어 붙는다.
        var added = newStorageKeys
            .Select((key, i) => ReviewImage.Create(key, keepImageIds.Count + i))
            .ToList();

        // 3) 변경. 남길 이미지의 순서는 keepImageIds 안의 위치로 매긴다 — _images의 물리적 순서는 EF 복원 시 보장되지 않아 의미가 없다.
        //    다시 매기는 도중 sort_order가 겹쳐도 ux_review_images_review_sort_order가 DEFERRED라 커밋 시점에만 검사한다.
        Content = content;

        var removed = _images.Where(image => !keepImageIds.Contains(image.Id)).Select(image => image.StorageKey).ToList();
        _images.RemoveAll(image => !keepImageIds.Contains(image.Id));

        for (var i = 0; i < keepImageIds.Count; i++)
            _images.Single(image => image.Id == keepImageIds[i]).ChangeSortOrder(i);

        _images.AddRange(added);
        Version++;
        return removed;
    }
}
