using MediatR;
using SpotIt.Domain.Enums;

namespace SpotIt.Application.Features.Posts.Commands.UpdatePostStatus;

public record UpdatePostStatusCommand(Guid PostId, PostStatus NewStatus, string? Note) : IRequest<Unit>;
