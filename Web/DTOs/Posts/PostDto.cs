namespace Web.DTOs.Posts;

public class CreatePostRequest
{
    public IFormFile Image { get; set; } = null!;
    public string? Description { get; set; }
}

public class PostResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;
    public string ImageUrl { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LikeCount { get; set; }
    public bool LikedByCurrentUser { get; set; }
}

public class PagedResponse<T>
{
    public List<T> Data { get; set; } = null!;
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
}
