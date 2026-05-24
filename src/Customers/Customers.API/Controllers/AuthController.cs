using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Customers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public record LoginRequestDto(string Email, string Password);

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequestDto request)
    {
        // Mock de validação de usuário (Em um cenário real, buscaria no banco com hash de senha)
        if (string.IsNullOrWhiteSpace(request.Email) || request.Password != "123456")
        {
            return Unauthorized(new { Message = "Credenciais inválidas. (Dica: a senha é 123456)" });
        }

        // Define a Role baseada no e-mail mockado
        var role = request.Email.StartsWith("admin") ? "Admin" : "User";

        // Cria as "Claims" (informações que vão dentro do token)
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, request.Email),
            new Claim(ClaimTypes.Role, role),
            new Claim("CustomerId", Guid.NewGuid().ToString()) // Simulando o ID do usuário
        };

        // Resgata as configurações do appsettings
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Monta o Token JWT
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2), // Token válido por 2 horas
            signingCredentials: creds
        );

        return Ok(new
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Role = role,
            Expires = token.ValidTo
        });
    }
}