import { cn } from "@/lib/utils";

type MetricCardProps = {
  label: string;
  value: string | number;
  suffix?: string;
  trend?: string;
  className?: string;
};

export function MetricCard({ label, value, suffix, trend, className }: MetricCardProps) {
  return (
    <div
      className={cn(
        "rounded-[var(--radius)] border border-border bg-card p-5 flex flex-col gap-2",
        className
      )}
    >
      <span className="text-xs uppercase tracking-wide text-muted-foreground">
        {label}
      </span>
      <div className="flex items-baseline gap-1.5">
        <span className="text-3xl font-mono text-foreground">{value}</span>
        {suffix && (
          <span className="text-sm text-muted-foreground">{suffix}</span>
        )}
      </div>
      {trend && (
        <span className="text-xs text-muted-foreground">{trend}</span>
      )}
    </div>
  );
}
