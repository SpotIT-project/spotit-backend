using MediatR;
using SpotIt.Application.Common;
using SpotIt.Application.DTOs;
using SpotIt.Domain.Enums;

namespace SpotIt.Application.Features.Posts.Queries.GetPosts;

public record GetPostsQuery(int Page, int PageSize,
    int? CategoryId, PostStatus? Status, DateTime? DateFrom, DateTime? DateTo, bool SortByPopularity)
    : IRequest<PagedResult<PostDto>>;
