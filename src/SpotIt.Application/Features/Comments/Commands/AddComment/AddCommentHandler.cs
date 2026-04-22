using MediatR;
using SpotIt.Application.Interfaces;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Comments.Commands.AddComment;

public class AddCommentHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<AddCommentCommand, Unit>
{
    public async Task<Unit> Handle(AddCommentCommand request, CancellationToken ct)
    {
        // TODO(human): create a Comment entity and save it
        // Comment needs: Id (Guid.CreateVersion7()), PostId, AuthorId (from currentUser),
        // Content, IsOfficialResponse, CreatedAt
        // Then: AddAsync + SaveChangesAsync, return Unit.Value

        var comment = new Comment
        {
            Id=Guid.CreateVersion7(),
            AuthorId=currentUser.UserId,
            PostId=request.PostId,
            Content = request.Content,
            IsOfficialResponse=request.IsOfficialResponse,
            CreatedAt=DateTime.UtcNow,
        };
         
        await uow.Comments.AddAsync(comment, ct);
        await uow.SaveChangesAsync(ct);

        return Unit.Value;

    }
}
