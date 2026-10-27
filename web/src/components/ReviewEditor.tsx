"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { MAX_IMAGE_BYTES } from "@/lib/store";
import {
  REVIEW_MAX,
  REVIEW_MIN,
  reviewLengthError,
  type ReviewImageRef,
  type User,
} from "@/lib/types";
import { useToast } from "./Toast";
import { Spinner } from "./Spinner";
import { Avatar } from "./Avatar";
import { CameraIcon, CloseIcon, PlusIcon } from "./icons";

// 편집 중인 사진 한 장. 기존 사진은 ID 로 남기고(RV-02 KeepImageIds), 새 사진은 파일을 들고 있다가 저장할 때 함께 보낸다.
// 원본은 고르는 즉시 올렸다 → 저장을 취소·실패하면 고아 이미지가 남았다(FE-2: 서버가 한 요청에서 검증 후 올린다).
export type EditorImage =
  | { kind: "existing"; id: string; url: string }
  | { kind: "new"; file: File; previewUrl: string };

// 서버가 판별하는 형식과 같게(매직 바이트 JPG·PNG·WEBP — ReviewImageFormat). 다른 형식은 고를 때 거른다.
const ACCEPT = "image/jpeg,image/png,image/webp";

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
  initialImages?: ReviewImageRef[];
  // 실패 문구를 직접 띄웠으면 resolve 하고, 예상 못 한 실패만 throw 한다(그때 일반 오류 문구).
  onSubmit: (content: string, images: EditorImage[]) => Promise<void>;
  onCancel?: () => void;
}) {
  const router = useRouter();
  const toast = useToast();
  const [content, setContent] = useState(initialContent);
  const [images, setImages] = useState<EditorImage[]>(() =>
    initialImages.map((i) => ({ kind: "existing", id: i.id, url: i.url }))
  );
  const [submitting, setSubmitting] = useState(false);

  // 미리보기용 blob URL 은 화면을 떠날 때 돌려준다(메모리).
  useEffect(
    () => () => images.forEach((i) => i.kind === "new" && URL.revokeObjectURL(i.previewUrl)),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    []
  );

  const len = content.length;
  const lenError = reviewLengthError(content);
  const canSubmit = !lenError && !submitting;

  const onPick = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files ?? []);
    e.target.value = "";
    if (files.length === 0) return;
    const room = 3 - images.length;
    if (room <= 0) {
      toast("사진은 최대 3장까지 첨부할 수 있어요.");
      return;
    }
    const picked: EditorImage[] = [];
    for (const f of files.slice(0, room)) {
      if (f.size > MAX_IMAGE_BYTES) {
        toast("이미지 용량이 너무 커요. 다른 사진을 선택해주세요.");
        continue;
      }
      picked.push({ kind: "new", file: f, previewUrl: URL.createObjectURL(f) });
    }
    setImages((prev) => [...prev, ...picked].slice(0, 3));
  };

  const removeImage = (i: number) =>
    setImages((prev) => {
      const target = prev[i];
      if (target?.kind === "new") URL.revokeObjectURL(target.previewUrl);
      return prev.filter((_, idx) => idx !== i);
    });

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
          {images.map((img, i) => (
            <div key={img.kind === "existing" ? img.id : img.previewUrl} className="relative h-28 w-28 shrink-0">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={img.kind === "existing" ? img.url : img.previewUrl}
                alt={`사진 ${i + 1}`}
                className="h-full w-full rounded-xl object-cover"
              />
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
              {images.length === 0 ? (
                <CameraIcon className="h-6 w-6" />
              ) : (
                <PlusIcon className="h-6 w-6" />
              )}
              <span className="text-xs">{images.length}/3</span>
              <input
                type="file"
                accept={ACCEPT}
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
