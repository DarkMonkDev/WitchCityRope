using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Authentication.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Services;

namespace WitchCityRope.Api.Features.Authentication.Services;

/// <summary>
/// Authentication service using direct Entity Framework access
/// Example of the simplified vertical slice architecture pattern
/// </summary>
public class AuthenticationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly ReturnUrlValidator _returnUrlValidator;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtService jwtService,
        ReturnUrlValidator returnUrlValidator,
        ILogger<AuthenticationService> logger)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _returnUrlValidator = returnUrlValidator;
        _logger = logger;
    }

    /// <summary>
    /// Get current user by ID - Simple Entity Framework service - NO MediatR complexity
    /// </summary>
    public async Task<(bool Success, AuthUserResponse? Response, string Error)> GetCurrentUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Direct Entity Framework query for user lookup
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return (false, null, "User not found");
            }

            var response = new AuthUserResponse(user);

            _logger.LogDebug("Current user retrieved successfully: {UserId} ({SceneName})", userId, user.SceneName);
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user: {UserId}", userId);
            return (false, null, "Failed to retrieve user information");
        }
    }

    /// <summary>
    /// Authenticate user with email and password with optional return URL validation
    /// Direct Entity Framework and Identity service calls - NO MediatR complexity
    /// Implements OWASP-compliant return URL validation per BR-1 and SEC-1 requirements
    /// </summary>
    /// <param name="request">Login request containing email, password, and optional return URL</param>
    /// <param name="httpContext">HTTP context for return URL validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login response with validated return URL (defaults to /dashboard if validation fails)</returns>
    public async Task<(bool Success, LoginResponse? Response, string Error)> LoginAsync(
        LoginRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to find user by email first (most common case, indexed)
            var user = await _userManager.FindByEmailAsync(request.EmailOrSceneName);

            // If not found by email, try by scene name (case-insensitive)
            if (user == null)
            {
                user = await _context.Users.FirstOrDefaultAsync(
                    u => EF.Functions.ILike(u.SceneName, request.EmailOrSceneName),
                    cancellationToken);
            }

            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email or scene name: {Identifier}", request.EmailOrSceneName);
                return (false, null, "Invalid email/scene name or password");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Update last login time using direct Entity Framework
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                // Validate return URL if provided (OWASP-compliant security validation)
                string? validatedReturnUrl = null;
                if (!string.IsNullOrWhiteSpace(request.ReturnUrl))
                {
                    var validationResult = _returnUrlValidator.ValidateReturnUrl(
                        request.ReturnUrl,
                        httpContext,
                        user.Id.ToString());

                    if (validationResult.IsValid)
                    {
                        validatedReturnUrl = validationResult.ValidatedUrl;
                        _logger.LogInformation(
                            "Return URL validated for user {UserId}: {OriginalUrl} -> {ValidatedUrl}",
                            user.Id, validationResult.OriginalUrl, validatedReturnUrl);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Return URL validation failed for user {UserId}: {OriginalUrl}, Reason: {Reason}",
                            user.Id, validationResult.OriginalUrl, validationResult.FailureReason);
                        // validatedReturnUrl remains null - frontend will default to /dashboard
                    }
                }

                // Generate JWT token
                var jwtToken = _jwtService.GenerateToken(user);
                var response = new LoginResponse
                {
                    Token = jwtToken.Token,
                    ExpiresAt = jwtToken.ExpiresAt,
                    User = new AuthUserResponse(user),
                    ReturnUrl = validatedReturnUrl // Null if not provided or validation failed
                };

                _logger.LogInformation("User logged in successfully: {Email}", user.Email);
                return (true, response, string.Empty);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Account locked out for user: {Identifier}", request.EmailOrSceneName);
                return (false, null, "Account is temporarily locked due to failed login attempts");
            }

            _logger.LogWarning("Invalid login attempt for {Identifier}", request.EmailOrSceneName);
            return (false, null, "Invalid email/scene name or password");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for {Identifier}", request.EmailOrSceneName);
            return (false, null, "Login could not be completed at this time");
        }
    }

    /// <summary>
    /// Register new user account with validation
    /// Direct Entity Framework and Identity operations - NO MediatR complexity
    /// </summary>
    public async Task<(bool Success, AuthUserResponse? Response, string Error)> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if email already exists using direct Entity Framework
            var existingUser = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email == request.Email, cancellationToken);

            if (existingUser)
            {
                return (false, null, "Email address is already registered");
            }

            // Check if scene name already exists using direct Entity Framework
            var existingSceneName = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.SceneName == request.SceneName, cancellationToken);

            if (existingSceneName)
            {
                return (false, null, "Scene name is already taken");
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                SceneName = request.SceneName,
                EmailConfirmed = true, // Auto-confirm for testing
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("User registration failed for {Email}: {Errors}", request.Email, errors);
                return (false, null, errors);
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            var response = new AuthUserResponse(user);

            _logger.LogInformation("User registered successfully: {Email} ({SceneName})", user.Email, user.SceneName);
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for {Email}", request.Email);
            return (false, null, "Registration could not be completed at this time");
        }
    }

    /// <summary>
    /// Generate service token for existing authenticated user
    /// Used for service-to-service authentication bridge - Direct Entity Framework access
    /// </summary>
    public async Task<(bool Success, LoginResponse? Response, string Error)> GetServiceTokenAsync(
        string userId,
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate user exists and email matches using direct Entity Framework
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId && u.Email == email, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Service token request failed - user not found or email mismatch: {UserId}, {Email}", userId, email);
                return (false, null, "User not found or email mismatch");
            }

            // Validate user is active
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("Service token request failed - user email not confirmed: {UserId}", userId);
                return (false, null, "Account is not active or email not verified");
            }

            // Generate JWT token
            var jwtToken = _jwtService.GenerateToken(user);
            var response = new LoginResponse
            {
                Token = jwtToken.Token,
                ExpiresAt = jwtToken.ExpiresAt,
                User = new AuthUserResponse(user)
            };

            _logger.LogDebug("Service token generated for user: {UserId}", userId);
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service token generation failed for {UserId}", userId);
            return (false, null, "Service token could not be generated at this time");
        }
    }
}