using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Api.Models.Auth;
using WitchCityRope.Api.Services;

namespace WitchCityRope.Api.Controllers;

/// <summary>
/// Authentication controller for user registration, login, and JWT token management
/// For authentication vertical slice test
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Register new user account
    /// </summary>
    /// <param name="registerDto">Registration data</param>
    /// <returns>Created user data</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error = "Validation failed",
                Details = string.Join("; ", errors)
            });
        }

        var (success, user, errorMessage) = await _authService.RegisterAsync(registerDto);

        if (success && user != null)
        {
            _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);
            return CreatedAtAction(
                nameof(GetUser),
                new { id = user.Id },
                new ApiResponse<UserDto>
                {
                    Success = true,
                    Data = user,
                    Message = "Account created successfully"
                });
        }

        _logger.LogWarning("Registration failed for {Email}: {Error}", registerDto.Email, errorMessage);
        return BadRequest(new ApiResponse<object>
        {
            Success = false,
            Error = errorMessage,
            Details = "Please check your input and try again"
        });
    }

    /// <summary>
    /// Authenticate user with email and password
    /// </summary>
    /// <param name="loginDto">Login credentials</param>
    /// <returns>User data and JWT token</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error = "Validation failed",
                Details = string.Join("; ", errors)
            });
        }

        var (success, response, errorMessage) = await _authService.LoginAsync(loginDto);

        if (success && response != null)
        {
            _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);
            return Ok(new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = response,
                Message = "Login successful"
            });
        }

        _logger.LogWarning("Login failed for {Email}: {Error}", loginDto.Email, errorMessage);
        return Unauthorized(new ApiResponse<object>
        {
            Success = false,
            Error = errorMessage,
            Details = "Please check your credentials and try again"
        });
    }

    /// <summary>
    /// Get current user information (requires authentication)
    /// Note: This is a placeholder for cookie-based authentication
    /// In real implementation, this would validate HttpOnly cookies
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User data</returns>
    [HttpGet("user/{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _authService.GetUserByIdAsync(id);

        if (user != null)
        {
            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Data = user
            });
        }

        return NotFound(new ApiResponse<object>
        {
            Success = false,
            Error = "User not found",
            Details = "The requested user does not exist"
        });
    }

    /// <summary>
    /// Generate JWT token for authenticated user (service-to-service authentication)
    /// Used by Web Service to get JWT tokens for API calls
    /// </summary>
    /// <param name="request">Service token request with user ID and email</param>
    /// <returns>JWT token for the user</returns>
    [HttpPost("service-token")]
    [AllowAnonymous] // Authentication is handled via shared secret
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetServiceToken([FromBody] ServiceTokenRequest request)
    {
        // Validate service secret from header
        var serviceSecret = Request.Headers["X-Service-Secret"].FirstOrDefault();
        var expectedSecret = _configuration["ServiceAuth:Secret"];

        if (string.IsNullOrEmpty(serviceSecret) || serviceSecret != expectedSecret)
        {
            _logger.LogWarning("Invalid service credentials in service token request");
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Error = "Invalid service credentials",
                Details = "Service secret is missing or incorrect"
            });
        }

        // Validate request data
        if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error = "Invalid request",
                Details = "User ID and email are required"
            });
        }

        var (success, response, errorMessage) = await _authService.GetServiceTokenAsync(request.UserId, request.Email);

        if (success && response != null)
        {
            _logger.LogDebug("Service token generated for user: {UserId}", request.UserId);
            return Ok(response);
        }

        if (errorMessage.Contains("not found"))
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Error = "User not found",
                Details = "No user found with the provided ID and email"
            });
        }

        return BadRequest(new ApiResponse<object>
        {
            Success = false,
            Error = errorMessage,
            Details = "Service token could not be generated"
        });
    }

    /// <summary>
    /// Logout endpoint (placeholder for throwaway implementation)
    /// In real implementation, this would clear HttpOnly cookies
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public IActionResult Logout()
    {
        // For throwaway implementation, just return success
        // Real implementation would clear cookies and invalidate tokens
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Logged out successfully"
        });
    }
}

/// <summary>
/// Standard API response wrapper for consistent response format
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public string? Details { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}