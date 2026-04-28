"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import * as signalR from "@microsoft/signalr"
import { AppSidebar } from "@/components/app-sidebar"
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb"
import { Separator } from "@/components/ui/separator"
import {
  SidebarInset,
  SidebarProvider,
  SidebarTrigger,
} from "@/components/ui/sidebar"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Monitor, User } from "lucide-react"
import {
  PieChart,
  Pie,
  Cell,
  Tooltip,
  Legend,
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
} from "recharts"

interface ActiveUser {
  id: string
  name: string
  status: "Activo" | "Inactivo" | "Ausente"
  activity: number | null
  currentApp: string | null
}

interface DashboardStats {
  activeUsersPercent: number
  inactivityPercent: number
  screensOnline: number
}

interface ChartData {
  activityByStatus: { name: string; value: number; color: string }[]
  topApps: { name: string; value: number; color: string }[]
}

interface DashboardPageProps {
  initialStats?: DashboardStats
  initialWorkers?: ActiveUser[]
  initialCharts?: ChartData
}

interface BackendWorker { id: number; name: string; is_active: boolean }
interface BackendSession { id: number; worker_id: number; ended_at?: string | null }
interface BackendActivity {
  id: number
  session_id: number
  is_foreground: boolean
  workSession?: { worker_id: number }
  application?: { display_name?: string; process_name: string }
}
interface BackendPeriod {
  id: number
  session_id: number
  status: string
  period_end?: string | null
  workSession?: { worker_id: number }
}
interface BackendAppActivity {
  id: number
  session_id: number
  application?: { display_name?: string; process_name?: string }
}

// ── Colores ──────────────────────────────────────────────────────────────────
const PIE_COLORS = { Activos: "#4ade80", Inactivos: "#64748b" }
const BAR_PALETTE = ["#4ade80", "#60a5fa", "#f472b6", "#fb923c", "#a78bfa", "#34d399"]
const TOOLTIP_STYLE = {
  contentStyle: {
    background: "#1e293b",
    border: "none",
    borderRadius: 8,
    color: "#f1f5f9",
    fontSize: 13,
  },
}

function MetricCard({
  title,
  value,
  icon,
  colorClass,
}: {
  title: string
  value: string | number
  icon: React.ReactNode
  colorClass: string
}) {
  return (
    <Card className={`${colorClass} border-0`}>
      <CardContent className="flex items-center justify-between p-6">
        <div>
          <p className="text-sm font-medium text-gray-300">{title}</p>
          <p className="text-4xl font-bold mt-1 text-white">{value}</p>
        </div>
        <div className="text-gray-400 [&_svg]:size-12">{icon}</div>
      </CardContent>
    </Card>
  )
}

function StatusBadge({ status }: { status: ActiveUser["status"] }) {
  const colors: Record<ActiveUser["status"], string> = {
    Activo: "text-gray-200",
    Inactivo: "text-gray-400",
    Ausente: "text-gray-500",
  }
  return <span className={`font-medium ${colors[status]}`}>{status}</span>
}

// ── Chart: Actividad en tiempo real (Donut activos/inactivos) ─────────────────
function ActivityDonut({ periods }: { periods: BackendPeriod[] }) {
  const open = periods.filter((p) => !p.period_end)
  const active = open.filter((p) => p.status === "active").length
  const inactive = open.filter((p) => p.status !== "active").length
  const data = [
    { name: "Activos", value: active },
    { name: "Inactivos", value: inactive },
  ]
  const total = active + inactive

  if (total === 0) {
    return (
      <div className="flex items-center justify-center h-40 text-gray-500 text-sm">
        <span className="italic opacity-50">Sin sesiones activas</span>
      </div>
    )
  }

  const RADIAN = Math.PI / 180
  const renderLabel = ({ cx, cy, midAngle, innerRadius, outerRadius, percent }: any) => {
    if (percent < 0.05) return null
    const r = innerRadius + (outerRadius - innerRadius) * 0.5
    const x = cx + r * Math.cos(-midAngle * RADIAN)
    const y = cy + r * Math.sin(-midAngle * RADIAN)
    return (
      <text x={x} y={y} fill="white" textAnchor="middle" dominantBaseline="central" fontSize={13} fontWeight={500}>
        {`${(percent * 100).toFixed(0)}%`}
      </text>
    )
  }

  return (
    <ResponsiveContainer width="100%" height={200}>
      <PieChart>
        <Pie
          data={data}
          cx="50%"
          cy="50%"
          innerRadius={50}
          outerRadius={75}
          paddingAngle={2}
          dataKey="value"
          labelLine={false}
          label={renderLabel}
        >
          {data.map((entry) => (
            <Cell key={entry.name} fill={PIE_COLORS[entry.name as keyof typeof PIE_COLORS]} />
          ))}
        </Pie>
        <Tooltip
          {...TOOLTIP_STYLE}
          formatter={(v: number, name: string) => [`${v} trabajadores`, name]}
        />
        <Legend
          formatter={(value, entry: any) => (
            <span style={{ color: "#94a3b8", fontSize: 12 }}>
              {value}: <strong style={{ color: "#f1f5f9" }}>{entry.payload.value}</strong>
            </span>
          )}
        />
      </PieChart>
    </ResponsiveContainer>
  )
}

