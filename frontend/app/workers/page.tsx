"use client";

// Página de gestión de workers (CRUD).
//
// Tira de dos endpoints:
//   - GET /api/workers           → lista completa (activos e inactivos)
//   - GET /api/stats/active-workers → estado en vivo de cada uno (Activo/Ausente/Inactivo)
//
// y permite crear / editar / desactivar / reactivar mediante:
//   - POST   /api/workers
//   - PUT    /api/workers/{id}
//   - DELETE /api/workers/{id}   (baja lógica: solo pone is_active = false)

import { useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { AppSidebar } from "@/components/app-sidebar";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { Separator } from "@/components/ui/separator";
import {
  SidebarInset,
  SidebarProvider,
  SidebarTrigger,
} from "@/components/ui/sidebar";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Pencil, Plus, Search, Trash2, RotateCcw, Download } from "lucide-react";
import { ThemeToggle } from "@/components/dashboard/theme-toggle";
import {
  WorkerFormSheet,
  WorkerFormData,
} from "@/components/workers/WorkerFormSheet";

type Worker = {
  id: number;
  name: string;
  email: string;
  hostname: string;
  department: string | null;
  is_active: boolean;
  created_at: string;
};

type LiveStatus = "Activo" | "Ausente" | "Inactivo";

// Forma de cada fila que devuelve GET /api/report
type ReportRow = {
  worker_name:    string;
  department:     string;
  hostname:       string;
  session_start:  string;
  session_end:    string | null;
  active_minutes: number;
  idle_minutes:   number;
  top_app:        string;
};

// Fecha de hoy en formato YYYY-MM-DD (valor inicial de los date pickers).
// Está fuera del componente para que no se recalcule en cada render.
const today = new Date().toISOString().split("T")[0];

// Escapa un campo para CSV: lo envuelve en comillas y duplica las comillas
// internas, por si el valor contiene comas o comillas.
function escaparCsv(valor: string): string {
  return `"${valor.replace(/"/g, '""')}"`;
}

