using MediatR;
using SpotIt.Application.Exceptions;
using SpotIt.Application.Interfaces;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Posts.Commands.UpdatePostStatus;

public class UpdatePostStatusHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpdatePostStatusCommand, Unit>
{
    public async Task<Unit> Handle(UpdatePostStatusCommand request, CancellationToken ct)
    {
        // TODO(human): implement the status update
        // Steps:
        // 1. Fetch the post by PostId — use uow.Posts.GetByIdAsync (tracked, so changes are detected)
        // 2. If null, throw NotFoundException
        // 3. Capture OldStatus before changing anything
        // 4. Set post.Status = request.NewStatus
        // 5. Create a StatusHistory record (PostId, ChangedByUserId, OldStatus, NewStatus, Note, ChangedAt)
        // 6. Add the history and save — both in one transaction
        var post = await uow.Posts.GetByIdAsync(request.PostId, ct);

        if (post is null) throw new NotFoundException(nameof(Post), request.PostId);

        var oldStatus = post.Status;
        post.Status = request.NewStatus;

        var statusHistory = new StatusHistory
        {
            PostId = request.PostId,
            ChangedByUserId = currentUser.UserId,
            OldStatus = oldStatus,
            NewStatus = request.NewStatus,
            Note = request.Note,
            ChangedAt = DateTime.UtcNow
        };

        await uow.StatusHistory.AddAsync(statusHistory, ct);
        await uow.SaveChangesAsync(ct);

        return Unit.Value;
        
    }
}
