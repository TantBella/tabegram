using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public interface IAuthService
{
    Task<(bool Success, Guid? UserId, string? Error)> RegisterAsync(string username, string password);
    Task<(bool Success, Guid? UserId, string? Username, string? Error)> LoginAsync(string username, string password);
    string GenerateToken(Guid userId, string username);
    (bool IsValid, string? HashedPassword) HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class AuthService : IAuthService
{
    private readonly Web.Data.AppDbContext _db;
    private readonly IConfiguration _config;
    private const int Iterations = 310000;
    private const int SaltLength = 32;
    private const int HashLength = 32;

    public AuthService(Web.Data.AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<(bool Success, Guid? UserId, string? Error)> RegisterAsync(string username, string password)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Username == username);
        if (userExists)
            return (false, null, "Username already exists");

        var (isValid, hashedPassword) = HashPassword(password);
        if (!isValid || hashedPassword == null)
            return (false, null, "Failed to hash password");

        var user = new Web.Models.User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = hashedPassword!,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return (true, user.Id, null);
    }

    public async Task<(bool Success, Guid? UserId, string? Username, string? Error)> LoginAsync(string username, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return (false, null, null, "Invalid credentials");

        if (!VerifyPassword(password, user.PasswordHash))
            return (false, null, null, "Invalid credentials");

        return (true, user.Id, user.Username, null);
    }

    public string GenerateToken(Guid userId, string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (bool IsValid, string? HashedPassword) HashPassword(string password)
    {
        try
        {
            byte[] salt = new byte[SaltLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashLength);
            string saltB64 = Convert.ToBase64String(salt);
            string hashB64 = Convert.ToBase64String(hash);
            return (true, $"{saltB64}:{hashB64}");
        }
        catch
        {
            return (false, null);
        }
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            var parts = hash.Split(':');
            if (parts.Length != 2)
                return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] storedHash = Convert.FromBase64String(parts[1]);

            byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashLength);
            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
        catch
        {
            return false;
        }
    }
}
