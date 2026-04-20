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
    [HttpPost]
    public async Task<ActionResult> CreateActivity([FromBody] AppActivity activity)
    {
        try
        {
            activity.started_at = DateTime.UtcNow;

            await _context.AppActivities.AddAsync(activity);
            await _context.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("NuevaActividad", activity); // emite la actividad

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