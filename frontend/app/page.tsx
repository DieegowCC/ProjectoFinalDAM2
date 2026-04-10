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
 
// ── Tipos que vendrán de tu BBDD ──────────────────────────────────────────────
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
 
// ── Props del componente (los datos llegarán via fetch/props desde tu BBDD) ───
interface DashboardPageProps {
  stats?: DashboardStats
  workers?: ActiveUser[]
  charts?: ChartData
}
 
// ── Componente de tarjeta de métrica ─────────────────────────────────────────
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
    <Card className={`${colorClass} border-0 text-white`}>
      <CardContent className="flex items-center justify-between p-6">
        <div>
          <p className="text-sm font-medium opacity-90">{title}</p>
          <p className="text-4xl font-bold mt-1">{value}</p>
        </div>
        <div className="opacity-80 [&_svg]:size-12">{icon}</div>
      </CardContent>
    </Card>
  )
}
 
// ── Componente de estado del trabajador ──────────────────────────────────────
function StatusBadge({ status }: { status: ActiveUser["status"] }) {
  const colors: Record<ActiveUser["status"], string> = {
    Activo: "text-green-400",
    Inactivo: "text-yellow-400",
    Ausente: "text-red-400",
  }
  return <span className={`font-medium ${colors[status]}`}>{status}</span>
}
 
// ── Placeholder de gráfico de dona ────────────────────────────────────────────
// Reemplaza este componente con tu librería de charts preferida (recharts, etc.)
function DonutChartPlaceholder({
  data,
}: {
  data?: { name: string; value: number; color: string }[]
}) {
  return (
    <div className="flex items-center justify-center h-40 text-muted-foreground text-sm">
      {/* Conecta aquí tu componente de chart (ej: <PieChart data={data} />) */}
      <span className="italic opacity-50">Chart — conectar a datos</span>
    </div>
  )
}
 
// ── Página principal ──────────────────────────────────────────────────────────
export default function Home({
  stats,
  workers,
  charts,
}: DashboardPageProps) {
  return (
    <SidebarProvider>
      <AppSidebar />
      <SidebarInset>
        {/* Header */}
        <header className="flex h-16 shrink-0 items-center gap-2 border-b px-4">
          <SidebarTrigger className="-ml-1" />
          <Separator
            orientation="vertical"
            className="mr-2 data-[orientation=vertical]:h-4"
          />
          <Breadcrumb>
            <BreadcrumbList>
              <BreadcrumbItem className="hidden md:block">
                <BreadcrumbLink href="#">Sett</BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbSeparator className="hidden md:block" />
              <BreadcrumbItem>
                <BreadcrumbPage>Admin's Dashboard</BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
        </header>
 
        <div className="flex flex-1 flex-col gap-6 p-6">
 
          {/* ── Fila de métricas ── */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <MetricCard
              title="Usuarios activos"
              value={stats ? `${stats.activeUsersPercent}%` : "—"}
              icon={<User />}
              colorClass="bg-amber-400"
            />
            <MetricCard
              title="Inactividad"
              value={stats ? `${stats.inactivityPercent}%` : "—"}
              icon={<Monitor />}
              colorClass="bg-green-500"
            />
            <MetricCard
              title="Pantallas"
              value={stats?.screensOnline ?? "—"}
              icon={<Monitor />}
              colorClass="bg-blue-600"
            />
          </div>
 
          {/* ── Fila inferior: tabla + charts ── */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 flex-1">
 
            {/* Tabla de trabajadores activos */}
            <Card className="lg:col-span-2">
              <CardHeader>
                <CardTitle className="text-base font-semibold">
                  Trabajadores activos
                </CardTitle>
              </CardHeader>
              <CardContent>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Usuarios</TableHead>
                      <TableHead>Estado</TableHead>
                      <TableHead>Actividad</TableHead>
                      <TableHead>APP actual</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {workers && workers.length > 0 ? (
                      workers.map((worker) => (
                        <TableRow key={worker.id}>
                          <TableCell className="font-medium">
                            {worker.name}
                          </TableCell>
                          <TableCell>
                            <StatusBadge status={worker.status} />
                          </TableCell>
                          <TableCell>
                            {worker.activity !== null
                              ? `${worker.activity}%`
                              : "—"}
                          </TableCell>
                          <TableCell>
                            {worker.currentApp ?? "—"}
                          </TableCell>
                        </TableRow>
                      ))
                    ) : (
                      // Filas vacías de esqueleto mientras no hay datos
                      Array.from({ length: 5 }).map((_, i) => (
                        <TableRow key={i}>
                          <TableCell className="text-muted-foreground/40">
                            —
                          </TableCell>
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
 
              {/* Actividad en tiempo real */}
              <Card className="flex-1">
                <CardHeader>
                  <CardTitle className="text-base font-semibold">
                    Actividad en tiempo real
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  {/*
                    Reemplaza DonutChartPlaceholder con tu componente de chart.
                    Ejemplo con recharts:
                    <PieChart width={200} height={160}>
                      <Pie data={charts?.activityByStatus} dataKey="value" innerRadius={50} outerRadius={70} />
                      <Legend />
                    </PieChart>
                  */}
                  <DonutChartPlaceholder data={charts?.activityByStatus} />
                </CardContent>
              </Card>
 
              {/* Apps más usadas */}
              <Card className="flex-1">
                <CardHeader>
                  <CardTitle className="text-base font-semibold">
                    Apps más usadas
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  {/*
                    Reemplaza DonutChartPlaceholder con tu componente de chart.
                    Ejemplo con recharts:
                    <PieChart width={200} height={160}>
                      <Pie data={charts?.topApps} dataKey="value" />
                      <Legend />
                    </PieChart>
                  */}
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