using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Services;
using Xunit;

namespace Server.Tests;

/// <summary>
/// Unit tests for the like/dislike toggle bug.
/// 
/// Bug condition: при повторном нажатии на активный лайк/дизлайк сервер возвращает 
/// 404 NotFound вместо 200 OK.
/// 
/// Root cause: LikesController.SetLike returns NotFound() when SetLikeAsync returns null,
/// but null is the correct return value when a like is successfully removed via toggle.
/// 
/// Validates: Requirements from bugfix.md
/// </summary>
public class InteractionServiceToggleBugTests
{
    private AppDbContext CreateInMemoryContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Test reproducing the toggle bug: when toggling off an active like,
    /// SetLikeAsync returns null (indicating successful removal), but the
    /// controller incorrectly interprets this as a 404 NotFound condition.
    /// 
    /// This test verifies that SetLikeAsync correctly returns null when
    /// removing a like via toggle - the bug is in the controller, not the service.
    /// </summary>
    [Fact]
    public async Task SetLikeAsync_TogglingOffExistingLike_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext("ToggleOffLike_Test");
        var service = new InteractionService(context);
        
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        
        // Create a user and video for the test
        var user = new User
        {
            Id = userId,
            Login = "testuser",
            PasswordHash = "hash",
            Role = "user"
        };
        var video = new Video
        {
            Id = videoId,
            AuthorId = userId,
            Title = "Test Video",
            Description = "Test",
            Category = "Test",
            FilePath = "/test.mp4",
            Status = "approved"
        };
        
        context.Users.Add(user);
        context.Videos.Add(video);
        await context.SaveChangesAsync();
        
        // First, create a like
        var createRequest = new LikeRequest(videoId, IsLike: true);
        var createdLike = await service.SetLikeAsync(userId, createRequest);
        
        // Verify the like was created
        Assert.NotNull(createdLike);
        Assert.True(createdLike.IsLike);
        
        // Act - Toggle the same like off (same user, same video, same IsLike value)
        var toggleRequest = new LikeRequest(videoId, IsLike: true);
        var result = await service.SetLikeAsync(userId, toggleRequest);
        
        // Assert - The service returns null when successfully removing a like
        // This is CORRECT behavior - null indicates the like was removed
        // The BUG is in LikesController which interprets null as NotFound
        Assert.Null(result);
        
        // Verify the like was actually removed from the database
        var likeInDb = await context.Likes
            .FirstOrDefaultAsync(l => l.VideoId == videoId && l.UserId == userId);
        Assert.Null(likeInDb);
    }

    /// <summary>
    /// Test that toggling off a dislike also returns null.
    /// Same bug affects dislikes.
    /// </summary>
    [Fact]
    public async Task SetLikeAsync_TogglingOffExistingDislike_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext("ToggleOffDislike_Test");
        var service = new InteractionService(context);
        
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Login = "testuser2",
            PasswordHash = "hash",
            Role = "user"
        };
        var video = new Video
        {
            Id = videoId,
            AuthorId = userId,
            Title = "Test Video 2",
            Description = "Test",
            Category = "Test",
            FilePath = "/test2.mp4",
            Status = "approved"
        };
        
        context.Users.Add(user);
        context.Videos.Add(video);
        await context.SaveChangesAsync();
        
        // First, create a dislike
        var createRequest = new LikeRequest(videoId, IsLike: false);
        var createdDislike = await service.SetLikeAsync(userId, createRequest);
        
        Assert.NotNull(createdDislike);
        Assert.False(createdDislike.IsLike);
        
        // Act - Toggle the same dislike off
        var toggleRequest = new LikeRequest(videoId, IsLike: false);
        var result = await service.SetLikeAsync(userId, toggleRequest);
        
        // Assert - Returns null when removing
        Assert.Null(result);
        
        // Verify it was removed
        var likeInDb = await context.Likes
            .FirstOrDefaultAsync(l => l.VideoId == videoId && l.UserId == userId);
        Assert.Null(likeInDb);
    }

    /// <summary>
    /// Test that switching from like to dislike returns the updated like (not null).
    /// This should work correctly because it's not a toggle-off, it's a toggle-switch.
    /// </summary>
    [Fact]
    public async Task SetLikeAsync_SwitchingFromLikeToDislike_ReturnsUpdatedLike()
    {
        // Arrange
        using var context = CreateInMemoryContext("SwitchLikeToDislike_Test");
        var service = new InteractionService(context);
        
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Login = "testuser3",
            PasswordHash = "hash",
            Role = "user"
        };
        var video = new Video
        {
            Id = videoId,
            AuthorId = userId,
            Title = "Test Video 3",
            Description = "Test",
            Category = "Test",
            FilePath = "/test3.mp4",
            Status = "approved"
        };
        
        context.Users.Add(user);
        context.Videos.Add(video);
        await context.SaveChangesAsync();
        
        // Create a like
        var createRequest = new LikeRequest(videoId, IsLike: true);
        await service.SetLikeAsync(userId, createRequest);
        
        // Act - Switch to dislike (different IsLike value)
        var switchRequest = new LikeRequest(videoId, IsLike: false);
        var result = await service.SetLikeAsync(userId, switchRequest);
        
        // Assert - Returns the updated like (not null)
        Assert.NotNull(result);
        Assert.False(result.IsLike);
        
        // Verify only one record exists with IsLike = false
        var likesInDb = await context.Likes
            .Where(l => l.VideoId == videoId && l.UserId == userId)
            .ToListAsync();
        Assert.Single(likesInDb);
        Assert.False(likesInDb[0].IsLike);
    }

    /// <summary>
    /// Test that creating a new like returns the created like (not null).
    /// </summary>
    [Fact]
    public async Task SetLikeAsync_CreatingNewLike_ReturnsCreatedLike()
    {
        // Arrange
        using var context = CreateInMemoryContext("CreateNewLike_Test");
        var service = new InteractionService(context);
        
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        
        var user = new User
        {
            Id = userId,
            Login = "testuser4",
            PasswordHash = "hash",
            Role = "user"
        };
        var video = new Video
        {
            Id = videoId,
            AuthorId = userId,
            Title = "Test Video 4",
            Description = "Test",
            Category = "Test",
            FilePath = "/test4.mp4",
            Status = "approved"
        };
        
        context.Users.Add(user);
        context.Videos.Add(video);
        await context.SaveChangesAsync();
        
        // Act - Create a new like
        var request = new LikeRequest(videoId, IsLike: true);
        var result = await service.SetLikeAsync(userId, request);
        
        // Assert - Returns the created like
        Assert.NotNull(result);
        Assert.True(result.IsLike);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(videoId, result.VideoId);
    }
}
