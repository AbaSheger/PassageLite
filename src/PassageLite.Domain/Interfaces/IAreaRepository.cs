using PassageLite.Domain.Entities;

namespace PassageLite.Domain.Interfaces;

public interface IAreaRepository
{
    Task<Area?> GetByIdAsync(Guid id);
    Task<IEnumerable<Area>> GetAllAsync();
    Task AddAsync(Area area);
    Task UpdateAsync(Area area);
}
