using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using settAPI.Classes;
using settAPI.Data;
using Microsoft.AspNetCore.SignalR;
using settAPI.Hubs;

namespace settAPI.Controllers;

// Controlador de WorkSessions (sesiones de trabajo).
//
// Una WorkSession representa un periodo entre que el agente arranca y se cierra
// en un PC concreto. La crea y la cierra el agente; el dashboard solo la observa
// a través de los eventos SignalR ("SesionAbierta" / "SesionCerrada") y de los
// endpoints de /api/stats. Por eso este controlador solo expone POST y PUT close:
// los GET listados y por id no los consume nadie.
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

    // POST /api/worksessions
    // El agente la llama al arrancar mandando { worker_id }.
    // La API pone started_at = ahora (UTC), guarda y emite "SesionAbierta" por SignalR
    // para que el dashboard refresque sus métricas.
    // [AllowAnonymous] porque el agente no maneja JWT.
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult> OpenSession([FromBody] WorkSession session)
    {
        try
        {
            session.started_at = DateTime.UtcNow;

            await _context.WorkSessions.AddAsync(session);
            await _context.SaveChangesAsync();
            await _hub.Clients.All.SendAsync("SesionAbierta", session);

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

    // PUT /api/worksessions/{id}/close
    // El agente la llama al cerrarse (Ctrl+C, apagado, etc.).
    // La API pone ended_at = ahora, calcula total_minutes y emite "SesionCerrada".
    // [AllowAnonymous] por el mismo motivo que el POST.
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
        await _hub.Clients.All.SendAsync("SesionCerrada", session);

        return Ok(new
        {
            mensaje = "Sesión cerrada correctamente",
            session
        });
    }
}
