using MediatR;

namespace SpotIt.Application.Features.Likes.Commands.UnlikePost;

public record UnLikePostCommand(Guid PostId) : IRequest<Unit>;