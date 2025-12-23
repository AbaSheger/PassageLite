using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassageLite.Application.DTOs;
using PassageLite.Application.Interfaces;

namespace PassageLite.Api.Controllers;

[Route("auth")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and get JWT token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for {Email}", request.Email);

        var result = await _authService.LoginAsync(request);

        if (result == null)
        {
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        _logger.LogInformation("Successful login for {Email}", request.Email);
        return Ok(result);
    }

    /// <summary>
    /// Get current authenticated user info
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var user = await _authService.GetCurrentUserAsync(userId);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(user);
    }
}
