using System;
using System.Collections.Generic;

namespace VideoHostingByWhoami.Model;

public class VideoItem
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string PreviewUrl { get; set; } = string.Empty;
    public int Duration { get; set; }
    public int Views { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime UploadedAt { get; set; }
    public int Likes { get; set; }
    public int Dislikes { get; set; }
    
    public string DurationFormatted => TimeSpan.FromSeconds(Duration).ToString(@"mm\:ss");
    public string ViewsFormatted => Views >= 1000 ? $"{Views / 1000}K" : Views.ToString();
}

public class Comment
{
    public Guid Id { get; set; }
    public Guid VideoId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public string TimeAgo
    {
        get
        {
            var diff = DateTime.UtcNow - CreatedAt;
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} мин. назад";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} ч. назад";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} дн. назад";
            return CreatedAt.ToString("dd.MM.yyyy");
        }
    }
}

public class Playlist
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<VideoItem> Videos { get; set; } = new();
}

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
