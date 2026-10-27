"use client";

import { useEffect, useState } from "react";
import { ApiError, changeAvatar, updateNickname, MAX_IMAGE_BYTES, type MeUser } from "@/lib/store";
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
  // 고른 사진은 저장할 때 올린다(미리보기만) — 고르는 즉시 올리면 취소해도 사진이 남았다(원본).
  const [avatarFile, setAvatarFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const avatarUrl = previewUrl ?? user.avatarUrl;

  useEffect(() => () => {
    if (previewUrl) URL.revokeObjectURL(previewUrl);
  }, [previewUrl]);

  const ok = isValidNickname(nickname);

  const onPick = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;
    if (file.size > MAX_IMAGE_BYTES) {
      toast("이미지 용량이 너무 커요. 다른 사진을 선택해주세요.");
      return;
    }
    setAvatarFile(file);
    setPreviewUrl(URL.createObjectURL(file));
  };

  const toUser = (me: MeUser): User => ({ ...user, nickname: me.nickname, avatarUrl: me.avatarUrl });

  const save = async () => {
    if (!ok) {
      toast("2~12자로 닉네임을 입력해 주세요.");
      return;
    }
    setSaving(true);
    try {
      // 닉네임과 사진은 API 가 따로다(PATCH /me · PUT /me/avatar). 바뀐 것만 보낸다.
      let saved: User = user;
      if (nickname.trim() !== user.nickname) {
        const res = await updateNickname(nickname);
        if (!res.ok) {
          toast(res.reason === "nickname_taken" ? "이미 사용 중인 닉네임이에요." : "2~12자로 닉네임을 입력해 주세요.");
          return;
        }
        saved = toUser(res.user);
      }
      if (avatarFile) saved = { ...saved, avatarUrl: (await changeAvatar(avatarFile)).avatarUrl };
      toast("프로필을 수정했어요.");
      onSaved(saved);
    } catch (e) {
      toast(
        e instanceof ApiError && e.title === "User.AvatarUnsupportedFormat"
          ? "JPG, PNG, WEBP 사진만 올릴 수 있어요."
          : "일시적인 오류가 발생했어요. 잠시 후 다시 시도해 주세요."
      );
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
              <CameraIcon className="h-4 w-4" />
              <input type="file" accept="image/jpeg,image/png,image/webp" className="hidden" onChange={onPick} />
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
          disabled={!ok || saving}
          className="mt-6 flex w-full items-center justify-center gap-2 rounded-2xl bg-key py-3.5 text-base font-semibold text-white transition active:scale-[0.99] disabled:bg-disabled"
        >
          {saving && <Spinner />}
          프로필 수정
        </button>
      </div>
    </div>
  );
}
