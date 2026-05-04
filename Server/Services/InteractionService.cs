using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services;

public interface IInteractionService
{
    // Comments
    Task<List<CommentDto>> GetCommentsAsync(Guid videoId);
    Task<CommentDto?> AddCommentAsync(Guid userId, CommentCreateRequest request);
    Task<bool> UpdateCommentAsync(Guid commentId, Guid userId, CommentUpdateRequest request);
    Task<bool> DeleteCommentAsync(Guid commentId, Guid userId);
    
    // Likes
    Task<LikeDto?> SetLikeAsync(Guid userId, LikeRequest request);
    Task<bool> RemoveLikeAsync(Guid userId, Guid videoId);
    Task<bool?> GetUserLikeStatusAsync(Guid userId, Guid videoId);
    
    // Playlists
    Task<List<PlaylistDto>> GetUserPlaylistsAsync(Guid userId);
    Task<PlaylistDto?> CreatePlaylistAsync(Guid userId, PlaylistCreateRequest request);
    Task<bool> AddVideoToPlaylistAsync(Guid playlistId, Guid userId, Guid videoId);
    Task<bool> RemoveVideoFromPlaylistAsync(Guid playlistId, Guid userId, Guid videoId);
    Task<bool> DeletePlaylistAsync(Guid playlistId, Guid userId);
}

public class InteractionService : IInteractionService
{
    private readonly AppDbContext _context;
    
    public InteractionService(AppDbContext context)
    {
        _context = context;
    }
    
    #region Comments
    
    public async Task<List<CommentDto>> GetCommentsAsync(Guid videoId)
    {
        var comments = await _context.Comments
            .Include(c => c.User)
            .Where(c => c.VideoId == videoId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        
        return comments.Select(c => new CommentDto(
            c.Id,
            c.VideoId,
            c.UserId,
            c.User.Login,
            c.Content,
            c.CreatedAt
        )).ToList();
    }
    
    public async Task<CommentDto?> AddCommentAsync(Guid userId, CommentCreateRequest request)
    {
        var video = await _context.Videos.FindAsync(request.VideoId);
        if (video == null) return null;
        
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            VideoId = request.VideoId,
            UserId = userId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
        
        var user = await _context.Users.FindAsync(userId);
        
        return new CommentDto(
            comment.Id,
            comment.VideoId,
            comment.UserId,
            user?.Login ?? "",
            comment.Content,
            comment.CreatedAt
        );
    }
    
    public async Task<bool> UpdateCommentAsync(Guid commentId, Guid userId, CommentUpdateRequest request)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null || comment.UserId != userId) return false;
        
        comment.Content = request.Content;
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> DeleteCommentAsync(Guid commentId, Guid userId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null) return false;
        
        var user = await _context.Users.FindAsync(userId);
        if (comment.UserId != userId && user?.Role != "moderator" && user?.Role != "admin") return false;
        
        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        return true;
    }
    
    #endregion
    
    #region Likes
    
    public async Task<LikeDto?> SetLikeAsync(Guid userId, LikeRequest request)
    {
        var existingLike = await _context.Likes
            .FirstOrDefaultAsync(l => l.VideoId == request.VideoId && l.UserId == userId);
        
        if (existingLike != null)
        {
            existingLike.IsLike = request.IsLike;
            await _context.SaveChangesAsync();
            
            return new LikeDto(existingLike.Id, existingLike.VideoId, existingLike.UserId, existingLike.IsLike);
        }
        
        var like = new Like
        {
            Id = Guid.NewGuid(),
            VideoId = request.VideoId,
            UserId = userId,
            IsLike = request.IsLike
        };
        
        _context.Likes.Add(like);
        await _context.SaveChangesAsync();
        
        return new LikeDto(like.Id, like.VideoId, like.UserId, like.IsLike);
    }
    
    public async Task<bool> RemoveLikeAsync(Guid userId, Guid videoId)
    {
        var like = await _context.Likes
            .FirstOrDefaultAsync(l => l.VideoId == videoId && l.UserId == userId);
        
        if (like == null) return false;
        
        _context.Likes.Remove(like);
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool?> GetUserLikeStatusAsync(Guid userId, Guid videoId)
    {
        var like = await _context.Likes
            .FirstOrDefaultAsync(l => l.VideoId == videoId && l.UserId == userId);
        
        return like?.IsLike;
    }
    
    #endregion
    
    #region Playlists
    
    public async Task<List<PlaylistDto>> GetUserPlaylistsAsync(Guid userId)
    {
        var playlists = await _context.Playlists
            .Include(p => p.PlaylistVideos)
            .ThenInclude(pv => pv.Video)
            .ThenInclude(v => v.Author)
            .Include(p => p.PlaylistVideos)
            .ThenInclude(pv => pv.Video)
            .ThenInclude(v => v.Likes)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        
        return playlists.Select(p => new PlaylistDto(
            p.Id,
            p.UserId,
            p.Name,
            p.CreatedAt,
            p.PlaylistVideos
                .OrderBy(pv => pv.Order)
                .Select(pv => new VideoDto(
                    pv.Video.Id,
                    pv.Video.AuthorId,
                    pv.Video.Author.Login,
                    pv.Video.Title,
                    pv.Video.Description,
                    pv.Video.Category,
                    pv.Video.Tags,
                    $"/api/video/{pv.Video.Id}/preview",
                    pv.Video.Duration,
                    pv.Video.Views,
                    pv.Video.Status,
                    pv.Video.UploadedAt,
                    pv.Video.Likes.Count(l => l.IsLike),
                    pv.Video.Likes.Count(l => !l.IsLike)
                )).ToList()
        )).ToList();
    }
    
    public async Task<PlaylistDto?> CreatePlaylistAsync(Guid userId, PlaylistCreateRequest request)
    {
        var playlist = new Playlist
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Playlists.Add(playlist);
        await _context.SaveChangesAsync();
        
        return new PlaylistDto(playlist.Id, playlist.UserId, playlist.Name, playlist.CreatedAt, new List<VideoDto>());
    }
    
    public async Task<bool> AddVideoToPlaylistAsync(Guid playlistId, Guid userId, Guid videoId)
    {
        var playlist = await _context.Playlists
            .Include(p => p.PlaylistVideos)
            .FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == userId);
        
        if (playlist == null) return false;
        
        var video = await _context.Videos.FindAsync(videoId);
        if (video == null || video.Status != "approved") return false;
        
        if (playlist.PlaylistVideos.Any(pv => pv.VideoId == videoId)) return false;
        
        var playlistVideo = new PlaylistVideo
        {
            Id = Guid.NewGuid(),
            PlaylistId = playlistId,
            VideoId = videoId,
            Order = playlist.PlaylistVideos.Count
        };
        
        _context.PlaylistVideos.Add(playlistVideo);
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> RemoveVideoFromPlaylistAsync(Guid playlistId, Guid userId, Guid videoId)
    {
        var playlistVideo = await _context.PlaylistVideos
            .FirstOrDefaultAsync(pv => pv.PlaylistId == playlistId && pv.VideoId == videoId);
        
        if (playlistVideo == null) return false;
        
        var playlist = await _context.Playlists.FindAsync(playlistId);
        if (playlist == null || playlist.UserId != userId) return false;
        
        _context.PlaylistVideos.Remove(playlistVideo);
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> DeletePlaylistAsync(Guid playlistId, Guid userId)
    {
        var playlist = await _context.Playlists.FindAsync(playlistId);
        if (playlist == null || playlist.UserId != userId) return false;
        
        _context.Playlists.Remove(playlist);
        await _context.SaveChangesAsync();
        return true;
    }
    
    #endregion
}
