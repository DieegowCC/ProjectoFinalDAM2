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

    // GET: api/admin
    [HttpGet]
    public async Task<ActionResult> GetAdmins()
    {
        List<Admin> admins = await _context.Admins.ToListAsync();
        return Ok(admins);
    }

    // GET: api/admin/5
    [HttpGet("{id}")]
    public async Task<ActionResult> GetAdmin(int id)
    {
        Admin? admin = await _context.Admins.FindAsync(id);

        if (admin == null)
            return NotFound(new { error = "Admin no encontrado", id });

        return Ok(admin);
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

    // PUT: api/admin/5
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAdmin(int id, [FromBody] Admin adminActualizado)
    {
        Admin? admin = await _context.Admins.FindAsync(id);

        if (admin == null)
            return NotFound(new { error = "Admin no encontrado", id });

        admin.username = adminActualizado.username;
        admin.password_hash = adminActualizado.password_hash;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Admin actualizado correctamente",
            admin
        });
    }

    // DELETE: api/admin/5
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAdmin(int id)
    {
        Admin? admin = await _context.Admins.FindAsync(id);

        if (admin == null)
            return NotFound(new { error = "Admin no encontrado", id });

        _context.Admins.Remove(admin);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Admin eliminado correctamente",
            id
        });
    }
}