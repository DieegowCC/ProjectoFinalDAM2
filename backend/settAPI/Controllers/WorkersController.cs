using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using settAPI.Classes;
using settAPI.Data;

namespace settAPI.Controllers;

// Controlador del CRUD de Workers (los empleados monitorizados).
//
// Lo usan dos clientes:
//   1. El frontend (dashboard) para listar/crear/editar/desactivar workers — necesita JWT.
//   2. El agente desktop (al arrancar) para resolver su propio Worker por hostname —
//      sin JWT, por eso ese único endpoint lleva [AllowAnonymous].
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkersController : ControllerBase
{
    private readonly AppDbContext _context;

    public WorkersController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/workers
    // Devuelve la lista completa de workers (activos e inactivos).
    // El frontend la usa en /workers para pintar la tabla de gestión.
    [HttpGet]
    public async Task<ActionResult> GetWorkers()
    {
        List<Worker> workers = await _context.Workers.ToListAsync();
        return Ok(workers);
    }

    // POST /api/workers
    // Crea un worker nuevo. El frontend manda el JSON con { name, email, hostname, department }.
    // La API rellena created_at y deja el worker activo por defecto.
    [HttpPost]
    public async Task<ActionResult> CreateWorker([FromBody] Worker worker)
    {
        try
        {
            worker.created_at = DateTime.UtcNow;
            worker.is_active = true;

            await _context.Workers.AddAsync(worker);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Worker creado correctamente",
                worker
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = "Error al guardar en la base de datos",
                detalle = ex.Message
            });
        }
    }

    // PUT /api/workers/{id}
    // Actualiza los datos de un worker existente (nombre, email, departamento, activo/inactivo).
    // Reactivar un worker desactivado se hace con este endpoint poniendo is_active = true.
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateWorker(int id, [FromBody] Worker workerActualizado)
    {
        Worker? worker = await _context.Workers.FindAsync(id);

        if (worker == null)
            return NotFound(new { error = "Worker no encontrado", id });

        worker.name = workerActualizado.name;
        worker.email = workerActualizado.email;
        worker.department = workerActualizado.department;
        worker.is_active = workerActualizado.is_active;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Worker actualizado correctamente",
            worker
        });
    }

    // DELETE /api/workers/{id}
    // Baja LÓGICA: marca is_active = false pero no borra la fila.
    // Así no perdemos el histórico de sesiones y actividad asociado a ese worker.
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeactivateWorker(int id)
    {
        Worker? worker = await _context.Workers.FindAsync(id);

        if (worker == null)
            return NotFound(new { error = "Worker no encontrado", id });

        worker.is_active = false;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Worker desactivado correctamente",
            worker
        });
    }

    // GET /api/workers/by-hostname/{hostname}
    // Lo llama el agente desktop al arrancar para saber qué Worker es él.
    // Comparamos en minúsculas para ignorar mayúsculas/minúsculas del hostname de Windows.
    // [AllowAnonymous] porque el agente no maneja JWT.
    [AllowAnonymous]
    [HttpGet("by-hostname/{hostname}")]
    public async Task<ActionResult> GetWorkerByHostname(string hostname)
    {
        Worker? worker = await _context.Workers
            .FirstOrDefaultAsync(w => w.hostname.ToLower() == hostname.ToLower() && w.is_active);

        if (worker == null)
            return NotFound(new
            {
                error = $"Worker no encontrado para hostname '{hostname}'. Registro automático pendiente de implementar."
            });

        return Ok(worker);
    }
}
