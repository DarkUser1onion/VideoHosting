using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Server.Controllers;
using Server.Models;
using Server.Services;
using System.Security.Claims;
using Xunit;

namespace Server.Tests;

/// <summary>
/// Unit tests for LikesController toggle bug.
/// 
/// Bug condition: при повторном нажатии на активный лайк/дизлайк сервер возвращает 
/// 404 NotFound вместо 200 OK.
/// 
/// Root cause: LikesController.SetLike returns NotFound() when SetLikeAsync returns null,
/// but null is the correct return value when a like is successfully removed via toggle.
/// 
/// These tests verify the buggy behavior at the controller level and will be used
/// to verify the fix.
/// 
/// Validates: Requirements from bugfix.md
/// </summary>
public class LikesControllerToggleBugTests
{
    private readonly Mock<IInteractionService> _mockInteractionService;
    private readonly LikesController _controller;

    public LikesControllerToggleBugTests()
    {
        _mockInteractionService = new Mock<IInteractionService>();
        _controller = new LikesController(_mockInteractionService.Object);
    }

    /// <summary>
    /// Helper to set up authenticated user in controller context.
    /// </summary>
    private void SetupAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    /// <summary>
    /// Test verifying FIXED behavior:
    /// When toggling off an existing like, SetLikeAsync returns null (correct),
    /// and the controller returns 200 OK with removed=true (FIXED).
    /// 
    /// Validates: Requirements 2.3, 2.4 from bugfix.md
    /// </summary>
    [Fact]
    public async Task SetLike_TogglingOffExistingLike_ReturnsOkWithRemovedTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        
        SetupAuthenticatedUser(userId);
        
        // SetLikeAsync returns null when removing a like via toggle
        _mockInteractionService
            .Setup(s => s.SetLikeAsync(userId, It.Is<LikeRequest>(r => r.VideoId == videoId && r.IsLike)))
            .ReturnsAsync((LikeDto?)null);
        
        var request = new LikeRequest(videoId, IsLike: true);
        
        // Act
        var result = await _controller.SetLike(request);
        
        // Assert - FIXED behavior: Returns Ok with removed=true
        var okResult = Assert.IsType<OkObjectResult>(result);
        // Verify the response contains removed = true
        var responseJson = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("removed", responseJson);
        Assert.Contains("true", responseJson);
    }

    /// <summary>
    /// Test verifying FIXED behavior for dislikes:
    /// When toggling off an existing dislike, SetLikeAsync returns null (correct),
    /// and the controller returns 200 OK with removed=true (FIXED).
    /// 
    /// Validates: Requirements 2.3, 2.5 from bugfix.md
    /// </summary>
    [Fact]
    public async Task SetLike_TogglingOffExistingDislike_ReturnsOkWithRemovedTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        
        SetupAuthenticatedUser(userId);
        
        // SetLikeAsync returns null when removing a dislike via toggle
        _mockInteractionService
            .Setup(s => s.SetLikeAsync(userId, It.Is<LikeRequest>(r => r.VideoId == videoId && !r.IsLike)))
            .ReturnsAsync((LikeDto?)null);
        
        var request = new LikeRequest(videoId, IsLike: false);
        
        // Act
        var result = await _controller.SetLike(request);
        
        // Assert - FIXED behavior: Returns Ok with removed=true
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseJson = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("removed", responseJson);
        Assert.Contains("true", responseJson);
    }

    /// <summary>
    /// Test verifying correct behavior when creating a new like.
    /// This should return 200 OK with the created like.
    /// </summary>
    [Fact]
    public async Task SetLike_CreatingNewLike_ReturnsOkWithLike()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var likeId = Guid.NewGuid();
        
        SetupAuthenticatedUser(userId);
        
        var expectedLike = new LikeDto(likeId, videoId, userId, IsLike: true);
        
        _mockInteractionService
            .Setup(s => s.SetLikeAsync(userId, It.Is<LikeRequest>(r => r.VideoId == videoId && r.IsLike)))
            .ReturnsAsync(expectedLike);
        
        var request = new LikeRequest(videoId, IsLike: true);
        
        // Act
        var result = await _controller.SetLike(request);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLike = Assert.IsType<LikeDto>(okResult.Value);
        Assert.Equal(likeId, returnedLike.Id);
        Assert.Equal(videoId, returnedLike.VideoId);
        Assert.Equal(userId, returnedLike.UserId);
        Assert.True(returnedLike.IsLike);
    }

    /// <summary>
    /// Test verifying correct behavior when switching from like to dislike.
    /// This should return 200 OK with the updated like.
    /// </summary>
    [Fact]
    public async Task SetLike_SwitchingFromLikeToDislike_ReturnsOkWithUpdatedLike()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var likeId = Guid.NewGuid();
        
        SetupAuthenticatedUser(userId);
        
        var expectedLike = new LikeDto(likeId, videoId, userId, IsLike: false);
        
        _mockInteractionService
            .Setup(s => s.SetLikeAsync(userId, It.Is<LikeRequest>(r => r.VideoId == videoId && !r.IsLike)))
            .ReturnsAsync(expectedLike);
        
        var request = new LikeRequest(videoId, IsLike: false);
        
        // Act
        var result = await _controller.SetLike(request);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLike = Assert.IsType<LikeDto>(okResult.Value);
        Assert.False(returnedLike.IsLike);
    }

    /// <summary>
    /// Test verifying unauthorized response when no user is authenticated.
    /// </summary>
    [Fact]
    public async Task SetLike_NoAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange - Set up an empty claims principal (no NameIdentifier claim)
        var identity = new ClaimsIdentity(); // Not authenticated, no claims
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
        
        var videoId = Guid.NewGuid();
        var request = new LikeRequest(videoId, IsLike: true);
        
        // Act
        var result = await _controller.SetLike(request);
        
        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    /// <summary>
    /// Test verifying correct behavior when creating a new dislike.
    /// This should return 200 OK with the created dislike.
    /// </summary>
    [Fact]
    public async Task SetLike_CreatingNewDislike_ReturnsOkWithDislike()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var likeId = Guid.NewGuid();
        
        SetupAuthenticatedUser(userId);
        
        var expectedLike = new LikeDto(likeId, videoId, userId, IsLike: false);
        
        _mockInteractionService
            .Setup(s => s.SetLikeAsync(userId, It.Is<LikeRequest>(r => r.VideoId == videoId && !r.IsLike)))
            .ReturnsAsync(expectedLike);
        
        var request = new LikeRequest(videoId, IsLike: false);
        
        // Act
        var result = await _controller.SetLike(request);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLike = Assert.IsType<LikeDto>(okResult.Value);
        Assert.False(returnedLike.IsLike);
    }
}
