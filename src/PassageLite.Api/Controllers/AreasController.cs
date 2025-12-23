using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassageLite.Application.DTOs;
using PassageLite.Application.Interfaces;
using PassageLite.Domain.Entities;

namespace PassageLite.Api.Controllers;

[Route("areas")]
[Authorize]
public class AreasController : BaseApiController
{
    private readonly IAreaService _areaService;
    private readonly ILogger<AreasController> _logger;

    public AreasController(IAreaService areaService, ILogger<AreasController> logger)
    {
        _areaService = areaService;
        _logger = logger;
    }

    /// <summary>
    /// Get all areas
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AreaDto>>> GetAreas()
    {
        var areas = await _areaService.GetAllAreasAsync();
        return Ok(areas);
    }

    /// <summary>
    /// Create a new area (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<AreaDto>> CreateArea([FromBody] CreateAreaRequest request)
    {
        _logger.LogInformation("Creating new area: {AreaName}", request.Name);
        var area = await _areaService.CreateAreaAsync(request);
        _logger.LogInformation("Area created with ID: {AreaId}", area.Id);
        return CreatedAtAction(nameof(GetAreas), new { id = area.Id }, area);
    }
}
