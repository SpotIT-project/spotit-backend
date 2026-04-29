using MediatR;

namespace SpotIt.Application.Features.Posts.Commands.DeletePost;

public record DeletePostCommand(Guid PostId) : IRequest<Unit>;
