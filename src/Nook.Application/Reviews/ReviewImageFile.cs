using System.Globalization;
using System.Text.RegularExpressions;

namespace Nook.Application.Reviews;

// 업로드된 이미지 파일 하나. Api가 multipart 파일을 이 모양으로 넘긴다.
// Content는 되감을 수 있어야 한다(CanSeek) — 형식 판별로 앞부분을 읽은 뒤 스토리지가 처음부터 다시 읽는다.
public sealed record ReviewImageFile(Stream Content, long Length)
{
    // RV-01 2c. 원본 nook MAX_IMAGE_BYTES와 같다(ADR-009). Domain의 ReviewImage는 키만 알고 바이트를 모르므로 여기에 둔다.
    // 올리면 Kestrel 요청 상한(기본 약 28.6MB)과 함께 본다: Review.MaxImages × MaxBytes.
    public const long MaxBytes = 5 * 1024 * 1024;
}

// 서버가 판별한 형식. 확장자·Content-Type은 회원이 보낸 파일명·헤더가 아니라 매직 바이트로 정한다(erd.md §3.1).
public sealed record ReviewImageFormat(string Extension, string ContentType)
{
    public static readonly ReviewImageFormat Jpeg = new("jpg", "image/jpeg");
    public static readonly ReviewImageFormat Png = new("png", "image/png");
    public static readonly ReviewImageFormat Webp = new("webp", "image/webp");

    // WebP 판별에 필요한 길이: "RIFF" + 크기 4바이트 + "WEBP".
    private const int HeaderLength = 12;

    // 모르는 형식이면 null. Stream 위치는 처음으로 되돌린다.
    public static async Task<ReviewImageFormat?> DetectAsync(Stream content, CancellationToken ct)
    {
        var header = new byte[HeaderLength];
        var read = await content.ReadAtLeastAsync(header, HeaderLength, throwOnEndOfStream: false, ct);
        content.Position = 0;
        return Detect(header.AsSpan(0, read));
    }

    internal static ReviewImageFormat? Detect(ReadOnlySpan<byte> header)
    {
        ReadOnlySpan<byte> jpeg = [0xFF, 0xD8, 0xFF];
        ReadOnlySpan<byte> png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        if (header.StartsWith(jpeg))
            return Jpeg;
        if (header.StartsWith(png))
            return Png;
        if (header.Length >= HeaderLength && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8))
            return Webp;
        return null;
    }
}

// erd.md §3.1 키 규칙: reviews/{yyyy}/{MM}/{dd}/{32자 hex}.{jpg|png|webp}. 최대 56자 = ReviewImage.StorageKeyMaxLength.
public static class ReviewImageKeys
{
    // GET /images/{key}의 키는 불신 입력이다 — 우리가 만드는 모양과 정확히 같을 때만 스토리지에 넘긴다(경로 조작 방어의 1차).
    private static readonly Regex Shape = new(
        @"^(reviews|avatars)/[0-9]{4}/[0-9]{2}/[0-9]{2}/[0-9a-f]{32}\.(jpg|png|webp)$",
        RegexOptions.CultureInvariant);

    public static bool IsValid(string? key) => key is not null && Shape.IsMatch(key);

    // 확장자 → Content-Type. 확장자는 서버가 매직 바이트로 정해 키에 넣은 것이라 믿을 수 있다(IsValid를 통과한 키만).
    public static string ContentTypeOf(string key) => Path.GetExtension(key) switch
    {
        ".jpg" => ReviewImageFormat.Jpeg.ContentType,
        ".png" => ReviewImageFormat.Png.ContentType,
        ".webp" => ReviewImageFormat.Webp.ContentType,
        _ => throw new ArgumentException($"알 수 없는 이미지 확장자입니다: {key}", nameof(key)),
    };

    // 날짜는 업로드 시각의 UTC 날짜(TimeProvider가 정한 값, ADR-007). 식별자는 v4 — 추측하기 어려울수록 좋다.
    // InvariantCulture: 사용자 지정 날짜 형식의 '/'는 문화권 구분자로 바뀔 수 있다.
    public static string New(DateTimeOffset uploadedAt, ReviewImageFormat format) => NewKey("reviews", uploadedAt, format);

    // PF-01 프로필 사진(FE-2). 같은 스토리지·같은 모양, prefix만 다르다 — 정리 배치가 prefix로 범위를 나눌 수 있게.
    public static string NewAvatar(DateTimeOffset uploadedAt, ReviewImageFormat format) => NewKey("avatars", uploadedAt, format);

    private static string NewKey(string prefix, DateTimeOffset uploadedAt, ReviewImageFormat format) =>
        string.Create(CultureInfo.InvariantCulture,
            $"{prefix}/{uploadedAt.UtcDateTime:yyyy'/'MM'/'dd}/{Guid.NewGuid():N}.{format.Extension}");
}
