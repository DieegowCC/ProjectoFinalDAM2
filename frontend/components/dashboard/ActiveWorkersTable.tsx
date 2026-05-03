"use client";

import { useMemo, useState } from "react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Input } from "@/components/ui/input";
import { Search } from "lucide-react";
import { StatusBadge, WorkerStatus } from "./StatusBadge";

export type WorkerStatusDto = {
  id: number;
  name: string;
  hostname: string;
  department: string | null;
  status: WorkerStatus;
  currentApp: string | null;
  loginTime: string | null;
  timeConnectedMinutes: number;
  activePercent: number;
};

type ActiveWorkersTableProps = {
  workers: WorkerStatusDto[];
  onRowClick?: (id: number) => void;
};

function formatMinutes(minutes: number): string {
  if (minutes <= 0) return "0m";
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  if (h === 0) return `${m}m`;
  return `${h}h ${m}m`;
}

function formatLoginTime(iso: string | null): string {
  if (!iso) return "—";
  try {
    const d = new Date(iso);
    return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  } catch {
    return "—";
  }
}

export function ActiveWorkersTable({
  workers,
  onRowClick,
}: ActiveWorkersTableProps) {
  const [query, setQuery] = useState("");

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return workers;
    return workers.filter(
      (w) =>
        w.name.toLowerCase().includes(q) ||
        w.hostname.toLowerCase().includes(q) ||
        (w.currentApp ?? "").toLowerCase().includes(q) ||
        (w.department ?? "").toLowerCase().includes(q) ||
        w.status.toLowerCase().includes(q)
    );
  }, [workers, query]);

  return (
    <div className="rounded-[var(--radius)] border border-border bg-card flex flex-col flex-1 min-h-0 overflow-hidden">
      <div className="px-5 py-4 border-b border-border flex items-center justify-between gap-4 shrink-0">
        <div>
          <h2 className="text-sm font-medium text-foreground">Workers</h2>
          <p className="text-xs text-muted-foreground mt-0.5">
            Estado en tiempo real · {filtered.length} de {workers.length}
          </p>
        </div>
        <div className="relative w-64 max-w-full">
          <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 size-3.5 text-muted-foreground pointer-events-none" />
          <Input
            type="text"
            placeholder="Buscar nombre, app, dept…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            className="pl-8 bg-muted border-border text-sm h-8"
          />
        </div>
      </div>
      <div className="overflow-y-auto flex-1">
      <Table>
        <TableHeader>
          <TableRow className="hover:bg-transparent">
            <TableHead className="text-xs uppercase tracking-wide text-muted-foreground px-5">
              Nombre
            </TableHead>
            <TableHead className="text-xs uppercase tracking-wide text-muted-foreground">
              Estado
            </TableHead>
            <TableHead className="text-xs uppercase tracking-wide text-muted-foreground">
              App actual
            </TableHead>
            <TableHead className="text-xs uppercase tracking-wide text-muted-foreground">
              Tiempo
            </TableHead>
            <TableHead className="text-xs uppercase tracking-wide text-muted-foreground">
              Login
            </TableHead>
            <TableHead className="text-xs uppercase tracking-wide text-muted-foreground">
              % activo
            </TableHead>
            <TableHead className="text-xs uppercase tracking-wide text-muted-foreground">
              Departamento
            </TableHead>
            <TableHead className="text-xs uppercase tracking-wide text-muted-foreground px-5">
              Hostname
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {filtered.length === 0 && (
            <TableRow>
              <TableCell
                colSpan={8}
                className="text-center text-sm text-muted-foreground py-8"
              >
                {workers.length === 0
                  ? "No hay workers registrados."
                  : "Sin resultados para esa búsqueda."}
              </TableCell>
            </TableRow>
          )}
          {filtered.map((w) => (
            <TableRow
              key={w.id}
              onClick={() => onRowClick?.(w.id)}
              className={onRowClick ? "cursor-pointer" : undefined}
            >
              <TableCell className="px-5 font-medium text-foreground">
                {w.name}
              </TableCell>
              <TableCell>
                <StatusBadge status={w.status} />
              </TableCell>
              <TableCell className="text-muted-foreground">
                {w.currentApp ?? "—"}
              </TableCell>
              <TableCell className="font-mono text-foreground">
                {formatMinutes(w.timeConnectedMinutes)}
              </TableCell>
              <TableCell className="font-mono text-muted-foreground">
                {formatLoginTime(w.loginTime)}
              </TableCell>
              <TableCell className="font-mono text-foreground">
                {w.activePercent.toFixed(1)}%
              </TableCell>
              <TableCell className="text-muted-foreground text-sm">
                {w.department ?? "—"}
              </TableCell>
              <TableCell className="px-5 font-mono text-xs text-muted-foreground">
                {w.hostname}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      </div>
    </div>
  );
}
