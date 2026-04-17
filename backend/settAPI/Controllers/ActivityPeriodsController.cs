using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using settAPI.Classes;
using settAPI.Data;
using Microsoft.AspNetCore.SignalR;
using settAPI.Hubs;
using System.Diagnostics;

namespace settAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivityPeriodsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<MonitoringHub> _hub;

    public ActivityPeriodsController(AppDbContext context, IHubContext<MonitoringHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    // GET: api/activityperiods — devuelve todos los periodos
    [HttpGet]
    public async Task<ActionResult> GetPeriods()
    {
        List<ActivityPeriod> periods = await _context.ActivityPeriods
            .Include(p => p.WorkSession)
            .ToListAsync();
        return Ok(periods);
    }

    // GET: api/activityperiods/5 — devuelve un periodo por id
    [HttpGet("{id}")]
    public async Task<ActionResult> GetPeriod(int id)
    {
        ActivityPeriod? period = await _context.ActivityPeriods
            .Include(p => p.WorkSession)
            .FirstOrDefaultAsync(p => p.id == id);

        if (period == null)
            return NotFound(new { error = "Periodo no encontrado", id });

        return Ok(period);
    }

    // GET: api/activityperiods/session/5 — devuelve todos los periodos de una sesión
    [HttpGet("session/{sessionId}")]
    public async Task<ActionResult> GetPeriodsBySession(int sessionId)
    {
        List<ActivityPeriod> periods = await _context.ActivityPeriods
            .Where(p => p.session_id == sessionId)
            .ToListAsync();
        return Ok(periods);
    }

    // POST: api/activityperiods — registra un nuevo periodo (active, idle o break)
    [HttpPost]
    public async Task<ActionResult> CreatePeriod([FromBody] ActivityPeriod period)
    {
        try
        {
            period.period_start = DateTime.UtcNow;

            await _context.ActivityPeriods.AddAsync(period);
            await _context.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("NuevoPeriodo", period); // emite la actividad a todos los clientes del dashboard conectados por WebSocket

            return Ok(new
            {
                mensaje = "Periodo registrado correctamente",
                period
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = "Error al registrar el periodo",
                detalle = ex.Message
            });
        }
    }

    // PUT: api/activityperiods/5/close — cierra el periodo actual
    [HttpPut("{id}/close")]
    public async Task<ActionResult> ClosePeriod(int id)
    {
        ActivityPeriod? period = await _context.ActivityPeriods.FindAsync(id);

        if (period == null)
            return NotFound(new { error = "Periodo no encontrado", id });

        period.period_end = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Periodo cerrado correctamente",
            period
        });
    }
}