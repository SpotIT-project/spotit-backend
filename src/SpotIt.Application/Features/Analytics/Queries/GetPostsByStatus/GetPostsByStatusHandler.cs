using MediatR;
using SpotIt.Application.DTOs;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Analytics.Queries.GetPostsByStatus;

public class GetPostsByStatusHandler(IUnitOfWork uow)
    : IRequestHandler<GetPostsByStatusQuery, IEnumerable<PostsByStatusDto>>
{
    public async Task<IEnumerable<PostsByStatusDto>> Handle(GetPostsByStatusQuery request, CancellationToken ct)
    {
        var posts = await uow.Posts.GetAllAsync(ct);

        return posts
            .GroupBy(p => p.Status)
            .Select(g => new PostsByStatusDto { Status = g.Key, Count = g.Count() });
    }
}
