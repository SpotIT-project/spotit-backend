using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotIt.Application.Features.Posts.Commands.CreatePost;
using SpotIt.Application.Features.Posts.Commands.UpdatePostStatus;
using SpotIt.Application.Features.Posts.Queries.GetPosts;
using SpotIt.Application.Features.Posts.Queries.GetPostById;
using SpotIt.Domain.Enums;
using SpotIt.Application.Features.Likes.Commands.LikePost;
using SpotIt.Application.Features.Likes.Commands.UnlikePost;

namespace SpotIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PostsController(IMediator mediator)
    {
        _mediator = mediator;

    }

    [HttpGet]
    public async Task<IActionResult> GetPosts([FromQuery] GetPostsQuery query)
    {
        var results = await _mediator.Send(query);
        return Ok(results);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPostById([FromRoute] Guid id)
    {
        var post= await _mediator.Send(new GetPostByIdQuery(id));
        return Ok(post);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostCommand command)
    {
        var id = await _mediator.Send(command);
        return Created();
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "CityHallEmployee,Admin")]
    public async Task<IActionResult> UpdateStatus([FromRoute] Guid id, [FromBody] UpdateStatusBody body)
    {
        await _mediator.Send(new UpdatePostStatusCommand(id, body.NewStatus, body.Note));
        return NoContent();
    }
    [HttpPost("{id}/likes")]
    public async Task<IActionResult> Like([FromRoute] Guid id)
    {
        await _mediator.Send(new LikePostCommand(id));
        return NoContent();
    }

    [HttpDelete("{id}/likes")]
    public async Task<IActionResult> UnLike([FromRoute] Guid id)
    {
        await _mediator.Send(new UnLikePostCommand(id));
        return NoContent();
    }
}

public record UpdateStatusBody(PostStatus NewStatus, string? Note);
