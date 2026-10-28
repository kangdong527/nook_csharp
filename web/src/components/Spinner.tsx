export function Spinner({ className = "" }: { className?: string }) {
  return (
    <span
      className={`inline-block animate-spin rounded-full border-2 border-white/40 border-t-white ${className}`}
      style={{ width: "1em", height: "1em" }}
      aria-label="loading"
    />
  );
}

export function FullSpinner() {
  return (
    <div className="flex flex-1 items-center justify-center py-20">
      <span className="inline-block h-7 w-7 animate-spin rounded-full border-[3px] border-line border-t-key" />
    </div>
  );
}
