using MediatR;
using SpotIt.Application.Common;
using SpotIt.Application.DTOs;

namespace SpotIt.Application.Features.Comments.Queries.GetComments;

public record GetCommentsQuery(Guid PostId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<CommentDto>>;
