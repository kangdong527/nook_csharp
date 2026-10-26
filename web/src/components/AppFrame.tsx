"use client";

import { usePathname } from "next/navigation";

export function AppFrame({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const isOnboarding = pathname === "/onboarding";

  const frameClassName = isOnboarding
    ? "relative flex min-h-screen w-full flex-col bg-white"
    : "relative mx-auto flex min-h-screen w-full max-w-[420px] flex-col bg-white sm:my-6 sm:min-h-[calc(100vh-3rem)] sm:rounded-[28px] sm:shadow-frame sm:overflow-hidden";

  return <div className={frameClassName}>{children}</div>;
}
