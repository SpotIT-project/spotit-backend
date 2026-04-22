using SpotIt.Domain.Enums;

namespace SpotIt.Application.DTOs;

public record PostsByStatusDto
{
    public PostStatus Status { get; init; }
    public int Count { get; init; }
}
