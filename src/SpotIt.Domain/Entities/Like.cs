namespace SpotIt.Domain.Entities;

public class Like
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid PostId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Post Post { get; set; } = null!;
}
