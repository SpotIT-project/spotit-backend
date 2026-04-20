using AutoMapper;
using MediatR;
using SpotIt.Application.Common;
using SpotIt.Application.DTOs;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Posts.Queries;

public class GetPostsHandler(IUnitOfWork uow, IMapper mapper) : IRequestHandler<GetPostsQuery, PagedResult<PostDto>>
{
    public async Task<PagedResult<PostDto>> Handle(GetPostsQuery request, CancellationToken ct)
    {
        var (items, totalCount) = await uow.Posts.GetPagedAsync(
            request.Page, request.PageSize, request.CategoryId,
            request.Status, request.DateFrom, request.DateTo,
            request.SortByPopularity, ct);

        return new PagedResult<PostDto>
        {
            Items = mapper.Map<IEnumerable<PostDto>>(items),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

