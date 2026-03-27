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
import { Button } from "@/components/ui/button"
import { DataTable } from "@/components/data-table"
import { columns, Payment } from "@/components/columns"
import data from "@/app/data.json";



export default function Home() {
  return (
    <SidebarProvider>
    <AppSidebar />
    <SidebarInset>
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
      <div className="flex flex-1 flex-col gap-4 p-4">
        <div className="grid auto-rows-min md:grid-cols-4 gap-10 md:gap-2 m-4">
          <div className="aspect-video rounded-xl col-start-3 row-start-1 md:col-start-4 md:row-start-1 md:col-span-1 md:row-span-1 bg-blue-400 p-10">
          <Button>Login</Button>
          <p>Avatar</p>
          </div>
          <div className="aspect-video rounded-xl col-start-2 row-start-2 md:col-start-2 md:row-start-2 md:col-span-1 md:row-span-1 bg-amber-300 p-10" />
          <div className="aspect-video rounded-xl col-start-3 row-start-2 md:col-start-3 md:row-start-2 md:col-span-1 md:row-span-1 bg-green-300" />
        </div>
        <div className="min-h-[100vh] flex-1 rounded-xl bg-muted/50 md:min-h-min">
        {/* <DataTable>columns={columns} data={data}</DataTable> */}
        </div>
      </div>
    </SidebarInset>
  </SidebarProvider>
  );
}

/* <div className="grid grid-cols-3 md:grid-cols-4 grid-rows-5 md:grid-rows-5 gap-10 md:gap-2 m-4">
<div className="col-start-1 row-start-1 row-span-5 md:col-start-1 md:row-start-1 md:col-span-1 md:row-span-5 bg-mist-600 rounded-md p-10">
  <h1>Sett</h1>
  <ul>
    <li>Inicio</li>
    <li>Herramientas</li>
    <li>Gestion de ordenadores</li>
    <li>Filtrar</li>
    <li>Asistencia</li>
  </ul>
</div>
<div className="col-start-3 row-start-1 md:col-start-4 md:row-start-1 md:col-span-1 md:row-span-1  bg-blue-400 rounded-md p-10">
  <p>Login</p>
  <p>Avatar</p>
</div>
<div className="col-start-2 row-start-2 md:col-start-2 md:row-start-2 md:col-span-1 md:row-span-1 bg-amber-300 rounded-md p-10">Usuarios activos</div>
<div className="col-start-3 row-start-2 md:col-start-3 md:row-start-2 md:col-span-1 md:row-span-1 bg-green-300 rounded-md p-10">Inactividad</div>
<div className="col-start-2 row-start-4 col-span-2 row-span-2 md:col-start-2 md:row-start-4 md:col-span-2 md:row-span-2  bg-mist-600 rounded-md p-10">Trabajadores activos</div>
<div className="hidden md:block md:col-start-4 md:row-start-2 md:col-span-1 md:row-span-1 bg-blue-600 rounded-md p-10">Pantallas</div>
<div className="hidden md:block md:col-start-4 md:row-start-4 md:col-span-1 md:row-span-1  bg-mist-600 rounded-md p-10">Actividad en tiempo real</div>
<div className="hidden md:block md:col-start-4 md:row-start-5 md:col-span-1 md:row-span-1  bg-mist-600 rounded-md p-10">Apps más usadas</div>
</div> */