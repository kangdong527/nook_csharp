"use client";

/* eslint-disable @typescript-eslint/no-explicit-any */
// 지도 화면의 클라이언트 잎. 인증 판정은 page.tsx(서버)가 이미 끝냈고,
// 여기는 `user` 를 **주어진 값**으로 받는다 → useRequireAuth 가 사라진 자리다.
//
// 지도는 CLAUDE.md 가 명시한 "use client" 예외다(Kakao SDK 는 브라우저 전용).

import { Suspense, useCallback, useEffect, useRef, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import {
  getGroupSavedPlaces,
  getMyGroups,
  getMySavedPlaces,
  getPlace,
} from "@/lib/store";
import { isKakaoConfigured, loadKakao, searchPlaces } from "@/lib/kakao";
import type {
  Group,
  GroupSavedPlace,
  KakaoPlace,
  SavedPlace,
  User,
} from "@/lib/types";
import { slotColor } from "@/lib/types";
import { useToast } from "@/components/Toast";
import { BottomNav } from "@/components/BottomNav";
import { PlaceSheet } from "@/components/PlaceSheet";
import { GroupControl } from "@/components/GroupControl";
import { GpsIcon, SearchIcon } from "@/components/icons";
import { Spinner } from "@/components/Spinner";

const SEOUL = { lat: 37.5665, lng: 126.978 };
const ACTIVE_GROUP_KEY = "nook.activeGroup";

type MarkerPlace = SavedPlace | GroupSavedPlace;

function MapView({ user }: { user: User }) {
  // 훅 의존성에는 객체가 아니라 id 를 넣는다 — user 는 서버가 렌더마다 새로
  // 만든 객체이므로, 그대로 deps 에 두면 참조가 달라져 불필요한 재실행이 된다.
  const userId = user.id;
  const toast = useToast();
  const router = useRouter();
  const searchParams = useSearchParams();
  const focusPlaceId = searchParams.get("placeId");
  const focusReviewId = searchParams.get("reviewId");
  const consumedFocusRef = useRef(false);

  const mapEl = useRef<HTMLDivElement>(null);
  const searchAreaRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<any>(null);
  const overlaysRef = useRef<any[]>([]);
  const selectedPinRef = useRef<any>(null);

  const [keyword, setKeyword] = useState("");
  const [results, setResults] = useState<KakaoPlace[] | null>(null);
  const [searching, setSearching] = useState(false);
  const [searchNotice, setSearchNotice] = useState<string | null>(null);
  const [searchFocused, setSearchFocused] = useState(false);
  const noticeTimer = useRef<number | null>(null);
  const [selected, setSelected] = useState<KakaoPlace | null>(null);
  const [highlightReviewId, setHighlightReviewId] = useState<string | null>(null);
  const [kakaoReady, setKakaoReady] = useState(false);

  const [groups, setGroups] = useState<Group[]>([]);
  const [activeGroupId, setActiveGroupId] = useState<string | null>(null);

  const configured = isKakaoConfigured();

  // 지도 초기화
  useEffect(() => {
    if (!configured) return;
    let alive = true;
    const closeResults = () => setResults(null);
    loadKakao()
      .then((kakao) => {
        if (!alive || !mapEl.current) return;
        const center = new kakao.maps.LatLng(SEOUL.lat, SEOUL.lng);
        const map = new kakao.maps.Map(mapEl.current, { center, level: 7 });
        map.setMaxLevel(11);
        kakao.maps.event.addListener(map, "dragstart", closeResults);
        kakao.maps.event.addListener(map, "zoom_changed", closeResults);
        kakao.maps.event.addListener(map, "click", closeResults);
        mapRef.current = map;
        setKakaoReady(true);
        // 현재 위치로 이동 시도
        moveToCurrentLocation(false);
      })
      .catch(() => toast("지도를 불러오지 못했어요. 잠시 후 다시 시도해 주세요."));
    return () => {
      alive = false;
      const map = mapRef.current;
      if (map && window.kakao) {
        window.kakao.maps.event.removeListener(map, "dragstart", closeResults);
        window.kakao.maps.event.removeListener(map, "zoom_changed", closeResults);
        window.kakao.maps.event.removeListener(map, "click", closeResults);
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [configured]);

  // 검색 결과 리스트 바깥 터치 시 닫기
  useEffect(() => {
    if (!results || results.length === 0) return;
    const onPointerDown = (ev: PointerEvent) => {
      const target = ev.target as Node | null;
      if (!target) return;
      if (searchAreaRef.current?.contains(target)) return;
      setResults(null);
    };
    window.addEventListener("pointerdown", onPointerDown);
    return () => window.removeEventListener("pointerdown", onPointerDown);
  }, [results]);

  // 저장된 장소 마커 렌더
  const renderMarkers = useCallback(
    (places: MarkerPlace[], kakao: any) => {
      const map = mapRef.current;
      if (!map) return;
      overlaysRef.current.forEach((o) => o.setMap(null));
      overlaysRef.current = [];

      places.forEach((p) => {
        const el = document.createElement("div");
        el.style.cursor = "pointer";
        el.style.transform = "translateY(-50%)";
        el.innerHTML = "reviewMemberCount" in p ? groupStarSvg(p) : starSvg(p.star);
        el.addEventListener("click", () => {
          setResults(null);
          setSelected(p);
        });
        const overlay = new kakao.maps.CustomOverlay({
          position: new kakao.maps.LatLng(p.lat, p.lng),
          content: el,
          yAnchor: 1,
          clickable: true,
        });
        overlay.setMap(map);
        overlaysRef.current.push(overlay);
      });
    },
    []
  );

  const reloadSaved = useCallback(async () => {
    if (!mapRef.current || !window.kakao) return;
    try {
      const places: MarkerPlace[] = activeGroupId
        ? await getGroupSavedPlaces(activeGroupId)
        : await getMySavedPlaces();
      renderMarkers(places, window.kakao);
    } catch {
      /* 마커 로드 실패는 조용히 무시 */
    }
  }, [userId, activeGroupId, renderMarkers]);

  useEffect(() => {
    if (kakaoReady) reloadSaved();
  }, [kakaoReady, reloadSaved]);

  // 다른 멤버의 변경은 서버가 알려 주지 않는다 → 화면으로 돌아올 때 다시 불러온다.
  // 탭 복귀(visibilitychange)와 창 전환(focus)이 함께 오면 한 번만 부른다.
  useEffect(() => {
    if (!kakaoReady) return;
    let last = 0;
    const onReturn = () => {
      if (document.visibilityState !== "visible") return;
      const now = Date.now();
      if (now - last < 1000) return;
      last = now;
      reloadSaved();
    };
    document.addEventListener("visibilitychange", onReturn);
    window.addEventListener("focus", onReturn);
    return () => {
      document.removeEventListener("visibilitychange", onReturn);
      window.removeEventListener("focus", onReturn);
    };
  }, [kakaoReady, reloadSaved]);

  // 선택한 장소 핀 표시 (검색 선택 / 마커 클릭 시)
  useEffect(() => {
    if (!kakaoReady || !mapRef.current || !window.kakao) return;
    if (selectedPinRef.current) {
      selectedPinRef.current.setMap(null);
      selectedPinRef.current = null;
    }
    if (!selected) return;
    const el = document.createElement("div");
    el.innerHTML = pinSvg();
    const overlay = new window.kakao.maps.CustomOverlay({
      position: new window.kakao.maps.LatLng(selected.lat, selected.lng),
      content: el,
      yAnchor: 1,
      zIndex: 10,
    });
    overlay.setMap(mapRef.current);
    selectedPinRef.current = overlay;
  }, [selected, kakaoReady]);

  // 내 그룹 목록 로드 + 저장된 활성 그룹 복원
  const reloadGroups = useCallback(async () => {
    setGroups(await getMyGroups());
  }, [userId]);

  useEffect(() => {
    const saved = window.localStorage.getItem(ACTIVE_GROUP_KEY);
    if (saved) setActiveGroupId(saved);
    reloadGroups();
  }, [reloadGroups]);

  // 그룹 목록 로드 후, 더 이상 속하지 않는 그룹이면 내 지도로 복귀
  useEffect(() => {
    if (activeGroupId && !groups.some((g) => g.id === activeGroupId)) {
      setActiveGroupId(null);
    }
  }, [groups, activeGroupId]);

  const selectGroup = useCallback((id: string | null) => {
    setActiveGroupId(id);
    if (id) window.localStorage.setItem(ACTIVE_GROUP_KEY, id);
    else window.localStorage.removeItem(ACTIVE_GROUP_KEY);
  }, []);

  // 리뷰 발행 직후: 해당 장소 바텀시트 열고 작성한 리뷰로 스크롤
  useEffect(() => {
    if (consumedFocusRef.current || !focusPlaceId) return;
    if (configured && !kakaoReady) return; // 지도 준비 후 이동
    consumedFocusRef.current = true;
    (async () => {
      const place = await getPlace(focusPlaceId);
      if (place) {
        setHighlightReviewId(focusReviewId);
        setSelected(place);
        if (mapRef.current && window.kakao) {
          mapRef.current.setLevel(4);
          mapRef.current.panTo(new window.kakao.maps.LatLng(place.lat, place.lng));
        }
      }
      router.replace("/map");
    })();
  }, [focusPlaceId, focusReviewId, configured, kakaoReady, router]);

  const moveToCurrentLocation = (notify = true) => {
    if (!navigator.geolocation || !mapRef.current || !window.kakao) {
      if (notify) toast("위치 정보를 사용할 수 없어요.");
      return;
    }
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        const { latitude, longitude } = pos.coords;
        const loc = new window.kakao.maps.LatLng(latitude, longitude);
        mapRef.current.setLevel(5);
        mapRef.current.panTo(loc);
      },
      () => {
        if (notify) toast("위치 권한이 없어 기본 위치로 표시해요.");
      },
      { enableHighAccuracy: true, timeout: 5000 }
    );
  };

  // 검색창 아래 안내 문구 (없으면 null). 잠시 후 자동으로 사라짐.
  const showNotice = (message: string) => {
    setSearchNotice(message);
    if (noticeTimer.current) window.clearTimeout(noticeTimer.current);
    noticeTimer.current = window.setTimeout(() => setSearchNotice(null), 2600);
  };

  const clearNotice = () => {
    setSearchNotice(null);
    if (noticeTimer.current) window.clearTimeout(noticeTimer.current);
  };

  useEffect(() => {
    return () => {
      if (noticeTimer.current) window.clearTimeout(noticeTimer.current);
    };
  }, []);

  const runSearch = async () => {
    const q = keyword.trim();
    if (!q) return;
    clearNotice();
    setSearching(true);
    try {
      const map = mapRef.current;
      const center = map
        ? { lat: map.getCenter().getLat(), lng: map.getCenter().getLng() }
        : SEOUL;
      const found = await searchPlaces(q, center);
      setResults(found);
      if (found.length === 0) {
        showNotice("검색 결과가 없어요");
      } else if (map && window.kakao) {
        map.panTo(new window.kakao.maps.LatLng(found[0].lat, found[0].lng));
      }
    } catch {
      showNotice("검색에 실패했어요. 다른 키워드로 검색해보세요!");
    } finally {
      setSearching(false);
    }
  };

  const pickResult = (p: KakaoPlace) => {
    setResults(null);
    setKeyword("");
    clearNotice();
    if (mapRef.current && window.kakao) {
      mapRef.current.setLevel(4);
      mapRef.current.panTo(new window.kakao.maps.LatLng(p.lat, p.lng));
    }
    setSelected(p);
  };

  return (
    <div className="relative flex flex-1 flex-col">
      {/* 지도 */}
      <div className="relative flex-1">
        {configured ? (
          <div ref={mapEl} className="kakao-map absolute inset-0" />
        ) : (
          <div className="absolute inset-0 flex flex-col items-center justify-center gap-3 bg-field px-8 text-center">
            <p className="text-sm font-semibold text-ink">카카오 지도 키가 필요해요</p>
            <p className="text-xs leading-relaxed text-sub">
              <code>.env.local</code> 에 <code>NEXT_PUBLIC_KAKAO_MAP_KEY</code> 를
              설정하면 지도가 표시됩니다.
            </p>
          </div>
        )}

        {/* 검색창 */}
        <div
          ref={searchAreaRef}
          className="absolute inset-x-0 top-0 z-20 px-4 pt-3"
        >
          <div className="flex items-center gap-2 rounded-2xl border border-line bg-white/95 px-4 py-2.5 shadow-card backdrop-blur">
            <SearchIcon className="h-5 w-5 text-sub" />
            <input
              value={keyword}
              onChange={(e) => {
                setKeyword(e.target.value);
                if (searchNotice) clearNotice();
              }}
              onFocus={() => setSearchFocused(true)}
              onBlur={() => setTimeout(() => setSearchFocused(false), 120)}
              onKeyDown={(e) => e.key === "Enter" && runSearch()}
              placeholder="주소, 상호명 검색"
              className="flex-1 bg-transparent text-sm outline-none placeholder:text-sub"
            />
            {searching && <Spinner className="text-key" />}
          </div>

          {/* 검색 진입 안내 (검색 전 빈 상태) */}
          {searchFocused && !searchNotice && keyword.trim() === "" && !results && (
            <div className="animate-fade-up mt-2 rounded-2xl border border-line bg-white/95 px-5 py-4 text-sm leading-relaxed text-sub shadow-card backdrop-blur">
              <p className="font-semibold text-ink">장소를 검색해 보세요</p>
              <ul className="mt-3 list-disc space-y-1 pl-5">
                <li>최근에 다녀온 카페, 맛집, 어디든</li>
                <li>자주 가는 동네 단골집</li>
                <li>저장만 해뒀던 그곳</li>
                <li>친구가 추천해준 곳</li>
              </ul>
            </div>
          )}

          {/* 검색 안내 문구 (검색창 바로 아래) */}
          {searchNotice && (
            <div className="animate-fade-up mt-2 rounded-2xl bg-key/95 px-4 py-3 text-center text-sm font-medium text-white shadow-card">
              {searchNotice}
            </div>
          )}

          {/* 검색 결과 */}
          {results && results.length > 0 && (
            <div className="mt-2 max-h-[50vh] overflow-y-auto rounded-2xl border border-line bg-white shadow-card">
              {results.map((p) => (
                <button
                  key={p.kakaoId}
                  onClick={() => pickResult(p)}
                  className="flex w-full flex-col items-start border-b border-line px-4 py-3 text-left last:border-0 hover:bg-field"
                >
                  <span className="text-sm font-semibold">{p.name}</span>
                  <span className="mt-0.5 truncate text-xs text-sub">
                    {p.roadAddress || p.address}
                  </span>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* 그룹 (공유 지도) */}
        {configured && (
          <GroupControl
            groups={groups}
            activeGroupId={activeGroupId}
            onSelect={selectGroup}
            onChanged={reloadGroups}
          />
        )}

        {/* GPS 버튼 */}
        {configured && (
          <button
            onClick={() => moveToCurrentLocation(true)}
            className="absolute bottom-4 right-4 z-20 flex h-11 w-11 items-center justify-center rounded-full border border-line bg-white text-ink shadow-card transition active:scale-95"
            aria-label="현재 위치"
          >
            <GpsIcon className="h-5 w-5" />
          </button>
        )}
      </div>

      <BottomNav />

      {selected && (
        <PlaceSheet
          kakaoPlace={selected}
          userId={userId}
          highlightReviewId={highlightReviewId}
          onClose={() => {
            setSelected(null);
            setHighlightReviewId(null);
          }}
          onChanged={reloadSaved}
        />
      )}
    </div>
  );
}

export function MapScreen({ user }: { user: User }) {
  // Suspense 는 useSearchParams() 때문에 필요하다(?placeId= 로 진입하는 경로).
  return (
    <Suspense
      fallback={
        <div className="flex flex-1 items-center justify-center">
          <Spinner className="text-key" />
        </div>
      }
    >
      <MapView user={user} />
    </Suspense>
  );
}

// 선택한 장소 핀 SVG (물방울 모양, 끝점이 좌표를 가리킴)
function pinSvg(): string {
  return `<svg width="32" height="40" viewBox="0 0 24 30" style="filter:drop-shadow(0 2px 3px rgba(0,0,0,0.35))">
    <path d="M12 0C5.6 0 .5 5 .5 11.2.5 19.3 12 30 12 30s11.5-10.7 11.5-18.8C23.5 5 18.4 0 12 0z"
      fill="#111111" stroke="#ffffff" stroke-width="1.2"/>
    <circle cx="12" cy="11.2" r="4.1" fill="#ffffff"/>
  </svg>`;
}

// 그룹 지도의 별 하나(PM 그룹맵 표시 정책, 서버가 장소마다 집계한 사실로 그린다 — GR-05):
//   리뷰 있음      → 가장 먼저 리뷰한 멤버의 색, 리뷰한 멤버가 2명 이상이면 안쪽에 숫자(리뷰 + 북마크도 리뷰 규칙)
//   북마크만 2개+  → 진한 회색(#4A4A4A) + 숫자
//   북마크만 1개   → 기존 회색 별 + 그 멤버 색 외곽선
function groupStarSvg(p: GroupSavedPlace): string {
  if (p.reviewMemberCount > 0) {
    return starSvg("fill", slotColor(p.firstReviewerSlot ?? 0), p.reviewMemberCount > 1 ? p.reviewMemberCount : undefined);
  }
  if (p.bookmarkCount > 1) return starSvg("fill", "#4A4A4A", p.bookmarkCount);
  return starSvg("gray", undefined, undefined, slotColor(p.soleBookmarkerSlot ?? 0));
}

// 마커용 별 SVG (color 지정 시 멤버 색상으로 표시 = 그룹 모드). count: 안쪽 숫자, outline: 외곽선 색.
function starSvg(star: "gray" | "fill", color?: string, count?: number, outline?: string): string {
  const label =
    count === undefined
      ? ""
      : `<text x="12" y="15.2" text-anchor="middle" font-size="7.5" font-weight="700" fill="#ffffff">${count}</text>`;
  if (star === "gray") {
    return `<svg width="30" height="30" viewBox="0 0 24 24" style="filter:drop-shadow(0 1px 2px rgba(0,0,0,0.3))">
    <path d="M12 2.6l2.9 5.88 6.49.94-4.7 4.58 1.11 6.46L12 17.9l-5.8 3.05 1.1-6.46-4.69-4.58 6.49-.94L12 2.6z"
      fill="#9aa0a6" stroke="${outline ?? "#ffffff"}" stroke-width="${outline ? 2 : 1.4}" stroke-linejoin="round"/>
  </svg>`;
  }
  const normalized = (color ?? "").toLowerCase();
  // 오너(검정 닉네임)의 별은 노랑으로 고정
  const fill =
    normalized === "#000000"
      ? "#ffc83d"
      : color ?? "#ffc83d";
  return `<svg width="30" height="30" viewBox="0 0 24 24" style="filter:drop-shadow(0 1px 2px rgba(0,0,0,0.3))">
    <path d="M12 2.6l2.9 5.88 6.49.94-4.7 4.58 1.11 6.46L12 17.9l-5.8 3.05 1.1-6.46-4.69-4.58 6.49-.94L12 2.6z"
      fill="${fill}" stroke="#ffffff" stroke-width="1.4" stroke-linejoin="round"/>${label}
  </svg>`;
}
