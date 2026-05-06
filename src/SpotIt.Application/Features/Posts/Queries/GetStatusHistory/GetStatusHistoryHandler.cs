using AutoMapper;
using MediatR;
using SpotIt.Application.DTOs;
using SpotIt.Application.Exceptions;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;

namespace SpotIt.Application.Features.Posts.Queries.GetStatusHistory;

public class GetStatusHistoryHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<GetStatusHistoryQuery, IEnumerable<StatusHistoryDto>>
{
    public async Task<IEnumerable<StatusHistoryDto>> Handle(GetStatusHistoryQuery request, CancellationToken ct)
    {
        var post = await uow.Posts.GetByIdAsync(request.PostId, ct);
        if (post is null) throw new NotFoundException(nameof(Post), request.PostId);

        var history = await uow.StatusHistory.FindAsync(h => h.PostId == request.PostId, ct);

        // Repository.FindAsync calls ToListAsync internally, so the result is already
        // materialized as a List<T>. OrderBy runs in-memory here — acceptable because
        // status history per post is expected to be a small set (< 100 rows).
        return mapper.Map<IEnumerable<StatusHistoryDto>>(history.OrderBy(h => h.ChangedAt));
    }
}
