using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotIt.Application.Features.Comments.Commands.AddComment;
using SpotIt.Application.Features.Comments.Queries.GetComments;

namespace SpotIt.API.Controllers;

[ApiController]
[Route("api/posts/{postId}/comments")]
[Authorize]
public class CommentsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetComments(
        [FromRoute] Guid postId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await mediator.Send(new GetCommentsQuery(postId, page, pageSize));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddComment([FromRoute] Guid postId, [FromBody] AddCommentBody body)
    {
        var isOfficial = User.IsInRole("CityHallEmployee") || User.IsInRole("Admin");
        await mediator.Send(new AddCommentCommand(postId, body.Content, isOfficial));
        return Created();
    }
}

public record AddCommentBody(string Content);
