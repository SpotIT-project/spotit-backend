using SpotIt.Domain.Entities;
using SpotIt.Domain.Enums;

namespace SpotIt.Domain.Interfaces;

public interface IPostRepository : IRepository<Post>
{
    Task<(IEnumerable<Post> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        int? categoryId,
        PostStatus? status,
        DateTime? from,
        DateTime? to,
        bool sortByPopularity = false,
        CancellationToken ct = default);
    Task<Post?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct);
}
