using System.Security.Claims;
using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using EdgeTech.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly PasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IJwtService _jwt;

    public AuthController(MongoDbContext db, IJwtService jwt)
    {
        _db = db;
        _passwordHasher = new PasswordHasher<ApplicationUser>();
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var normalizedEmail = req.Email.Trim().ToLowerInvariant();
        var existing = await _db.Users.Find(u => u.Email == normalizedEmail).FirstOrDefaultAsync();
        if (existing != null)
            return BadRequest(new { errors = new[] { "Email is already in use" } });

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FirstName = req.FirstName,
            LastName = req.LastName,
            Role = "User",
            EmailConfirmed = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, req.Password);

        await _db.Users.InsertOneAsync(user);

        var roles = new List<string> { user.Role };
        var token = _jwt.GenerateAccessToken(user, roles);
        var refreshToken = _jwt.GenerateRefreshToken();

        return Ok(new AuthResponse(token, refreshToken, DateTime.UtcNow.AddHours(1),
            new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role)));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var normalizedEmail = req.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Find(u => u.Email == normalizedEmail).FirstOrDefaultAsync();
        if (user == null) return Unauthorized(new { message = "Invalid credentials" });

        var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        if (verify == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid credentials" });

        var roles = new List<string> { user.Role };
        var token = _jwt.GenerateAccessToken(user, roles);
        var refreshToken = _jwt.GenerateRefreshToken();

        return Ok(new AuthResponse(token, refreshToken, DateTime.UtcNow.AddHours(1),
            new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role)));
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null) return NotFound();

        return Ok(new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role));
    }
}
