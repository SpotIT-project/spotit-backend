using MediatR;
using SpotIt.Application.Common;
using SpotIt.Application.DTOs;
using SpotIt.Domain.Enums;

namespace SpotIt.Application.Features.Posts.Queries.GetPosts;

public record GetPostsQuery(int Page = 1, int PageSize = 10,
    int? CategoryId = null, PostStatus? Status = null, DateTime? DateFrom = null, DateTime? DateTo = null,
    bool SortByPopularity = false, string? Search = null)
    : IRequest<PagedResult<PostDto>>;
