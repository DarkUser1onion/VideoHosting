using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services;
using System.Security.Claims;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlaylistsController : ControllerBase
{
    private readonly IInteractionService _interactionService;
    
    public PlaylistsController(IInteractionService interactionService)
    {
        _interactionService = interactionService;
    }
    
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetUserPlaylists()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var playlists = await _interactionService.GetUserPlaylistsAsync(userId);
        return Ok(playlists);
    }
    
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreatePlaylist([FromBody] PlaylistCreateRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var playlist = await _interactionService.CreatePlaylistAsync(userId, request);
        
        return Ok(playlist);
    }
    
    [Authorize]
    [HttpPost("{playlistId}/videos")]
    public async Task<IActionResult> AddVideoToPlaylist(Guid playlistId, [FromBody] PlaylistAddVideoRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var success = await _interactionService.AddVideoToPlaylistAsync(playlistId, userId, request.VideoId);
        
        if (!success) return BadRequest();
        return Ok();
    }
    
    [Authorize]
    [HttpDelete("{playlistId}/videos/{videoId}")]
    public async Task<IActionResult> RemoveVideoFromPlaylist(Guid playlistId, Guid videoId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var success = await _interactionService.RemoveVideoFromPlaylistAsync(playlistId, userId, videoId);
        
        if (!success) return NotFound();
        return Ok();
    }
    
    [Authorize]
    [HttpDelete("{playlistId}")]
    public async Task<IActionResult> DeletePlaylist(Guid playlistId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var success = await _interactionService.DeletePlaylistAsync(playlistId, userId);
        
        if (!success) return NotFound();
        return Ok();
    }
}
