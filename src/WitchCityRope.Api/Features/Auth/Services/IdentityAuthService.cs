using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Features.Auth.Models;
using WitchCityRope.Api.Interfaces;
using WitchCityRope.Core;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Exceptions;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Api.Features.Auth.Services
{
    /// <summary>
    /// Authentication service implementation using ASP.NET Core Identity
    /// </summary>
    public class IdentityAuthService : IAuthService
    {
        private readonly UserManager<WitchCityRopeUser> _userManager;
        private readonly SignInManager<WitchCityRopeUser> _signInManager;
        private readonly IJwtService _jwtService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<IdentityAuthService> _logger;
        private readonly IEmailService _emailService;
        private readonly IEncryptionService _encryptionService;

        public IdentityAuthService(
            UserManager<WitchCityRopeUser> userManager,
            SignInManager<WitchCityRopeUser> signInManager,
            IJwtService jwtService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<IdentityAuthService> logger,
            IEmailService emailService,
            IEncryptionService encryptionService)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Login attempt for email: {Email}", request.Email);

            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found for email {Email}", request.Email);
                throw new Api.Exceptions.UnauthorizedException("Invalid email or password");
            }

            // Check if account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: Account inactive for user {UserId}", user.Id);
                throw new Api.Exceptions.UnauthorizedException("Account is deactivated");
            }

            // Check if email is verified
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("Login failed: Email not verified for user {UserId}", user.Id);
                throw new Api.Exceptions.UnauthorizedException("Please verify your email before logging in");
            }

            // Check if account is locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Login failed: Account locked out for user {UserId}", user.Id);
                throw new Api.Exceptions.UnauthorizedException("Account is locked due to multiple failed login attempts");
            }

            var httpContext = _httpContextAccessor.HttpContext;
            var isCookieAuth = httpContext != null && 
                              httpContext.Request.Headers["Accept"].ToString().Contains("text/html");

            bool isPasswordValid;
            
            if (isCookieAuth)
            {
                // For cookie authentication (web UI), use SignInManager
                var signInResult = await _signInManager.PasswordSignInAsync(
                    user,
                    request.Password,
                    isPersistent: true,
                    lockoutOnFailure: true);

                if (signInResult.RequiresTwoFactor)
                {
                    _logger.LogInformation("Two-factor authentication required for user {UserId}", user.Id);
                    throw new Api.Exceptions.UnauthorizedException("Two-factor authentication required");
                }

                if (signInResult.IsLockedOut)
                {
                    _logger.LogWarning("Account locked out after failed attempt for user {UserId}", user.Id);
                    throw new Api.Exceptions.UnauthorizedException("Account is locked due to multiple failed login attempts");
                }

                isPasswordValid = signInResult.Succeeded;
            }
            else
            {
                // For JWT authentication (API), use UserManager
                isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
                
                if (!isPasswordValid)
                {
                    // Increment failed login count
                    await _userManager.AccessFailedAsync(user);
                    
                    if (await _userManager.IsLockedOutAsync(user))
                    {
                        _logger.LogWarning("Account locked out after failed attempt for user {UserId}", user.Id);
                        throw new Api.Exceptions.UnauthorizedException("Account is locked due to multiple failed login attempts");
                    }
                }
                else
                {
                    // Reset failed login count on successful password check
                    await _userManager.ResetAccessFailedCountAsync(user);
                }
            }

            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);
                throw new Api.Exceptions.UnauthorizedException("Invalid email or password");
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Login successful for user {UserId}", user.Id);

            // For cookie auth, we don't need to return JWT tokens
            if (isCookieAuth)
            {
                return new LoginResponse
                {
                    Token = string.Empty, // No JWT for cookie auth
                    RefreshToken = string.Empty,
                    ExpiresAt = DateTime.UtcNow.AddDays(30), // Cookie expiration
                    User = MapToUserDto(user)
                };
            }

            // Generate JWT token for API authentication
            var userWithAuth = await MapToUserWithAuth(user);
            var jwtToken = _jwtService.GenerateToken(userWithAuth);

            // Store refresh token
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = jwtToken.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsRevoked = false
            };

            user.RefreshTokens.Add(refreshToken);
            await _userManager.UpdateAsync(user);

            return new LoginResponse
            {
                Token = jwtToken.Token,
                RefreshToken = jwtToken.RefreshToken,
                ExpiresAt = jwtToken.ExpiresAt,
                User = MapToUserDto(user)
            };
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: Email already exists {Email}", request.Email);
                throw new Api.Exceptions.ConflictException("An account with this email already exists");
            }

            // Check if scene name already exists
            var sceneNameExists = await _userManager.Users
                .AnyAsync(u => u.SceneNameValue == request.SceneName);
            if (sceneNameExists)
            {
                _logger.LogWarning("Registration failed: Scene name already exists {SceneName}", request.SceneName);
                throw new Api.Exceptions.ConflictException("This scene name is already taken");
            }

            // Encrypt legal name
            var encryptedLegalName = _encryptionService.Encrypt(request.LegalName);

            // Create value objects
            var email = EmailAddress.Create(request.Email);
            var sceneName = SceneName.Create(request.SceneName);

            // Create new user
            var user = new WitchCityRopeUser(
                encryptedLegalName,
                sceneName,
                email,
                request.DateOfBirth,
                UserRole.Attendee);

            // Set optional properties
            if (!string.IsNullOrWhiteSpace(request.PronouncedName))
            {
                user.UpdatePronouncedName(request.PronouncedName);
            }

            if (!string.IsNullOrWhiteSpace(request.Pronouns))
            {
                user.UpdatePronouns(request.Pronouns);
            }

            // Create user with password
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Registration failed for {Email}: {Errors}", request.Email, errors);
                throw new Api.Exceptions.ValidationException($"Registration failed: {errors}");
            }

            // Generate email verification token
            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            user.EmailVerificationToken = emailToken;
            user.EmailVerificationTokenCreatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Send verification email
            bool emailSent = false;
            try
            {
                await _emailService.SendVerificationEmailAsync(
                    user.Email,
                    user.DisplayName,
                    emailToken);
                emailSent = true;
                _logger.LogInformation("Verification email sent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
            }

            _logger.LogInformation("Registration successful for user {UserId}", user.Id);

            return new RegisterResponse
            {
                UserId = user.Id,
                Email = user.Email,
                EmailVerificationSent = emailSent,
                Message = emailSent 
                    ? "Registration successful! Please check your email to verify your account."
                    : "Registration successful! However, we couldn't send the verification email. Please contact support."
            };
        }

        public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentException("Refresh token is required", nameof(refreshToken));

            _logger.LogInformation("Refresh token attempt");

            // Find the refresh token in the database
            var storedToken = await _userManager.Users
                .SelectMany(u => u.RefreshTokens)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

            if (storedToken == null)
            {
                _logger.LogWarning("Refresh token not found or revoked");
                throw new Api.Exceptions.UnauthorizedException("Invalid refresh token");
            }

            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token expired");
                throw new Api.Exceptions.UnauthorizedException("Refresh token expired");
            }

            // Get the user
            var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("User not found or inactive for refresh token");
                throw new Api.Exceptions.UnauthorizedException("Invalid refresh token");
            }

            // Revoke the old refresh token
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;

            // Generate new tokens
            var userWithAuth = await MapToUserWithAuth(user);
            var jwtToken = _jwtService.GenerateToken(userWithAuth);

            // Store new refresh token
            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = jwtToken.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsRevoked = false
            };

            storedToken.ReplacedByToken = jwtToken.RefreshToken;
            user.RefreshTokens.Add(newRefreshToken);
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Refresh token successful for user {UserId}", user.Id);

            return new LoginResponse
            {
                Token = jwtToken.Token,
                RefreshToken = jwtToken.RefreshToken,
                ExpiresAt = jwtToken.ExpiresAt,
                User = MapToUserDto(user)
            };
        }

        public async Task VerifyEmailAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Verification token is required", nameof(token));

            _logger.LogInformation("Email verification attempt");

            // Find user by token
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

            if (user == null)
            {
                _logger.LogWarning("Email verification failed: Invalid token");
                throw new NotFoundException("Invalid verification token");
            }

            // Check if token is expired (24 hours)
            if (user.EmailVerificationTokenCreatedAt.HasValue &&
                user.EmailVerificationTokenCreatedAt.Value.AddHours(24) < DateTime.UtcNow)
            {
                _logger.LogWarning("Email verification failed: Token expired for user {UserId}", user.Id);
                throw new Api.Exceptions.ValidationException("Verification token has expired");
            }

            // Confirm the email
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Email confirmation failed for user {UserId}: {Errors}", user.Id, errors);
                throw new Api.Exceptions.ValidationException($"Email verification failed: {errors}");
            }

            // Clear the verification token
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenCreatedAt = null;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Email verified successfully for user {UserId}", user.Id);
        }

        private UserDto MapToUserDto(WitchCityRopeUser user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                SceneName = user.SceneNameValue ?? string.Empty,
                Role = user.Role,
                IsActive = user.IsActive
            };
        }

        /// <summary>
        /// Authenticates a user that is already authenticated in the Web service
        /// </summary>
        public async Task<LoginResponse> WebServiceLoginAsync(WebServiceLoginRequest request)
        {
            try
            {
                _logger.LogInformation("Web service login requested for email: {Email}", request.Email);

                // Validate the request
                if (request.AuthenticationType != "web-service")
                {
                    throw new Services.UnauthorizedException("Invalid authentication type");
                }

                // Find user by email
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null || user.Id.ToString() != request.UserId)
                {
                    _logger.LogWarning("Web service login failed: User not found or ID mismatch for {Email}", request.Email);
                    throw new Services.UnauthorizedException("User not found or ID mismatch");
                }

                // Check if account is active
                if (!user.IsActive)
                {
                    _logger.LogWarning("Web service login failed: Account inactive for {Email}", request.Email);
                    throw new Services.UnauthorizedException("Account is not active");
                }

                // Generate tokens
                var userWithAuth = await MapToUserWithAuth(user);
                var token = _jwtService.GenerateToken(userWithAuth);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Web service login successful for {Email}", request.Email);

                return new LoginResponse
                {
                    Token = token.Token,
                    RefreshToken = refreshToken,
                    ExpiresAt = token.ExpiresAt,
                    User = MapToUserDto(user)
                };
            }
            catch (Services.UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during web service login for {Email}", request.Email);
                throw new Services.UnauthorizedException("Authentication failed");
            }
        }

        /// <summary>
        /// Gets a JWT token for a user on behalf of the Web service
        /// This is used for service-to-service authentication where the Web service
        /// needs to get a JWT token for an already authenticated user
        /// </summary>
        public async Task<LoginResponse> GetServiceTokenAsync(string userId, string email)
        {
            try
            {
                _logger.LogInformation("Service token requested for user {UserId} with email {Email}", userId, email);

                // Find user by ID
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Service token request failed: User not found for {UserId}", userId);
                    throw new Services.UnauthorizedException("User not found or email mismatch");
                }
                
                if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Service token request failed: Email mismatch. Expected: {Expected}, Got: {Got}", user.Email, email);
                    throw new Services.UnauthorizedException("User not found or email mismatch");
                }

                // Check if account is active
                if (!user.IsActive)
                {
                    _logger.LogWarning("Service token request failed: Account inactive for user {UserId}", userId);
                    throw new Services.UnauthorizedException("Account is deactivated");
                }

                // Check if email is verified
                if (!user.EmailConfirmed)
                {
                    _logger.LogWarning("Service token request failed: Email not verified for user {UserId}", userId);
                    throw new Services.UnauthorizedException("Email not verified");
                }

                // Generate JWT token and refresh token
                var userWithAuth = await MapToUserWithAuth(user);
                var token = _jwtService.GenerateToken(userWithAuth);
                var refreshToken = _jwtService.GenerateRefreshToken();

                _logger.LogInformation("Service token generated successfully for user {UserId}", userId);

                return new LoginResponse
                {
                    Token = token.Token,
                    RefreshToken = refreshToken,
                    ExpiresAt = token.ExpiresAt,
                    User = MapToUserDto(user)
                };
            }
            catch (Services.UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating service token for user {UserId}", userId);
                throw new Services.UnauthorizedException("Failed to generate service token");
            }
        }

        private Task<UserWithAuth> MapToUserWithAuth(WitchCityRopeUser identityUser)
        {
            var userWithAuth = new UserWithAuth
            {
                User = identityUser,
                PasswordHash = identityUser.PasswordHash ?? string.Empty,
                EmailVerified = identityUser.EmailConfirmed,
                PronouncedName = identityUser.PronouncedName,
                Pronouns = identityUser.Pronouns,
                LastLoginAt = identityUser.LastLoginAt
            };

            return Task.FromResult(userWithAuth);
        }
    }
}