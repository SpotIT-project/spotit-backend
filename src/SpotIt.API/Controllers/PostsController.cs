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
using SpotIt.Application.Features.Posts.Commands.DeletePost;
using SpotIt.Application.Features.Posts.Commands.UploadPostPhoto;
using SpotIt.Application.Authorization;
using SpotIt.Application.Features.Posts.Queries.GetStatusHistory;

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
    [Authorize(Policy = Permissions.Posts.UpdateStatus)]
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        await _mediator.Send(new DeletePostCommand(id));
        return NoContent();
    }

    [HttpPost("{id}/photo")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPhoto([FromRoute] Guid id, [FromForm] IFormFile photo)
    {
        var url = await _mediator.Send(new UploadPostPhotoCommand(id, photo));
        return Ok(new { url });
    }

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetStatusHistory(Guid id)
    {
        var history = await _mediator.Send(new GetStatusHistoryQuery(id));
        return Ok(history);
    }
}

public record UpdateStatusBody(PostStatus NewStatus, string? Note);
