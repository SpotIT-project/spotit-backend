namespace SpotIt.Application.DTOs;

public record StatusHistoryDto(int Id, string NewStatus, string? Note, string ChangedByUserId, DateTime ChangedAt);
