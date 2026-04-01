using SpotIt.Domain.Enums;

namespace SpotIt.Domain.Entities;

public class StatusHistory
{
    public int Id { get; set; }
    public Guid PostId { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public PostStatus OldStatus { get; set; }
    public PostStatus NewStatus { get; set; }
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Post Post { get; set; } = null!;
    public ApplicationUser ChangedBy { get; set; } = null!;
}
