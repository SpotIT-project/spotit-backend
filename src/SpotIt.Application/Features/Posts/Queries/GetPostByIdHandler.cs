using AutoMapper;
using MediatR;
using SpotIt.Application.DTOs;
using SpotIt.Application.Exceptions;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Posts.Queries;

public class GetPostByIdHandler(IUnitOfWork uow, IMapper mapper) : IRequestHandler<GetPostByIdQuery, PostDto>
{
    public async Task<PostDto> Handle(GetPostByIdQuery request, CancellationToken ct)
    {
        var post = await uow.Posts.GetByIdWithDetailsAsync(request.Id, ct);
        if (post is null) throw new NotFoundException(nameof(Post), request.Id);

        return mapper.Map<PostDto>(post);
    }
}
