using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotIt.Application.Features.Analytics.Queries.GetPostsByStatus;
using SpotIt.Application.Features.Analytics.Queries.GetTopCategories;
using SpotIt.Application.Authorization;

namespace SpotIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.Analytics.View)]
public class AnalyticsController(IMediator mediator) : ControllerBase
{
    [HttpGet("by-status")]
    public async Task<IActionResult> GetByStatus()
    {
        var result = await mediator.Send(new GetPostsByStatusQuery());
        return Ok(result);
    }

    [HttpGet("top-categories")]
    public async Task<IActionResult> GetTopCategories()
    {
        var result = await mediator.Send(new GetTopCategoriesQuery());
        return Ok(result);
    }
}
