using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Server.Models;
using Server.Services;
using System.Security.Claims;

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
        var (success, message, user) = await _authService.RegisterAsync(
            request.Login,
            request.Password,
            request.RegisterAsModerator,
            request.ModeratorPassword
        );
        
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

    [Authorize(Roles = "admin")]
    [HttpPost("create-moderator")]
    public async Task<IActionResult> CreateModerator([FromBody] CreateModeratorRequest request)
    {
        var (success, message, user) = await _authService.CreateModeratorAsync(request.Login, request.Password);
        if (!success)
            return BadRequest(new { message });

        return Ok(new UserDto(user!.Id, user.Login, user.Role, user.CreatedAt));
    }
    
    // User management for moderators/admins
    [Authorize(Roles = "moderator,admin")]
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var requesterId = Guid.Parse(userIdClaim);
        var users = await _authService.GetAllUsersAsync(requesterId);
        return Ok(users);
    }
    
    [Authorize(Roles = "moderator,admin")]
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var requesterId = Guid.Parse(userIdClaim);
        var success = await _authService.DeleteUserAsync(id, requesterId);
        if (!success)
            return BadRequest(new { message = "Недостаточно прав для удаления пользователя" });
        return Ok(new { message = "Пользователь удалён" });
    }
    
    [Authorize(Roles = "moderator,admin")]
    [HttpPut("videos/{id}")]
    public async Task<IActionResult> ModerateUpdateVideo(Guid id, [FromBody] VideoUpdateRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var success = await _authService.ModerateUpdateVideoAsync(id, userId, request);
        if (!success)
            return NotFound(new { message = "Видео не найдено" });
        return Ok(new { message = "Видео обновлено" });
    }
}
