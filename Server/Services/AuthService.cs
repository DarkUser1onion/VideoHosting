using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.Models;

namespace Server.Services;

public interface IAuthService
{
    Task<(bool Success, string Message, User? User)> RegisterAsync(string login, string password, bool registerAsModerator, string? moderatorPassword);
    Task<(bool Success, string? Token, User? User)> LoginAsync(string login, string password);
    Task<(bool Success, string Message, User? User)> CreateModeratorAsync(string login, string password);
    string GenerateJwtToken(User user);
    ClaimsPrincipal? ValidateToken(string token);
    Task<List<UserDto>> GetAllUsersAsync(Guid requesterId);
    Task<bool> DeleteUserAsync(Guid userId, Guid requesterId);
    Task<bool> ModerateUpdateVideoAsync(Guid videoId, Guid moderatorId, VideoUpdateRequest request);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    
    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }
    
    public async Task<(bool Success, string Message, User? User)> RegisterAsync(string login, string password, bool registerAsModerator, string? moderatorPassword)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return (false, "Login and password are required", null);
        
        if (await _context.Users.AnyAsync(u => u.Login == login))
            return (false, "User with this login already exists", null);
        
        if (registerAsModerator)
        {
            var configuredPassword = _configuration["Auth:ModeratorRegistrationPassword"] ?? "moderator-secret";
            if (string.IsNullOrWhiteSpace(moderatorPassword) || moderatorPassword != configuredPassword)
                return (false, "Неверный пароль модератора", null);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Login = login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = registerAsModerator ? "moderator" : "user",
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return (true, "Registration successful", user);
    }
    
    public async Task<(bool Success, string? Token, User? User)> LoginAsync(string login, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (false, null, null);
        
        var token = GenerateJwtToken(user);
        return (true, token, user);
    }

    public async Task<(bool Success, string Message, User? User)> CreateModeratorAsync(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return (false, "Логин и пароль обязательны", null);

        if (await _context.Users.AnyAsync(u => u.Login == login))
            return (false, "Пользователь с таким логином уже существует", null);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Login = login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "moderator",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return (true, "Модератор создан", user);
    }
    
    public string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "YourSuperSecretKeyForDevelopment2024MinLength32Chars";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role)
        };
        
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "VideoHostingServer",
            audience: _configuration["Jwt:Audience"] ?? "VideoHostingClient",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "YourSuperSecretKeyForDevelopment2024MinLength32Chars";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"] ?? "VideoHostingServer",
                ValidAudience = _configuration["Jwt:Audience"] ?? "VideoHostingClient",
                IssuerSigningKey = key
            }, out _);
            
            return principal;
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<List<UserDto>> GetAllUsersAsync(Guid requesterId)
    {
        var requester = await _context.Users.FindAsync(requesterId);
        if (requester == null) return new List<UserDto>();
        
        var query = _context.Users.AsQueryable();
        
        // Админ видит всех кроме себя
        if (requester.Role == "admin")
        {
            query = query.Where(u => u.Id != requesterId);
        }
        // Модератор видит только пользователей (не админов и не модераторов)
        else if (requester.Role == "moderator")
        {
            query = query.Where(u => u.Role == "user");
        }
        // Обычный пользователь не видит никого
        else
        {
            return new List<UserDto>();
        }
        
        var users = await query
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();
        
        return users.Select(u => new UserDto(u.Id, u.Login, u.Role, u.CreatedAt)).ToList();
    }
    
    public async Task<bool> DeleteUserAsync(Guid userId, Guid requesterId)
    {
        var user = await _context.Users.FindAsync(userId);
        var requester = await _context.Users.FindAsync(requesterId);
        
        if (user == null || requester == null) return false;
        
        // Админ может удалить кого угодно (кроме себя)
        if (requester.Role == "admin" && user.Id != requesterId)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
        
        // Модератор может удалить только пользователей (не админов и не модераторов)
        if (requester.Role == "moderator" && user.Role == "user")
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
        
        return false;
    }
    
    public async Task<bool> ModerateUpdateVideoAsync(Guid videoId, Guid moderatorId, VideoUpdateRequest request)
    {
        var video = await _context.Videos.FindAsync(videoId);
        if (video == null) return false;
        
        // Moderators can edit any video
        if (request.Title != null) video.Title = request.Title;
        if (request.Description != null) video.Description = request.Description;
        if (request.Category != null) video.Category = request.Category;
        if (request.Tags != null) video.Tags = request.Tags;
        
        await _context.SaveChangesAsync();
        return true;
    }
}
