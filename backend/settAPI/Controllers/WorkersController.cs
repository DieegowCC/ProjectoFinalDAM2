using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using settAPI.Classes;
using settAPI.Data;

namespace settAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkersController : ControllerBase
{
    private readonly AppDbContext _context;

    public WorkersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/workers — devuelve todos los workers
    [HttpGet]
    public async Task<ActionResult> GetWorkers()
    {
        List<Worker> workers = await _context.Workers.ToListAsync();
        return Ok(workers);
    }

    // GET: api/workers/5 — devuelve un worker por id
    [HttpGet("{id}")]
    public async Task<ActionResult> GetWorker(int id)
    {
        Worker? worker = await _context.Workers.FindAsync(id);

        if (worker == null)
            return NotFound(new { error = "Worker no encontrado", id });

        return Ok(worker);
    }

    // POST: api/workers — crea un nuevo worker
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

    // PUT: api/workers/5 — actualiza un worker existente
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

    // DELETE: api/workers/5 — desactiva un worker (baja lógica, no borra el registro)
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
}