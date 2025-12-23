using PassageLite.Domain.Entities;

namespace PassageLite.Domain.Interfaces;

public interface IAccessGrantRepository
{
    Task<AccessGrant?> GetByIdAsync(Guid id);
    Task<AccessGrant?> GetActiveGrantAsync(Guid userId, Guid areaId);
    Task<IEnumerable<AccessGrant>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<AccessGrant>> GetAllAsync();
    Task AddAsync(AccessGrant grant);
    Task UpdateAsync(AccessGrant grant);
}
