using Web.DTOs.Auth;
using Web.Services;

namespace Web.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", Register).WithName("Register").WithOpenApi();
        group.MapPost("/login", Login).WithName("Login").WithOpenApi();
    }

    private static async Task<IResult> Register(RegisterRequest request, IAuthService authService)
    {
        // Validate username: 3-50 characters, alphanumeric and underscore only
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3 || request.Username.Length > 50)
            return Results.BadRequest(new { error = "Username must be 3-50 characters" });

        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_]{3,50}$"))
            return Results.BadRequest(new { error = "Username must contain only letters, numbers, and underscore" });

        // Validate password: minimum 8 characters
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return Results.BadRequest(new { error = "Password must be at least 8 characters" });

        var (success, userId, error) = await authService.RegisterAsync(request.Username, request.Password);
        if (!success)
            return Results.Conflict(new { error });

        var token = authService.GenerateToken(userId!.Value, request.Username);
        return Results.Created($"/users/{userId}", new AuthResponse
        {
            UserId = userId!.Value,
            Username = request.Username,
            Token = token
        });
    }

    private static async Task<IResult> Login(LoginRequest request, IAuthService authService)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new { error = "Username and password are required" });

        var (success, userId, username, error) = await authService.LoginAsync(request.Username, request.Password);
        if (!success)
            return Results.Json(new { error = "Invalid credentials" }, statusCode: 401);

        var token = authService.GenerateToken(userId!.Value, username!);
        return Results.Ok(new AuthResponse
        {
            UserId = userId!.Value,
            Username = username!,
            Token = token
        });
    }
}
