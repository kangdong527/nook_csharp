"use client";

import { useState } from "react";
import {
  createGroup,
  joinByInviteCode,
  leaveGroup,
  renameGroup,
} from "@/lib/store";
import { GROUP_MAX_COUNT, type Group } from "@/lib/types";
import { useToast } from "./Toast";
import { Spinner } from "./Spinner";
import {
  CheckIcon,
  CloseIcon,
  MoreIcon,
  PeopleIcon,
  PersonPlusIcon,
} from "./icons";

export function GroupControl({
  userId,
  groups,
  activeGroupId,
  onSelect,
  onChanged,
}: {
  userId: string;
  groups: Group[];
  activeGroupId: string | null;
  onSelect: (groupId: string | null) => void;
  onChanged: () => Promise<void> | void;
}) {
  const toast = useToast();
  const [expanded, setExpanded] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [joinOpen, setJoinOpen] = useState(false);
  const [editGroup, setEditGroup] = useState<Group | null>(null);

  const handleSelect = (id: string | null) => {
    onSelect(id);
    setExpanded(false);
  };

  return (
    <>
      {/* 그룹 버튼 (접힘) */}
      {!expanded && (
        <button
          onClick={() => setExpanded(true)}
          className="absolute right-4 top-[72px] z-30 flex h-11 w-11 items-center justify-center rounded-full bg-key text-white shadow-card transition active:scale-95"
          aria-label="그룹"
        >
          <PeopleIcon className="h-6 w-6" />
        </button>
      )}

      {/* 펼친 패널 */}
      {expanded && (
        <>
          <button
            className="fixed inset-0 z-30 cursor-default"
            onClick={() => setExpanded(false)}
            aria-label="닫기"
          />
          <div className="absolute right-4 top-[72px] z-40 flex w-60 flex-col items-end gap-2">
            <button
              onClick={() => setExpanded(false)}
              className="flex h-11 w-11 items-center justify-center rounded-full bg-key text-white shadow-card"
              aria-label="그룹 닫기"
            >
              <PeopleIcon className="h-6 w-6" />
            </button>

            <div className="w-full overflow-hidden rounded-2xl border border-line bg-white shadow-card">
              <div className="max-h-64 overflow-y-auto py-1">
                <GroupRow
                  label="내 지도"
                  dotColor={null}
                  active={activeGroupId === null}
                  onClick={() => handleSelect(null)}
                />
                {groups.map((g) => (
                  <GroupRow
                    key={g.id}
                    label={g.name}
                    sub={`${g.memberCount}명`}
                    dotColor={g.myColor}
                    active={g.id === activeGroupId}
                    onClick={() => handleSelect(g.id)}
                    onMore={() => {
                      setExpanded(false);
                      setEditGroup(g);
                    }}
                  />
                ))}
                {groups.length === 0 && (
                  <p className="px-4 py-3 text-xs leading-relaxed text-sub">
                    아직 그룹이 없어요.
                    <br />
                    그룹을 만들거나 초대 코드로 합류해 보세요.
                  </p>
                )}
              </div>

              <div className="border-t border-line">
                <MenuButton
                  icon={<PersonPlusIcon className="h-4 w-4" />}
                  label="그룹 만들기"
                  onClick={() => {
                    if (groups.length >= GROUP_MAX_COUNT) {
                      toast(`그룹은 최대 ${GROUP_MAX_COUNT}개까지 만들 수 있어요.`);
                      return;
                    }
                    setExpanded(false);
                    setCreateOpen(true);
                  }}
                />
                <MenuButton
                  icon={<PeopleIcon className="h-4 w-4" />}
                  label="초대 코드 입력"
                  onClick={() => {
                    if (groups.length >= GROUP_MAX_COUNT) {
                      toast(`그룹은 최대 ${GROUP_MAX_COUNT}개까지 참여할 수 있어요.`);
                      return;
                    }
                    setExpanded(false);
                    setJoinOpen(true);
                  }}
                />
              </div>
            </div>
          </div>
        </>
      )}

      {createOpen && (
        <CreateGroupModal
          onClose={() => setCreateOpen(false)}
          onCreated={async (group) => {
            await onChanged();
            onSelect(group.id);
            setCreateOpen(false);
            toast(`'${group.name}' 그룹을 만들었어요.`);
          }}
          userId={userId}
        />
      )}

      {joinOpen && (
        <JoinGroupModal
          userId={userId}
          onClose={() => setJoinOpen(false)}
          onJoined={async (group) => {
            await onChanged();
            onSelect(group.id);
            setJoinOpen(false);
            toast(`'${group.name}' 그룹에 합류했어요.`);
          }}
        />
      )}

      {editGroup && (
        <EditGroupModal
          group={editGroup}
          userId={userId}
          onClose={() => setEditGroup(null)}
          onSaved={async () => {
            await onChanged();
            setEditGroup(null);
          }}
          onLeft={async () => {
            if (activeGroupId === editGroup.id) onSelect(null);
            await onChanged();
            setEditGroup(null);
            toast("그룹에서 나왔어요.");
          }}
        />
      )}
    </>
  );
}

