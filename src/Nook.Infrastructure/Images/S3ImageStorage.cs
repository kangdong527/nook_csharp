using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Nook.Application.Images;
using Nook.Application.Reviews;

namespace Nook.Infrastructure.Images;

// 운영 스토리지(dep-plan 결정 3). 버킷은 비공개 — 브라우저는 GET /images/{key}(API 프록시)로만 읽는다.
// 키 형식 검사는 LocalDiskImageStorage와 같은 규칙(ReviewImageKeys): 쓰기는 예외, 읽기·삭제는 조용히 무시.
// 암호화는 버킷 기본 암호화(SSE)에 맡긴다 — 요청마다 지정하면 빠뜨린 경로 하나가 평문으로 남는다.
public sealed class S3ImageStorage(IAmazonS3 s3, string bucket) : IImageStorage
{
    public async Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken ct)
    {
        // 우리가 만든 키만 들어온다 — 어긋나면 호출 경로 결함이라 예외.
        if (!ReviewImageKeys.IsValid(storageKey))
            throw new ArgumentException($"저장소 키 형식이 아닙니다: {storageKey}", nameof(storageKey));

        // S3 PUT은 객체 단위로 원자적이다 — 실패하면 키가 보이지 않는다(로컬 디스크의 임시 파일 + Move가 필요 없다).
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,   // 스트림은 호출 쪽 것이다
        }, ct);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        if (!ReviewImageKeys.IsValid(storageKey))
            return;

        // S3 DELETE는 없는 키에도 성공(204)한다 — 멱등이 저장소 성질로 보장된다.
        await s3.DeleteObjectAsync(bucket, storageKey, ct);
    }

    public async Task<StoredImageStream?> OpenAsync(string storageKey, CancellationToken ct)
    {
        if (!ReviewImageKeys.IsValid(storageKey))
            return null;

        try
        {
            var response = await s3.GetObjectAsync(bucket, storageKey, ct);
            // Content-Type은 S3 메타데이터가 아니라 키에서 정한다 — 업로드 때 서버가 매직 바이트로 고른 확장자라 믿을 수 있고,
            // 저장소 구현이 바뀌어도 응답 헤더가 같다.
            return new StoredImageStream(response.ResponseStream, ReviewImageKeys.ContentTypeOf(storageKey));
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            // 404만 "없음". 403(권한 부족)은 설정 사고라 그대로 올려 500으로 드러낸다.
            // 주의: s3:ListBucket 권한이 없으면 S3는 없는 키에도 404 대신 403을 준다 → 인스턴스 역할에 ListBucket 필요(5단계).
            return null;
        }
    }
}
