"use client";

import { useState } from "react";
import { updateProfile, uploadImage, MAX_IMAGE_BYTES } from "@/lib/store";
import { isValidNickname, type User } from "@/lib/types";
import { useToast } from "./Toast";
import { Spinner } from "./Spinner";
import { Avatar } from "./Avatar";
import { CameraIcon, CloseIcon } from "./icons";

export function ProfileEditModal({
  user,
  onClose,
  onSaved,
}: {
  user: User;
  onClose: () => void;
  onSaved: (u: User) => void;
}) {
  const toast = useToast();
  const [nickname, setNickname] = useState(user.nickname);
  const [avatarUrl, setAvatarUrl] = useState<string | null>(user.avatarUrl);
  const [uploading, setUploading] = useState(false);
  const [saving, setSaving] = useState(false);

  const ok = isValidNickname(nickname);

  const onPick = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;
    if (file.size > MAX_IMAGE_BYTES) {
      toast("이미지 용량이 너무 커요. 다른 사진을 선택해주세요.");
      return;
    }
    setUploading(true);
    try {
      setAvatarUrl(await uploadImage(file));
    } catch {
      toast("일시적인 오류가 발생했어요. 잠시 후 다시 시도해 주세요.");
    } finally {
      setUploading(false);
    }
  };

  const save = async () => {
    if (!ok) {
      toast("2~12자로 닉네임을 입력해 주세요.");
      return;
    }
    setSaving(true);
    try {
      const res = await updateProfile(user.id, { nickname, avatarUrl });
      if (!res.ok) {
        toast(
          res.reason === "nickname"
            ? "이미 사용 중인 닉네임이에요."
            : "일시적인 오류가 발생했어요. 잠시 후 다시 시도해 주세요."
        );
        return;
      }
      toast("프로필을 수정했어요.");
      onSaved(res.user);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 mx-auto flex max-w-[420px] items-center justify-center px-6">
      <button className="absolute inset-0 bg-black/45" onClick={onClose} aria-label="닫기" />
      <div className="animate-pop-in relative w-full rounded-3xl bg-white p-6">
        <button
          onClick={onClose}
          className="absolute right-4 top-4 flex h-8 w-8 items-center justify-center text-sub"
          aria-label="취소"
        >
          <CloseIcon className="h-5 w-5" />
        </button>

        <div className="flex flex-col items-center pt-2">
          <div className="relative">
            <Avatar url={avatarUrl} size={84} />
            <label className="absolute -bottom-1 -right-1 flex h-8 w-8 cursor-pointer items-center justify-center rounded-full border border-line bg-white text-ink shadow-card">
              {uploading ? <Spinner className="text-key" /> : <CameraIcon className="h-4 w-4" />}
              <input type="file" accept="image/*" className="hidden" onChange={onPick} />
            </label>
          </div>
        </div>

        <div className="mt-6">
          <input
            value={nickname}
            onChange={(e) => setNickname(e.target.value.slice(0, 12))}
            placeholder="닉네임 (2~12자)"
            className="w-full border-b border-line bg-transparent pb-2 text-center text-lg outline-none placeholder:text-disabled focus:border-key"
          />
        </div>

        <button
          onClick={save}
          disabled={!ok || saving || uploading}
          className="mt-6 flex w-full items-center justify-center gap-2 rounded-2xl bg-key py-3.5 text-base font-semibold text-white transition active:scale-[0.99] disabled:bg-disabled"
        >
          {saving && <Spinner />}
          프로필 수정
        </button>
      </div>
    </div>
  );
}
