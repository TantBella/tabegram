namespace Web.DTOs.Users;

public class UserPostsResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;
    public List<UserPostDto> Posts { get; set; } = null!;
}

public class UserPostDto
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LikeCount { get; set; }
}
