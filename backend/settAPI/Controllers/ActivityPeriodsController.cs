using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using settAPI.Classes;
using settAPI.Data;
using Microsoft.AspNetCore.SignalR;
using settAPI.Hubs;
using Microsoft.AspNetCore.Authorization;

namespace settAPI.Controllers;

// Controlador de ActivityPeriods — los tramos de "estado del worker":
// "active" (hay actividad de teclado/ratón) o "idle" (no hay actividad).
//
// El agente crea un periodo nuevo cada vez que cambia el estado del usuario, y
// cierra el anterior. El dashboard usa el último periodo de cada sesión abierta
// para decidir si un worker está "Activo" o "Ausente".
[Authorize]
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

    // POST /api/activityperiods
    // El agente la llama al cambiar de estado, mandando { session_id, status }.
    // status puede ser "active", "idle" o "break".
    // [AllowAnonymous] porque el agente no maneja JWT.
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult> CreatePeriod([FromBody] ActivityPeriod period)
    {
        try
        {
            period.period_start = DateTime.UtcNow;

            await _context.ActivityPeriods.AddAsync(period);
            await _context.SaveChangesAsync();

            // Cargamos la sesión a mano por el mismo motivo que en AppActivityController:
            // las navigation properties con FK snake_case no se rellenan solas.
            WorkSession? session = await _context.WorkSessions.FindAsync(period.session_id);

            // DTO plano con solo lo que el dashboard necesita para reaccionar
            // (worker_id para saber qué fila refrescar; status para saber si pintar
            //  Activo o Ausente).
            var payload = new
            {
                id = period.id,
                session_id = period.session_id,
                status = period.status,
                workSession = session == null ? null : new { worker_id = session.worker_id }
            };

            await _hub.Clients.All.SendAsync("NuevoPeriodo", payload);

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

    // PUT /api/activityperiods/{id}/close
    // Cierra el periodo actual cuando el agente detecta un cambio de estado.
    [AllowAnonymous]
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