function GroupRow({
  label,
  sub,
  dotColor,
  active,
  onClick,
  onMore,
}: {
  label: string;
  sub?: string;
  dotColor: string | null;
  active: boolean;
  onClick: () => void;
  onMore?: () => void;
}) {
  return (
    <div
      className={`flex items-center gap-2 px-3 ${active ? "bg-field" : ""}`}
    >
      <button
        onClick={onClick}
        className="flex flex-1 items-center gap-2.5 py-2.5 text-left"
      >
        <span
          className="h-3 w-3 shrink-0 rounded-full border border-line"
          style={{ backgroundColor: dotColor ?? "#ffffff" }}
        />
        <span className="min-w-0 flex-1">
          <span className="block truncate text-sm font-medium text-ink">
            {label}
          </span>
          {sub && <span className="block text-xs text-sub">{sub}</span>}
        </span>
        {active && <CheckIcon className="h-4 w-4 shrink-0 text-key" />}
      </button>
      {onMore && (
        <button
          onClick={onMore}
          className="flex h-7 w-7 items-center justify-center rounded-full text-sub hover:bg-line"
          aria-label="그룹 수정"
        >
          <MoreIcon className="h-4 w-4" />
        </button>
      )}
    </div>
  );
}

function MenuButton({
  icon,
  label,
  onClick,
}: {
  icon: React.ReactNode;
  label: string;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      className="flex w-full items-center gap-2.5 px-4 py-3 text-left text-sm font-medium text-ink hover:bg-field"
    >
      <span className="text-sub">{icon}</span>
      {label}
    </button>
  );
}

// 지도4 스타일의 어두운 입력 팝업
function DarkPopup({
  title,
  value,
  onChange,
  placeholder,
  onClose,
  onConfirm,
  loading,
  confirmDisabled,
}: {
  title: string;
  value: string;
  onChange: (v: string) => void;
  placeholder: string;
  onClose: () => void;
  onConfirm: () => void;
  loading: boolean;
  confirmDisabled: boolean;
}) {
  return (
    <div className="fixed inset-0 z-50 mx-auto flex max-w-[420px] items-center justify-center px-6">
      <button
        className="absolute inset-0 bg-black/40"
        onClick={onClose}
        aria-label="닫기"
      />
      <div className="animate-pop-in relative w-full rounded-3xl bg-key px-5 py-5 text-white shadow-sheet">
        <div className="flex items-center justify-between">
          <button onClick={onClose} aria-label="취소" className="p-1">
            <CloseIcon className="h-5 w-5" />
          </button>
          <h2 className="text-base font-semibold">{title}</h2>
          <button
            onClick={onConfirm}
            disabled={confirmDisabled || loading}
            aria-label="확인"
            className="flex h-8 w-8 items-center justify-center rounded-full bg-white/15 disabled:opacity-40"
          >
            {loading ? <Spinner /> : <CheckIcon className="h-5 w-5" />}
          </button>
        </div>
        <input
          autoFocus
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onKeyDown={(e) =>
            e.key === "Enter" && !confirmDisabled && !loading && onConfirm()
          }
          placeholder={placeholder}
          className="mt-5 w-full border-b border-white/30 bg-transparent pb-2 text-center text-lg outline-none placeholder:text-white/40 focus:border-white"
        />
      </div>
    </div>
  );
}

function CreateGroupModal({
  userId,
  onClose,
  onCreated,
}: {
  userId: string;
  onClose: () => void;
  onCreated: (group: Group) => void;
}) {
  const toast = useToast();
  const [name, setName] = useState("");
  const [loading, setLoading] = useState(false);

  const submit = async () => {
    const n = name.trim();
    if (n.length < 1 || n.length > 16) {
      toast("그룹 이름은 1~16자까지 입력할 수 있어요.");
      return;
    }
    setLoading(true);
    const res = await createGroup(userId, n);
    setLoading(false);
    if (!res.ok) {
      toast(
        res.reason === "setup"
          ? "Supabase에 groups.sql이 적용되지 않았어요. DB 스키마를 먼저 적용해 주세요."
          : res.reason === "user_missing"
          ? "세션이 만료되었어요. 다시 로그인한 뒤 시도해 주세요."
          : res.reason === "too_many_groups"
          ? `그룹은 최대 ${GROUP_MAX_COUNT}개까지 만들 수 있어요.`
          : "그룹 생성에 실패했어요. 잠시 후 다시 시도해 주세요."
      );
      return;
    }
    onCreated(res.group);
  };

  return (
    <DarkPopup
      title="그룹 만들기"
      value={name}
      onChange={(v) => setName(v.slice(0, 16))}
      placeholder="그룹 이름 입력"
      onClose={onClose}
      onConfirm={submit}
      loading={loading}
      confirmDisabled={name.trim().length === 0}
    />
  );
}

