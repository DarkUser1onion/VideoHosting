using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services;
using System.Security.Claims;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LikesController : ControllerBase
{
    private readonly IInteractionService _interactionService;
    
    public LikesController(IInteractionService interactionService)
    {
        _interactionService = interactionService;
    }
    
    [HttpGet("status/{videoId}")]
    [Authorize]
    public async Task<IActionResult> GetLikeStatus(Guid videoId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Ok(new { hasLike = (bool?)null });
        
        var userId = Guid.Parse(userIdClaim);
        var status = await _interactionService.GetUserLikeStatusAsync(userId, videoId);
        
        return Ok(new { hasLike = status });
    }
    
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> SetLike([FromBody] LikeRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var like = await _interactionService.SetLikeAsync(userId, request);
        
        if (like == null) return NotFound();
        return Ok(like);
    }
    
    [Authorize]
    [HttpDelete("{videoId}")]
    public async Task<IActionResult> RemoveLike(Guid videoId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var success = await _interactionService.RemoveLikeAsync(userId, videoId);
        
        if (!success) return NotFound();
        return Ok();
    }
}
