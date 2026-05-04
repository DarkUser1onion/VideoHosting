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
    Task<(bool Success, string Message, User? User)> RegisterAsync(string login, string password);
    Task<(bool Success, string? Token, User? User)> LoginAsync(string login, string password);
    string GenerateJwtToken(User user);
    ClaimsPrincipal? ValidateToken(string token);
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
    
    public async Task<(bool Success, string Message, User? User)> RegisterAsync(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return (false, "Login and password are required", null);
        
        if (await _context.Users.AnyAsync(u => u.Login == login))
            return (false, "User with this login already exists", null);
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Login = login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "user",
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
}
