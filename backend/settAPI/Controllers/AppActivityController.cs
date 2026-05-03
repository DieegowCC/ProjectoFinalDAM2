using Microsoft.AspNetCore.Mvc;
using settAPI.Classes;
using settAPI.Data;
using Microsoft.AspNetCore.SignalR;
using settAPI.Hubs;
using Microsoft.AspNetCore.Authorization;

namespace settAPI.Controllers;

// Controlador de AppActivities — los registros de "qué app tenía abierta el worker
// y desde cuándo hasta cuándo".
//
// Solo lo usa el agente: cada vez que el usuario cambia de ventana en su PC, el
// agente cierra la actividad anterior (PUT close) y abre una nueva (POST). El
// dashboard se entera por el evento SignalR "NuevaActividad" y refresca stats.
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AppActivityController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<MonitoringHub> _hub;

    public AppActivityController(AppDbContext context, IHubContext<MonitoringHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    // POST /api/appactivity
    // El agente la llama al detectar un cambio de ventana, mandando
    // { session_id, applications_id, is_foreground }.
    // [AllowAnonymous] porque el agente no maneja JWT.
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult> CreateActivity([FromBody] AppActivity activity)
    {
        try
        {
            activity.started_at = DateTime.UtcNow;

            await _context.AppActivities.AddAsync(activity);
            await _context.SaveChangesAsync();

            // Para emitir por SignalR cargamos a mano la sesión y la aplicación.
            // No usamos las navigation properties (.WorkSession / .Application) porque
            // las FKs en snake_case no se rellenan automáticamente al hacer Add.
            WorkSession? session = await _context.WorkSessions.FindAsync(activity.session_id);
            Application? application = activity.applications_id.HasValue
                ? await _context.Applications.FindAsync(activity.applications_id.Value)
                : null;

            // Construimos un objeto plano (DTO anónimo) con SOLO los campos que el
            // dashboard necesita. Si mandásemos la entity de EF directamente, podría
            // arrastrar referencias circulares y dar problemas al serializar a JSON.
            var payload = new
            {
                id = activity.id,
                session_id = activity.session_id,
                is_foreground = activity.is_foreground,
                workSession = session == null ? null : new { worker_id = session.worker_id },
                application = application == null ? null : new
                {
                    display_name = application.display_name,
                    process_name = application.process_name
                }
            };

            await _hub.Clients.All.SendAsync("NuevaActividad", payload);

            return Ok(new
            {
                mensaje = "Actividad registrada correctamente",
                activity
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = "Error al registrar la actividad",
                detalle = ex.Message
            });
        }
    }

    // PUT /api/appactivity/{id}/close
    // Cierra la actividad cuando el worker cambia a otra ventana.
    // Solo pone ended_at = ahora; no emite evento (la sustituye un POST inmediato).
    [AllowAnonymous]
    [HttpPut("{id}/close")]
    public async Task<ActionResult> CloseActivity(int id)
    {
        AppActivity? activity = await _context.AppActivities.FindAsync(id);

        if (activity == null)
            return NotFound(new { error = "Actividad no encontrada", id });

        activity.ended_at = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Actividad cerrada correctamente",
            activity
        });
    }
}
