using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.Models.Auth;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Exceptions;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Infrastructure.Security;

namespace WitchCityRope.Infrastructure.Services
{
    /// <summary>
    /// Service implementation for identity-related operations using ASP.NET Core Identity
    /// </summary>
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<WitchCityRopeUser> _userManager;
        private readonly SignInManager<WitchCityRopeUser> _signInManager;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IEmailService _emailService;
        private readonly IEncryptionService _encryptionService;
        private readonly WitchCityRopeIdentityDbContext _context;
        private readonly ILogger<IdentityService> _logger;

        public IdentityService(
            UserManager<WitchCityRopeUser> userManager,
            SignInManager<WitchCityRopeUser> signInManager,
            JwtTokenService jwtTokenService,
            IEmailService emailService,
            IEncryptionService encryptionService,
            WitchCityRopeIdentityDbContext context,
            ILogger<IdentityService> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Validate age (must be 21+)
                var age = CalculateAge(request.DateOfBirth);
                if (age < 21)
                {
                    throw new ValidationException("User must be at least 21 years old to register.");
                }

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    throw new ConflictException("A user with this email address already exists.");
                }

                // Check if scene name is already taken
                var sceneNameTaken = await _context.Users
                    .AnyAsync(u => u.SceneName.Value == request.SceneName);
                if (sceneNameTaken)
                {
                    throw new ConflictException("This scene name is already taken.");
                }

                // Encrypt legal name
                var encryptedLegalName = await _encryptionService.EncryptAsync(request.LegalName);

                // Create new user
                var user = new WitchCityRopeUser(
                    encryptedLegalName,
                    SceneName.Create(request.SceneName),
                    EmailAddress.Create(request.Email),
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
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("User registration failed: {Errors}", errors);
                    throw new ValidationException($"Registration failed: {errors}");
                }

                // Assign default role
                await _userManager.AddToRoleAsync(user, "Attendee");

                // Generate email verification token
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                // Send verification email
                var emailSent = await SendVerificationEmailAsync(user, emailToken);

                _logger.LogInformation("User {Email} registered successfully with ID {UserId}", 
                    request.Email, user.Id);

                return new RegisterResponse
                {
                    UserId = user.Id,
                    Email = user.Email!,
                    EmailVerificationSent = emailSent,
                    Message = emailSent 
                        ? "Registration successful. Please check your email to verify your account."
                        : "Registration successful. Email verification could not be sent."
                };
            }
            catch (Core.DomainException ex)
            {
                _logger.LogWarning(ex, "Domain validation error during registration");
                throw new ValidationException(ex.Message);
            }
            catch (Exception ex) when (ex is not ValidationException && ex is not ConflictException)
            {
                _logger.LogError(ex, "Unexpected error during registration");
                throw;
            }
        }

        public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new UnauthorizedException("Invalid refresh token");
            }

            // Find the refresh token in the database
            var storedToken = await _context.Set<RefreshToken>()
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

            if (storedToken == null)
            {
                throw new UnauthorizedException("Invalid refresh token");
            }

            // Check if token has expired
            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new UnauthorizedException("Refresh token has expired");
            }

            // Get the associated user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == storedToken.UserId);

            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedException("User not found or inactive");
            }

            // Check if email is confirmed
            if (!user.EmailConfirmed)
            {
                throw new UnauthorizedException("Email not confirmed");
            }

            // Get user roles and claims
            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);

            // Generate new JWT token
            var newToken = _jwtTokenService.GenerateToken(user);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(60); // 60 minutes for JWT

            // Revoke old refresh token
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReplacedByToken = newRefreshToken;

            // Create new refresh token entity
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(30), // 30-day refresh token lifetime
                CreatedAt = DateTime.UtcNow
            };

            user.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Token refreshed for user {UserId}", user.Id);

            return new LoginResponse
            {
                Token = newToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = expiresAt,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    SceneName = user.SceneNameValue,
                    Role = user.Role, // Use the user's actual role from WitchCityRopeUser
                    IsActive = user.IsActive
                }
            };
        }

        public async Task<bool> VerifyEmailAsync(Guid userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new NotFoundException($"User with ID {userId} not found");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("Email verified for user {UserId}", userId);
                return true;
            }

            _logger.LogWarning("Email verification failed for user {UserId}: {Errors}", 
                userId, string.Join("; ", result.Errors.Select(e => e.Description)));
            return false;
        }

        private async Task<bool> SendVerificationEmailAsync(WitchCityRopeUser user, string token)
        {
            try
            {
                var verificationUrl = $"https://witchcityrope.com/verify-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
                
                var subject = "Verify Your WitchCityRope Account";
                var body = $@"
                    <h2>Welcome to WitchCityRope!</h2>
                    <p>Hi {user.SceneNameValue},</p>
                    <p>Thank you for registering. Please verify your email address by clicking the link below:</p>
                    <p><a href='{verificationUrl}'>Verify Email</a></p>
                    <p>If you cannot click the link, copy and paste this URL into your browser:</p>
                    <p>{verificationUrl}</p>
                    <p>This link will expire in 24 hours.</p>
                    <p>If you did not create an account, please ignore this email.</p>
                    <br/>
                    <p>Best regards,<br/>The WitchCityRope Team</p>
                ";

                await _emailService.SendEmailAsync(EmailAddress.Create(user.Email!), subject, body, isHtml: true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
                return false;
            }
        }

        private static int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}