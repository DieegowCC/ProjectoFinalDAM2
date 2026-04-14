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
  stats?: DashboardStats
  workers?: ActiveUser[]
  charts?: ChartData
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
  stats,
  workers,
  charts,
}: DashboardPageProps) {
  return (
    <SidebarProvider
      style={{ fontFamily: "Verdana, Geneva, Tahoma, sans-serif" }}
    >
      <AppSidebar />
      <SidebarInset className="bg-gray-950">
        {/* Header */}
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

          {/* Botón login */}
          <Button
            asChild
            variant="outline"
            size="sm"
            className="ml-auto border-gray-600 text-white hover:bg-gray-700 hover:text-white bg-transparent"
          >
            <Link href="/login">
              <LogIn className="size-4" />
              Login
            </Link>
          </Button>
        </header>

        <div className="flex flex-1 flex-col gap-6 p-6">

          {/* ── Fila de métricas ── */}
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

          {/* ── Fila inferior: tabla + charts ── */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 flex-1">

            {/* Tabla de trabajadores activos */}
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
                    {workers && workers.length > 0 ? (
                      workers.map((worker) => (
                        <TableRow
                          key={worker.id}
                          className="border-gray-700 hover:bg-gray-750"
                        >
                          <TableCell className="font-medium text-white">
                            {worker.name}
                          </TableCell>
                          <TableCell>
                            <StatusBadge status={worker.status} />
                          </TableCell>
                          <TableCell className="text-gray-300">
                            {worker.activity !== null
                              ? `${worker.activity}%`
                              : "—"}
                          </TableCell>
                          <TableCell className="text-gray-300">
                            {worker.currentApp ?? "—"}
                          </TableCell>
                        </TableRow>
                      ))
                    ) : (
                      Array.from({ length: 5 }).map((_, i) => (
                        <TableRow key={i} className="border-gray-700">
                          <TableCell className="text-gray-600">—</TableCell>
                          <TableCell />
                          <TableCell />
                          <TableCell />
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>

            {/* Panel lateral de charts */}
            <div className="flex flex-col gap-4">
              <Card className="flex-1 bg-gray-800 border-gray-700">
                <CardHeader>
                  <CardTitle className="text-base text-white">
                    Actividad en tiempo real
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <DonutChartPlaceholder data={charts?.activityByStatus} />
                </CardContent>
              </Card>

              <Card className="flex-1 bg-gray-800 border-gray-700">
                <CardHeader>
                  <CardTitle className="text-base text-white">
                    Apps más usadas
                  </CardTitle>
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