using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using settAPI.Classes;
using settAPI.Data;

namespace settAPI.Controllers;

// Genera el informe histórico de actividad por rango de fechas.
// El frontend llama a este endpoint con un "desde" y un "hasta", y devuelve
// una lista de filas (una por sesión) que el frontend convierte a CSV.
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/report?from=2026-05-01&to=2026-05-05
    // Devuelve una fila por sesión de cada worker activo en ese rango.
    // Cada fila incluye: worker, departamento, inicio/fin de sesión,
    // minutos activos, minutos inactivos y la app más usada en esa sesión.
    [HttpGet]
    public async Task<ActionResult> GetReport([FromQuery] string from, [FromQuery] string to)
    {
        // DateTime.Parse devuelve Kind=Unspecified, que Npgsql (el driver de
        // PostgreSQL) rechaza al comparar con columnas timestamptz.
        // SpecifyKind le dice explícitamente que es UTC y el problema desaparece.
        DateTime fromUtc = DateTime.SpecifyKind(DateTime.Parse(from).Date, DateTimeKind.Utc);
        DateTime toUtc   = DateTime.SpecifyKind(DateTime.Parse(to).Date.AddDays(1), DateTimeKind.Utc);

        try
        {
            List<Worker> workers = await _context.Workers
                .Where(w => w.is_active)
                .ToListAsync();

            List<object> filas = new List<object>();

            foreach (Worker worker in workers)
            {
                // Sesiones que se solapan con el rango pedido.
                // La condición cubre tres casos: sesión dentro del rango,
                // sesión que empieza antes pero acaba dentro, y sesión aún abierta.
                List<WorkSession> sesiones = await _context.WorkSessions
                    .Where(s => s.worker_id == worker.id
                             && s.started_at < toUtc
                             && (s.ended_at == null || s.ended_at >= fromUtc))
                    .OrderBy(s => s.started_at)
                    .ToListAsync();

                foreach (WorkSession sesion in sesiones)
                {
                    // Sumamos segundos activos e inactivos de los periodos de esta sesión
                    List<ActivityPeriod> periodos = await _context.ActivityPeriods
                        .Where(p => p.session_id == sesion.id)
                        .ToListAsync();

                    int segundosActivo   = 0;
                    int segundosInactivo = 0;

                    foreach (ActivityPeriod p in periodos)
                    {
                        DateTime fin = p.period_end ?? sesion.ended_at ?? DateTime.UtcNow;
                        int s = (int)(fin - p.period_start).TotalSeconds;
                        if (s < 0) s = 0;

                        if (p.status == "active") segundosActivo += s;
                        else                      segundosInactivo += s;
                    }

                    // App más usada: cargamos las actividades de esta sesión,
                    // acumulamos segundos por nombre de app y cogemos la mayor.
                    string appPrincipal = "-";

                    List<AppActivity> actividades = await _context.AppActivities
                        .Where(a => a.session_id == sesion.id)
                        .ToListAsync();

                    if (actividades.Count > 0)
                    {
                        List<int> appIds = actividades
                            .Where(a => a.applications_id.HasValue)
                            .Select(a => a.applications_id!.Value)
                            .Distinct()
                            .ToList();

                        Dictionary<int, Application> appsDict = await _context.Applications
                            .Where(a => appIds.Contains(a.id))
                            .ToDictionaryAsync(a => a.id);

                        Dictionary<string, int> segundosPorApp = new Dictionary<string, int>();

                        foreach (AppActivity act in actividades)
                        {
                            if (!act.applications_id.HasValue) continue;
                            if (!appsDict.TryGetValue(act.applications_id.Value, out Application? app)) continue;

                            string nombre = app.display_name ?? app.process_name;
                            DateTime fin  = act.ended_at ?? DateTime.UtcNow;
                            int s = (int)(fin - act.started_at).TotalSeconds;
                            if (s < 0) s = 0;

                            if (segundosPorApp.ContainsKey(nombre))
                                segundosPorApp[nombre] += s;
                            else
                                segundosPorApp[nombre] = s;
                        }

                        if (segundosPorApp.Count > 0)
                            appPrincipal = segundosPorApp.OrderByDescending(kv => kv.Value).First().Key;
                    }

                    filas.Add(new
                    {
                        worker_name    = worker.name,
                        department     = worker.department ?? "-",
                        hostname       = worker.hostname,
                        session_start  = sesion.started_at,
                        session_end    = sesion.ended_at,
                        active_minutes = segundosActivo / 60,
                        idle_minutes   = segundosInactivo / 60,
                        top_app        = appPrincipal
                    });
                }
            }

            return Ok(filas);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Error al generar el informe", detalle = ex.Message });
        }
    }
}
