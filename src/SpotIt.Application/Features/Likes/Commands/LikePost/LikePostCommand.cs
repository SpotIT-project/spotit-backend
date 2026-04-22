using MediatR;

namespace SpotIt.Application.Features.Likes.Commands.LikePost;

public record LikePostCommand(Guid PostId) : IRequest<Unit>;