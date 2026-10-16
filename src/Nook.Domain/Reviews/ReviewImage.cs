namespace Nook.Domain.Reviews;

// 리뷰 이미지. Review 애그리거트 내부 엔티티 — Review를 거쳐서만 만들어지고 바뀐다.
// 이미지 하나만 보고 판단할 수 있는 규칙만 가진다. 장수 제한(R-RV-02)과 순서는 Review가 지킨다.
// 저장 값은 URL이 아니라 스토리지 키다(키 → URL 변환은 한 곳에서).
// 순서 변경은 같은 Id로 SortOrder만 바꾸고, 교체는 기존 이미지를 지우고 새 Id로 만든다.
// Id로 둘을 구분해야 교체로 빠진 이미지의 스토리지 객체를 찾아 지울 수 있다(RV-02 실패 흐름).
public sealed class ReviewImage
{
    public const int StorageKeyMaxLength = 56;

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
    //   * Id는 Create 안에서 발급. 키에 리뷰·이미지 Id를 넣지 않으므로(erd.md §3.1) 업로드 전에 Id를 미리 받을 필요가 없다.
    internal static ReviewImage Create(string storageKey, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        // Length 사용 근거:
        // 전제: Length ≥ 코드 포인트 수 => 코드 포인트 수도 반드시 상한 이하로 비교됨. 도메인 통과 후 DB 길이 검증에 걸리지 않음
        var storageKeyLength = storageKey.Length;
        if (storageKeyLength > StorageKeyMaxLength)
            throw new ArgumentOutOfRangeException(nameof(storageKey), $"스토리지 키는 {StorageKeyMaxLength}자 이하여야 합니다");

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
