using Microsoft.AspNetCore.Mvc;
using ReelSchedulerPro.Application.Services;
using ReelSchedulerPro.Shared.DTOs.Authentication;
using ReelSchedulerPro.Shared.Constants;
using Serilog;

namespace ReelSchedulerPro.Api.Controllers;

/// <summary>
/// Authentication controller with complete implementation
/// </summary>
[ApiController]
[Route(ApiConstants.Endpoints.Auth)]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", request.Email);
            var response = await _authService.LoginAsync(request, cancellationToken);
            _logger.LogInformation("Login successful for email: {Email}", request.Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email: {Email}", request.Email);
            return Unauthorized(new { error = "Invalid credentials" });
        }
    }

    /// <summary>
    /// Register new user with organization
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Registration attempt for email: {Email}", request.Email);
            var response = await _authService.RegisterAsync(request, cancellationToken);
            _logger.LogInformation("Registration successful for email: {Email}", request.Email);
            return CreatedAtAction(nameof(Register), response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for email: {Email}", request.Email);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Token refresh attempt");
            var response = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return Unauthorized(new { error = "Invalid refresh token" });
        }
    }

    /// <summary>
    /// Verify email address with verification code
    /// </summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyEmail([FromQuery] string email, [FromQuery] string code, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Email verification attempt for: {Email}", email);
            var result = await _authService.VerifyEmailAsync(email, code, cancellationToken);
            if (result)
            {
                _logger.LogInformation("Email verified successfully for: {Email}", email);
                return Ok(new { message = "Email verified successfully" });
            }
            return BadRequest(new { error = "Invalid verification code" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification failed for: {Email}", email);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get current user profile (requires authentication)
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            var email = User.FindFirst("email")?.Value;
            var fullName = User.FindFirst("name")?.Value;
            var role = User.FindFirst("role")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated" });

            var user = new UserDto
            {
                Id = Guid.Parse(userId),
                Email = email ?? string.Empty,
                FullName = fullName ?? string.Empty,
                Role = role ?? "User"
            };

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user");
            return Unauthorized(new { error = "Invalid token" });
        }
    }
}

/// <summary>
/// Refresh token request model
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
