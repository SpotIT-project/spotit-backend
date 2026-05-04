using MediatR;
using SpotIt.Application.DTOs;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Analytics.Queries.GetTopCategories;

public class GetTopCategoriesHandler(IUnitOfWork uow)
    : IRequestHandler<GetTopCategoriesQuery, IEnumerable<TopCategoryDto>>
{
    public async Task<IEnumerable<TopCategoryDto>> Handle(GetTopCategoriesQuery request, CancellationToken ct)
    {
        var results = await uow.Posts.GetTopCategoriesAsync(5, ct);
        return results.Select(r => new TopCategoryDto { CategoryName = r.CategoryName, PostCount = r.PostCount });
    }
}
