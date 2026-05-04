using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services;
using System.Security.Claims;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModerationController : ControllerBase
{
    private readonly IModerationService _moderationService;
    
    public ModerationController(IModerationService moderationService)
    {
        _moderationService = moderationService;
    }
    
    [Authorize(Roles = "moderator,admin")]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingVideos()
    {
        var videos = await _moderationService.GetPendingVideosAsync();
        return Ok(videos);
    }
    
    [Authorize(Roles = "moderator,admin")]
    [HttpPost]
    public async Task<IActionResult> ModerateVideo([FromBody] ModerationRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var success = await _moderationService.ModerateVideoAsync(userId, request);
        
        if (!success) return BadRequest();
        return Ok();
    }
    
    [Authorize]
    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var notifications = await _moderationService.GetUserNotificationsAsync(userId);
        return Ok(notifications);
    }
    
    [Authorize]
    [HttpPost("notifications/{notificationId}/read")]
    public async Task<IActionResult> MarkNotificationAsRead(Guid notificationId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var success = await _moderationService.MarkNotificationAsReadAsync(notificationId, userId);
        
        if (!success) return NotFound();
        return Ok();
    }
}
