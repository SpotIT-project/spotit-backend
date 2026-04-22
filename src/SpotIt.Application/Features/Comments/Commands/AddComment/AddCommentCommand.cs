using MediatR;

namespace SpotIt.Application.Features.Comments.Commands.AddComment;

public record AddCommentCommand(Guid PostId, string Content, bool IsOfficialResponse) : IRequest<Unit>;
