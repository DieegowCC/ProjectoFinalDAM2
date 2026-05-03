# SETT

Aplicación de monitorización de actividad de empleados. El agente corre en
segundo plano en el PC del trabajador y captura qué aplicación tiene activa y
si está usando el ordenador (teclado/ratón). La API persiste los datos en
PostgreSQL y los emite por SignalR. El dashboard del admin muestra en tiempo
real quién está conectado, qué app usa y métricas agregadas (tiempo productivo,
inactividad, top apps, timeline horario…).

> Proyecto final de DAM 2.

---

## Componentes

El repositorio contiene **3 proyectos** que se ejecutan a la vez:

| Carpeta | Qué es | Stack | Puerto |
|---|---|---|---|
| [`backend/settAPI`](backend/settAPI) | API REST + Hub de SignalR | ASP.NET Core 9, EF Core, PostgreSQL, JWT | `5263` |
| [`desktop-service/settAGENT`](desktop-service/settAGENT) | Servicio de fondo Windows que captura ventana activa e idle | .NET 8 Worker Service, P/Invoke a `user32.dll`, Serilog | — |
| [`frontend`](frontend) | Dashboard de admin | Next.js 16, React 19, Tailwind 4, shadcn/ui, recharts, SignalR client | `3000` |

```
┌──────────────────┐       HTTP        ┌──────────────────┐
│   settAGENT      │ ────────────────▶ │     settAPI      │
│  (Windows svc)   │                   │  ASP.NET Core 9  │
│  - Ventana       │                   │  - JWT auth      │
│  - Idle/active   │                   │  - SignalR Hub   │
│  - MAC / host    │                   │  - EF Core       │
└──────────────────┘                   └─────────┬────────┘
                                                 │
                                  PostgreSQL ◀───┤
                                                 │
                                 SignalR (WS)    │
                                                 ▼
                                        ┌──────────────────┐
                                        │     frontend     │
                                        │  Next.js 16 SPA  │
                                        │  (admin dash)    │
                                        └──────────────────┘
```

---

## Cómo arrancar (orden estricto)

Todo en local (Windows). Los 4 pasos van en terminales distintas:

```bash
# 1. PostgreSQL en Docker
docker run -d --name settapi_db \
  -e POSTGRES_DB=sett_db \
  -e POSTGRES_USER=sett \
  -e POSTGRES_PASSWORD=sett_local_dev \
  -p 5432:5432 postgres:latest

# 2. API (aplica migraciones + seed automáticamente al arrancar)
cd backend/settAPI && dotnet run
# → http://localhost:5263 (Swagger en /swagger)

# 3. Frontend
cd frontend && npm install && npm run dev
# → http://localhost:3000

# 4. Agente (solo Windows; depende de user32.dll)
cd desktop-service/settAGENT && dotnet run
```

**Login demo:** `admin` / `admin` (lo siembra `DbSeeder.cs` la primera vez).

> Si ves un error como *"Worker no encontrado para hostname X"* en los logs del
> agente, ve al dashboard `/workers`, crea un Worker con `hostname` = el de tu PC
> (`echo %COMPUTERNAME%`) y reinicia el agente. El seed crea uno con tu hostname
> automáticamente la primera vez, pero no si la BBDD ya estaba poblada.

---

## Endpoints principales

> Todos los endpoints (excepto los marcados como `[AllowAnonymous]`) requieren
> el header `Authorization: Bearer <jwt>` que se obtiene tras hacer login.

### Auth
| Método | Ruta | Quién | Uso |
|---|---|---|---|
| POST | `/api/auth/login` | público | Login del admin → devuelve JWT (8 h) |

### Workers (CRUD del frontend)
| Método | Ruta | Quién |
|---|---|---|
| GET | `/api/workers` | frontend |
| POST | `/api/workers` | frontend |
| PUT | `/api/workers/{id}` | frontend |
| DELETE | `/api/workers/{id}` | frontend (baja lógica: `is_active = false`) |
| GET | `/api/workers/by-hostname/{hostname}` | agente (`[AllowAnonymous]`) |

### Telemetría (la manda el agente, sin JWT)
| Método | Ruta | Cuándo |
|---|---|---|
| POST | `/api/worksessions` | Al arrancar el agente |
| PUT | `/api/worksessions/{id}/close` | Al apagarse |
| POST | `/api/appactivity` | Al cambiar de ventana |
| PUT | `/api/appactivity/{id}/close` | Al cerrar la actividad anterior |
| POST | `/api/activityperiods` | Al cambiar `active` ↔ `idle` |
| PUT | `/api/activityperiods/{id}/close` | Al cerrar el periodo anterior |
| GET | `/api/applications` | Buscar app en el catálogo |
| POST | `/api/applications` | Registrar app no vista antes |

### Stats (las consume el dashboard)
| Método | Ruta | Devuelve |
|---|---|---|
| GET | `/api/stats/dashboard` | Métricas globales del día (Madrid): activos/ausentes/inactivos, productivos, inactividad, sesiones hoy, apps únicas hoy |
| GET | `/api/stats/active-workers` | Una fila por worker con su estado en vivo |
| GET | `/api/stats/top-apps?limit=N` | Top N apps de hoy + "Otros" |
| GET | `/api/stats/hourly` | 24 puntos (uno por hora del día) con activos y minutos productivos |
| GET | `/api/stats/worker/{id}` | Detalle de un worker para el panel lateral |

