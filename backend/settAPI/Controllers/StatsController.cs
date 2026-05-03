using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using settAPI.Classes;
using settAPI.Classes.Dtos;
using settAPI.Data;

namespace settAPI.Controllers;

// Controlador de estadísticas. Agrega datos de Workers + WorkSessions +
// ActivityPeriods + AppActivities + Applications y los devuelve ya calculados
// al frontend en forma de DTOs (clases en Classes/Dtos/StatsDtos.cs).
//
// El dashboard hace 4 llamadas iniciales (dashboard, active-workers, top-apps,
// hourly) y, cuando llega un evento SignalR, vuelve a pedirlas. La página de
// detalle de un worker usa /api/stats/worker/{id}.
//
// Todos los cálculos se hacen sobre el DÍA actual en hora de Madrid (no UTC, se convierte a UTC para filtrar la BBDD):
// los helpers GetMadridTodayRange / GetMadridTimeZone convierten al rango UTC
// que abarca ese día para filtrar la BBDD (donde los timestamps se guardan en UTC).

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StatsController(AppDbContext context)
    {
        _context = context;
    }

    // Helper: zona horaria de Madrid (compatible con Windows y Linux)
    private static TimeZoneInfo GetMadridTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"); }
    }

    // Helper: rango UTC que abarca el día actual en Madrid
    private static (DateTime startUtc, DateTime endUtc, DateTime todayMadrid) GetMadridTodayRange()
    {
        TimeZoneInfo tz = GetMadridTimeZone();
        DateTime nowMadrid = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        DateTime todayMadrid = nowMadrid.Date;
        DateTime startUtc = TimeZoneInfo.ConvertTimeToUtc(todayMadrid, tz);
        DateTime endUtc = TimeZoneInfo.ConvertTimeToUtc(todayMadrid.AddDays(1), tz);
        return (startUtc, endUtc, todayMadrid);
    }

    // Helper: calcula el "fin real" de un ActivityPeriod.
    // Si period_end tiene valor, lo usa. Si no, mira si la sesión padre
    // ya está cerrada para usar su ended_at en vez de DateTime.UtcNow.
    // Esto evita que periodos huérfanos (de sesiones crasheadas) inflen
    // el tiempo productivo indefinidamente.
    private static DateTime GetPeriodEffectiveEnd(ActivityPeriod period, Dictionary<int, WorkSession> sessions)
    {
        if (period.period_end.HasValue)
            return period.period_end.Value;
        if (sessions.TryGetValue(period.session_id, out WorkSession? s) && s.ended_at.HasValue)
            return s.ended_at.Value;
        return DateTime.UtcNow;
    }

    // Helper: lo mismo pero para AppActivity.
    private static DateTime GetActivityEffectiveEnd(AppActivity activity, Dictionary<int, WorkSession> sessions)
    {
        if (activity.ended_at.HasValue)
            return activity.ended_at.Value;
        if (sessions.TryGetValue(activity.session_id, out WorkSession? s) && s.ended_at.HasValue)
            return s.ended_at.Value;
        return DateTime.UtcNow;
    }

    // GET /api/stats/dashboard
    // Devuelve las métricas globales del día (en zona Madrid):
    //   - cuántos workers están activos / ausentes / inactivos AHORA
    //   - minutos productivos vs minutos totales hoy
    //   - % de inactividad
    //   - sesiones iniciadas hoy y nº de apps distintas usadas hoy
    //
    // Pasos:
    //   1. Listamos workers activos.
    //   2. Recorremos cada sesión abierta y miramos su ÚLTIMO ActivityPeriod:
    //        - status "active" → worker activo
    //        - status "idle"   → worker ausente
    //        - sin sesión abierta o sin periodos → inactivo
    //   3. Para los minutos del día sumamos en SEGUNDOS la duración de cada
    //      ActivityPeriod cuyo period_start cae dentro del día (la conversión
    //      a minutos se hace al final, así no perdemos actividades cortas).
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
    {
        (DateTime todayStartUtc, DateTime todayEndUtc, _) = GetMadridTodayRange();

        List<Worker> workers = await _context.Workers
            .Where(w => w.is_active)
            .ToListAsync();

        List<WorkSession> openSessions = await _context.WorkSessions
            .Where(s => s.ended_at == null)
            .ToListAsync();

        int activeWorkers = 0;
        int absentWorkers = 0;

        foreach (WorkSession session in openSessions)
        {
            ActivityPeriod? lastPeriod = await _context.ActivityPeriods
                .Where(p => p.session_id == session.id)
                .OrderByDescending(p => p.period_start)
                .FirstOrDefaultAsync();

            if (lastPeriod == null) continue;

            if (lastPeriod.status == "active")
                activeWorkers++;
            else if (lastPeriod.status == "idle")
                absentWorkers++;
        }

        int totalWorkers = workers.Count;
        int inactiveWorkers = totalWorkers - activeWorkers - absentWorkers;
        if (inactiveWorkers < 0) inactiveWorkers = 0;

        List<ActivityPeriod> periodsToday = await _context.ActivityPeriods
            .Where(p => p.period_start >= todayStartUtc && p.period_start < todayEndUtc)
            .ToListAsync();

        // Cargar sesiones padre para no usar UtcNow en periodos de sesiones ya cerradas
        List<int> periodSessionIds = periodsToday.Select(p => p.session_id).Distinct().ToList();
        Dictionary<int, WorkSession> sessionsForPeriods = await _context.WorkSessions
            .Where(s => periodSessionIds.Contains(s.id))
            .ToDictionaryAsync(s => s.id);

        int productiveSeconds = 0;
        int totalSeconds = 0;

        foreach (ActivityPeriod period in periodsToday)
        {
            DateTime end = GetPeriodEffectiveEnd(period, sessionsForPeriods);
            int seconds = (int)(end - period.period_start).TotalSeconds;
            if (seconds < 0) seconds = 0;

            totalSeconds += seconds;
            if (period.status == "active")
                productiveSeconds += seconds;
        }

        int productiveMinutes = productiveSeconds / 60;
        int totalMinutes = totalSeconds / 60;
        if (productiveMinutes == 0 && productiveSeconds > 0) productiveMinutes = 1;
        if (totalMinutes == 0 && totalSeconds > 0) totalMinutes = 1;

        double inactivityRate = 0;
        if (totalSeconds > 0)
            inactivityRate = Math.Round(((double)(totalSeconds - productiveSeconds) / totalSeconds) * 100, 1);

        // Sesiones iniciadas hoy (incluye cerradas)
        int sessionsTodayCount = await _context.WorkSessions
            .Where(s => s.started_at >= todayStartUtc && s.started_at < todayEndUtc)
            .CountAsync();

        // Apps distintas en uso hoy
        int uniqueAppsTodayCount = await _context.AppActivities
            .Where(a => a.started_at >= todayStartUtc && a.started_at < todayEndUtc && a.applications_id != null)
            .Select(a => a.applications_id)
            .Distinct()
            .CountAsync();

        DashboardStatsDto dto = new DashboardStatsDto
        {
            activeWorkers = activeWorkers,
            absentWorkers = absentWorkers,
            inactiveWorkers = inactiveWorkers,
            totalWorkers = totalWorkers,
            productiveTimeMinutesToday = productiveMinutes,
            totalTimeMinutesToday = totalMinutes,
            inactivityRatePercent = inactivityRate,
            sessionsTodayCount = sessionsTodayCount,
            uniqueAppsTodayCount = uniqueAppsTodayCount
        };

        return Ok(dto);
    }

    // GET /api/stats/active-workers
    // Devuelve UNA fila por cada worker activo (la tabla principal del dashboard).
    // Para cada worker calcula:
    //   - status: "Activo" / "Ausente" / "Inactivo" (ver lógica del dashboard)
    //   - currentApp: el display_name de la última app que el agente notificó
    //   - loginTime: el started_at de su última sesión
    //   - timeConnectedMinutes: minutos transcurridos desde que abrió sesión
    //     (o desde que la cerró, si está inactivo)
    //   - activePercent: % de tiempo con status "active" sobre el total de la
    //     sesión actual
    //
    // Recorremos worker a worker para no liar los joins de EF: cargamos primero
    // su sesión abierta (si existe), después su último periodo y su última
    // actividad, y al final calculamos el porcentaje de actividad sumando
    // segundos de los periodos de esa sesión.
    [HttpGet("active-workers")]
    public async Task<ActionResult<List<WorkerStatusDto>>> GetActiveWorkers()
    {
        List<Worker> workers = await _context.Workers
            .Where(w => w.is_active)
            .ToListAsync();

        List<WorkerStatusDto> result = new List<WorkerStatusDto>();

        foreach (Worker worker in workers)
        {
            WorkSession? openSession = await _context.WorkSessions
                .Where(s => s.worker_id == worker.id && s.ended_at == null)
                .OrderByDescending(s => s.started_at)
                .FirstOrDefaultAsync();

            WorkSession? lastSession = openSession;
            if (lastSession == null)
            {
                lastSession = await _context.WorkSessions
                    .Where(s => s.worker_id == worker.id)
                    .OrderByDescending(s => s.started_at)
                    .FirstOrDefaultAsync();
            }

            string status = "Inactivo";
            string? currentApp = null;
            DateTime? loginTime = lastSession?.started_at;
            int timeMinutes = 0;
            double activePercent = 0;

            if (openSession != null)
            {
                ActivityPeriod? lastPeriod = await _context.ActivityPeriods
                    .Where(p => p.session_id == openSession.id)
                    .OrderByDescending(p => p.period_start)
                    .FirstOrDefaultAsync();

                if (lastPeriod != null && lastPeriod.status == "idle")
                    status = "Ausente";
                else
                    status = "Activo";

                // Cargamos la última app activity y luego la Application por separado
                // (las navigation properties no se llenan automáticamente con FKs snake_case)
                AppActivity? lastActivity = await _context.AppActivities
                    .Where(a => a.session_id == openSession.id && a.ended_at == null)
                    .OrderByDescending(a => a.started_at)
                    .FirstOrDefaultAsync();

                if (lastActivity != null && lastActivity.applications_id.HasValue)
                {
                    Application? app = await _context.Applications.FindAsync(lastActivity.applications_id.Value);
                    if (app != null)
                        currentApp = app.display_name ?? app.process_name;
                }

                timeMinutes = (int)(DateTime.UtcNow - openSession.started_at).TotalMinutes;

                List<ActivityPeriod> sessionPeriods = await _context.ActivityPeriods
                    .Where(p => p.session_id == openSession.id)
                    .ToListAsync();

                int activeSec = 0;
                int totalSec = 0;
                foreach (ActivityPeriod p in sessionPeriods)
                {
                    DateTime end = p.period_end ?? DateTime.UtcNow;
                    int s = (int)(end - p.period_start).TotalSeconds;
                    if (s < 0) s = 0;
                    totalSec += s;
                    if (p.status == "active") activeSec += s;
                }
                if (totalSec > 0)
                    activePercent = Math.Round(((double)activeSec / totalSec) * 100, 1);
            }
            else if (lastSession != null && lastSession.ended_at != null)
            {
                timeMinutes = (int)(DateTime.UtcNow - lastSession.ended_at.Value).TotalMinutes;
            }

            WorkerStatusDto dto = new WorkerStatusDto
            {
                id = worker.id,
                name = worker.name,
                hostname = worker.hostname,
                department = worker.department,
                status = status,
                currentApp = currentApp,
                loginTime = loginTime,
                timeConnectedMinutes = timeMinutes,
                activePercent = activePercent
            };

            result.Add(dto);
        }

        return Ok(result);
    }

    // GET /api/stats/top-apps?limit=5
    // Devuelve las N apps más usadas hoy (por minutos), más una entrada "Otros"
    // que agrupa todo lo que se queda fuera del top. Lo usa el donut chart
    // "Top apps" del dashboard.
    //
    // Pasos:
    //   1. Cogemos todas las AppActivities iniciadas hoy.
    //   2. Cargamos a mano las Applications correspondientes (Include no funciona
    //      con FKs en snake_case).
    //   3. Acumulamos SEGUNDOS por nombre de app (display_name o, si no,
    //      process_name) en un diccionario.
    //   4. Ordenamos descendente, cogemos los N primeros y agrupamos el resto
    //      como "Otros".
    [HttpGet("top-apps")]
    public async Task<ActionResult<List<TopAppDto>>> GetTopApps([FromQuery] int limit = 5)
    {
        (DateTime todayStartUtc, DateTime todayEndUtc, _) = GetMadridTodayRange();

        List<AppActivity> activitiesToday = await _context.AppActivities
            .Where(a => a.started_at >= todayStartUtc && a.started_at < todayEndUtc)
            .ToListAsync();

        // Cargar sesiones padre para no usar UtcNow en actividades de sesiones ya cerradas
        List<int> actSessionIds = activitiesToday.Select(a => a.session_id).Distinct().ToList();
        Dictionary<int, WorkSession> actSessions = await _context.WorkSessions
            .Where(s => actSessionIds.Contains(s.id))
            .ToDictionaryAsync(s => s.id);

        // Cargar Applications manualmente (Include no funciona con FKs snake_case)
        List<int> appIds = activitiesToday
            .Where(a => a.applications_id.HasValue)
            .Select(a => a.applications_id!.Value)
            .Distinct()
            .ToList();

        Dictionary<int, Application> appsDict = await _context.Applications
            .Where(a => appIds.Contains(a.id))
            .ToDictionaryAsync(a => a.id);

        // Acumulamos en segundos para no perder actividades cortas (< 1 min)
        Dictionary<string, int> secondsByApp = new Dictionary<string, int>();

        foreach (AppActivity activity in activitiesToday)
        {
            if (!activity.applications_id.HasValue) continue;
            if (!appsDict.TryGetValue(activity.applications_id.Value, out Application? app)) continue;

            string name = app.display_name ?? app.process_name;
            DateTime end = GetActivityEffectiveEnd(activity, actSessions);
            int seconds = (int)(end - activity.started_at).TotalSeconds;
            if (seconds < 0) seconds = 0;

            if (secondsByApp.ContainsKey(name))
                secondsByApp[name] += seconds;
            else
                secondsByApp[name] = seconds;
        }

        // Convertimos a minutos al final; mínimo 1 min para cualquier app con tiempo > 0
        List<TopAppDto> sorted = secondsByApp
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new TopAppDto { name = kv.Key, minutes = Math.Max(1, kv.Value / 60) })
            .ToList();

        List<TopAppDto> top = sorted.Take(limit).ToList();
        int othersMinutes = sorted.Skip(limit).Sum(a => a.minutes);

        if (othersMinutes > 0)
            top.Add(new TopAppDto { name = "Otros", minutes = othersMinutes });

        return Ok(top);
    }

    // GET /api/stats/hourly
    // Devuelve 24 puntos (uno por cada hora del día en Madrid) con:
    //   - cuántos workers DISTINTOS estuvieron activos en esa hora
    //   - cuántos minutos productivos hubo en esa hora (sumando todos los workers)
    //
    // Para cada hora recorremos los ActivityPeriods de hoy y calculamos cuánto
    // se solapa cada periodo con esa hora (overlap = max(start) ... min(end)).
    // Si el solape es positivo, ese periodo cuenta para esa hora.
    [HttpGet("hourly")]
    public async Task<ActionResult<List<HourlyDataPointDto>>> GetHourlyTimeline()
    {
        TimeZoneInfo tz = GetMadridTimeZone();
        (DateTime todayStartUtc, DateTime todayEndUtc, DateTime todayMadrid) = GetMadridTodayRange();

        List<ActivityPeriod> periodsToday = await _context.ActivityPeriods
            .Where(p => p.period_start >= todayStartUtc && p.period_start < todayEndUtc)
            .ToListAsync();

        // Cargar WorkSessions manualmente para saber el worker_id
        List<int> sessionIds = periodsToday.Select(p => p.session_id).Distinct().ToList();
        Dictionary<int, WorkSession> sessionsDict = await _context.WorkSessions
            .Where(s => sessionIds.Contains(s.id))
            .ToDictionaryAsync(s => s.id);

        List<HourlyDataPointDto> result = new List<HourlyDataPointDto>();

        for (int hour = 0; hour < 24; hour++)
        {
            DateTime hourStartMadrid = todayMadrid.AddHours(hour);
            DateTime hourEndMadrid = hourStartMadrid.AddHours(1);
            DateTime hourStartUtc = TimeZoneInfo.ConvertTimeToUtc(hourStartMadrid, tz);
            DateTime hourEndUtc = TimeZoneInfo.ConvertTimeToUtc(hourEndMadrid, tz);

            HashSet<int> activeWorkerIds = new HashSet<int>();
            int productiveSec = 0;

            foreach (ActivityPeriod period in periodsToday)
            {
                if (period.status != "active") continue;
                DateTime pEnd = GetPeriodEffectiveEnd(period, sessionsDict);

                // Si el periodo se solapa con esta hora
                DateTime overlapStart = period.period_start > hourStartUtc ? period.period_start : hourStartUtc;
                DateTime overlapEnd = pEnd < hourEndUtc ? pEnd : hourEndUtc;

                if (overlapStart < overlapEnd)
                {
                    int seconds = (int)(overlapEnd - overlapStart).TotalSeconds;
                    if (seconds < 0) seconds = 0;
                    productiveSec += seconds;

                    if (sessionsDict.TryGetValue(period.session_id, out WorkSession? sess))
                        activeWorkerIds.Add(sess.worker_id);
                }
            }

            int productiveMin = productiveSec / 60;
            if (productiveMin == 0 && productiveSec > 0) productiveMin = 1;

            result.Add(new HourlyDataPointDto
            {
                hour = hour,
                activeWorkers = activeWorkerIds.Count,
                productiveMinutes = productiveMin
            });
        }

        return Ok(result);
    }

    // GET /api/stats/worker/{id}
    // Devuelve toda la info de un worker concreto para la vista de detalle:
    //   - sus datos básicos (nombre, email, hostname, departamento)
    //   - estadísticas agregadas: total de sesiones, minutos totales, productivos hoy
    //   - status actual (Activo / Ausente / Inactivo)
    //   - top 5 apps de TODOS los tiempos para este worker
    //   - últimas 5 sesiones
    //
    // El frontend lo llama desde WorkerDetailSheet al pulsar una fila de la tabla.
    [HttpGet("worker/{id}")]
    public async Task<ActionResult<WorkerDetailDto>> GetWorkerDetail(int id)
    {
        Worker? worker = await _context.Workers.FindAsync(id);
        if (worker == null)
            return NotFound(new { error = "Worker no encontrado", id });

        (DateTime todayStartUtc, DateTime todayEndUtc, _) = GetMadridTodayRange();

        List<WorkSession> allSessions = await _context.WorkSessions
            .Where(s => s.worker_id == id)
            .OrderByDescending(s => s.started_at)
            .ToListAsync();

        WorkSession? openSession = allSessions.FirstOrDefault(s => s.ended_at == null);
        WorkSession? lastSession = allSessions.FirstOrDefault();

        // Stats agregadas
        int totalMinutesAllTime = allSessions.Sum(s =>
            s.total_minutes ?? (int)((s.ended_at ?? DateTime.UtcNow) - s.started_at).TotalMinutes);

        // Productivos hoy (periodos active de hoy de este worker, en Madrid)
        List<int> sessionIds = allSessions.Select(s => s.id).ToList();
        Dictionary<int, WorkSession> workerSessionsDict = allSessions.ToDictionary(s => s.id);
        List<ActivityPeriod> periodsToday = await _context.ActivityPeriods
            .Where(p => sessionIds.Contains(p.session_id)
                && p.period_start >= todayStartUtc
                && p.period_start < todayEndUtc
                && p.status == "active")
            .ToListAsync();

        int productiveTodaySec = 0;
        foreach (ActivityPeriod p in periodsToday)
        {
            DateTime end = GetPeriodEffectiveEnd(p, workerSessionsDict);
            int s = (int)(end - p.period_start).TotalSeconds;
            if (s > 0) productiveTodaySec += s;
        }
        int productiveToday = productiveTodaySec / 60;
        if (productiveToday == 0 && productiveTodaySec > 0) productiveToday = 1;

        // Status actual
        string status = "Inactivo";
        if (openSession != null)
        {
            ActivityPeriod? lastPeriod = await _context.ActivityPeriods
                .Where(p => p.session_id == openSession.id)
                .OrderByDescending(p => p.period_start)
                .FirstOrDefaultAsync();
            if (lastPeriod != null && lastPeriod.status == "idle")
                status = "Ausente";
            else
                status = "Activo";
        }

        // Top 5 apps de este worker (todos los tiempos) — manualmente cargadas
        List<AppActivity> allActivities = await _context.AppActivities
            .Where(a => sessionIds.Contains(a.session_id))
            .ToListAsync();

        List<int> appIds = allActivities
            .Where(a => a.applications_id.HasValue)
            .Select(a => a.applications_id!.Value)
            .Distinct()
            .ToList();
        Dictionary<int, Application> appsDict = await _context.Applications
            .Where(a => appIds.Contains(a.id))
            .ToDictionaryAsync(a => a.id);

        Dictionary<string, int> appSeconds = new Dictionary<string, int>();
        foreach (AppActivity a in allActivities)
        {
            if (!a.applications_id.HasValue) continue;
            if (!appsDict.TryGetValue(a.applications_id.Value, out Application? app)) continue;

            string n = app.display_name ?? app.process_name;
            DateTime end = GetActivityEffectiveEnd(a, workerSessionsDict);
            int s = (int)(end - a.started_at).TotalSeconds;
            if (s < 0) s = 0;
            if (appSeconds.ContainsKey(n)) appSeconds[n] += s;
            else appSeconds[n] = s;
        }

        List<TopAppDto> topApps = appSeconds
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value)
            .Take(5)
            .Select(kv => new TopAppDto { name = kv.Key, minutes = Math.Max(1, kv.Value / 60) })
            .ToList();

        // Recent sessions (5)
        List<SessionSummaryDto> recent = allSessions.Take(5).Select(s => new SessionSummaryDto
        {
            id = s.id,
            started_at = s.started_at,
            ended_at = s.ended_at,
            total_minutes = s.total_minutes
        }).ToList();

        WorkerDetailDto dto = new WorkerDetailDto
        {
            id = worker.id,
            name = worker.name,
            email = worker.email,
            hostname = worker.hostname,
            department = worker.department,
            is_active = worker.is_active,
            created_at = worker.created_at,
            totalSessions = allSessions.Count,
            totalMinutesAllTime = totalMinutesAllTime,
            productiveMinutesToday = productiveToday,
            lastSeen = lastSession?.ended_at ?? lastSession?.started_at,
            status = status,
            topApps = topApps,
            recentSessions = recent
        };

        return Ok(dto);
    }
}
