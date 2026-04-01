using SpotIt.Domain.Entities;

namespace SpotIt.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IPostRepository Posts { get; }
    IRepository<Comment> Comments { get; }
    IRepository<Category> Categories { get; }
    IRepository<Like> Likes { get; }
    IRepository<StatusHistory> StatusHistory { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
