using Microsoft.Extensions.Logging;
using Nook.Application.Images;

namespace Nook.Application.Reviews;

// RV-02·RV-03 공통: DB 커밋 후 빠진 이미지를 스토리지에서 지운다(ADR-009 결정 1).
internal static class ReviewImageCleanup
{
    // 쓰기는 이미 성공했으므로 실패해도 응답을 바꾸지 않는다 — 남은 객체는 배치가 정리.
    // 요청 ct를 받지 않는다: 커밋 뒤 클라이언트가 끊겨도 취소 예외가 성공한 요청을 500으로 바꾸면 안 된다.
    public static async Task DeleteAfterCommitAsync(IImageStorage storage, IReadOnlyList<string> keys, ILogger logger)
    {
        foreach (var key in keys)
        {
            try
            {
                await storage.DeleteAsync(key, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "리뷰 이미지 삭제 실패, 배치가 정리한다: {StorageKey}", key);
            }
        }
    }
}
