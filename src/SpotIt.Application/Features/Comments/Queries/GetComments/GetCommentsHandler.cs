using AutoMapper;
using MediatR;
using SpotIt.Application.Common;
using SpotIt.Application.DTOs;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Comments.Queries.GetComments;

public class GetCommentsHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<GetCommentsQuery, PagedResult<CommentDto>>
{
    public async Task<PagedResult<CommentDto>> Handle(GetCommentsQuery request, CancellationToken ct)
    {
        var comments = await uow.Comments.GetByPostIdAsync(request.PostId, ct);

        var totalCount = comments.Count();
        var items = comments
            .OrderBy(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedResult<CommentDto>
        {
            Items = mapper.Map<IEnumerable<CommentDto>>(items),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
