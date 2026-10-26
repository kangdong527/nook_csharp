"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { uploadImage, MAX_IMAGE_BYTES } from "@/lib/store";
import {
  REVIEW_MAX,
  REVIEW_MIN,
  reviewLengthError,
  type User,
} from "@/lib/types";
import { useToast } from "./Toast";
import { Spinner } from "./Spinner";
import { Avatar } from "./Avatar";
import { CameraIcon, CloseIcon, PlusIcon } from "./icons";

export function ReviewEditor({
  mode,
  user,
  placeName,
  initialContent = "",
  initialImages = [],
  onSubmit,
  onCancel,
}: {
  mode: "new" | "edit";
  user: User;
  placeName: string;
  initialContent?: string;
  initialImages?: string[];
  onSubmit: (content: string, images: string[]) => Promise<void>;
  onCancel?: () => void;
}) {
  const router = useRouter();
  const toast = useToast();
  const [content, setContent] = useState(initialContent);
  const [images, setImages] = useState<string[]>(initialImages);
  const [uploading, setUploading] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const len = content.length;
  const lenError = reviewLengthError(content);
  const canSubmit = !lenError && !submitting && !uploading;

  const onPick = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files ?? []);
    e.target.value = "";
    if (files.length === 0) return;
    const room = 3 - images.length;
    if (room <= 0) {
      toast("사진은 최대 3장까지 첨부할 수 있어요.");
      return;
    }
    setUploading(true);
    try {
      const picked = files.slice(0, room);
      const urls: string[] = [];
      for (const f of picked) {
        if (f.size > MAX_IMAGE_BYTES) {
          toast("이미지 용량이 너무 커요. 다른 사진을 선택해주세요.");
          continue;
        }
        urls.push(await uploadImage(f));
      }
      setImages((prev) => [...prev, ...urls].slice(0, 3));
    } catch {
      toast("일시적인 오류가 발생했어요. 잠시 후 다시 시도해 주세요.");
    } finally {
      setUploading(false);
    }
  };

  const removeImage = (i: number) =>
    setImages((prev) => prev.filter((_, idx) => idx !== i));

  const submit = async () => {
    if (lenError) {
      toast(lenError);
      return;
    }
    setSubmitting(true);
    try {
      await onSubmit(content, images);
    } catch {
      toast("일시적인 오류가 발생했어요. 잠시 후 다시 시도해 주세요.");
      setSubmitting(false);
    }
  };

  return (
    <div className="flex flex-1 flex-col">
      {/* 상단 바 */}
      <header className="flex items-center justify-between border-b border-line px-4 py-3">
        <button
          onClick={() => (onCancel ? onCancel() : router.back())}
          className="text-sm text-sub"
        >
          취소
        </button>
        <h1 className="text-base font-semibold">
          {mode === "new" ? "새로운 기록" : "기록 수정"}
        </h1>
        <button
          onClick={submit}
          disabled={!canSubmit}
          className="flex items-center gap-1 text-sm font-semibold text-key disabled:text-disabled"
        >
          {submitting && <Spinner className="text-key" />}
          {mode === "new" ? "발행" : "완료"}
        </button>
      </header>

      <div className="flex flex-1 flex-col overflow-y-auto px-5 py-4">
        {/* 작성자 + 장소 */}
        <div className="flex items-center gap-2.5">
          <Avatar url={user.avatarUrl} size={36} />
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold">{user.nickname}</p>
            <p className="truncate text-xs text-key">📍 {placeName}</p>
          </div>
        </div>

        {/* 본문 */}
        <textarea
          autoFocus
          value={content}
          onChange={(e) => setContent(e.target.value.slice(0, REVIEW_MAX))}
          placeholder="리뷰 내용을 입력해 주세요!"
          rows={6}
          className="mt-4 w-full resize-none bg-transparent text-[15px] leading-relaxed outline-none placeholder:text-disabled"
        />

        <div className="flex items-center justify-between text-xs">
          <span className={lenError && len > 0 ? "text-like" : "text-sub"}>
            {lenError && len > 0 ? lenError : `최소 ${REVIEW_MIN}자`}
          </span>
          <span className="text-sub">
            {len}/{REVIEW_MAX}
          </span>
        </div>

        {/* 사진 */}
        <div className="no-scrollbar mt-4 flex gap-2 overflow-x-auto">
          {images.map((url, i) => (
            <div key={i} className="relative h-28 w-28 shrink-0">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={url} alt={`사진 ${i + 1}`} className="h-full w-full rounded-xl object-cover" />
              <button
                onClick={() => removeImage(i)}
                className="absolute right-1 top-1 flex h-6 w-6 items-center justify-center rounded-full bg-black/55 text-white"
                aria-label="사진 삭제"
              >
                <CloseIcon className="h-3.5 w-3.5" />
              </button>
            </div>
          ))}

          {images.length < 3 && (
            <label className="flex h-28 w-28 shrink-0 cursor-pointer flex-col items-center justify-center gap-1 rounded-xl border border-dashed border-line text-sub">
              {uploading ? (
                <Spinner className="text-key" />
              ) : images.length === 0 ? (
                <CameraIcon className="h-6 w-6" />
              ) : (
                <PlusIcon className="h-6 w-6" />
              )}
              <span className="text-xs">{images.length}/3</span>
              <input
                type="file"
                accept="image/*"
                multiple
                className="hidden"
                onChange={onPick}
              />
            </label>
          )}
        </div>
      </div>
    </div>
  );
}
