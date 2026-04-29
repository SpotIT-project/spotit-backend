using MediatR;
using SpotIt.Application.Exceptions;
using SpotIt.Application.Interfaces;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Posts.Commands.UploadPostPhoto;

public class UploadPostPhotoHandler(IUnitOfWork uow, IFileStorageService fileStorage)
    : IRequestHandler<UploadPostPhotoCommand, string>
{
    public async Task<string> Handle(UploadPostPhotoCommand request, CancellationToken ct)
    {
        var post = await uow.Posts.GetByIdAsync(request.PostId, ct)
            ?? throw new NotFoundException(nameof(Post), request.PostId);

        if (post.PhotoUrl is not null)
            fileStorage.Delete(post.PhotoUrl);

        var url = await fileStorage.SaveAsync(request.Photo, ct);

        post.PhotoUrl = url;
        post.UpdatedAt = DateTime.UtcNow;
        uow.Posts.Update(post);
        await uow.SaveChangesAsync(ct);

        return url;
    }
}
