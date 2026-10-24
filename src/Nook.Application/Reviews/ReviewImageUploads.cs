using Nook.SharedKernel;

namespace Nook.Application.Reviews;

// RV-01·RV-02 공통: 이미지 검증(2c)과 업로드. 두 유스케이스 모두 "전부 검증한 뒤에만 올린다"를 지켜야 해서 한 곳에 둔다.
internal static class ReviewImageUploads
{
    // 크기·형식을 전부 본다. 하나라도 틀리면 첫 오류를 돌려주고, 스토리지는 건드리지 않는다.
    public static async Task<Result<IReadOnlyList<ReviewImageFormat>>> ValidateAsync(
        IReadOnlyList<ReviewImageFile> images, CancellationToken ct)
    {
        var formats = new List<ReviewImageFormat>(images.Count);
        foreach (var image in images)
        {
            if (image.Length > ReviewImageFile.MaxBytes)
                return ReviewWriteErrors.ImageTooLarge;

            var format = await ReviewImageFormat.DetectAsync(image.Content, ct);
            if (format is null)
                return ReviewWriteErrors.UnsupportedImageFormat;

            formats.Add(format);
        }
        return formats;
    }

    // 키는 업로드와 따로 만든다 — RV-02는 키로 Edit(도메인 검증)를 먼저 끝낸 뒤에 올려, 거절될 요청이 이미지를 남기지 않게 한다.
    // uploadedAt은 핸들러가 정한 "지금" 하나(ADR-007) — 키 날짜가 리뷰 시각과 갈리지 않게.
    public static IReadOnlyList<string> NewKeys(IReadOnlyList<ReviewImageFormat> formats, DateTimeOffset uploadedAt) =>
        [.. formats.Select(format => ReviewImageKeys.New(uploadedAt, format))];

    // 트랜잭션 밖에서 부른다(CLAUDE.md). 실패는 잡지 않는다 — 예외 → 500, 먼저 올라간 이미지는 배치가 정리(ADR-009).
    public static async Task UploadAsync(
        IReviewImageStorage storage,
        IReadOnlyList<ReviewImageFile> images,
        IReadOnlyList<ReviewImageFormat> formats,
        IReadOnlyList<string> keys,
        CancellationToken ct)
    {
        for (var i = 0; i < images.Count; i++)
            await storage.PutAsync(keys[i], images[i].Content, formats[i].ContentType, ct);
    }
}
