using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PassageLite.Api.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub) 
                          ?? User.FindFirst(ClaimTypes.NameIdentifier);
        
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        
        return userId;
    }

    protected string GetCurrentUserRole()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role);
        return roleClaim?.Value ?? string.Empty;
    }
}
