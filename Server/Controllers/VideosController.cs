using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services;
using System.Security.Claims;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideosController : ControllerBase
{
    private readonly IVideoService _videoService;
    private readonly IConfiguration _configuration;
    
    public VideosController(IVideoService videoService, IConfiguration configuration)
    {
        _videoService = videoService;
        _configuration = configuration;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetVideos([FromQuery] string? search, [FromQuery] string? category)
    {
        var videos = await _videoService.GetAllVideosAsync(search, category);
        return Ok(videos);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetVideo(Guid id)
    {
        var video = await _videoService.GetVideoByIdAsync(id);
        if (video == null) return NotFound();
        
        await _videoService.IncrementViewsAsync(id);
        return Ok(video);
    }
    
    [HttpGet("{id}/preview")]
    public async Task<IActionResult> GetPreview(Guid id)
    {
        var video = await _videoService.GetVideoByIdAsync(id);
        if (video == null) return NotFound();
        
        var uploadsPath = _configuration["UploadsPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        var previewPath = Path.Combine(uploadsPath, id.ToString(), "preview.jpg");
        
        if (!System.IO.File.Exists(previewPath))
        {
            // Return a default placeholder image
            return File(Array.Empty<byte>(), "image/jpeg");
        }
        
        var fileBytes = await System.IO.File.ReadAllBytesAsync(previewPath);
        return File(fileBytes, "image/jpeg");
    }
    
    [HttpGet("{id}/stream")]
    public async Task<IActionResult> StreamVideo(Guid id)
    {
        try
        {
            var uploadsPath = _configuration["UploadsPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            var hlsPath = await _videoService.GetVideoStreamPathAsync(id);
            
            var fileBytes = await System.IO.File.ReadAllBytesAsync(hlsPath);
            return File(fileBytes, "application/vnd.apple.mpegurl");
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }
    
    [HttpGet("{id}/stream/{segment}")]
    public async Task<IActionResult> GetSegment(Guid id, string segment)
    {
        try
        {
            var video = await _videoService.GetVideoByIdAsync(id);
            if (video == null) return NotFound();
            
            var uploadsPath = _configuration["UploadsPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            var segmentPath = Path.Combine(uploadsPath, id.ToString(), segment);
            
            if (!System.IO.File.Exists(segmentPath))
                return NotFound();
            
            var fileBytes = await System.IO.File.ReadAllBytesAsync(segmentPath);
            return File(fileBytes, "video/MP2T");
        }
        catch
        {
            return NotFound();
        }
    }
    
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UploadVideo([FromForm] VideoUploadRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var uploadsPath = _configuration["UploadsPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        
        Directory.CreateDirectory(uploadsPath);
        
        var result = await _videoService.UploadVideoAsync(
            userId,
            new VideoCreateRequest(request.Title, request.Description, request.Category, request.Tags ?? new List<string>()),
            request.File,
            uploadsPath
        );
        
        return Ok(result);
    }
    
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVideo(Guid id, [FromBody] VideoUpdateRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var success = await _videoService.UpdateVideoAsync(id, userId, request);
        
        if (!success) return NotFound();
        return Ok();
    }
    
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVideo(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        var success = await _videoService.DeleteVideoAsync(id, userId);
        
        if (!success) return NotFound();
        return Ok();
    }
}

// Form model for upload
public class VideoUploadRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string>? Tags { get; set; }
    public IFormFile File { get; set; } = null!;
}
