using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services;

public interface IModerationService
{
    Task<List<VideoDto>> GetPendingVideosAsync();
    Task<bool> ModerateVideoAsync(Guid moderatorId, ModerationRequest request);
    Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId);
    Task<bool> MarkNotificationAsReadAsync(Guid notificationId, Guid userId);
}

public class ModerationService : IModerationService
{
    private readonly AppDbContext _context;
    
    public ModerationService(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<VideoDto>> GetPendingVideosAsync()
    {
        var videos = await _context.Videos
            .Include(v => v.Author)
            .Include(v => v.Likes)
            .Where(v => v.Status == "pending" || v.Status == "processing")
            .OrderBy(v => v.UploadedAt)
            .ToListAsync();
        
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
    
    public async Task<bool> ModerateVideoAsync(Guid moderatorId, ModerationRequest request)
    {
        var video = await _context.Videos
            .Include(v => v.Moderation)
            .Include(v => v.Author)
            .FirstOrDefaultAsync(v => v.Id == request.VideoId);
        
        if (video == null) return false;
        
        var moderator = await _context.Users.FindAsync(moderatorId);
        if (moderator == null || (moderator.Role != "moderator" && moderator.Role != "admin")) return false;
        
        video.Status = request.Approve ? "approved" : "rejected";
        
        if (video.Moderation == null)
        {
            video.Moderation = new Moderation
            {
                VideoId = video.Id,
                ModeratorId = moderatorId,
                Status = request.Approve ? "approved" : "rejected",
                Reason = request.Reason,
                ModeratedAt = DateTime.UtcNow
            };
        }
        else
        {
            video.Moderation.ModeratorId = moderatorId;
            video.Moderation.Status = request.Approve ? "approved" : "rejected";
            video.Moderation.Reason = request.Reason;
            video.Moderation.ModeratedAt = DateTime.UtcNow;
        }
        
        // Create notification for author
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = video.AuthorId,
            Title = request.Approve ? "Видео одобрено" : "Видео отклонено",
            Message = request.Approve 
                ? $"Ваше видео \"{video.Title}\" было одобрено и опубликовано."
                : $"Ваше видео \"{video.Title}\" было отклонено. Причина: {request.Reason ?? "Не указана"}",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();
        
        return notifications.Select(n => new NotificationDto(
            n.Id,
            n.UserId,
            n.Title,
            n.Message,
            n.IsRead,
            n.CreatedAt
        )).ToList();
    }
    
    public async Task<bool> MarkNotificationAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification == null || notification.UserId != userId) return false;
        
        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }
}
