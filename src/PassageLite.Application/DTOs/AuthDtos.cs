using System.ComponentModel.DataAnnotations;

namespace PassageLite.Application.DTOs;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password
);

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    UserDto User
);

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role
);
