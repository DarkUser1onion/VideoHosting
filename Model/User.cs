using System;

namespace VideoHostingByWhoami.Model;

public class User
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
    public DateTime CreatedAt { get; set; }
}

public class AuthResult
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public User? User { get; set; }
    public string Message { get; set; } = string.Empty;
}

public record OperationResult(bool Success, string Message);
