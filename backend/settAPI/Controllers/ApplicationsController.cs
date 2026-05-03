using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using settAPI.Classes;
using settAPI.Data;

namespace settAPI.Controllers;

// Controlador de Applications — el catálogo de programas detectados (Chrome,
// Visual Studio Code, etc.). Cada AppActivity referencia una entrada de aquí.
//
// Solo lo usa el agente:
//   - GET para buscar si una app ya está registrada antes de crearla.
//   - POST para registrarla si no existía.
// El frontend nunca lo toca directamente — ve los nombres a través de /api/stats.
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ApplicationsController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/applications
    // Devuelve TODAS las apps registradas. El agente la usa al detectar una nueva
    // ventana: primero comprueba si ya está en el catálogo y, si no, la crea.
    // [AllowAnonymous] porque el agente no maneja JWT.
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult> GetApplications()
    {
        List<Application> applications = await _context.Applications.ToListAsync();
        return Ok(applications);
    }

    // POST /api/applications
    // El agente la llama cuando detecta una app que no estaba en el catálogo.
    // Manda { process_name, display_name }.
    [AllowAnonymous]
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
}
