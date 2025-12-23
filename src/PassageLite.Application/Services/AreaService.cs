using PassageLite.Application.DTOs;
using PassageLite.Application.Interfaces;
using PassageLite.Domain.Entities;
using PassageLite.Domain.Interfaces;

namespace PassageLite.Application.Services;

public class AreaService : IAreaService
{
    private readonly IUnitOfWork _unitOfWork;

    public AreaService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<AreaDto>> GetAllAreasAsync()
    {
        var areas = await _unitOfWork.Areas.GetAllAsync();
        return areas.Select(a => new AreaDto(a.Id, a.Name, a.Description, a.CreatedAt));
    }

    public async Task<AreaDto?> GetAreaByIdAsync(Guid id)
    {
        var area = await _unitOfWork.Areas.GetByIdAsync(id);
        
        if (area == null)
        {
            return null;
        }

        return new AreaDto(area.Id, area.Name, area.Description, area.CreatedAt);
    }

    public async Task<AreaDto> CreateAreaAsync(CreateAreaRequest request)
    {
        var area = new Area
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Areas.AddAsync(area);
        await _unitOfWork.SaveChangesAsync();

        return new AreaDto(area.Id, area.Name, area.Description, area.CreatedAt);
    }
}
