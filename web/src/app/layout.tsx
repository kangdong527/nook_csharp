import type { Metadata, Viewport } from "next";
import "./globals.css";
import { ToastProvider } from "@/components/Toast";
import { AppFrame } from "@/components/AppFrame";

export const metadata: Metadata = {
  title: "nook",
  description: "장소를 기록하고 공유하는 지도 기반 기록 SNS, nook",
};

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  maximumScale: 1,
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="ko">
      <head>
        <link
          rel="stylesheet"
          as="style"
          href="https://cdn.jsdelivr.net/gh/orioncactus/pretendard@v1.3.9/dist/web/static/pretendard.min.css"
        />
      </head>
      <body>
        <ToastProvider>
          <AppFrame>{children}</AppFrame>
        </ToastProvider>
      </body>
    </html>
  );
}
