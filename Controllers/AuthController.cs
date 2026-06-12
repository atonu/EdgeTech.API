using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using EdgeTech.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtService _jwt;

    public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IJwtService jwt)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            FirstName = req.FirstName,
            LastName = req.LastName,
            EmailConfirmed = true
        };
        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, "User");
        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwt.GenerateAccessToken(user, roles);
        var refreshToken = _jwt.GenerateRefreshToken();

        return Ok(new AuthResponse(token, refreshToken, DateTime.UtcNow.AddHours(1),
            new UserDto(user.Id, user.Email!, user.FirstName, user.LastName, roles.FirstOrDefault() ?? "User")));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null) return Unauthorized(new { message = "Invalid credentials" });

        var result = await _signInManager.CheckPasswordSignInAsync(user, req.Password, false);
        if (!result.Succeeded) return Unauthorized(new { message = "Invalid credentials" });

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwt.GenerateAccessToken(user, roles);
        var refreshToken = _jwt.GenerateRefreshToken();

        return Ok(new AuthResponse(token, refreshToken, DateTime.UtcNow.AddHours(1),
            new UserDto(user.Id, user.Email!, user.FirstName, user.LastName, roles.FirstOrDefault() ?? "User")));
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserDto(user.Id, user.Email!, user.FirstName, user.LastName, roles.FirstOrDefault() ?? "User"));
    }
}
