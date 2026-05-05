namespace Server.Models;

// User entity
public class User
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<Video> Videos { get; set; } = new List<Video>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
}

// Video entity
public class Video
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string FilePath { get; set; } = string.Empty;
    public string? PreviewPath { get; set; }
    public int Duration { get; set; }
    public int Views { get; set; }
    public string Status { get; set; } = "pending"; // pending, approved, rejected
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    public User Author { get; set; } = null!;
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public Moderation? Moderation { get; set; }
}

// Comment entity
public class Comment
{
    public Guid Id { get; set; }
    public Guid VideoId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Video Video { get; set; } = null!;
    public User User { get; set; } = null!;
}

// Like entity
public class Like
{
    public Guid Id { get; set; }
    public Guid VideoId { get; set; }
    public Guid UserId { get; set; }
    public bool IsLike { get; set; }
    
    public Video Video { get; set; } = null!;
    public User User { get; set; } = null!;
}

// Playlist entity
public class Playlist
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public User User { get; set; } = null!;
    public ICollection<PlaylistVideo> PlaylistVideos { get; set; } = new List<PlaylistVideo>();
}

// PlaylistVideo junction table
public class PlaylistVideo
{
    public Guid Id { get; set; }
    public Guid PlaylistId { get; set; }
    public Guid VideoId { get; set; }
    public int Order { get; set; }
    
    public Playlist Playlist { get; set; } = null!;
    public Video Video { get; set; } = null!;
}

// Moderation entity
public class Moderation
{
    public Guid VideoId { get; set; }
    public Guid? ModeratorId { get; set; }  // Nullable - not set until moderation happens
    public string Status { get; set; } = "pending"; // pending, approved, rejected
    public string? Reason { get; set; }
    public DateTime? ModeratedAt { get; set; }  // Nullable - not set until moderation happens
    
    public Video Video { get; set; } = null!;
    public User? Moderator { get; set; }
}

// Notification entity
public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public User User { get; set; } = null!;
}
