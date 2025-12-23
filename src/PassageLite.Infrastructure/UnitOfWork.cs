using PassageLite.Domain.Interfaces;
using PassageLite.Infrastructure.Data;
using PassageLite.Infrastructure.Repositories;

namespace PassageLite.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IUserRepository? _users;
    private IAreaRepository? _areas;
    private IAccessGrantRepository? _accessGrants;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IAreaRepository Areas => _areas ??= new AreaRepository(_context);
    public IAccessGrantRepository AccessGrants => _accessGrants ??= new AccessGrantRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
