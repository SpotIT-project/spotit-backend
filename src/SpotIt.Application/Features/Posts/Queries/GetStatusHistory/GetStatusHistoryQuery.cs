using MediatR;
using SpotIt.Application.DTOs;

namespace SpotIt.Application.Features.Posts.Queries.GetStatusHistory;

public record GetStatusHistoryQuery(Guid PostId) : IRequest<IEnumerable<StatusHistoryDto>>;
