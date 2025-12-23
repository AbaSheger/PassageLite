using Microsoft.EntityFrameworkCore;
using PassageLite.Domain.Entities;
using PassageLite.Domain.Interfaces;
using PassageLite.Infrastructure.Data;

namespace PassageLite.Infrastructure.Repositories;

public class AccessGrantRepository : IAccessGrantRepository
{
    private readonly AppDbContext _context;

    public AccessGrantRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AccessGrant?> GetByIdAsync(Guid id)
    {
        return await _context.AccessGrants
            .Include(g => g.User)
            .Include(g => g.Area)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<AccessGrant?> GetActiveGrantAsync(Guid userId, Guid areaId)
    {
        return await _context.AccessGrants
            .Include(g => g.Area)
            .FirstOrDefaultAsync(g => 
                g.UserId == userId && 
                g.AreaId == areaId && 
                !g.IsRevoked);
    }

    public async Task<IEnumerable<AccessGrant>> GetByUserIdAsync(Guid userId)
    {
        return await _context.AccessGrants
            .Include(g => g.Area)
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AccessGrant>> GetAllAsync()
    {
        return await _context.AccessGrants
            .Include(g => g.User)
            .Include(g => g.Area)
            .ToListAsync();
    }

    public async Task AddAsync(AccessGrant grant)
    {
        await _context.AccessGrants.AddAsync(grant);
    }

    public Task UpdateAsync(AccessGrant grant)
    {
        _context.AccessGrants.Update(grant);
        return Task.CompletedTask;
    }
}
