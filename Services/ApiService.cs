using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VideoHostingByWhoami.Model;

namespace VideoHostingByWhoami.Services;

public interface IApiService
{
    string BaseUrl { get; }
    string? Token { get; set; }
    User? CurrentUser { get; set; }
    bool IsAuthenticated => !string.IsNullOrEmpty(Token);
    
    // Auth
    Task<AuthResult> LoginAsync(string login, string password);
    Task<AuthResult> RegisterAsync(string login, string password);
    Task<OperationResult> CreateModeratorAsync(string login, string password);
    
    // Videos
    Task<List<VideoItem>> GetVideosAsync(string? search = null, string? category = null);
    Task<VideoItem?> GetVideoAsync(Guid id);
    Task<VideoPlaybackInfo?> GetVideoPlaybackAsync(Guid id);
    Task<byte[]> GetPreviewAsync(Guid videoId);
    Task<bool> UploadVideoAsync(string title, string description, string category, List<string> tags, string filePath, string? previewFilePath = null);
    Task<bool> DeleteVideoAsync(Guid videoId);
    
    // Comments
    Task<List<Comment>> GetCommentsAsync(Guid videoId);
    Task<bool> AddCommentAsync(Guid videoId, string content);
    Task<bool> DeleteCommentAsync(Guid commentId);
    
    // Likes
    Task<bool?> GetLikeStatusAsync(Guid videoId);
    Task<bool> SetLikeAsync(Guid videoId, bool isLike);
    Task<bool> RemoveLikeAsync(Guid videoId);
    
    // Playlists
    Task<List<Playlist>> GetPlaylistsAsync();
    Task<bool> CreatePlaylistAsync(string name);
    Task<bool> AddVideoToPlaylistAsync(Guid playlistId, Guid videoId);
    Task<bool> RemoveVideoFromPlaylistAsync(Guid playlistId, Guid videoId);
    Task<bool> DeletePlaylistAsync(Guid playlistId);
    
    // Notifications
    Task<List<Notification>> GetNotificationsAsync();
    Task<bool> MarkNotificationReadAsync(Guid notificationId);
    
    // Moderation
    Task<List<VideoItem>> GetPendingVideosAsync();
    Task<bool> ModerateVideoAsync(Guid videoId, bool approve, string? reason = null);
    Task<bool> UpdateVideoAsync(Guid videoId, string? title, string? description, string? category, List<string>? tags);
    
    // User management (moderator/admin only)
    Task<List<UserItem>> GetAllUsersAsync();
    Task<bool> DeleteUserAsync(Guid userId);
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public string BaseUrl { get; } = "http://localhost:5000/api";
    public string? Token { get; set; }
    public User? CurrentUser { get; set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
    
    public ApiService()
    {
        _httpClient = new HttpClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
    
    private void SetAuthHeader()
    {
        if (!string.IsNullOrEmpty(Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
    
    private async Task<T?> GetAsync<T>(string endpoint)
    {
        SetAuthHeader();
        var response = await _httpClient.GetAsync($"{BaseUrl}/{endpoint}");
        if (!response.IsSuccessStatusCode) return default;
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }
    
    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        SetAuthHeader();
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{BaseUrl}/{endpoint}", content);
        if (!response.IsSuccessStatusCode) return default;
        var responseContent = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return default;
        }
        return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
    }

    private async Task<(bool Success, string Message)> PostWithoutResponseAsync<TRequest>(string endpoint, TRequest data)
    {
        SetAuthHeader();
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{BaseUrl}/{endpoint}", content);
        if (response.IsSuccessStatusCode)
        {
            return (true, string.Empty);
        }

        var body = await response.Content.ReadAsStringAsync();
        var message = TryExtractMessage(body) ?? "Ошибка запроса";
        return (false, message);
    }

    private static string? TryExtractMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            var error = JsonSerializer.Deserialize<ApiErrorResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (!string.IsNullOrWhiteSpace(error?.Message))
                return error.Message;
        }
        catch
        {
            // Ignore parse errors and use fallback.
        }

        return null;
    }
    
    private async Task<bool> DeleteAsync(string endpoint)
    {
        SetAuthHeader();
        var response = await _httpClient.DeleteAsync($"{BaseUrl}/{endpoint}");
        return response.IsSuccessStatusCode;
    }
    
    // Auth
    public async Task<AuthResult> LoginAsync(string login, string password)
    {
        var result = await PostAsync<LoginRequest, AuthResponse>("auth/login", new LoginRequest(login, password));
        if (result != null)
        {
            Token = result.Token;
            CurrentUser = result.User;
            return new AuthResult { Success = true, Token = result.Token, User = result.User };
        }
        return new AuthResult { Success = false, Message = "Неверный логин или пароль" };
    }
    
    public async Task<AuthResult> RegisterAsync(string login, string password)
    {
        var json = JsonSerializer.Serialize(new RegisterRequest(login, password, false, null), _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{BaseUrl}/auth/register", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new AuthResult { Success = false, Message = TryExtractMessage(responseContent) ?? "Ошибка регистрации" };
        }

        var result = JsonSerializer.Deserialize<AuthResponse>(responseContent, _jsonOptions);
        if (result != null)
        {
            Token = result.Token;
            CurrentUser = result.User;
            return new AuthResult { Success = true, Token = result.Token, User = result.User };
        }
        return new AuthResult { Success = false, Message = "Ошибка регистрации" };
    }

    public async Task<OperationResult> CreateModeratorAsync(string login, string password)
    {
        var (success, message) = await PostWithoutResponseAsync("auth/create-moderator", new CreateModeratorRequest(login, password));
        return new OperationResult(success, success ? "Модератор создан" : message);
    }
    
    // Videos
    public async Task<List<VideoItem>> GetVideosAsync(string? search = null, string? category = null)
    {
        var endpoint = "videos";
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrEmpty(category)) queryParams.Add($"category={Uri.EscapeDataString(category)}");
        if (queryParams.Any()) endpoint += "?" + string.Join("&", queryParams);
        
        var videos = await GetAsync<List<VideoItem>>(endpoint);
        return videos ?? new List<VideoItem>();
    }
    
