using Microsoft.EntityFrameworkCore;
using PassageLite.Domain.Entities;
using PassageLite.Domain.Interfaces;
using PassageLite.Infrastructure.Data;

namespace PassageLite.Infrastructure.Repositories;

public class AreaRepository : IAreaRepository
{
    private readonly AppDbContext _context;

    public AreaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Area?> GetByIdAsync(Guid id)
    {
        return await _context.Areas.FindAsync(id);
    }

    public async Task<IEnumerable<Area>> GetAllAsync()
    {
        return await _context.Areas.ToListAsync();
    }

    public async Task AddAsync(Area area)
    {
        await _context.Areas.AddAsync(area);
    }

    public Task UpdateAsync(Area area)
    {
        _context.Areas.Update(area);
        return Task.CompletedTask;
    }
}
