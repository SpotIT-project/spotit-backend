using MediatR;
using SpotIt.Application.DTOs;

namespace SpotIt.Application.Features.Comments.Queries.GetComments;

public record GetCommentsQuery(Guid PostId) : IRequest<IEnumerable<CommentDto>>;
