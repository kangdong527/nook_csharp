"use client";

import { useState } from "react";
import { UserIcon } from "./icons";

export function Avatar({
  url,
  size = 40,
  className = "",
}: {
  url: string | null;
  size?: number;
  className?: string;
}) {
  const [error, setError] = useState(false);
  const showImage = url && !error;
  return (
    <span
      className={`inline-flex shrink-0 items-center justify-center overflow-hidden rounded-full bg-field text-sub ${className}`}
      style={{ width: size, height: size }}
    >
      {showImage ? (
        // eslint-disable-next-line @next/next/no-img-element
        <img
          src={url}
          alt="프로필"
          className="h-full w-full object-cover"
          onError={() => setError(true)}
        />
      ) : (
        <UserIcon className="h-1/2 w-1/2" />
      )}
    </span>
  );
}
