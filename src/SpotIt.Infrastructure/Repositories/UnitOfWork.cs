using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;
using SpotIt.Infrastructure.Data;

namespace SpotIt.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IPostRepository Posts { get; }
    public IRepository<Comment> Comments { get; }
    public IRepository<Category> Categories { get; }
    public IRepository<Like> Likes { get; }
    public IRepository<StatusHistory> StatusHistory { get; }
    public IRepository<RefreshToken> RefreshTokens { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Posts = new PostRepository(context);
        Comments = new Repository<Comment>(context);
        Categories = new Repository<Category>(context);
        Likes = new Repository<Like>(context);
        StatusHistory = new Repository<StatusHistory>(context);
        RefreshTokens = new Repository<RefreshToken>(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public void Dispose()
        => _context.Dispose();
}
