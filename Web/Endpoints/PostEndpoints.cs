using Web.DTOs.Posts;
using Web.DTOs.Users;
using Web.Models;
using Web.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Web.Endpoints;

public static class PostEndpoints
{
    public static void MapPostEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/posts").WithTags("Posts");

        group.MapGet("", GetPosts).WithName("GetPosts").WithOpenApi();
        group.MapPost("", CreatePost).WithName("CreatePost").RequireAuthorization().WithOpenApi();
        group.MapPost("/{id}/like", ToggleLike).WithName("ToggleLike").RequireAuthorization().WithOpenApi();

        var userGroup = app.MapGroup("/users").WithTags("Users");
        userGroup.MapGet("/{id}/posts", GetUserPosts).WithName("GetUserPosts").WithOpenApi();

        var uploadGroup = app.MapGroup("/uploads").WithTags("Uploads");
        uploadGroup.MapGet("/{filename}", ServeImage).WithName("ServeImage").WithOpenApi();
    }

    private static async Task<IResult> GetPosts(
        int page = 1,
        int pageSize = 10,
        Web.Data.AppDbContext db = null!,
        ClaimsPrincipal user = null!)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = null;
        if (!string.IsNullOrEmpty(currentUserId) && Guid.TryParse(currentUserId, out var id))
            userId = id;

        var total = await db.Posts.CountAsync();
        var posts = await db.Posts
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Likes)
            .ToListAsync();

        var postResponses = posts
            .Select(p => new PostResponse
            {
                Id = p.Id,
                UserId = p.UserId,
                Username = p.User.Username,
                ImageUrl = $"/uploads/{Path.GetFileName(p.ImagePath)}",
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                LikeCount = p.Likes.Count,
                LikedByCurrentUser = userId.HasValue && p.Likes.Any(l => l.UserId == userId)
            })
            .ToList();

        return Results.Ok(new PagedResponse<PostResponse>
        {
            Data = postResponses,
            Page = page,
            PageSize = pageSize,
            Total = total
        });
    }

    private static async Task<IResult> CreatePost(
        HttpContext context,
        IFormCollection form,
        IImageService imageService,
        Web.Data.AppDbContext db)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Results.Unauthorized();

        if (!form.Files.Any() || form.Files[0].Length == 0)
            return Results.BadRequest(new { error = "Image file is required" });

        var file = form.Files[0];
        var (success, filename, error) = await imageService.SaveImageAsync(file);
        if (!success)
            return Results.BadRequest(new { error });

        var description = form["Description"].ToString();
        var post = new Post
        {
            Id = Guid.NewGuid(),
            UserId = userGuid,
            ImagePath = filename,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        db.Posts.Add(post);
        await db.SaveChangesAsync();

        return Results.Created($"/posts/{post.Id}", new { postId = post.Id });
    }

    private static async Task<IResult> ToggleLike(
        Guid id,
        HttpContext context,
        Web.Data.AppDbContext db)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Results.Unauthorized();

        var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == id);
        if (post == null)
            return Results.NotFound();

        using (var transaction = db.Database.BeginTransaction())
        {
            var existing = await db.Likes.FirstOrDefaultAsync(l => l.PostId == id && l.UserId == userGuid);
            if (existing == null)
            {
                var like = new Like
                {
                    Id = Guid.NewGuid(),
                    PostId = id,
                    UserId = userGuid,
                    CreatedAt = DateTime.UtcNow
                };
                db.Likes.Add(like);
            }
            else
            {
                db.Likes.Remove(existing);
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        return Results.Ok();
    }

    private static async Task<IResult> GetUserPosts(
        Guid id,
        Web.Data.AppDbContext db)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return Results.NotFound();

        var postsData = await db.Posts
            .Where(p => p.UserId == id)
            .Include(p => p.Likes)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var posts = postsData
            .Select(p => new UserPostDto
            {
                Id = p.Id,
                ImageUrl = $"/uploads/{Path.GetFileName(p.ImagePath)}",
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                LikeCount = p.Likes.Count
            })
            .ToList();

        return Results.Ok(new UserPostsResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Posts = posts
        });
    }

    private static IResult ServeImage(string filename, IConfiguration config)
    {
        // Prevent path traversal: only allow the filename without directory components
        var safeFilename = Path.GetFileName(filename);
        if (string.IsNullOrEmpty(safeFilename) || !safeFilename.Equals(filename))
            return Results.BadRequest(new { error = "Invalid filename" });

        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), config["Uploads:BasePath"]!);
        var filePath = Path.Combine(uploadsPath, safeFilename);

        // Verify the resolved path is within the uploads directory
        var fullPath = Path.GetFullPath(filePath);
        var fullUploadsPath = Path.GetFullPath(uploadsPath);
        if (!fullPath.StartsWith(fullUploadsPath))
            return Results.BadRequest(new { error = "Invalid file path" });

        if (!File.Exists(filePath))
            return Results.NotFound();

        var contentType = GetContentType(filePath);
        return Results.File(new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read), contentType, enableRangeProcessing: true);
    }

    private static string GetContentType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
