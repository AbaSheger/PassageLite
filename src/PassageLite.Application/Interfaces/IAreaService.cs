using PassageLite.Application.DTOs;

namespace PassageLite.Application.Interfaces;

public interface IAreaService
{
    Task<IEnumerable<AreaDto>> GetAllAreasAsync();
    Task<AreaDto?> GetAreaByIdAsync(Guid id);
    Task<AreaDto> CreateAreaAsync(CreateAreaRequest request);
}
