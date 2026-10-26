"use client";

import Image from "next/image";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

const ONBOARDING_HIDE_KEY = "nook.onboarding.hide.v2";

const slides = [
  "/onboarding/slide-1.png",
  "/onboarding/slide-2.png",
  "/onboarding/slide-3.png",
];

/**
 * `fromSignup` 은 서버가 searchParams 에서 읽어 내려준다
 * (useSearchParams 가 사라져 Suspense 래핑도 필요 없어졌다).
 */
export function OnboardingSlides({ fromSignup }: { fromSignup: boolean }) {
  const router = useRouter();
  const [index, setIndex] = useState(0);

  useEffect(() => {
    const timer = window.setInterval(() => {
      setIndex((prev) => (prev + 1) % slides.length);
    }, 3000);
    return () => window.clearInterval(timer);
  }, []);

  const finish = () => {
    // 이 기기에서 온보딩을 봤다는 표시. 서버는 읽을 수 없다(EntryRedirect 주석 참조).
    window.localStorage.setItem(ONBOARDING_HIDE_KEY, "1");
    router.replace(fromSignup ? "/map" : "/login");
  };

  return (
    <div className="relative flex h-[100dvh] flex-col overflow-hidden bg-[#f6f7f8] pb-4 pt-0">
      <div className="min-h-0 flex-1 overflow-hidden">
        <div
          className="flex h-full transition-transform duration-300 ease-out"
          style={{ transform: `translateX(-${index * 100}%)` }}
        >
          {slides.map((src, i) => (
            <div
              key={src}
              className="flex h-full w-full shrink-0 items-center justify-center overflow-hidden"
            >
              <Image
                src={src}
                alt="온보딩 화면"
                width={404}
                height={881}
                priority={i === 0}
                unoptimized
                className="h-full w-auto object-contain"
              />
            </div>
          ))}
        </div>
      </div>

      <div className="mt-3 flex items-center justify-center gap-2">
        {slides.map((_, i) => (
          <button
            type="button"
            key={i}
            aria-label={`${i + 1}번째 온보딩`}
            onClick={() => setIndex(i)}
            className={`h-2.5 rounded-full transition-all ${i === index ? "w-6 bg-key" : "w-2.5 bg-black/15"}`}
          />
        ))}
      </div>

      <div className="mt-2 flex flex-col items-center gap-2">
        <button
          type="button"
          onClick={finish}
          className="text-[#9fa2a6] underline underline-offset-4"
          style={{
            fontFamily: '"Noto Sans KR", Pretendard, sans-serif',
            fontWeight: 400,
            fontStyle: "normal",
            fontSize: "11.52px",
            lineHeight: "100%",
            letterSpacing: "0",
            textAlign: "center",
            verticalAlign: "middle",
          }}
        >
          건너뛰기
        </button>
      </div>
    </div>
  );
}
