using PassageLite.Application.DTOs;
using PassageLite.Application.Interfaces;
using PassageLite.Domain.Interfaces;

namespace PassageLite.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;

    public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var userDto = new UserDto(user.Id, user.Email, user.FullName, user.Role);
        var token = _jwtService.GenerateToken(userDto);
        var expiresAt = _jwtService.GetTokenExpiration();

        return new LoginResponse(token, expiresAt, userDto);
    }

    public async Task<UserDto?> GetCurrentUserAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        
        if (user == null)
        {
            return null;
        }

        return new UserDto(user.Id, user.Email, user.FullName, user.Role);
    }
}
