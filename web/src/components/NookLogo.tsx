export function NookLogo({ className = "" }: { className?: string }) {
  return (
    <img
      src="/onboarding/nook_logo.png?v=20260611-0132"
      alt="nook"
      width={3506}
      height={1218}
      className={`h-10 w-auto object-contain ${className}`}
    />
  );
}
