using PassageLite.Application.DTOs;

namespace PassageLite.Application.Interfaces;

public interface IAccessService
{
    Task<AccessGrantDto> GrantAccessAsync(GrantAccessRequest request);
    Task<bool> RevokeAccessAsync(RevokeAccessRequest request);
    Task<IEnumerable<AccessGrantDto>> GetUserAccessGrantsAsync(Guid userId);
    Task<AccessCheckResult> CheckAccessAsync(Guid userId, Guid areaId);
}
