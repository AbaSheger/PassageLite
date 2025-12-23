using System.ComponentModel.DataAnnotations;

namespace PassageLite.Application.DTOs;

public record AreaDto(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt
);

public record CreateAreaRequest(
    [Required, StringLength(256, MinimumLength = 1)] string Name,
    [StringLength(1024)] string? Description
);
