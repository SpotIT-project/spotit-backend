using AutoMapper;
using MediatR;
using SpotIt.Application.DTOs;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Comments.Queries.GetComments;

public class GetCommentsHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<GetCommentsQuery, IEnumerable<CommentDto>>
{
    public async Task<IEnumerable<CommentDto>> Handle(GetCommentsQuery request, CancellationToken ct)
    {
        // TODO(human): fetch comments for request.PostId using uow.Comments.GetByPostIdAsync
        // then map the result to IEnumerable<CommentDto> using mapper and return it
        // (one line fetch, one line return)

        var comments = await uow.Comments.GetByPostIdAsync(request.PostId, ct);
        return mapper.Map<IEnumerable<CommentDto>>(comments);
    }
}
