using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using settAPI.Classes;
using settAPI.Data;
using Microsoft.AspNetCore.SignalR;
using settAPI.Hubs;
using Microsoft.AspNetCore.Authorization;

namespace settAPI.Controllers;

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

    // GET: api/appactivity — devuelve toda la actividad
    [HttpGet]
    public async Task<ActionResult> GetActivities()
    {
        List<AppActivity> activities = await _context.AppActivities
            .Include(a => a.WorkSession)
            .Include(a => a.Application)
            .ToListAsync();
        return Ok(activities);
    }

    // GET: api/appactivity/5 — devuelve una actividad por id
    [HttpGet("{id}")]
    public async Task<ActionResult> GetActivity(int id)
    {
        AppActivity? activity = await _context.AppActivities
            .Include(a => a.WorkSession)
            .Include(a => a.Application)
            .FirstOrDefaultAsync(a => a.id == id);

        if (activity == null)
            return NotFound(new { error = "Actividad no encontrada", id });

        return Ok(activity);
    }

    // GET: api/appactivity/session/5 — devuelve toda la actividad de una sesión
    [HttpGet("session/{sessionId}")]
    public async Task<ActionResult> GetActivitiesBySession(int sessionId)
    {
        List<AppActivity> activities = await _context.AppActivities
            .Where(a => a.session_id == sessionId)
            .Include(a => a.Application)
            .ToListAsync();
        return Ok(activities);
    }

    // POST: api/appactivity — registra una nueva actividad (el desktop la llama al cambiar de ventana)
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult> CreateActivity([FromBody] AppActivity activity)
    {
        try
        {
            activity.started_at = DateTime.UtcNow;

            await _context.AppActivities.AddAsync(activity);
            await _context.SaveChangesAsync();

            // Cargamos los datos relacionados directamente (las navigation properties
            // no se llenan porque las FK con snake_case no siguen la convención de EF)
            WorkSession? session = await _context.WorkSessions.FindAsync(activity.session_id);
            Application? application = activity.applications_id.HasValue
                ? await _context.Applications.FindAsync(activity.applications_id.Value)
                : null;

            // DTO plano para el frontend — evita problemas con entities de EF
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

            await _hub.Clients.All.SendAsync("NuevaActividad", payload); // emite la actividad
            
            object? workSessionData = null;
            object? applicationData = null;

            if (activity.WorkSession != null)
            {   
                workSessionData = new { worker_id = activity.WorkSession.worker_id };
            }

            if (activity.Application != null)
            {
                applicationData = new
                {
                    display_name = activity.Application.display_name,
                    process_name = activity.Application.process_name
                };
            }

            await _hub.Clients.All.SendAsync("NuevaActividad", new
            {
                id = activity.id,
                session_id = activity.session_id,
                is_foreground = activity.is_foreground,
                workSession = workSessionData,
                application = applicationData
            });

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

    // PUT: api/appactivity/5/close — cierra una actividad cuando el worker cambia de ventana
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