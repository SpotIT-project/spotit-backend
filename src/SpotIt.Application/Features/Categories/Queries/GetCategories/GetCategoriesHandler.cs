using AutoMapper;
using MediatR;
using SpotIt.Application.DTOs;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Categories.Queries.GetCategories;

public class GetCategoriesHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<GetCategoriesQuery, IEnumerable<CategoryDto>>
{
    public async Task<IEnumerable<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var categories = await uow.Categories.GetAllAsync(ct);
        return mapper.Map<IEnumerable<CategoryDto>>(categories);
    }
}
