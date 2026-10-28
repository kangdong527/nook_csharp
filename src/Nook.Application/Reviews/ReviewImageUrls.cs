namespace Nook.Application.Reviews;

// 저장소 키 → 화면이 쓰는 URL. 변환은 이 한 곳(erd.md §3 "키 → URL 변환은 한 곳").
// 기준 주소는 실행 환경이 정한다 — 개발은 BFF 중계(/api/nook/images/), DEP는 S3·CDN 주소(구현체·설정만 교체).
public interface IReviewImageUrls
{
    string For(string storageKey);
}
