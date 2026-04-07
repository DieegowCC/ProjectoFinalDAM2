using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using settAPI.Classes;
using settAPI.Data;

namespace settAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    // POST: api/admin
    [HttpPost]
    public async Task<ActionResult> CreateAdmin([FromBody] Admin admin)
    {
        try
        {
            admin.created_at = DateTime.UtcNow;

            await _context.Admins.AddAsync(admin);
            await _context.SaveChangesAsync(); 

            return Ok(new
            {
                mensaje = "Admin creado correctamente",
                admin
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

    // GET: api/admin
    [HttpGet]
    public async Task<ActionResult> GetAdmins()
    {
        var admins = await _context.Admins.ToListAsync();
        return Ok(admins);
    }
}