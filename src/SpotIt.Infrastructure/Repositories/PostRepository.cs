using Microsoft.EntityFrameworkCore;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Enums;
using SpotIt.Domain.Interfaces;
using SpotIt.Infrastructure.Data;

namespace SpotIt.Infrastructure.Repositories;

public class PostRepository : Repository<Post>, IPostRepository
{
    public PostRepository(AppDbContext context) : base(context) { }

    public async Task<Post?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct)
    {
        return await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.Likes)
            .AsNoTracking()
            .FirstOrDefaultAsync(p=>p.Id==id,ct);
    }

    public async Task<(IEnumerable<Post> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        int? categoryId,
        PostStatus? status,
        DateTime? from,
        DateTime? to,
        bool sortByPopularity = false,
        string? search = null,
        CancellationToken ct = default)
    {
        var query = _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.Likes)
            .AsNoTracking();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (from.HasValue)
            query = query.Where(p => p.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.CreatedAt <= to.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Title, pattern) ||
                EF.Functions.ILike(p.Description, pattern));
        }

        var total = await query.CountAsync(ct);

        var orderedQuery = sortByPopularity
            ? query.OrderByDescending(p => p.Likes.Count).ThenByDescending(p => p.CreatedAt)
            : query.OrderByDescending(p => p.CreatedAt);

        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    
}
