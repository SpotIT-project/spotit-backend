using MediatR;
using SpotIt.Application.Exceptions;
using SpotIt.Application.Interfaces;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Likes.Commands.UnlikePost;

public class UnLikePostHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UnLikePostCommand, Unit>
{
    public async Task<Unit> Handle(UnLikePostCommand request, CancellationToken ct)
    {
        var likes = await uow.Likes.FindAsync(
            x => x.PostId == request.PostId && x.UserId == currentUser.UserId, ct);

        var like = likes.FirstOrDefault();
        if (like is null) throw new NotFoundException(nameof(Like), request.PostId);

        uow.Likes.Remove(like);
        await uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
