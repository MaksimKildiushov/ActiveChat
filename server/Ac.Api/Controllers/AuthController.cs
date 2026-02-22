using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ac.Api.Models.Auth;
using Ac.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Ac.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    UserManager<UserEntity> userManager,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>Регистрация нового пользователя.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var user = new UserEntity
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Created = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "User registered successfully." });
    }

    /// <summary>Вход — возвращает JWT-токен.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { error = "Invalid email or password." });

        var (token, expiresAt) = GenerateJwtToken(user);

        return Ok(new AuthResponse(
            Token: token,
            Email: user.Email!,
            DisplayName: user.DisplayName,
            ExpiresAt: expiresAt));
    }

    private (string token, DateTime expiresAt) GenerateJwtToken(UserEntity user)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresInMinutes = int.TryParse(jwtSettings["ExpiresInMinutes"], out var mins) ? mins : 60;
        var expiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
