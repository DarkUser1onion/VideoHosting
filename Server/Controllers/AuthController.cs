using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services;

namespace Server.Controllers;

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
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (success, message, user) = await _authService.RegisterAsync(request.Login, request.Password);
        
        if (!success)
            return BadRequest(new { message });
        
        var token = _authService.GenerateJwtToken(user!);
        
        return Ok(new AuthResponse(token, new UserDto(user!.Id, user.Login, user.Role, user.CreatedAt)));
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, token, user) = await _authService.LoginAsync(request.Login, request.Password);
        
        if (!success)
            return Unauthorized(new { message = "Invalid login or password" });
        
        return Ok(new AuthResponse(token!, new UserDto(user!.Id, user.Login, user.Role, user.CreatedAt)));
    }
}
