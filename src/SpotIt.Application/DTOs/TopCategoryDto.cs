namespace SpotIt.Application.DTOs;

public record TopCategoryDto
{
    public string CategoryName { get; init; } = null!;
    public int PostCount { get; init; }
}
