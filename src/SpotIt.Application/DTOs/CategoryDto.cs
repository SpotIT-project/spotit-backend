namespace SpotIt.Application.DTOs;

public record CategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string? IconUrl { get; init; }
}