export default function WorkersPage() {
  const router = useRouter();
  const [workers, setWorkers] = useState<Worker[]>([]);
  const [statuses, setStatuses] = useState<Record<number, LiveStatus>>({});
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<WorkerFormData | null>(null);

  // Estado del selector de rango de fechas para el informe
  const [reportFrom, setReportFrom] = useState(today);
  const [reportTo, setReportTo]     = useState(today);
  const [downloading, setDownloading] = useState(false);

  // Pide la lista de workers + sus estados en vivo en paralelo y combina los
  // resultados. Lo guardamos en dos estados separados (workers y statuses)
  // porque la lista cambia poco (cuando creas/editas) y los estados cambian
  // mucho (cada vez que un worker se conecta o se va idle).
  const fetchWorkers = useCallback(async () => {
    const token = localStorage.getItem("token");
    if (!token) {
      router.replace("/login");
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const opts = {
        headers: { Authorization: `Bearer ${token}` },
        cache: "no-store" as RequestCache,
      };
      const [workersRes, statusRes] = await Promise.all([
        fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/workers`, opts),
        fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/stats/active-workers`, opts),
      ]);
      if (workersRes.status === 401 || statusRes.status === 401) {
        localStorage.removeItem("token");
        router.replace("/login");
        return;
      }
      if (!workersRes.ok) throw new Error(`HTTP ${workersRes.status}`);
      const data: Worker[] = await workersRes.json();
      setWorkers(data);

      if (statusRes.ok) {
        const liveData: { id: number; status: LiveStatus }[] = await statusRes.json();
        const map: Record<number, LiveStatus> = {};
        liveData.forEach((w) => {
          map[w.id] = w.status;
        });
        setStatuses(map);
      }
    } catch (e) {
      setError(String(e));
    } finally {
      setLoading(false);
    }
  }, [router]);

  useEffect(() => {
    fetchWorkers();
  }, [fetchWorkers]);

  // Filtrado por la barra de búsqueda. useMemo evita recalcular en cada render
  // si no han cambiado ni la lista ni el texto buscado.
  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return workers;
    return workers.filter(
      (w) =>
        w.name.toLowerCase().includes(q) ||
        w.email.toLowerCase().includes(q) ||
        w.hostname.toLowerCase().includes(q) ||
        (w.department ?? "").toLowerCase().includes(q)
    );
  }, [workers, query]);

  const handleNew = () => {
    setEditing(null);
    setFormOpen(true);
  };

  const handleEdit = (w: Worker) => {
    setEditing({
      id: w.id,
      name: w.name,
      email: w.email,
      hostname: w.hostname,
      department: w.department,
      is_active: w.is_active,
    });
    setFormOpen(true);
  };

  const handleDelete = async (w: Worker) => {
    if (!confirm(`¿Desactivar a ${w.name}? (baja lógica, se puede revertir)`))
      return;
    try {
      const token = localStorage.getItem("token");
      await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/api/workers/${w.id}`,
        {
          method: "DELETE",
          headers: { Authorization: `Bearer ${token}` },
        }
      );
      fetchWorkers();
    } catch (e) {
      setError(String(e));
    }
  };

  // Llama al endpoint de informe y descarga el resultado como CSV
  const handleDownloadReport = async () => {
    const token = localStorage.getItem("token");
    if (!token) return;

    setDownloading(true);
    try {
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/api/report?from=${reportFrom}&to=${reportTo}`,
        { headers: { Authorization: `Bearer ${token}` } }
      );

      if (!res.ok) throw new Error(`HTTP ${res.status}`);

      const filas: ReportRow[] = await res.json();

      // Construimos el CSV: cabecera + una línea por sesión
      const cabecera = "Worker,Departamento,Hostname,Inicio sesión,Fin sesión,Minutos activos,Minutos inactivos,App principal";
      const lineas = filas.map((f) => {
        const inicio = new Date(f.session_start).toLocaleString("es-ES");
        const fin    = f.session_end ? new Date(f.session_end).toLocaleString("es-ES") : "En curso";
        return [
          escaparCsv(f.worker_name),
          escaparCsv(f.department),
          escaparCsv(f.hostname),
          escaparCsv(inicio),
          escaparCsv(fin),
          f.active_minutes,
          f.idle_minutes,
          escaparCsv(f.top_app),
        ].join(",");
      });

      const csv  = [cabecera, ...lineas].join("\n");
      const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
      const url  = URL.createObjectURL(blob);

      const a    = document.createElement("a");
      a.href     = url;
      a.download = `informe-actividad-${reportFrom}-${reportTo}.csv`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e) {
      setError(String(e));
    } finally {
      setDownloading(false);
    }
  };

  const handleReactivate = async (w: Worker) => {
    try {
      const token = localStorage.getItem("token");
      await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/workers/${w.id}`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          name: w.name,
          email: w.email,
          hostname: w.hostname,
          department: w.department,
          is_active: true,
        }),
      });
      fetchWorkers();
    } catch (e) {
      setError(String(e));
    }
  };

  return (
    <SidebarProvider>
      <AppSidebar />
      <SidebarInset className="bg-background">
        <header className="flex h-14 shrink-0 items-center gap-2 border-b border-border px-4 bg-surface">
          <SidebarTrigger className="-ml-1" />
          <Separator
            orientation="vertical"
            className="mr-2 data-[orientation=vertical]:h-4"
          />
          <Breadcrumb>
            <BreadcrumbList>
              <BreadcrumbItem className="hidden md:block">
                <span className="text-muted-foreground text-sm">Sett</span>
              </BreadcrumbItem>
              <BreadcrumbSeparator className="hidden md:block" />
              <BreadcrumbItem>
                <BreadcrumbPage className="text-foreground text-sm">
                  Workers
                </BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
          <div className="ml-auto">
            <ThemeToggle />
          </div>
        </header>

        <div className="flex flex-1 flex-col gap-4 p-6">
          <div className="rounded-[var(--radius)] border border-border bg-card flex flex-col">
            <div className="px-5 py-4 border-b border-border flex items-center justify-between gap-4 flex-wrap">
              <div>
                <h2 className="text-sm font-medium text-foreground">
                  Gestión de workers
                </h2>
                <p className="text-xs text-muted-foreground mt-0.5">
                  {filtered.length} de {workers.length} workers ·{" "}
                  {workers.filter((w) => w.is_active).length} habilitados ·{" "}
                  {Object.values(statuses).filter((s) => s === "Activo").length} trabajando
                </p>
              </div>
              <div className="flex items-center gap-3">
                <div className="relative w-64 max-w-full">
                  <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 size-3.5 text-muted-foreground pointer-events-none" />
                  <Input
                    type="text"
                    placeholder="Buscar…"
                    value={query}
                    onChange={(e) => setQuery(e.target.value)}
                    className="pl-8 bg-muted border-border text-sm h-8"
                  />
                </div>
                <Button
                  size="sm"
                  onClick={handleNew}
                  className="bg-foreground text-background hover:bg-foreground/90"
                >
                  <Plus className="size-4" />
                  Nuevo worker
                </Button>
              </div>
            </div>

            {error && (
              <p className="px-5 py-3 text-sm text-destructive border-b border-border">
                {error}
              </p>
            )}

            <Table>
              <TableHeader>
                <TableRow className="hover:bg-transparent">
                  <TableHead className="text-xs uppercase tracking-wide text-muted-foreground px-5">
                    Nombre
                  </TableHead>
                  <TableHead className="text-xs uppercase tracking-wide text-muted-foreground">
                    Email
                  </TableHead>
                  <TableHead className="text-xs uppercase tracking-wide text-muted-foreground">
                    Departamento
                  </TableHead>
                  <TableHead className="text-xs uppercase tracking-wide text-muted-foreground">
                    Hostname
                  </TableHead>
                  <TableHead className="text-xs uppercase tracking-wide text-muted-foreground">
                    Estado
                  </TableHead>
                  <TableHead className="text-xs uppercase tracking-wide text-muted-foreground text-right px-5">
                    Acciones
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading && (
                  <TableRow>
                    <TableCell
                      colSpan={6}
                      className="text-center text-sm text-muted-foreground py-8"
                    >
                      Cargando…
                    </TableCell>
                  </TableRow>
                )}
                {!loading && filtered.length === 0 && (
                  <TableRow>
                    <TableCell
                      colSpan={6}
                      className="text-center text-sm text-muted-foreground py-8"
                    >
                      {workers.length === 0
                        ? "Aún no hay workers. Crea el primero con el botón “Nuevo worker”."
                        : "Sin resultados."}
                    </TableCell>
                  </TableRow>
                )}
                {!loading &&
                  filtered.map((w) => (
                    <TableRow key={w.id}>
                      <TableCell className="px-5 font-medium text-foreground">
                        {w.name}
                      </TableCell>
                      <TableCell className="text-muted-foreground">
                        {w.email}
                      </TableCell>
                      <TableCell className="text-muted-foreground">
                        {w.department ?? "—"}
                      </TableCell>
                      <TableCell className="font-mono text-xs text-muted-foreground">
                        {w.hostname}
                      </TableCell>
                      <TableCell>
                        {!w.is_active ? (
                          <span className="inline-flex items-center gap-1.5 text-xs text-muted-foreground">
                            <span className="size-1.5 rounded-full bg-muted-foreground" />
                            Desactivado
                          </span>
                        ) : statuses[w.id] === "Activo" ? (
                          <span className="inline-flex items-center gap-1.5 text-xs text-foreground">
                            <span className="size-1.5 rounded-full bg-[#6EC531] animate-pulse" />
                            Activo
                          </span>
                        ) : statuses[w.id] === "Ausente" ? (
                          <span className="inline-flex items-center gap-1.5 text-xs text-foreground">
                            <span className="size-1.5 rounded-full bg-[#f59e0b]" />
                            Ausente
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1.5 text-xs text-muted-foreground">
                            <span className="size-1.5 rounded-full bg-muted-foreground" />
                            Inactivo
                          </span>
                        )}
                      </TableCell>
                      <TableCell className="px-5">
                        <div className="flex items-center justify-end gap-1">
                          <Button
                            variant="ghost"
                            size="icon-sm"
                            onClick={() => handleEdit(w)}
                            aria-label="Editar"
                          >
                            <Pencil className="size-3.5" />
                          </Button>
                          {w.is_active ? (
                            <Button
                              variant="ghost"
                              size="icon-sm"
                              onClick={() => handleDelete(w)}
                              aria-label="Desactivar"
                              className="text-destructive hover:text-destructive"
                            >
                              <Trash2 className="size-3.5" />
                            </Button>
                          ) : (
                            <Button
                              variant="ghost"
                              size="icon-sm"
                              onClick={() => handleReactivate(w)}
                              aria-label="Reactivar"
                            >
                              <RotateCcw className="size-3.5" />
                            </Button>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
              </TableBody>
            </Table>
          </div>

          {/* Selector de rango de fechas para descargar el informe de actividad */}
          <div className="rounded-[var(--radius)] border border-border bg-card px-5 py-4">
            <div className="mb-3">
              <h2 className="text-sm font-medium text-foreground">Informe de actividad</h2>
              <p className="text-xs text-muted-foreground mt-0.5">
                Selecciona un rango de fechas para descargar el historial de sesiones en CSV.
              </p>
            </div>
            <div className="flex items-center gap-3 flex-wrap">
              <div className="flex items-center gap-2">
                <label className="text-xs text-muted-foreground whitespace-nowrap">Desde</label>
                <input
                  type="date"
                  value={reportFrom}
                  max={reportTo}
                  onChange={(e) => setReportFrom(e.target.value)}
                  className="h-8 rounded-md border border-border bg-muted px-2 text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-ring"
                />
              </div>
              <div className="flex items-center gap-2">
                <label className="text-xs text-muted-foreground whitespace-nowrap">Hasta</label>
                <input
                  type="date"
                  value={reportTo}
                  min={reportFrom}
                  onChange={(e) => setReportTo(e.target.value)}
                  className="h-8 rounded-md border border-border bg-muted px-2 text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-ring"
                />
              </div>
              <Button
                size="sm"
                onClick={handleDownloadReport}
                disabled={downloading}
                className="bg-foreground text-background hover:bg-foreground/90"
              >
                <Download className="size-4" />
                {downloading ? "Generando…" : "Descargar CSV"}
              </Button>
            </div>
          </div>
        </div>

        <WorkerFormSheet
          open={formOpen}
          onOpenChange={setFormOpen}
          initialData={editing}
          onSaved={fetchWorkers}
        />
      </SidebarInset>
    </SidebarProvider>
  );
}
