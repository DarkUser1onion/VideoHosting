using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services;
using System.Security.Claims;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly IInteractionService _interactionService;
    
    public CommentsController(IInteractionService interactionService)
    {
        _interactionService = interactionService;
    }
    
    [HttpGet("video/{videoId}")]
    public async Task<IActionResult> GetComments(Guid videoId)
    {
        var comments = await _interactionService.GetCommentsAsync(videoId);
        return Ok(comments);
    }
    
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddComment([FromBody] CommentCreateRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var comment = await _interactionService.AddCommentAsync(userId, request);
        
        if (comment == null) return NotFound();
        return Ok(comment);
    }
    
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateComment(Guid id, [FromBody] CommentUpdateRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var success = await _interactionService.UpdateCommentAsync(id, userId, request);
        
        if (!success) return NotFound();
        return Ok();
    }
    
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComment(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var success = await _interactionService.DeleteCommentAsync(id, userId);
        
        if (!success) return NotFound();
        return Ok();
    }
}
