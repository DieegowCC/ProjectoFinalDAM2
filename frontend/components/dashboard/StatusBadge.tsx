import { cn } from "@/lib/utils";

export type WorkerStatus = "Activo" | "Ausente" | "Inactivo";

type StatusBadgeProps = {
  status: WorkerStatus;
  className?: string;
};

const styles: Record<WorkerStatus, { container: string; dot: string }> = {
  Activo: {
    container: "bg-[#6EC531]/10 text-[#6EC531]",
    dot: "bg-[#6EC531] animate-pulse",
  },
  Ausente: {
    container: "bg-[#f59e0b]/10 text-[#f59e0b]",
    dot: "bg-[#f59e0b]",
  },
  Inactivo: {
    container: "bg-[#71717a]/10 text-[#a1a1aa]",
    dot: "bg-[#71717a]",
  },
};

export function StatusBadge({ status, className }: StatusBadgeProps) {
  const style = styles[status];
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium",
        style.container,
        className
      )}
    >
      <span className={cn("size-1.5 rounded-full", style.dot)} />
      {status}
    </span>
  );
}
