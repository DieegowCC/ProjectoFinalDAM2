using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using settAPI.Classes;
using settAPI.Data;

namespace settAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ApplicationsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/applications — devuelve todas las aplicaciones registradas
    [HttpGet]
    public async Task<ActionResult> GetApplications()
    {
        List<Application> applications = await _context.Applications.ToListAsync();
        return Ok(applications);
    }

    // GET: api/applications/5 — devuelve una aplicación por id
    [HttpGet("{id}")]
    public async Task<ActionResult> GetApplication(int id)
    {
        Application? application = await _context.Applications.FindAsync(id);

        if (application == null)
            return NotFound(new { error = "Aplicación no encontrada", id });

        return Ok(application);
    }

    // POST: api/applications — registra una nueva aplicación detectada por el desktop
    [HttpPost]
    public async Task<ActionResult> CreateApplication([FromBody] Application application)
    {
        try
        {
            await _context.Applications.AddAsync(application);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Aplicación registrada correctamente",
                application
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = "Error al registrar la aplicación",
                detalle = ex.Message
            });
        }
    }

    // PUT: api/applications/5 — actualiza los datos de una aplicación
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateApplication(int id, [FromBody] Application applicationActualizada)
    {
        Application? application = await _context.Applications.FindAsync(id);

        if (application == null)
            return NotFound(new { error = "Aplicación no encontrada", id });

        application.process_name = applicationActualizada.process_name;
        application.display_name = applicationActualizada.display_name;
        application.category = applicationActualizada.category;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Aplicación actualizada correctamente",
            application
        });
    }

    // DELETE: api/applications/5 — elimina una aplicación del registro
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteApplication(int id)
    {
        Application? application = await _context.Applications.FindAsync(id);

        if (application == null)
            return NotFound(new { error = "Aplicación no encontrada", id });

        _context.Applications.Remove(application);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Aplicación eliminada correctamente",
            id
        });
    }
}