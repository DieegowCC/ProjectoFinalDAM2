"use client";

// Panel lateral con el detalle de un worker concreto.
// Se abre cuando pulsas una fila de la tabla del dashboard.
// Pide GET /api/stats/worker/{id} y pinta sus stats agregadas, top apps y
// últimas sesiones.

import { useEffect, useState } from "react";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { StatusBadge, WorkerStatus } from "./StatusBadge";
import { TopAppDto } from "./TopAppsDonutChart";

type SessionSummary = {
  id: number;
  started_at: string;
  ended_at: string | null;
  total_minutes: number | null;
};

export type WorkerDetail = {
  id: number;
  name: string;
  email: string;
  hostname: string;
  department: string | null;
  is_active: boolean;
  created_at: string;
  totalSessions: number;
  totalMinutesAllTime: number;
  productiveMinutesToday: number;
  lastSeen: string | null;
  status: WorkerStatus;
  topApps: TopAppDto[];
  recentSessions: SessionSummary[];
};

type WorkerDetailSheetProps = {
  workerId: number | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

function formatMinutes(minutes: number): string {
  if (minutes <= 0) return "0m";
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  if (h === 0) return `${m}m`;
  if (m === 0) return `${h}h`;
  return `${h}h ${m}m`;
}

function formatDate(iso: string | null): string {
  if (!iso) return "—";
  try {
    return new Date(iso).toLocaleString();
  } catch {
    return "—";
  }
}

export function WorkerDetailSheet({
  workerId,
  open,
  onOpenChange,
}: WorkerDetailSheetProps) {
  const [data, setData] = useState<WorkerDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open || !workerId) {
      setData(null);
      return;
    }
    const token = localStorage.getItem("token");
    if (!token) return;

    setLoading(true);
    setError(null);
    fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/stats/worker/${workerId}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
      .then((r) => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`);
        return r.json();
      })
      .then((d: WorkerDetail) => setData(d))
      .catch((e) => setError(String(e)))
      .finally(() => setLoading(false));
  }, [workerId, open]);

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="bg-card overflow-y-auto sm:max-w-lg">
        <SheetHeader>
          <SheetTitle className="text-foreground">
            {data?.name ?? "Detalle del worker"}
          </SheetTitle>
          <SheetDescription>
            {data?.department ?? "Sin departamento"} · {data?.hostname ?? ""}
          </SheetDescription>
        </SheetHeader>

        <div className="px-4 pb-6 flex flex-col gap-5">
          {loading && (
            <p className="text-sm text-muted-foreground">Cargando…</p>
          )}
          {error && <p className="text-sm text-destructive">{error}</p>}

          {data && (
            <>
              <div className="flex items-center gap-3">
                <StatusBadge status={data.status} />
                <span className="text-xs text-muted-foreground">
                  {data.email}
                </span>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="rounded-md border border-border bg-muted p-3">
                  <span className="text-xs uppercase text-muted-foreground tracking-wide">
                    Sesiones
                  </span>
                  <p className="text-xl font-mono text-foreground mt-1">
                    {data.totalSessions}
                  </p>
                </div>
                <div className="rounded-md border border-border bg-muted p-3">
                  <span className="text-xs uppercase text-muted-foreground tracking-wide">
                    Tiempo total
                  </span>
                  <p className="text-xl font-mono text-foreground mt-1">
                    {formatMinutes(data.totalMinutesAllTime)}
                  </p>
                </div>
                <div className="rounded-md border border-border bg-muted p-3">
                  <span className="text-xs uppercase text-muted-foreground tracking-wide">
                    Productivo hoy
                  </span>
                  <p className="text-xl font-mono text-foreground mt-1">
                    {formatMinutes(data.productiveMinutesToday)}
                  </p>
                </div>
                <div className="rounded-md border border-border bg-muted p-3">
                  <span className="text-xs uppercase text-muted-foreground tracking-wide">
                    Última actividad
                  </span>
                  <p className="text-xs font-mono text-foreground mt-1 break-all">
                    {formatDate(data.lastSeen)}
                  </p>
                </div>
              </div>

              <div>
                <h3 className="text-sm font-medium text-foreground mb-2">
                  Top apps
                </h3>
                {data.topApps.length === 0 ? (
                  <p className="text-xs text-muted-foreground">
                    Sin apps registradas.
                  </p>
                ) : (
                  <ul className="flex flex-col gap-1">
                    {data.topApps.map((app, i) => (
                      <li
                        key={app.name}
                        className="flex items-center justify-between text-sm py-1.5 border-b border-border last:border-0"
                      >
                        <span className="flex items-center gap-2 text-foreground">
                          <span className="text-xs font-mono text-muted-foreground w-5">
                            {i + 1}.
                          </span>
                          <span className="truncate max-w-[18rem]">
                            {app.name}
                          </span>
                        </span>
                        <span className="font-mono text-muted-foreground">
                          {formatMinutes(app.minutes)}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>

              <div>
                <h3 className="text-sm font-medium text-foreground mb-2">
                  Últimas sesiones
                </h3>
                {data.recentSessions.length === 0 ? (
                  <p className="text-xs text-muted-foreground">
                    Sin sesiones registradas.
                  </p>
                ) : (
                  <ul className="flex flex-col gap-1">
                    {data.recentSessions.map((s) => (
                      <li
                        key={s.id}
                        className="flex items-center justify-between text-xs py-1.5 border-b border-border last:border-0"
                      >
                        <span className="font-mono text-foreground">
                          {formatDate(s.started_at)}
                        </span>
                        <span className="font-mono text-muted-foreground">
                          {s.ended_at
                            ? `${formatMinutes(s.total_minutes ?? 0)}`
                            : "en curso"}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </>
          )}
        </div>
      </SheetContent>
    </Sheet>
  );
}
