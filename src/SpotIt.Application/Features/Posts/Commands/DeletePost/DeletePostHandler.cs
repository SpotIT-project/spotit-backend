using MediatR;
using SpotIt.Application.Exceptions;
using SpotIt.Application.Interfaces;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Posts.Commands.DeletePost;

public class DeletePostHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<DeletePostCommand, Unit>
{
    public async Task<Unit> Handle(DeletePostCommand request, CancellationToken ct)
    {
        var post = await uow.Posts.GetByIdAsync(request.PostId, ct)
            ?? throw new NotFoundException(nameof(Post), request.PostId);

        if (post.AuthorId != currentUser.UserId)
            throw new UnauthorizedAccessException("You can only delete your own posts.");

        uow.Posts.Remove(post);
        await uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
