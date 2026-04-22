using Microsoft.EntityFrameworkCore;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;
using SpotIt.Infrastructure.Data;

namespace SpotIt.Infrastructure.Repositories;

public class CommentRepository(AppDbContext context) : Repository<Comment>(context), ICommentRepository
{
    public async Task<IEnumerable<Comment>> GetByPostIdAsync(Guid postId, CancellationToken ct = default)
        => await _context.Comments
            .Include(c => c.Author)
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
}
