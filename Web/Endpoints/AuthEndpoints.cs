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
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest("Username and password are required");

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
            return Results.BadRequest("Username and password are required");

        var (success, userId, username, error) = await authService.LoginAsync(request.Username, request.Password);
        if (!success)
            return Results.Unauthorized();

        var token = authService.GenerateToken(userId!.Value, username!);
        return Results.Ok(new AuthResponse
        {
            UserId = userId!.Value,
            Username = username!,
            Token = token
        });
    }
}
