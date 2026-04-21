"use client"

import { useEffect, useState } from "react"
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
import { Button } from "@/components/ui/button"
import { Monitor, User, LogIn } from "lucide-react"
import Link from "next/link"

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

function DonutChartPlaceholder({
  data,
}: {
  data?: { name: string; value: number; color: string }[]
}) {
  return (
    <div className="flex items-center justify-center h-40 text-gray-500 text-sm">
      <span className="italic opacity-50">Chart — conectar a datos</span>
    </div>
  )
}

export default function Home({
  initialStats,
  initialWorkers,
  initialCharts,
}: DashboardPageProps) {
  const router = useRouter()
  const [stats, setStats] = useState<DashboardStats | undefined>(initialStats)
  const [workers, setWorkers] = useState<ActiveUser[]>(initialWorkers ?? [])
  const [charts, setCharts] = useState<ChartData | undefined>(initialCharts)
  const [connected, setConnected] = useState(false)

  useEffect(() => {
    if (!localStorage.getItem('token')) {
      router.replace('/login')
    }
  }, [router])

  // Carga inicial: workers + sesiones activas via REST
  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) return
    const headers = { Authorization: `Bearer ${token}` }
    const apiUrl = process.env.NEXT_PUBLIC_API_URL
    Promise.all([
      fetch(`${apiUrl}/api/workers`, { headers }).then(r => r.json()),
      fetch(`${apiUrl}/api/worksessions`, { headers }).then(r => r.json()),
    ]).then(([workersData, sessionsData]: [BackendWorker[], BackendSession[]]) => {
      const activeIds = new Set(sessionsData.filter(s => !s.ended_at).map(s => s.worker_id))
      setWorkers(workersData.map(w => ({
        id: w.id.toString(),
        name: w.name,
        status: activeIds.has(w.id) ? "Activo" as const : "Inactivo" as const,
        activity: null,
        currentApp: null,
      })))
    }).catch(err => console.error('Error cargando datos iniciales:', err))
  }, [])

  // SignalR: actualizaciones en tiempo real
  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${process.env.NEXT_PUBLIC_API_URL}/hubs/monitoring`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.on("SesionAbierta", (session: BackendSession) => {
      setWorkers(prev => prev.map(w =>
        w.id === session.worker_id.toString() ? { ...w, status: "Activo" as const } : w
      ))
    })

    connection.on("SesionCerrada", (session: BackendSession) => {
      setWorkers(prev => prev.map(w =>
        w.id === session.worker_id.toString() ? { ...w, status: "Inactivo" as const, currentApp: null } : w
      ))
    })

    connection.on("NuevaActividad", (activity: BackendActivity) => {
      if (!activity.is_foreground || !activity.workSession) return
      const appName = activity.application?.display_name ?? activity.application?.process_name ?? null
      setWorkers(prev => prev.map(w =>
        w.id === activity.workSession!.worker_id.toString() ? { ...w, currentApp: appName } : w
      ))
    })

    connection
      .start()
      .then(() => setConnected(true))
      .catch(err => console.error('SignalR error:', err))

    return () => { connection.stop() }
  }, [])

  return (
    <SidebarProvider
      style={{ fontFamily: "Verdana, Geneva, Tahoma, sans-serif" }}
    >
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
                <BreadcrumbPage className="text-white">
                  Admin's Dashboard
                </BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>

          <div className="ml-auto flex items-center gap-3">
            <Button
              asChild
              variant="outline"
              size="sm"
              className="border-gray-600 text-white hover:bg-gray-700 hover:text-white bg-transparent"
            >
              <Link href="/login">
                <LogIn className="size-4" />
                Login
              </Link>
            </Button>
          </div>
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
                <CardTitle className="text-base text-white">
                  Trabajadores activos
                </CardTitle>
              </CardHeader>
              <CardContent>
                <Table>
                  <TableHeader>
                    <TableRow className="border-gray-700 hover:bg-gray-750">
                      <TableHead className="text-gray-400">Usuarios</TableHead>
                      <TableHead className="text-gray-400">Estado</TableHead>
                      <TableHead className="text-gray-400">Actividad</TableHead>
                      <TableHead className="text-gray-400">APP actual</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {workers.length > 0 ? (
                      workers.map((worker) => (
                        <TableRow key={worker.id} className="border-gray-700 hover:bg-gray-750">
                          <TableCell className="font-medium text-white">{worker.name}</TableCell>
                          <TableCell><StatusBadge status={worker.status} /></TableCell>
                          <TableCell className="text-gray-300">
                            {worker.activity !== null ? `${worker.activity}%` : "—"}
                          </TableCell>
                          <TableCell className="text-gray-300">{worker.currentApp ?? "—"}</TableCell>
                        </TableRow>
                      ))
                    ) : (
                      Array.from({ length: 5 }).map((_, i) => (
                        <TableRow key={i} className="border-gray-700">
                          <TableCell className="text-gray-600">—</TableCell>
                          <TableCell /><TableCell /><TableCell />
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>

            <div className="flex flex-col gap-4">
              <Card className="flex-1 bg-gray-800 border-gray-700">
                <CardHeader>
                  <CardTitle className="text-base text-white">Actividad en tiempo real</CardTitle>
                </CardHeader>
                <CardContent>
                  <DonutChartPlaceholder data={charts?.activityByStatus} />
                </CardContent>
              </Card>

              <Card className="flex-1 bg-gray-800 border-gray-700">
                <CardHeader>
                  <CardTitle className="text-base text-white">Apps más usadas</CardTitle>
                </CardHeader>
                <CardContent>
                  <DonutChartPlaceholder data={charts?.topApps} />
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </SidebarInset>
    </SidebarProvider>
  )
}