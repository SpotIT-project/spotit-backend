namespace SpotIt.Application.DTOs;

public record CommentDto
{
    public Guid Id { get; init; }
    public string Content { get; init; } = null!;
    public string AuthorName { get; init; } = null!;
    public bool IsOfficialResponse { get; init; }
    public DateTime CreatedAt { get; init; }
}
