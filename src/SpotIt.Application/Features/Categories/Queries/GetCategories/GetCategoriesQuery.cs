using MediatR;
using SpotIt.Application.DTOs;

namespace SpotIt.Application.Features.Categories.Queries.GetCategories;

public record GetCategoriesQuery : IRequest<IEnumerable<CategoryDto>>;
