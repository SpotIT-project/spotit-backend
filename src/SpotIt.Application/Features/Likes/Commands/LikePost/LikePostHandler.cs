using MediatR;
using SpotIt.Application.Interfaces;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Likes.Commands.LikePost;

public class LikePostHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<LikePostCommand, Unit>
{
    public async Task<Unit> Handle(LikePostCommand request, CancellationToken ct)
    {
        var existing = await uow.Likes.FindAsync(
            x => x.PostId == request.PostId && x.UserId == currentUser.UserId, ct);

        if (existing.Any()) throw new InvalidOperationException("Post is already liked.");

        var like = new Like
        {
            PostId = request.PostId,
            UserId = currentUser.UserId,
            CreatedAt = DateTime.UtcNow,
        };

        await uow.Likes.AddAsync(like, ct);
        await uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
