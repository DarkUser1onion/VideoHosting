using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services;

public interface IVideoService
{
    Task<List<VideoDto>> GetAllVideosAsync(string? search, string? category);
    Task<VideoDto?> GetVideoByIdAsync(Guid id);
    Task<VideoUploadResponse> UploadVideoAsync(Guid authorId, VideoCreateRequest request, IFormFile file, string uploadsPath);
    Task<bool> UpdateVideoAsync(Guid videoId, Guid userId, VideoUpdateRequest request);
    Task<bool> DeleteVideoAsync(Guid videoId, Guid userId);
    Task IncrementViewsAsync(Guid videoId);
    Task<string> GetVideoStreamPathAsync(Guid videoId);
}

public class VideoService : IVideoService
{
    private readonly AppDbContext _context;
    private readonly IVideoProcessingService _videoProcessing;
    
    public VideoService(AppDbContext context, IVideoProcessingService videoProcessing)
    {
        _context = context;
        _videoProcessing = videoProcessing;
    }
    
    public async Task<List<VideoDto>> GetAllVideosAsync(string? search, string? category)
    {
        var query = _context.Videos
            .Include(v => v.Author)
            .Include(v => v.Likes)
            .Where(v => v.Status == "approved");
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(v => v.Title.Contains(search) || v.Description.Contains(search));
        }
        
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(v => v.Category == category);
        }
        
        var videos = await query.OrderByDescending(v => v.UploadedAt).ToListAsync();
        
        return videos.Select(v => new VideoDto(
            v.Id,
            v.AuthorId,
            v.Author.Login,
            v.Title,
            v.Description,
            v.Category,
            v.Tags,
            $"/api/video/{v.Id}/preview",
            v.Duration,
            v.Views,
            v.Status,
            v.UploadedAt,
            v.Likes.Count(l => l.IsLike),
            v.Likes.Count(l => !l.IsLike)
        )).ToList();
    }
    
    public async Task<VideoDto?> GetVideoByIdAsync(Guid id)
    {
        var video = await _context.Videos
            .Include(v => v.Author)
            .Include(v => v.Likes)
            .FirstOrDefaultAsync(v => v.Id == id);
        
        if (video == null) return null;
        
        return new VideoDto(
            video.Id,
            video.AuthorId,
            video.Author.Login,
            video.Title,
            video.Description,
            video.Category,
            video.Tags,
            $"/api/video/{video.Id}/preview",
            video.Duration,
            video.Views,
            video.Status,
            video.UploadedAt,
            video.Likes.Count(l => l.IsLike),
            video.Likes.Count(l => !l.IsLike)
        );
    }
    
    public async Task<VideoUploadResponse> UploadVideoAsync(Guid authorId, VideoCreateRequest request, IFormFile file, string uploadsPath)
    {
        var videoId = Guid.NewGuid();
        var videoFolder = Path.Combine(uploadsPath, videoId.ToString());
        Directory.CreateDirectory(videoFolder);
        
        var originalPath = Path.Combine(videoFolder, $"original{Path.GetExtension(file.FileName)}");
        
        using (var stream = new FileStream(originalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        
        var video = new Video
        {
            Id = videoId,
            AuthorId = authorId,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Tags = request.Tags,
            FilePath = originalPath,
            Status = "processing",
            UploadedAt = DateTime.UtcNow
        };
        
        _context.Videos.Add(video);
        
        // Create moderation entry
        var moderation = new Moderation
        {
            VideoId = videoId,
            Status = "pending"
        };
        _context.Moderations.Add(moderation);
        
        await _context.SaveChangesAsync();
        
        // Start async processing
        _ = Task.Run(async () => await _videoProcessing.ProcessVideoAsync(videoId, originalPath, videoFolder));
        
        return new VideoUploadResponse(videoId, "Video uploaded and processing started");
    }
    
    public async Task<bool> UpdateVideoAsync(Guid videoId, Guid userId, VideoUpdateRequest request)
    {
        var video = await _context.Videos.FindAsync(videoId);
        if (video == null || video.AuthorId != userId) return false;
        
        if (request.Title != null) video.Title = request.Title;
        if (request.Description != null) video.Description = request.Description;
        if (request.Category != null) video.Category = request.Category;
        if (request.Tags != null) video.Tags = request.Tags;
        
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> DeleteVideoAsync(Guid videoId, Guid userId)
    {
        var video = await _context.Videos.FindAsync(videoId);
        if (video == null) return false;
        
        // Check if user is author or moderator
        var user = await _context.Users.FindAsync(userId);
        if (video.AuthorId != userId && user?.Role != "moderator" && user?.Role != "admin") return false;
        
        _context.Videos.Remove(video);
        await _context.SaveChangesAsync();
        
        // Delete files
        if (Directory.Exists(Path.GetDirectoryName(video.FilePath)))
        {
            Directory.Delete(Path.GetDirectoryName(video.FilePath)!, true);
        }
        
        return true;
    }
    
    public async Task IncrementViewsAsync(Guid videoId)
    {
        var video = await _context.Videos.FindAsync(videoId);
        if (video != null)
        {
            video.Views++;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<string> GetVideoStreamPathAsync(Guid videoId)
    {
        var video = await _context.Videos.FindAsync(videoId);
        if (video == null) throw new FileNotFoundException("Video not found");
        
        var hlsPath = Path.Combine(Path.GetDirectoryName(video.FilePath)!, "playlist.m3u8");
        if (!File.Exists(hlsPath)) throw new FileNotFoundException("HLS playlist not found");
        
        return hlsPath;
    }
}