// ── Chart: Apps más usadas (barras horizontales) ──────────────────────────────
const CHART_TYPES = ["Vertical", "Horizontal", "Donut"] as const
type ChartType = typeof CHART_TYPES[number]

function TopAppsChart({ appActivities }: { appActivities: BackendAppActivity[] }) {
  const [chartType, setChartType] = useState<ChartType>("Horizontal")

  // Agrupamos por display_name y contamos
  const counts: Record<string, number> = {}
  for (const act of appActivities) {
    const name =
      act.application?.display_name ||
      act.application?.process_name ||
      "Desconocida"
    counts[name] = (counts[name] || 0) + 1
  }
  const data = Object.entries(counts)
    .map(([name, value]) => ({
      name: name.length > 16 ? name.slice(0, 14) + "…" : name,
      value,
    }))
    .sort((a, b) => b.value - a.value)
    .slice(0, 6)

  if (data.length === 0) {
    return (
      <div className="flex items-center justify-center h-40 text-gray-500 text-sm">
        <span className="italic opacity-50">Sin actividad registrada</span>
      </div>
    )
  }

  const axisStyle = { fill: "#64748b", fontSize: 11 }
  const hBarHeight = Math.max(data.length * 40 + 60, 160)

  return (
    <div>
      {/* Selector de tipo */}
      <div className="flex gap-1 mb-3 flex-wrap">
        {CHART_TYPES.map((t) => (
          <button
            key={t}
            onClick={() => setChartType(t)}
            style={{
              padding: "3px 10px",
              borderRadius: 6,
              border: `1px solid ${chartType === t ? "#4ade80" : "#334155"}`,
              background: chartType === t ? "#4ade80" : "transparent",
              color: chartType === t ? "#0f172a" : "#94a3b8",
              fontSize: 11,
              fontWeight: chartType === t ? 600 : 400,
              cursor: "pointer",
            }}
          >
            {t}
          </button>
        ))}
      </div>

      {chartType === "Vertical" && (
        <ResponsiveContainer width="100%" height={180}>
          <BarChart data={data} margin={{ top: 4, right: 4, left: -20, bottom: 0 }}>
            <XAxis dataKey="name" tick={axisStyle} />
            <YAxis tick={axisStyle} allowDecimals={false} />
            <Tooltip {...TOOLTIP_STYLE} formatter={(v: number) => [`${v} usos`, "Actividad"]} />
            <Bar dataKey="value" radius={[4, 4, 0, 0]}>
              {data.map((_, i) => (
                <Cell key={i} fill={BAR_PALETTE[i % BAR_PALETTE.length]} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      )}

      {chartType === "Horizontal" && (
        <ResponsiveContainer width="100%" height={hBarHeight}>
          <BarChart data={data} layout="vertical" margin={{ top: 4, right: 8, left: 4, bottom: 4 }}>
            <XAxis type="number" tick={axisStyle} allowDecimals={false} />
            <YAxis type="category" dataKey="name" tick={axisStyle} width={100} />
            <Tooltip {...TOOLTIP_STYLE} formatter={(v: number) => [`${v} usos`, "Actividad"]} />
            <Bar dataKey="value" radius={[0, 4, 4, 0]}>
              {data.map((_, i) => (
                <Cell key={i} fill={BAR_PALETTE[i % BAR_PALETTE.length]} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      )}

      {chartType === "Donut" && (
        <ResponsiveContainer width="100%" height={200}>
          <PieChart>
            <Pie data={data} cx="50%" cy="50%" innerRadius={45} outerRadius={75} paddingAngle={2} dataKey="value">
              {data.map((_, i) => (
                <Cell key={i} fill={BAR_PALETTE[i % BAR_PALETTE.length]} />
              ))}
            </Pie>
            <Tooltip
              {...TOOLTIP_STYLE}
              formatter={(v: number, name: string) => [`${v} usos`, name]}
            />
            <Legend
              formatter={(value) => (
                <span style={{ color: "#94a3b8", fontSize: 11 }}>{value}</span>
              )}
            />
          </PieChart>
        </ResponsiveContainer>
      )}
    </div>
  )
}

// ── Página principal ──────────────────────────────────────────────────────────
export default function Home({
  initialStats,
  initialWorkers,
  initialCharts,
}: DashboardPageProps) {
  const router = useRouter()
  const [stats, setStats] = useState<DashboardStats | undefined>(initialStats)
  const [workers, setWorkers] = useState<ActiveUser[]>(initialWorkers ?? [])
  const [periods, setPeriods] = useState<BackendPeriod[]>([])
  const [appActivities, setAppActivities] = useState<BackendAppActivity[]>([])

  useEffect(() => {
    if (!localStorage.getItem("token")) {
      router.replace("/login")
    }
  }, [router])

  // Carga inicial: workers + sesiones + periodos + actividades
  useEffect(() => {
    const token = localStorage.getItem("token")
    if (!token) return
    const headers = { Authorization: `Bearer ${token}` }
    const apiUrl = process.env.NEXT_PUBLIC_API_URL

    Promise.all([
      fetch(`${apiUrl}/api/workers`, { headers }).then((r) => r.json()),
      fetch(`${apiUrl}/api/worksessions`, { headers }).then((r) => r.json()),
      fetch(`${apiUrl}/api/activityperiods`, { headers }).then((r) => r.json()),
      fetch(`${apiUrl}/api/appactivity`, { headers }).then((r) => r.json()),
    ])
      .then(([workersData, sessionsData, periodsData, appData]: [
        BackendWorker[],
        BackendSession[],
        BackendPeriod[],
        BackendAppActivity[],
      ]) => {
        const activeIds = new Set(
          sessionsData.filter((s) => !s.ended_at).map((s) => s.worker_id)
        )
        setWorkers(
          workersData.map((w) => ({
            id: w.id.toString(),
            name: w.name,
            status: activeIds.has(w.id) ? "Activo" : "Inactivo",
            activity: null,
            currentApp: null,
          }))
        )
        setPeriods(periodsData)
        setAppActivities(appData)
      })
      .catch((err) => console.error("Error cargando datos iniciales:", err))
  }, [])

  // SignalR: actualizaciones en tiempo real
  useEffect(() => {
    const token = localStorage.getItem("token")
    if (!token) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${process.env.NEXT_PUBLIC_API_URL}/hubs/monitoring`, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.on("SesionAbierta", (session: BackendSession) => {
      setWorkers((prev) =>
        prev.map((w) =>
          w.id === session.worker_id.toString() ? { ...w, status: "Activo" } : w
        )
      )
    })

    connection.on("SesionCerrada", (session: BackendSession) => {
      setWorkers((prev) =>
        prev.map((w) =>
          w.id === session.worker_id.toString()
            ? { ...w, status: "Inactivo", currentApp: null }
            : w
        )
      )
    })

    connection.on("NuevaActividad", (activity: BackendActivity) => {
      if (!activity.is_foreground || !activity.workSession) return
      const appName =
        activity.application?.display_name ??
        activity.application?.process_name ??
        null
      setWorkers((prev) =>
        prev.map((w) =>
          w.id === activity.workSession!.worker_id.toString()
            ? { ...w, currentApp: appName }
            : w
        )
      )
      // Añadimos la nueva actividad al estado para que el chart se actualice
      setAppActivities((prev) => [...prev, activity as BackendAppActivity])
    })

    connection.on("NuevoPeriodo", (period: BackendPeriod) => {
      // Actualizamos el estado de periodos para que el donut se refresque
      setPeriods((prev) => {
        const exists = prev.find((p) => p.id === period.id)
        if (exists) return prev.map((p) => (p.id === period.id ? period : p))
        return [...prev, period]
      })
    })

    connection.on("WorkerConnected", () => {})
    connection.on("WorkerDisconnected", () => {})

    connection
      .start()
      .catch((err) => console.error("SignalR error:", err))

    return () => { connection.stop() }
  }, [])

  return (
    <SidebarProvider style={{ fontFamily: "Verdana, Geneva, Tahoma, sans-serif" }}>
      <AppSidebar />
      <SidebarInset className="bg-gray-950">
        <header className="flex h-16 shrink-0 items-center gap-2 border-b border-gray-800 px-4 bg-gray-900">
          <SidebarTrigger className="-ml-1 text-gray-300 hover:text-white hover:bg-gray-800" />
          <Separator
            orientation="vertical"
            className="mr-2 data-[orientation=vertical]:h-4 bg-gray-700"
          />
          <Breadcrumb>
            <BreadcrumbList>
              <BreadcrumbItem className="hidden md:block">
                <BreadcrumbLink href="#" className="text-gray-400 hover:text-white">
                  Sett
                </BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbSeparator className="hidden md:block text-gray-600" />
              <BreadcrumbItem>
                <BreadcrumbPage className="text-white">Admin's Dashboard</BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
        </header>

        <div className="flex flex-1 flex-col gap-6 p-6">
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <MetricCard
              title="Usuarios activos"
              value={stats ? `${stats.activeUsersPercent}%` : "—"}
              icon={<User />}
              colorClass="bg-gray-700"
            />
            <MetricCard
              title="Inactividad"
              value={stats ? `${stats.inactivityPercent}%` : "—"}
              icon={<Monitor />}
              colorClass="bg-gray-600"
            />
            <MetricCard
              title="Pantallas"
              value={stats?.screensOnline ?? "—"}
              icon={<Monitor />}
              colorClass="bg-gray-500"
            />
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 flex-1">
            <Card className="lg:col-span-2 bg-gray-800 border-gray-700">
              <CardHeader>
                <CardTitle className="text-base text-white">Trabajadores activos</CardTitle>
              </CardHeader>
              <CardContent>
                <Table>
                  <TableHeader>
                    <TableRow className="border-gray-700 hover:bg-gray-750">
                      <TableHead className="text-gray-400">Usuarios</TableHead>
                      <TableHead className="text-gray-400">Estado</TableHead>
                      <TableHead className="text-gray-400">APP actual</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {workers.length > 0 ? (
                      workers.map((worker) => (
                        <TableRow key={worker.id} className="border-gray-700 hover:bg-gray-750">
                          <TableCell className="font-medium text-white">{worker.name}</TableCell>
                          <TableCell><StatusBadge status={worker.status} /></TableCell>
                          <TableCell className="text-gray-300">{worker.currentApp ?? "—"}</TableCell>
                        </TableRow>
                      ))
                    ) : (
                      Array.from({ length: 5 }).map((_, i) => (
                        <TableRow key={i} className="border-gray-700">
                          <TableCell className="text-gray-600">—</TableCell>
                          <TableCell /><TableCell />
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>

            <div className="flex flex-col gap-4">
              {/* ── Actividad en tiempo real ── */}
              <Card className="flex-1 bg-gray-800 border-gray-700">
                <CardHeader>
                  <CardTitle className="text-base text-white">Actividad en tiempo real</CardTitle>
                </CardHeader>
                <CardContent>
                  <ActivityDonut periods={periods} />
                </CardContent>
              </Card>

              {/* ── Apps más usadas ── */}
              <Card className="flex-1 bg-gray-800 border-gray-700">
                <CardHeader>
                  <CardTitle className="text-base text-white">Apps más usadas</CardTitle>
                </CardHeader>
                <CardContent>
                  <TopAppsChart appActivities={appActivities} />
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </SidebarInset>
    </SidebarProvider>
  )
}