function JoinGroupModal({
  userId,
  onClose,
  onJoined,
}: {
  userId: string;
  onClose: () => void;
  onJoined: (group: Group) => void;
}) {
  const toast = useToast();
  const [code, setCode] = useState("");
  const [loading, setLoading] = useState(false);

  const submit = async () => {
    setLoading(true);
    const res = await joinByInviteCode(userId, code);
    setLoading(false);
    if (res.ok) {
      onJoined(res.group);
      return;
    }
    toast(
      res.reason === "not_found"
        ? "유효하지 않은 초대 코드예요. 다시 확인해 주세요."
        : res.reason === "already"
        ? "이미 합류한 그룹이에요."
        : res.reason === "too_many_groups"
        ? `그룹은 최대 ${GROUP_MAX_COUNT}개까지 참여할 수 있어요.`
        : res.reason === "full"
        ? "이 그룹은 정원(4명)이 가득 찼어요."
        : "합류에 실패했어요. 잠시 후 다시 시도해 주세요."
    );
  };

  return (
    <DarkPopup
      title="초대 코드 입력"
      value={code}
      onChange={(v) => setCode(v.toUpperCase().slice(0, 6))}
      placeholder="초대코드 입력"
      onClose={onClose}
      onConfirm={submit}
      loading={loading}
      confirmDisabled={code.trim().length === 0}
    />
  );
}

function EditGroupModal({
  group,
  userId,
  onClose,
  onSaved,
  onLeft,
}: {
  group: Group;
  userId: string;
  onClose: () => void;
  onSaved: () => void;
  onLeft: () => void;
}) {
  const toast = useToast();
  const [name, setName] = useState(group.name);
  const [saving, setSaving] = useState(false);
  const [leaving, setLeaving] = useState(false);
  const [copied, setCopied] = useState(false);

  const save = async () => {
    const n = name.trim();
    if (n.length < 1 || n.length > 16) {
      toast("그룹 이름은 1~16자까지 입력할 수 있어요.");
      return;
    }
    setSaving(true);
    const ok = await renameGroup(group.id, n);
    setSaving(false);
    if (!ok) {
      toast("저장에 실패했어요. 잠시 후 다시 시도해 주세요.");
      return;
    }
    onSaved();
  };

  const copyCode = async () => {
    try {
      await navigator.clipboard.writeText(group.inviteCode);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1500);
    } catch {
      toast("초대 코드 복사에 실패했어요.");
    }
  };

  const leave = async () => {
    setLeaving(true);
    await leaveGroup(group.id, userId);
    setLeaving(false);
    onLeft();
  };

  return (
    <div className="fixed inset-0 z-50 mx-auto flex max-w-[420px] items-center justify-center px-6">
      <button
        className="absolute inset-0 bg-black/40"
        onClick={onClose}
        aria-label="닫기"
      />
      <div className="animate-pop-in relative w-full rounded-3xl bg-white p-5 shadow-sheet">
        <div className="flex items-center justify-between">
          <button onClick={onClose} aria-label="닫기" className="p-1 text-ink">
            <CloseIcon className="h-5 w-5" />
          </button>
          <h2 className="text-base font-semibold">그룹 수정</h2>
          <button
            onClick={save}
            disabled={saving || name.trim().length === 0}
            aria-label="저장"
            className="flex h-8 w-8 items-center justify-center rounded-full bg-field text-ink disabled:opacity-40"
          >
            {saving ? <Spinner className="text-key" /> : <CheckIcon className="h-5 w-5" />}
          </button>
        </div>

        <label className="mt-5 block text-xs font-medium text-sub">그룹 이름</label>
        <input
          value={name}
          onChange={(e) => setName(e.target.value.slice(0, 16))}
          placeholder="그룹 이름 입력"
          className="mt-2 w-full border-b border-line bg-transparent pb-2 text-lg outline-none placeholder:text-disabled focus:border-key"
        />

        <label className="mt-6 block text-xs font-medium text-sub">초대 코드</label>
        <button
          onClick={copyCode}
          className="mt-2 flex w-full items-center justify-between rounded-2xl bg-field px-4 py-3 text-left"
        >
          <span className="text-lg font-bold tracking-[0.2em]">
            {group.inviteCode}
          </span>
          <span className="text-xs font-medium text-sub">
            {copied ? "복사됨" : "복사"}
          </span>
        </button>

        <button
          onClick={leave}
          disabled={leaving}
          className="mt-6 flex w-full items-center justify-center gap-2 rounded-2xl border border-line py-3 text-sm font-semibold text-like disabled:opacity-50"
        >
          {leaving && <Spinner className="text-like" />}
          그룹 나가기
        </button>
      </div>
    </div>
  );
}
