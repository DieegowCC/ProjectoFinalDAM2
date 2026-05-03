"use client";

// Dashboard principal — muestra el estado en tiempo real de todos los workers.
//
// Flujo de datos:
//   1. Al montar la página: pedimos las 4 estadísticas a la API
//      (dashboard, active-workers, top-apps, hourly).
//   2. Abrimos una conexión SignalR con la API. Cuando el agente notifica un
//      cambio (sesión abierta, nueva actividad, periodo nuevo, sesión cerrada)
//      la API emite un evento por SignalR y nosotros volvemos a pedir las 4
//      stats. Para no recargar 4 veces seguidas si caen varios eventos en
//      ráfaga, aplicamos un debounce de 500 ms.
//   3. Si SignalR pierde la conexión, "withAutomaticReconnect()" la recupera
//      automáticamente — no hace falta polling manual.

import { useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import * as signalR from "@microsoft/signalr";
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
import { MetricCard } from "@/components/dashboard/MetricCard";
import {
  ActiveWorkersTable,
  WorkerStatusDto,
} from "@/components/dashboard/ActiveWorkersTable";
import { ActivityDonutChart } from "@/components/dashboard/ActivityDonutChart";
import {
  TopAppsDonutChart,
  TopAppDto,
} from "@/components/dashboard/TopAppsDonutChart";
import {
  HourlyActivityChart,
  HourlyDataPoint,
} from "@/components/dashboard/HourlyActivityChart";
import { WorkerDetailSheet } from "@/components/dashboard/WorkerDetailSheet";
import { ThemeToggle } from "@/components/dashboard/theme-toggle";

type DashboardStats = {
  activeWorkers: number;
  absentWorkers: number;
  inactiveWorkers: number;
  totalWorkers: number;
  productiveTimeMinutesToday: number;
  totalTimeMinutesToday: number;
  inactivityRatePercent: number;
  sessionsTodayCount: number;
  uniqueAppsTodayCount: number;
};

// Espera mínima entre dos refrescos consecutivos disparados por SignalR.
// Con esto, si llegan 5 eventos en 200 ms, solo hacemos 1 refetch al final.
const REFETCH_DEBOUNCE_MS = 500;

// Helpers de formato para mostrar tiempos en pantalla.
function formatHours(minutes: number): string {
  if (minutes <= 0) return "0";
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  if (h === 0) return `${m}m`;
  if (m === 0) return `${h}h`;
  return `${h}h ${m}m`;
}

function formatTime(d: Date | null): string {
  if (!d) return "—";
  return d.toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

export default function DashboardPage() {
  const router = useRouter();
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [workers, setWorkers] = useState<WorkerStatusDto[]>([]);
  const [topApps, setTopApps] = useState<TopAppDto[]>([]);
  const [hourly, setHourly] = useState<HourlyDataPoint[]>([]);
  const [lastUpdate, setLastUpdate] = useState<Date | null>(null);
  const [selectedWorkerId, setSelectedWorkerId] = useState<number | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);

  // fetchAll: pide las 4 stats en paralelo y actualiza el estado.
  // Si la API responde 401 (token caducado o inválido), redirigimos al login.
  const fetchAll = useCallback(async () => {
    const token = localStorage.getItem("token");
    if (!token) {
      router.replace("/login");
      return;
    }
    const headers = { Authorization: `Bearer ${token}` };
    const apiUrl = process.env.NEXT_PUBLIC_API_URL;

    try {
      const fetchOpts = { headers, cache: "no-store" as RequestCache };
      const [statsRes, workersRes, topAppsRes, hourlyRes] = await Promise.all([
        fetch(`${apiUrl}/api/stats/dashboard`, fetchOpts),
        fetch(`${apiUrl}/api/stats/active-workers`, fetchOpts),
        fetch(`${apiUrl}/api/stats/top-apps?limit=5`, fetchOpts),
        fetch(`${apiUrl}/api/stats/hourly`, fetchOpts),
      ]);

      const algunaSinAuth =
        statsRes.status === 401 ||
        workersRes.status === 401 ||
        topAppsRes.status === 401 ||
        hourlyRes.status === 401;

      if (algunaSinAuth) {
        localStorage.removeItem("token");
        router.replace("/login");
        return;
      }

      if (statsRes.ok) setStats(await statsRes.json());
      if (workersRes.ok) setWorkers(await workersRes.json());
      if (topAppsRes.ok) setTopApps(await topAppsRes.json());
      if (hourlyRes.ok) setHourly(await hourlyRes.json());
      setLastUpdate(new Date());
    } catch (err) {
      console.error("Error cargando stats:", err);
    }
  }, [router]);

  // Carga inicial: una sola vez al montar la página.
  useEffect(() => {
    if (!localStorage.getItem("token")) {
      router.replace("/login");
      return;
    }
    fetchAll();
  }, [fetchAll, router]);

  // Conexión SignalR + debounce del refetch.
  // El timer vive en una ref para que sobreviva entre renders y se pueda
  // limpiar tanto al recibir un nuevo evento como al desmontar.
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (!token) return;

    const triggerRefetch = () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
      debounceRef.current = setTimeout(fetchAll, REFETCH_DEBOUNCE_MS);
    };

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${process.env.NEXT_PUBLIC_API_URL}/hubs/monitoring`, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Eventos que la API emite cuando cambia algo relevante.
    connection.on("SesionAbierta", triggerRefetch);
    connection.on("SesionCerrada", triggerRefetch);
    connection.on("NuevaActividad", triggerRefetch);
    connection.on("NuevoPeriodo", triggerRefetch);
    // Estos dos son ruido (admins conectándose al hub); los registramos vacíos
    // para que SignalR no escupa warnings al recibirlos.
    connection.on("WorkerConnected", () => {});
    connection.on("WorkerDisconnected", () => {});

    connection.start().catch((err) => console.error("SignalR error:", err));

    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
      connection.stop();
    };
  }, [fetchAll]);

  // Etiquetas listas para mostrar en las MetricCards.
  const productiveLabel = stats
    ? formatHours(stats.productiveTimeMinutesToday)
    : "—";
  const inactivityLabel = stats
    ? `${stats.inactivityRatePercent.toFixed(1)}`
    : "—";
  const activeFraction = stats
    ? `${stats.activeWorkers}/${stats.totalWorkers}`
    : "—";
  const sessionsLabel = stats ? String(stats.sessionsTodayCount) : "—";
  const uniqueAppsLabel = stats ? String(stats.uniqueAppsTodayCount) : "—";

  const handleRowClick = (id: number) => {
    setSelectedWorkerId(id);
    setDetailOpen(true);
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
                  Dashboard
                </BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
          <div className="ml-auto flex items-center gap-3">
            <span className="text-xs font-mono text-muted-foreground hidden sm:flex items-center gap-1.5">
              <span className="size-1.5 rounded-full bg-[#6EC531] animate-pulse" />
              {lastUpdate ? `Actualizado ${formatTime(lastUpdate)}` : "Cargando…"}
            </span>
            <ThemeToggle />
          </div>
        </header>

        <div className="flex flex-1 flex-col gap-5 p-6">
          {/* Métricas globales */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <MetricCard
              label="Workers activos"
              value={activeFraction}
            />
            <MetricCard
              label="Tiempo productivo hoy"
              value={productiveLabel}
            />
            <MetricCard
              label="Inactividad"
              value={inactivityLabel}
              suffix="%"
            />
          </div>

          {/* Tabla + donuts */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 flex-1 min-h-0">
            <div className="lg:col-span-2 flex flex-col gap-4 min-h-0">
              {/* Métricas específicas de workers */}
              <div className="grid grid-cols-2 gap-4">
                <MetricCard label="Sesiones hoy" value={sessionsLabel} />
                <MetricCard label="Apps únicas hoy" value={uniqueAppsLabel} />
              </div>
              <ActiveWorkersTable
                workers={workers}
                onRowClick={handleRowClick}
              />
            </div>
            <div className="flex flex-col gap-4">
              <ActivityDonutChart
                active={stats?.activeWorkers ?? 0}
                absent={stats?.absentWorkers ?? 0}
                inactive={stats?.inactiveWorkers ?? 0}
              />
              <TopAppsDonutChart apps={topApps} />
            </div>
          </div>

          {/* Timeline horario */}
          <HourlyActivityChart data={hourly} />
        </div>

        <WorkerDetailSheet
          workerId={selectedWorkerId}
          open={detailOpen}
          onOpenChange={setDetailOpen}
        />
      </SidebarInset>
    </SidebarProvider>
  );
}
