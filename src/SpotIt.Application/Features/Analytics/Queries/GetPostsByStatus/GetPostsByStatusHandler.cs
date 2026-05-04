using MediatR;
using SpotIt.Application.DTOs;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Analytics.Queries.GetPostsByStatus;

public class GetPostsByStatusHandler(IUnitOfWork uow)
    : IRequestHandler<GetPostsByStatusQuery, IEnumerable<PostsByStatusDto>>
{
    public async Task<IEnumerable<PostsByStatusDto>> Handle(GetPostsByStatusQuery request, CancellationToken ct)
    {
        var counts = await uow.Posts.GetStatusCountsAsync(ct);
        return counts.Select(kvp => new PostsByStatusDto { Status = kvp.Key, Count = kvp.Value });
    }
}
