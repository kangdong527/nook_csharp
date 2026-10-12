namespace Nook.Domain.Reviews;

// 리뷰 이미지. Review 애그리거트 내부 엔티티 — Review를 거쳐서만 만들어지고 바뀐다.
// 이미지 하나만 보고 판단할 수 있는 규칙만 가진다. 장수 제한(R-RV-02)과 순서는 Review가 지킨다.
// 저장 값은 URL이 아니라 스토리지 키다(키 → URL 변환은 한 곳에서).
// 순서 변경은 같은 Id로 SortOrder만 바꾸고, 교체는 기존 이미지를 지우고 새 Id로 만든다.
// Id로 둘을 구분해야 교체로 빠진 이미지의 스토리지 객체를 찾아 지울 수 있다(RV-02 실패 흐름).
public sealed class ReviewImage
{
    public ReviewImageId Id { get; }
    public string StorageKey { get; }
    public int SortOrder { get; private set; }

    private ReviewImage(ReviewImageId id, string storageKey, int sortOrder)
    {
        Id = id;
        StorageKey = storageKey;
        SortOrder = sortOrder;
    }

    // 새 이미지 추가. Review만 부른다.
    // StorageKey는 사용자가 아니라 업로드를 마친 서버 코드가 만들어 넘기므로, null·공백은 호출 코드의 실수로 보고 예외.
    //   * Id는 Create 안에서 발급.
    //   * 만약 스토리지 키에 이미지 Id를 넣는 규칙(reviews /{imageId}.jpg 같은)을 쓰게 되면
    //      => 업로드 전에 Id가 먼저 있어야 하므로 Create(ReviewImageId id, string storageKey, int sortOrder)로 Id를 받는 형태로 변경.
    internal static ReviewImage Create(string storageKey, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);

        return new ReviewImage(ReviewImageId.New(), storageKey, sortOrder);
    }

    // 순서 변경. 같은 이미지이므로 Id와 StorageKey는 유지하고 위치만 바꾼다.
    internal void ChangeSortOrder(int sortOrder)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);
        SortOrder = sortOrder;
    }
}
