using Web.Data;
using Web.Models;
using Web.Services;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Web.Seeds;

public static class DbSeeder
{
    public static async Task SeedAsync(WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            if (db.Users.Any())
                return; // Already seeded

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), config["Uploads:BasePath"]!);
            Directory.CreateDirectory(uploadsPath);

            // Create 3 users
            var users = new List<User>();
            var usernames = new[] { "alice", "bob", "charlie" };
            var colors = new[] { Color.FromArgb(255, 245, 0), Color.FromArgb(255, 100, 0), Color.FromArgb(255, 105, 180) };

            foreach (var username in usernames)
            {
                var (success, hashedPassword) = authService.HashPassword($"{username}123");
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    PasswordHash = hashedPassword!,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                };
                users.Add(user);
                db.Users.Add(user);
            }
            await db.SaveChangesAsync();

            // Create 8 posts with mixed distribution
            var postTexts = new[]
            {
                "Beautiful sunset 🌅",
                "Coffee time ☕",
                "Nature walk 🌿",
                "Digital art creation 🎨",
                "Sunset vibes ✨",
                "Weekend adventure 🏔️",
                "Fresh flowers 🌼",
                "Golden hour ✨"
            };

            var posts = new List<Post>();
            var userIndex = 0;
            for (int i = 0; i < 8; i++)
            {
                var user = users[userIndex % users.Count];
                var imageFilename = GenerateTestImage(uploadsPath, colors[userIndex % colors.Length], i);

                var post = new Post
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    ImagePath = imageFilename,
                    Description = postTexts[i],
                    CreatedAt = DateTime.UtcNow.AddDays(-(8 - i))
                };
                posts.Add(post);
                db.Posts.Add(post);
                userIndex++;
            }
            await db.SaveChangesAsync();

            // Create some likes
            var likes = new List<Like>();
            for (int i = 0; i < posts.Count; i++)
            {
                var post = posts[i];
                // Random users like posts
                var likerIndices = new[] { (i + 1) % users.Count, (i + 2) % users.Count };
                foreach (var likerIdx in likerIndices)
                {
                    if (users[likerIdx].Id != post.UserId)
                    {
                        var like = new Like
                        {
                            Id = Guid.NewGuid(),
                            PostId = post.Id,
                            UserId = users[likerIdx].Id,
                            CreatedAt = DateTime.UtcNow.AddDays(-(8 - i - 1))
                        };
                        likes.Add(like);
                        db.Likes.Add(like);
                    }
                }
            }
            await db.SaveChangesAsync();
        }
    }

    private static string GenerateTestImage(string uploadsPath, Color bgColor, int imageNumber)
    {
        var filename = $"{Guid.NewGuid()}.png";
        var filePath = Path.Combine(uploadsPath, filename);

        using (var bitmap = new Bitmap(400, 400))
        {
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(bgColor);

                // Add some variation with gradient
                var lightColor = Color.FromArgb(
                    Math.Min(bgColor.R + 50, 255),
                    Math.Min(bgColor.G + 50, 255),
                    Math.Min(bgColor.B + 50, 255)
                );
                using (var brush = new LinearGradientBrush(
                    new PointF(0, 0),
                    new PointF(400, 400),
                    bgColor,
                    lightColor))
                {
                    g.FillRectangle(brush, 0, 0, 400, 400);
                }

                // Add text with image number
                using (var font = new Font("Arial", 48, FontStyle.Bold))
                {
                    var text = $"Photo {imageNumber + 1}";
                    var textSize = g.MeasureString(text, font);
                    var x = (400 - textSize.Width) / 2;
                    var y = (400 - textSize.Height) / 2;
                    using (var brush = new SolidBrush(Color.White))
                    {
                        g.DrawString(text, font, brush, x, y);
                    }
                }
            }

            bitmap.Save(filePath, ImageFormat.Png);
        }

        return filename;
    }
}
