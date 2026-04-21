using MediatR;
using SpotIt.Application.DTOs;

namespace SpotIt.Application.Features.Analytics.Queries.GetTopCategories;

public record GetTopCategoriesQuery : IRequest<IEnumerable<TopCategoryDto>>;