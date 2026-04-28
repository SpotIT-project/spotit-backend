using SpotIt.Domain.Enums;

namespace SpotIt.Application.DTOs;

public record PostDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string Description { get; init; } = null!;
    public int CategoryId { get; init; }
    public PostStatus Status { get; init; }
    public bool IsAnonymous { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CategoryName { get; init; } = null!;
    public int LikesCount { get; init; }
    public string? AuthorId { get; init; }
    public string? AuthorName { get; init; }
    public string? PhotoUrl { get; init; }
}
