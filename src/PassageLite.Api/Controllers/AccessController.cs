using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassageLite.Application.DTOs;
using PassageLite.Application.Interfaces;
using PassageLite.Domain.Entities;

namespace PassageLite.Api.Controllers;

[Route("access")]
[Authorize]
public class AccessController : BaseApiController
{
    private readonly IAccessService _accessService;
    private readonly ILogger<AccessController> _logger;

    public AccessController(IAccessService accessService, ILogger<AccessController> logger)
    {
        _accessService = accessService;
        _logger = logger;
    }

    /// <summary>
    /// Grant access to a user for an area (Admin only)
    /// </summary>
    [HttpPost("grant")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<AccessGrantDto>> GrantAccess([FromBody] GrantAccessRequest request)
    {
        try
        {
            _logger.LogInformation("Granting access for user {UserId} to area {AreaId}", request.UserId, request.AreaId);
            var grant = await _accessService.GrantAccessAsync(request);
            _logger.LogInformation("Access granted: {GrantId}", grant.Id);
            return Ok(grant);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Failed to grant access: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Revoke access from a user for an area (Admin only)
    /// </summary>
    [HttpPost("revoke")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> RevokeAccess([FromBody] RevokeAccessRequest request)
    {
        _logger.LogInformation("Revoking access for user {UserId} from area {AreaId}", request.UserId, request.AreaId);
        var success = await _accessService.RevokeAccessAsync(request);

        if (!success)
        {
            return NotFound(new { message = "No active access grant found" });
        }

        _logger.LogInformation("Access revoked for user {UserId} from area {AreaId}", request.UserId, request.AreaId);
        return Ok(new { message = "Access revoked successfully" });
    }

    /// <summary>
    /// Get current user's access grants
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<AccessGrantDto>>> GetMyAccessGrants()
    {
        var userId = GetCurrentUserId();
        var grants = await _accessService.GetUserAccessGrantsAsync(userId);
        return Ok(grants);
    }

    /// <summary>
    /// Check if current user has access to an area
    /// </summary>
    [HttpGet("check")]
    public async Task<ActionResult<AccessCheckResult>> CheckAccess([FromQuery] Guid areaId)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Checking access for user {UserId} to area {AreaId}", userId, areaId);
        var result = await _accessService.CheckAccessAsync(userId, areaId);
        return Ok(result);
    }
}
