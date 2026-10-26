import { createClient } from "@supabase/supabase-js";

const url = process.env.NEXT_PUBLIC_SUPABASE_URL;
const anonKey = process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY;

if (!url || !anonKey) {
  console.warn(
    "[supabase] NEXT_PUBLIC_SUPABASE_URL / NEXT_PUBLIC_SUPABASE_ANON_KEY 가 설정되지 않았습니다."
  );
}

// 빌드/프리렌더 시 env 미설정이어도 throw 되지 않도록 placeholder 사용
// (런타임에는 실제 NEXT_PUBLIC_* 값이 주입됨)
export const supabase = createClient(
  url || "https://placeholder.supabase.co",
  anonKey || "placeholder-anon-key",
  {
    auth: { persistSession: false, autoRefreshToken: false },
  }
);

export const STORAGE_BUCKET = "images";
