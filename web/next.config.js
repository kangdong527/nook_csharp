/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  // 이미지 최적화(/_next/image) 허용 범위 — DEP 보안(Infrastructure protection), Claude 작성(사용자 요청).
  // next/image 사용처는 onboarding 슬라이드 3장(public/onboarding/*.png)뿐이다.
  // 리뷰·프로필 사진은 <img src="/api/nook/images/..."> 로 BFF 를 거친다(next/image 아님).
  //   - remotePatterns 없음: 외부 URL 을 받아 가공하는 오픈 프록시를 닫는다(대역폭·CPU 남용, 파서 취약점 노출 차단)
  //   - localPatterns: 로컬 이미지도 이 경로만 최적화한다
  // S3 사진을 next/image 로 보여 줄 때가 오면 그 버킷 호스트 하나만 remotePatterns 에 추가한다.
  images: {
    localPatterns: [{ pathname: "/onboarding/**", search: "" }],
  },
};

module.exports = nextConfig;
