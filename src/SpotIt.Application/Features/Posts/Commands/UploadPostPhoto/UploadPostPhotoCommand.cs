using MediatR;
using Microsoft.AspNetCore.Http;

namespace SpotIt.Application.Features.Posts.Commands.UploadPostPhoto;

public record UploadPostPhotoCommand(Guid PostId, IFormFile Photo) : IRequest<string>;
