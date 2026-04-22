using MediatR;
using SpotIt.Application.DTOs;

namespace SpotIt.Application.Features.Analytics.Queries.GetPostsByStatus;

public record GetPostsByStatusQuery : IRequest<IEnumerable<PostsByStatusDto>>;