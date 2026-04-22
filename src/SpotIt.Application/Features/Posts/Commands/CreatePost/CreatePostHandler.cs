using MediatR;
using SpotIt.Application.Interfaces;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Enums;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Posts.Commands.CreatePost;

public class CreatePostHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<CreatePostCommand, Guid>
{
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken ct)
    {
        var newPost = new Post
        {
            Id = Guid.CreateVersion7(),
            AuthorId = currentUser.UserId,
            Title = request.Title,
            Description = request.Description,
            CategoryId = request.CategoryId,
            IsAnonymous = request.IsAnonymous
        };

        var newStatusHistory = new StatusHistory
        {
            PostId = newPost.Id,
            NewStatus = PostStatus.Pending,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = currentUser.UserId,
        };

        await uow.Posts.AddAsync(newPost, ct);
        await uow.StatusHistory.AddAsync(newStatusHistory, ct);
        await uow.SaveChangesAsync(ct);

        return newPost.Id;
    }
}
