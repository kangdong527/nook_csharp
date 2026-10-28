"use client";

import type { KakaoPlace } from "./types";

// 카카오 SDK 타입을 엄격히 정의하지 않고 any 로 다룬다 (MVP)
/* eslint-disable @typescript-eslint/no-explicit-any */
declare global {
  interface Window {
    kakao: any;
  }
}

const KEY = process.env.NEXT_PUBLIC_KAKAO_MAP_KEY;

let loadPromise: Promise<any> | null = null;

export function isKakaoConfigured(): boolean {
  return !!KEY;
}

// SDK 를 1회만 로드하고 window.kakao 를 반환
export function loadKakao(): Promise<any> {
  if (typeof window === "undefined") return Promise.reject(new Error("no window"));
  if (window.kakao && window.kakao.maps) return Promise.resolve(window.kakao);
  if (loadPromise) return loadPromise;

  loadPromise = new Promise((resolve, reject) => {
    if (!KEY) {
      reject(new Error("NEXT_PUBLIC_KAKAO_MAP_KEY 가 설정되지 않았습니다."));
      return;
    }
    const existing = document.getElementById("kakao-sdk") as HTMLScriptElement | null;
    const onReady = () => window.kakao.maps.load(() => resolve(window.kakao));
    if (existing) {
      if (window.kakao && window.kakao.maps) resolve(window.kakao);
      else existing.addEventListener("load", onReady);
      return;
    }
    const script = document.createElement("script");
    script.id = "kakao-sdk";
    script.async = true;
    script.src = `https://dapi.kakao.com/v2/maps/sdk.js?appkey=${KEY}&libraries=services&autoload=false`;
    script.addEventListener("load", onReady);
    script.addEventListener("error", () => reject(new Error("카카오 SDK 로드 실패")));
    document.head.appendChild(script);
  });
  return loadPromise;
}

// 카카오 로컬 키워드 검색 → KakaoPlace[]
// 1) 지도 중심 주변(반경)을 우선 검색하고
// 2) 주변 결과가 없으면 전국 범위로 다시 검색한다.
//    (서울에서 '거제시 식당'을 검색하는 등 먼 지역도 찾을 수 있도록)
export async function searchPlaces(
  keyword: string,
  center?: { lat: number; lng: number }
): Promise<KakaoPlace[]> {
  const q = keyword.trim();
  if (!q) return [];
  const kakao = await loadKakao();

  if (center) {
    const near = await runKeywordSearch(kakao, q, center);
    if (near.length > 0) return near;
  }
  return runKeywordSearch(kakao, q);
}

function runKeywordSearch(
  kakao: any,
  q: string,
  center?: { lat: number; lng: number }
): Promise<KakaoPlace[]> {
  return new Promise((resolve, reject) => {
    const places = new kakao.maps.services.Places();
    const options: any = { size: 15 };
    if (center) {
      options.location = new kakao.maps.LatLng(center.lat, center.lng);
      options.radius = 20000;
      options.sort = kakao.maps.services.SortBy.DISTANCE;
    }
    places.keywordSearch(
      q,
      (data: any[], status: string) => {
        if (status === kakao.maps.services.Status.OK) {
          resolve(data.map(mapKakaoResult));
        } else if (status === kakao.maps.services.Status.ZERO_RESULT) {
          resolve([]);
        } else {
          reject(new Error("검색에 실패했어요."));
        }
      },
      options
    );
  });
}

function mapKakaoResult(d: any): KakaoPlace {
  return {
    kakaoId: String(d.id),
    name: d.place_name ?? "",
    category: d.category_name ?? "",
    address: d.address_name ?? "",
    roadAddress: d.road_address_name ?? "",
    phone: d.phone ?? "",
    placeUrl: d.place_url ?? "",
    lat: parseFloat(d.y),
    lng: parseFloat(d.x),
  };
}
