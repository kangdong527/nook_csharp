"use client";

import { createContext, useContext, useState } from "react";

type ToastItem = { id: number; message: string };

const ToastContext = createContext<(message: string) => void>(() => {});

export function useToast() {
  return useContext(ToastContext);
}

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [items, setItems] = useState<ToastItem[]>([]);

  const show = (message: string) => {
    const id = Date.now() + Math.random();
    setItems((prev) => [...prev, { id, message }]);
    window.setTimeout(() => {
      setItems((prev) => prev.filter((t) => t.id !== id));
    }, 2600);
  };

  return (
    <ToastContext.Provider value={show}>
      {children}
      <div className="pointer-events-none fixed inset-x-0 bottom-24 z-[100] mx-auto flex max-w-[420px] flex-col items-center gap-2 px-6">
        {items.map((t) => (
          <div
            key={t.id}
            className="animate-toast-in pointer-events-auto w-full rounded-2xl bg-key/95 px-4 py-3 text-center text-sm font-medium text-white shadow-card"
          >
            {t.message}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}