---

## Eventos SignalR

El backend expone el hub en `/hubs/monitoring`. El dashboard se conecta y
escucha estos eventos para refrescar las stats sin tener que hacer polling:

| Evento | Lo emite | Significa |
|---|---|---|
| `SesionAbierta` | `WorkSessionsController.OpenSession` | El agente arrancó en algún PC |
| `SesionCerrada` | `WorkSessionsController.CloseSession` | El agente se apagó |
| `NuevaActividad` | `AppActivityController.CreateActivity` | El worker cambió de ventana |
| `NuevoPeriodo` | `ActivityPeriodsController.CreatePeriod` | El worker cambió de active ↔ idle |
| `WorkerConnected` / `WorkerDisconnected` | `MonitoringHub` | Se conectó/desconectó un cliente del hub (admin abriendo el dashboard) |

El dashboard reacciona a los 4 primeros con un `refetch` debounced de 500 ms
de las 4 stats. Los 2 últimos están registrados pero sin lógica.

---

## Convenciones del equipo

Si vas a tocar código, respeta esto:

- **C# sin `var`** — siempre tipos explícitos:
  `Worker? worker = await _context.Workers.FindAsync(id);` (no `var worker = ...`).
- **snake_case en modelos y BBDD** — `worker_id`, `started_at`, `ended_at`,
  mapeado en `AppDbContext.OnModelCreating`.
- **Navigation properties nullable** — `public Worker? Worker { get; set; }`
  (no `= null!`), para que ASP.NET no las exija al hacer model binding desde
  el agente.
- **Endpoints de telemetría del agente son `[AllowAnonymous]`** — el agente
  no maneja JWT (es deuda técnica conocida, ver más abajo).
- **SignalR cliente** se conecta con `skipNegotiation: true, transport: WebSockets`
  por compatibilidad entre cliente v10 y servidor v9.
- **Next.js**: `reactStrictMode: false` en `next.config.ts` para evitar el
  doble-mount que rompe la conexión SignalR en desarrollo.

---

## Estructura del repositorio

```
ProjectoFinalDAM2/
├── backend/
│   ├── docker-compose.yml              # PostgreSQL local
│   └── settAPI/                        # API .NET 9
│       ├── Classes/                    # Modelos EF + DTOs (Classes/Dtos/)
│       ├── Controllers/                # Auth, Workers, WorkSessions, AppActivity,
│       │                               # ActivityPeriods, Applications, Stats
│       ├── Data/
│       │   ├── AppDbContext.cs
│       │   └── DbSeeder.cs             # Siembra admin/admin + workers de demo
│       ├── Hubs/MonitoringHub.cs       # Hub de SignalR
│       ├── Migrations/                 # Migraciones EF Core
│       └── Program.cs                  # Punto de entrada (JWT, CORS, SignalR, cleanup…)
├── desktop-service/
│   └── settAGENT/                      # Worker Service .NET 8
│       ├── Collectors/                 # ActiveWindow, Activity (idle), SystemInfo
│       ├── Models/AgentSettings.cs
│       ├── Services/ApiSenderService.cs # Todas las llamadas HTTP a la API
│       ├── Worker.cs                   # Bucle principal del agente
│       └── appsettings.json
├── frontend/                           # Next.js 16
│   ├── app/
│   │   ├── dashboard/page.tsx          # Dashboard principal
│   │   ├── login/page.tsx
│   │   ├── workers/page.tsx            # CRUD de workers
│   │   └── forgot/page.tsx             # Pantalla de recuperación (solo UI por ahora)
│   ├── components/
│   │   ├── dashboard/                  # MetricCard, ActiveWorkersTable, charts,
│   │   │                               # WorkerDetailSheet, StatusBadge…
│   │   ├── workers/                    # WorkerFormSheet
│   │   ├── app-sidebar.tsx             # Navegación lateral
│   │   ├── theme-toggle.tsx            # Botón claro / oscuro
│   │   ├── login-form.tsx
│   │   └── ui/                         # Componentes shadcn (no tocar)
│   └── .env.local                      # NEXT_PUBLIC_API_URL=http://localhost:5263
└── README.md                           # ← este archivo
```

## Deuda técnica conocida (fuera de scope de la demo)

- **Auth machine-to-machine no implementada**: el agente manda telemetría a
  endpoints `[AllowAnonymous]`. Pendiente de mover a un esquema con API key
  por worker.
- **Hub SignalR público**: por el mismo motivo de arriba.
- **Secretos en `appsettings.json`**: la clave JWT y la contraseña de Postgres
  están en el repo. Mover a User Secrets / variables de entorno.
- **Auto-registro de worker**: si el agente arranca en un PC cuyo hostname no
  está en la BBDD, ABORTA. La auto-creación queda diferida.
- **Página `/forgot`**: existe la UI pero el flujo de recuperación de
  contraseña no tiene endpoint backend todavía.

---

## Autores

[![Contribuidores](https://contrib.rocks/image?repo=DieegowCC/ProjectoFinalDAM2&max=3&columns=3)](https://github.com/DieegowCC/ProjectoFinalDAM2/graphs/contributors)
