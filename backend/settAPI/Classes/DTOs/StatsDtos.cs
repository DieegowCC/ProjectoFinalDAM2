namespace settAPI.Classes.Dtos
{
    public class DashboardStatsDto
    {
        public int activeWorkers { get; set; }
        public int absentWorkers { get; set; }
        public int inactiveWorkers { get; set; }
        public int totalWorkers { get; set; }
        public int productiveTimeMinutesToday { get; set; }
        public int totalTimeMinutesToday { get; set; }
        public double inactivityRatePercent { get; set; }
        public int sessionsTodayCount { get; set; }
        public int uniqueAppsTodayCount { get; set; }
    }

    public class WorkerStatusDto
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public string hostname { get; set; } = "";
        public string? department { get; set; }
        public string status { get; set; } = "";       // "Activo" | "Ausente" | "Inactivo"
        public string? currentApp { get; set; }
        public DateTime? loginTime { get; set; }
        public int timeConnectedMinutes { get; set; }
        public double activePercent { get; set; }
    }

    public class TopAppDto
    {
        public string name { get; set; } = "";
        public int minutes { get; set; }
    }

    public class HourlyDataPointDto
    {
        public int hour { get; set; }                  // 0..23 (UTC)
        public int activeWorkers { get; set; }         // workers con periodo "active" en esa hora
        public int productiveMinutes { get; set; }     // suma de minutos productivos en esa hora
    }

    public class WorkerDetailDto
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public string email { get; set; } = "";
        public string hostname { get; set; } = "";
        public string? department { get; set; }
        public bool is_active { get; set; }
        public DateTime created_at { get; set; }

        // Stats calculados (no vienen de la base, se calculan en StatsController)
        public int totalSessions { get; set; }              // Número total de sesiones que ha tenido este worker (históricas + actuales)
        public int totalMinutesAllTime { get; set; }        // Minutos totales de trabajo acumulados en todas las sesiones de todos los días
        public int productiveMinutesToday { get; set; }     // Minutos en los que el agente marcó al worker como "active" hoy (teclado/ratón activo)
        public DateTime? lastSeen { get; set; }             // Cuándo se vio al worker por última vez: la fecha en que cerró o abrió su última sesión
        public string status { get; set; } = "Inactivo";    // Estado actual del worker: "Activo", "Ausente" o "Inactivo" (calculado en StatsController)

        // Top apps de este worker (todos los tiempos)
        public List<TopAppDto> topApps { get; set; } = new();

        // Últimas sesiones (5)
        public List<SessionSummaryDto> recentSessions { get; set; } = new();
    }

    public class SessionSummaryDto
    {
        public int id { get; set; }
        public DateTime started_at { get; set; }
        public DateTime? ended_at { get; set; }
        public int? total_minutes { get; set; }
    }
}
