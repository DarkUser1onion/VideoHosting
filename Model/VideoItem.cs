using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;

namespace VideoHostingByWhoami.Model;

public class VideoItem : INotifyPropertyChanged
{
    private int _likes;
    private int _dislikes;
    private int _views;
    private int _duration;
    
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string PreviewUrl { get; set; } = string.Empty;
    public Bitmap? PreviewImage { get; set; }
    
    public int Duration
    {
        get => _duration;
        set { _duration = value; OnPropertyChanged(); OnPropertyChanged(nameof(DurationFormatted)); }
    }
    
    public int Views
    {
        get => _views;
        set { _views = value; OnPropertyChanged(); OnPropertyChanged(nameof(ViewsFormatted)); }
    }
    
    public string Status { get; set; } = "pending";
    public DateTime UploadedAt { get; set; }
    
    public int Likes
    {
        get => _likes;
        set { _likes = value; OnPropertyChanged(); }
    }
    
    public int Dislikes
    {
        get => _dislikes;
        set { _dislikes = value; OnPropertyChanged(); }
    }
    
    public string DurationFormatted => TimeSpan.FromSeconds(Duration).ToString(@"mm\:ss");
    public string ViewsFormatted => Views >= 1000 ? $"{Views / 1000}K" : Views.ToString();
    public string CategoryDisplay => Category switch
    {
        "education" => "Образование",
        "entertainment" => "Развления",
        "technology" => "Технологии",
        "music" => "Музыка",
        "sport" => "Спорт",
        "games" => "Игры",
        "news" => "Новости",
        "other" => "Другое",
        _ => Category
    };
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
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

public class UserItem
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public string RoleDisplay => Role switch
    {
        "admin" => "Администратор",
        "moderator" => "Модератор",
        "user" => "Пользователь",
        _ => Role
    };
}
