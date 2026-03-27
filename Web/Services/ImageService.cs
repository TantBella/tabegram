namespace Web.Services;

public interface IImageService
{
    Task<(bool Success, string? Filename, string? Error)> SaveImageAsync(IFormFile file);
    Task<(bool IsValid, string? Error)> ValidateImageAsync(IFormFile file);
}

public class ImageService : IImageService
{
    private readonly IConfiguration _config;
    private readonly string _uploadsPath;
    private readonly string[] _allowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public ImageService(IConfiguration config)
    {
        _config = config;
        _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), _config["Uploads:BasePath"]!);
        Directory.CreateDirectory(_uploadsPath);
    }

    public async Task<(bool Success, string? Filename, string? Error)> SaveImageAsync(IFormFile file)
    {
        var (isValid, error) = await ValidateImageAsync(file);
        if (!isValid)
            return (false, null, error);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var filename = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadsPath, filename);

        try
        {
            using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30)))
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cts.Token);
            }

            return (true, filename, null);
        }
        catch (System.OperationCanceledException)
        {
            // Clean up incomplete file
            try { File.Delete(filePath); } catch { }
            return (false, null, "File upload timed out");
        }
        catch (Exception ex)
        {
            // Clean up incomplete file
            try { File.Delete(filePath); } catch { }
            return (false, null, $"Error saving file: {ex.Message}");
        }
    }

    public async Task<(bool IsValid, string? Error)> ValidateImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return (false, "File is required");

        if (file.Length > MaxFileSize)
            return (false, $"File size must not exceed {MaxFileSize / 1024 / 1024} MB");

        if (!_allowedMimeTypes.Contains(file.ContentType))
            return (false, "Invalid file type. Only JPEG, PNG, GIF, and WebP are allowed");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
            return (false, "Invalid file extension");

        return (true, null);
    }
}
