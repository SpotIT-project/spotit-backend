using MediatR;

namespace SpotIt.Application.Features.Posts.Commands.CreatePost;

public record CreatePostCommand(string Title, string Description, int CategoryId, bool IsAnonymous) : IRequest<Guid>;
