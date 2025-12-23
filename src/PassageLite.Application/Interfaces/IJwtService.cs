using PassageLite.Application.DTOs;

namespace PassageLite.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(UserDto user);
    DateTime GetTokenExpiration();
}
