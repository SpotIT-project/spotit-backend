using SpotIt.Domain.Entities;

namespace SpotIt.Domain.Interfaces;

public interface ICommentRepository : IRepository<Comment>
{
    Task<IEnumerable<Comment>> GetByPostIdAsync(Guid postId, CancellationToken ct = default);
}
