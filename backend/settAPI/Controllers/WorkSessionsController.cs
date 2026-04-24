using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using settAPI.Classes;
using settAPI.Data;
using Microsoft.AspNetCore.SignalR;
using settAPI.Hubs;

namespace settAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkSessionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<MonitoringHub> _hub;

    public WorkSessionsController(AppDbContext context, IHubContext<MonitoringHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    // GET: api/worksessions — devuelve todas las sesiones
    [HttpGet]
    public async Task<ActionResult> GetSessions()
    {
        List<WorkSession> sessions = await _context.WorkSessions
            .Include(s => s.Worker)
            .ToListAsync();
        return Ok(sessions);
    }

    // GET: api/worksessions/5 — devuelve una sesión por id
    [HttpGet("{id}")]
    public async Task<ActionResult> GetSession(int id)
    {
        WorkSession? session = await _context.WorkSessions
            .Include(s => s.Worker)
            .FirstOrDefaultAsync(s => s.id == id);

        if (session == null)
            return NotFound(new { error = "Sesión no encontrada", id });

        return Ok(session);
    }

    // GET: api/worksessions/worker/5 — devuelve todas las sesiones de un worker
    [HttpGet("worker/{workerId}")]
    public async Task<ActionResult> GetSessionsByWorker(int workerId)
    {
        List<WorkSession> sessions = await _context.WorkSessions
            .Where(s => s.worker_id == workerId)
            .ToListAsync();
        return Ok(sessions);
    }

    // POST: api/worksessions — abre una nueva sesión (el desktop la llama al arrancar)
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult> OpenSession([FromBody] WorkSession session)
    {
        try
        {
            session.started_at = DateTime.UtcNow;

            await _context.WorkSessions.AddAsync(session);
            await _context.SaveChangesAsync();
            await _hub.Clients.All.SendAsync("SesionAbierta", session);   // notifica al dashboard que un worker ha iniciado sesión

            return Ok(new
            {
                mensaje = "Sesión abierta correctamente",
                session
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = "Error al abrir la sesión",
                detalle = ex.Message
            });
        }
    }

    // PUT: api/worksessions/5/close — cierra una sesión activa (el desktop la llama al apagarse)
    [AllowAnonymous]
    [HttpPut("{id}/close")]
    public async Task<ActionResult> CloseSession(int id)
    {
        WorkSession? session = await _context.WorkSessions.FindAsync(id);

        if (session == null)
            return NotFound(new { error = "Sesión no encontrada", id });

        session.ended_at = DateTime.UtcNow;
        session.total_minutes = (int)(session.ended_at.Value - session.started_at).TotalMinutes;

        await _context.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("SesionCerrada", session);   // notifica al dashboard que un worker ha cerrado sesión

        return Ok(new
        {
            mensaje = "Sesión cerrada correctamente",
            session
        });
    }
}