import type { Config } from "tailwindcss";

const config: Config = {
  content: ["./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        // 키 컬러 (nook = 블랙/화이트 기반)
        key: "#111111",
        keySoft: "#2b2b2b",
        ink: "#111111",
        sub: "#8a8f98",
        line: "#ededed",
        field: "#f4f5f6", // 입력 필드 배경
        disabled: "#d2d4d8", // 비활성 버튼 회색 배경
        // 별(장소 상태)
        starEmpty: "#c4c7cc", // 빈 별(테두리)
        starGray: "#9aa0a6", // 회색 별(북마크)
        starFill: "#ffc83d", // 채운 별(리뷰 작성)
        like: "#ff4d4f",
      },
      fontFamily: {
        sans: [
          "Pretendard",
          "-apple-system",
          "BlinkMacSystemFont",
          "system-ui",
          "Roboto",
          "sans-serif",
        ],
      },
      boxShadow: {
        frame: "0 1px 3px rgba(16,24,40,0.06), 0 12px 40px rgba(16,24,40,0.08)",
        card: "0 1px 2px rgba(16,24,40,0.05), 0 2px 8px rgba(16,24,40,0.06)",
        bar: "0 -1px 0 rgba(16,24,40,0.06), 0 -8px 24px rgba(16,24,40,0.06)",
        sheet: "0 -2px 12px rgba(16,24,40,0.12)",
      },
      keyframes: {
        "fade-up": {
          "0%": { opacity: "0", transform: "translateY(8px)" },
          "100%": { opacity: "1", transform: "translateY(0)" },
        },
        "pop-in": {
          "0%": { opacity: "0", transform: "scale(0.96)" },
          "100%": { opacity: "1", transform: "scale(1)" },
        },
        "sheet-up": {
          "0%": { transform: "translateY(100%)" },
          "100%": { transform: "translateY(0)" },
        },
        "toast-in": {
          "0%": { opacity: "0", transform: "translateY(12px)" },
          "100%": { opacity: "1", transform: "translateY(0)" },
        },
      },
      animation: {
        "fade-up": "fade-up 0.3s ease-out both",
        "pop-in": "pop-in 0.2s ease-out both",
        "sheet-up": "sheet-up 0.28s cubic-bezier(0.22,1,0.36,1) both",
        "toast-in": "toast-in 0.2s ease-out both",
      },
    },
  },
  plugins: [],
};

export default config;
