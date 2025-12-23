using System.ComponentModel.DataAnnotations;

namespace PassageLite.Application.DTOs;

public record GrantAccessRequest(
    [Required] Guid UserId,
    [Required] Guid AreaId,
    [Required] DateTime ValidFrom,
    [Required] DateTime ValidTo
);

public record RevokeAccessRequest(
    [Required] Guid UserId,
    [Required] Guid AreaId
);

public record AccessGrantDto(
    Guid Id,
    Guid UserId,
    Guid AreaId,
    string AreaName,
    DateTime ValidFrom,
    DateTime ValidTo,
    bool IsRevoked,
    bool IsCurrentlyValid
);

public record AccessCheckResult(
    bool HasAccess,
    string Reason
);
