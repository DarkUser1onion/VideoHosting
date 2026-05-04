namespace Server.Models;

// Auth DTOs
public record RegisterRequest(string Login, string Password);
public record LoginRequest(string Login, string Password);
public record AuthResponse(string Token, UserDto User);

// User DTOs
public record UserDto(Guid Id, string Login, string Role, DateTime CreatedAt);
public record UserUpdateRequest(string? Login, string? Password);

// Video DTOs
public record VideoCreateRequest(string Title, string Description, string Category, List<string> Tags);
public record VideoUpdateRequest(string? Title, string? Description, string? Category, List<string>? Tags);
public record VideoDto(
    Guid Id, 
    Guid AuthorId, 
    string AuthorName,
    string Title, 
    string Description, 
    string Category, 
    List<string> Tags, 
    string PreviewUrl,
    int Duration, 
    int Views, 
    string Status, 
    DateTime UploadedAt,
    int Likes,
    int Dislikes
);

// Comment DTOs
public record CommentCreateRequest(Guid VideoId, string Content);
public record CommentUpdateRequest(string Content);
public record CommentDto(
    Guid Id, 
    Guid VideoId, 
    Guid UserId, 
    string UserName,
    string Content, 
    DateTime CreatedAt
);

// Like DTOs
public record LikeRequest(Guid VideoId, bool IsLike);
public record LikeDto(Guid Id, Guid VideoId, Guid UserId, bool IsLike);

// Playlist DTOs
public record PlaylistCreateRequest(string Name);
public record PlaylistAddVideoRequest(Guid VideoId);
public record PlaylistDto(
    Guid Id, 
    Guid UserId, 
    string Name, 
    DateTime CreatedAt,
    List<VideoDto> Videos
);

// Moderation DTOs
public record ModerationRequest(Guid VideoId, bool Approve, string? Reason);
public record ModerationDto(
    Guid VideoId, 
    Guid ModeratorId,
    string Status, 
    string? Reason, 
    DateTime ModeratedAt
);

// Notification DTOs
public record NotificationDto(
    Guid Id, 
    Guid UserId, 
    string Title, 
    string Message, 
    bool IsRead, 
    DateTime CreatedAt
);

// Video upload response
public record VideoUploadResponse(Guid VideoId, string Message);
