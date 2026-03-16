using Microsoft.AspNetCore.Mvc;
using Phoenix.Application.Services;

namespace Phoenix.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        var result = await _authService.RegisterUserAsync(request);
        if (result == null)
        {
            return BadRequest(new { message = "Email is already in use." });
        }

        return Ok(new { message = "User registered successfully." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var token = await _authService.LoginAsync(request);
        if (token == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(new { token });
    }
}
