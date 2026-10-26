"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { MapIcon, PlusIcon, UserIcon } from "./icons";

export function BottomNav() {
  const pathname = usePathname();
  const router = useRouter();
  const isMap = pathname === "/map";
  const isProfile = pathname.startsWith("/profile");

  return (
    <nav className="sticky bottom-0 z-30 flex items-center justify-around border-t border-line bg-white/95 px-6 pb-[max(env(safe-area-inset-bottom),10px)] pt-2.5 backdrop-blur">
      <Link
        href="/map"
        className={`flex h-11 w-11 items-center justify-center ${isMap ? "text-key" : "text-sub"}`}
        aria-label="지도"
      >
        <MapIcon className="h-6 w-6" />
      </Link>

      <button
        onClick={() => router.push("/review/new")}
        className="flex h-12 w-12 items-center justify-center rounded-full bg-key text-white shadow-card transition active:scale-95"
        aria-label="기록 작성"
      >
        <PlusIcon className="h-6 w-6" />
      </button>

      <Link
        href="/profile"
        className={`flex h-11 w-11 items-center justify-center ${isProfile ? "text-key" : "text-sub"}`}
        aria-label="프로필"
      >
        <UserIcon className="h-6 w-6" />
      </Link>
    </nav>
  );
}
