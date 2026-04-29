using SpotIt.Domain.Enums;

namespace SpotIt.Domain.Entities;

public class Post
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public PostStatus Status { get; set; } = PostStatus.Pending;
    public string AuthorId { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser Author { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<Like> Likes { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<StatusHistory> StatusHistories { get; set; } = [];
}