    public async Task<VideoItem?> GetVideoAsync(Guid id)
    {
        return await GetAsync<VideoItem>($"videos/{id}");
    }

    public async Task<VideoPlaybackInfo?> GetVideoPlaybackAsync(Guid id)
    {
        return await GetAsync<VideoPlaybackInfo>($"videos/{id}/playback");
    }
    
    public async Task<byte[]> GetPreviewAsync(Guid videoId)
    {
        var response = await _httpClient.GetAsync($"{BaseUrl}/videos/{videoId}/preview");
        if (!response.IsSuccessStatusCode) return Array.Empty<byte>();
        return await response.Content.ReadAsByteArrayAsync();
    }
    
    public async Task<bool> UploadVideoAsync(string title, string description, string category, List<string> tags, string filePath, string? previewFilePath = null)
    {
        SetAuthHeader();

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(title), "Title");
        content.Add(new StringContent(description), "Description");
        content.Add(new StringContent(category), "Category");
        
        foreach (var tag in tags)
        {
            content.Add(new StringContent(tag), "Tags");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Файл видео не найден", filePath);
        }

        await using var fileStream = File.OpenRead(filePath);
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "File", Path.GetFileName(filePath));

        if (!string.IsNullOrWhiteSpace(previewFilePath))
        {
            if (!File.Exists(previewFilePath))
            {
                throw new FileNotFoundException("Файл обложки не найден", previewFilePath);
            }

            // Keep preview as byte content to avoid stream-lifetime issues.
            var previewBytes = await File.ReadAllBytesAsync(previewFilePath);
            var previewContent = new ByteArrayContent(previewBytes);
            previewContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(previewContent, "PreviewFile", Path.GetFileName(previewFilePath));
        }
        
        var response = await _httpClient.PostAsync($"{BaseUrl}/videos", content);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            var message = TryExtractMessage(body) ?? $"Ошибка загрузки (HTTP {(int)response.StatusCode})";
            throw new InvalidOperationException(message);
        }

        return true;
    }
    
    public async Task<bool> DeleteVideoAsync(Guid videoId)
    {
        return await DeleteAsync($"videos/{videoId}");
    }
    
    // Comments
    public async Task<List<Comment>> GetCommentsAsync(Guid videoId)
    {
        var comments = await GetAsync<List<Comment>>($"comments/video/{videoId}");
        return comments ?? new List<Comment>();
    }
    
    public async Task<bool> AddCommentAsync(Guid videoId, string content)
    {
        var result = await PostAsync<CommentRequest, Comment>("comments", new CommentRequest(videoId, content));
        return result != null;
    }
    
    public async Task<bool> DeleteCommentAsync(Guid commentId)
    {
        return await DeleteAsync($"comments/{commentId}");
    }
    
    // Likes
    public async Task<bool?> GetLikeStatusAsync(Guid videoId)
    {
        var result = await GetAsync<LikeStatusResponse>($"likes/status/{videoId}");
        return result?.HasLike;
    }
    
    public async Task<bool> SetLikeAsync(Guid videoId, bool isLike)
    {
        SetAuthHeader();
        var json = JsonSerializer.Serialize(new LikeRequest(videoId, isLike), _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{BaseUrl}/likes", content);
        
        // Если сервер вернул 200 OK - операция успешна (даже если тело пустое при удалении лайка)
        if (!response.IsSuccessStatusCode) return false;
        
        var responseContent = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            // Пустой ответ = лайк был удалён (повторный клик)
            return true;
        }
        
        // Непустой ответ = лайк был создан/изменён
        return true;
    }
    
    public async Task<bool> RemoveLikeAsync(Guid videoId)
    {
        return await DeleteAsync($"likes/{videoId}");
    }
    
    // Playlists
    public async Task<List<Playlist>> GetPlaylistsAsync()
    {
        var playlists = await GetAsync<List<Playlist>>("playlists");
        return playlists ?? new List<Playlist>();
    }
    
    public async Task<bool> CreatePlaylistAsync(string name)
    {
        var result = await PostAsync<PlaylistRequest, Playlist>("playlists", new PlaylistRequest(name));
        return result != null;
    }
    
    public async Task<bool> AddVideoToPlaylistAsync(Guid playlistId, Guid videoId)
    {
        var result = await PostAsync<AddVideoRequest, object>($"playlists/{playlistId}/videos", new AddVideoRequest(videoId));
        return result != null;
    }
    
    public async Task<bool> RemoveVideoFromPlaylistAsync(Guid playlistId, Guid videoId)
    {
        return await DeleteAsync($"playlists/{playlistId}/videos/{videoId}");
    }
    
    public async Task<bool> DeletePlaylistAsync(Guid playlistId)
    {
        return await DeleteAsync($"playlists/{playlistId}");
    }
    
    // Notifications
    public async Task<List<Notification>> GetNotificationsAsync()
    {
        var notifications = await GetAsync<List<Notification>>("moderation/notifications");
        return notifications ?? new List<Notification>();
    }
    
    public async Task<bool> MarkNotificationReadAsync(Guid notificationId)
    {
        SetAuthHeader();
        var response = await _httpClient.PostAsync($"{BaseUrl}/moderation/notifications/{notificationId}/read", null);
        return response.IsSuccessStatusCode;
    }
    
    // Moderation
    public async Task<List<VideoItem>> GetPendingVideosAsync()
    {
        var videos = await GetAsync<List<VideoItem>>("moderation/pending");
        return videos ?? new List<VideoItem>();
    }
    
    public async Task<bool> ModerateVideoAsync(Guid videoId, bool approve, string? reason = null)
    {
        var (success, _) = await PostWithoutResponseAsync("moderation", new ModerationRequest(videoId, approve, reason));
        return success;
    }
    
    public async Task<bool> UpdateVideoAsync(Guid videoId, string? title, string? description, string? category, List<string>? tags)
    {
        var request = new VideoUpdateRequest(title, description, category, tags);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        SetAuthHeader();
        var response = await _httpClient.PutAsync($"{BaseUrl}/videos/{videoId}", content);
        return response.IsSuccessStatusCode;
    }
    
    // User management
    public async Task<List<UserItem>> GetAllUsersAsync()
    {
        var users = await GetAsync<List<UserItem>>("auth/users");
        return users ?? new List<UserItem>();
    }
    
    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        return await DeleteAsync($"auth/users/{userId}");
    }
}

// DTOs for API
record LoginRequest(string Login, string Password);
record RegisterRequest(string Login, string Password, bool RegisterAsModerator, string? ModeratorPassword);
record CreateModeratorRequest(string Login, string Password);
record ApiErrorResponse(string Message);
record AuthResponse(string Token, User User);
record VideoUpdateRequest(string? Title, string? Description, string? Category, List<string>? Tags);
record CommentRequest(Guid VideoId, string Content);
record LikeRequest(Guid VideoId, bool IsLike);
record LikeResponse(Guid Id, Guid VideoId, Guid UserId, bool IsLike);
record LikeStatusResponse(bool? HasLike);
record PlaylistRequest(string Name);
record AddVideoRequest(Guid VideoId);
record ModerationRequest(Guid VideoId, bool Approve, string? Reason);
