namespace Web.DTOs.Auth;

public class RegisterRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class AuthResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Token { get; set; } = null!;
}
