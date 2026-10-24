namespace Nook.Application.Likes;

// LK-01 응답. Changed=false는 "이미 그 상태"(BM과 같은 멱등 성공). 좋아요 수는 카운터 전략(D9) 뒤에 붙인다.
public sealed record LikeView(bool Liked, bool Changed);
