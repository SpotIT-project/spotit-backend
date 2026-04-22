using MediatR;
using Microsoft.AspNetCore.Mvc;
using SpotIt.Application.Features.Categories.Queries.GetCategories;

namespace SpotIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await mediator.Send(new GetCategoriesQuery());
        return Ok(categories);
    }
}
