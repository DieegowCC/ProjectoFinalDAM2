using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using settAPI.Classes;
using settAPI.Data;

namespace settAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration; 

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // POST: api/auth/login — recibe usuario y contraseña y devuelve un JWT
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginRequest request)
    {
        Admin? admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.username == request.Username);

        if (admin == null || !BCrypt.Net.BCrypt.Verify(request.Password, admin.password_hash))
            return Unauthorized(new { error = "Credenciales incorrectas" });

        string token = GenerarToken(admin);

        return Ok(new
        {
            mensaje = "Login correcto",
            token
        });
    }

    private string GenerarToken(Admin admin)
    {
        string jwtKey = _configuration["Jwt:Key"]!;                       // clave secreta definida en appsettings.json
        string jwtIssuer = _configuration["Jwt:Issuer"]!;                    // emisor del token

        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, admin.id.ToString()),        // id del admin dentro del token
            new Claim(ClaimTypes.Name, admin.username)                        // username dentro del token
        };

        JwtSecurityToken token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),                  // el token expira en 8 horas
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// Modelo que recibe el endpoint de login
public class LoginRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}