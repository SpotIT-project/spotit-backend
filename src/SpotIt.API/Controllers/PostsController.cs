using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotIt.Application.Features.Posts.Commands;
using SpotIt.Application.Features.Posts.Queries;

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
        var id=await _mediator.Send(command);
        return Created();
    }
}
