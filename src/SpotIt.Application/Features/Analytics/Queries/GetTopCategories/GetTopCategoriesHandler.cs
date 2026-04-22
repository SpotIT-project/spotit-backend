using MediatR;
using SpotIt.Application.DTOs;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Analytics.Queries.GetTopCategories;

public class GetTopCategoriesHandler(IUnitOfWork uow)
    : IRequestHandler<GetTopCategoriesQuery, IEnumerable<TopCategoryDto>>
{
    public async Task<IEnumerable<TopCategoryDto>> Handle(GetTopCategoriesQuery request, CancellationToken ct)
    {
        var posts = await uow.Posts.GetAllAsync(ct);
        var categories = await uow.Categories.GetAllAsync(ct);

        return posts
            .GroupBy(p => p.CategoryId)
            .Join(categories,
                group => group.Key,
                cat => cat.Id,
                (group, cat) => new TopCategoryDto { CategoryName = cat.Name, PostCount = group.Count() })
            .OrderByDescending(x => x.PostCount)
            .Take(5);
    }
}
