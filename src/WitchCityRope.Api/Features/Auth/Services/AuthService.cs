using System;
using System.Threading.Tasks;
using WitchCityRope.Api.Features.Auth.Models;
using WitchCityRope.Api.Interfaces;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Api.Features.Auth.Services
{
    /// <summary>
    /// Service responsible for authentication and user registration
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IEncryptionService _encryptionService;

        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtService jwtService,
            IEmailService emailService,
            IEncryptionService encryptionService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _emailService = emailService;
            _encryptionService = encryptionService;
        }

        /// <summary>
        /// Authenticates a user with email and password
        /// </summary>
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            // Validate user credentials
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedException("Invalid email or password");
            }

            // Check if account is active
            if (!user.IsActive)
            {
                throw new UnauthorizedException("Account is not active");
            }

            // Check if email is verified
            if (!user.EmailVerified)
            {
                throw new UnauthorizedException("Please verify your email address before logging in");
            }

            // Generate JWT token
            var userWithAuth = new UserWithAuth { User = user.User, PasswordHash = user.PasswordHash, EmailVerified = user.EmailVerified };
            var token = _jwtService.GenerateToken(userWithAuth);
            var refreshToken = _jwtService.GenerateRefreshToken();
            
            // Store refresh token
            await _userRepository.StoreRefreshTokenAsync(userWithAuth.Id, refreshToken, token.ExpiresAt.AddDays(7));
            
            // Update last login timestamp
            await _userRepository.UpdateLastLoginAsync(user.Id);

            return new LoginResponse
            {
                Token = token.Token,
                RefreshToken = refreshToken,
                ExpiresAt = token.ExpiresAt,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email.Value,
                    SceneName = user.SceneName.Value,
                    Role = user.Role.ToString(),
                    IsActive = user.IsActive
                }
            };
        }

        /// <summary>
        /// Registers a new user account
        /// </summary>
        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            // Validate age requirement (must be 21+ per domain rules)
            var age = DateTime.UtcNow.Year - request.DateOfBirth.Year;
            if (request.DateOfBirth.Date > DateTime.UtcNow.AddYears(-age)) age--;
            if (age < 21)
            {
                throw new ValidationException("Must be at least 21 years old to register");
            }

            // Check if email already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ConflictException("Email address is already registered");
            }

            // Check if scene name is already taken
            var sceneNameTaken = await _userRepository.IsSceneNameTakenAsync(request.SceneName);
            if (sceneNameTaken)
            {
                throw new ConflictException("Scene name is already taken");
            }

            // Create value objects
            var email = EmailAddress.Create(request.Email);
            var sceneName = SceneName.Create(request.SceneName);
            
            // Encrypt legal name for privacy
            var encryptedLegalName = _encryptionService.Encrypt(request.LegalName);

            // Create new user using Core entity
            var user = new User(
                encryptedLegalName: encryptedLegalName,
                sceneName: sceneName,
                email: email,
                dateOfBirth: request.DateOfBirth,
                role: UserRole.Attendee // New users start as Attendees
            );

            // Hash password and store authentication details
            var passwordHash = _passwordHasher.HashPassword(request.Password);
            var verificationToken = GenerateVerificationToken();

            // Create user authentication record
            var userAuth = new UserAuthentication
            {
                UserId = user.Id,
                PasswordHash = passwordHash,
                EmailVerificationToken = verificationToken,
                EmailVerificationTokenCreatedAt = DateTime.UtcNow
            };

            // Save user and authentication details
            await _userRepository.CreateAsync(user, userAuth);

            // Send verification email
            var emailSent = await _emailService.SendVerificationEmailAsync(
                email.Value,
                sceneName.Value,
                verificationToken
            );

            return new RegisterResponse
            {
                UserId = user.Id,
                Email = email.Value,
                EmailVerificationSent = emailSent,
                Message = "Registration successful. Please check your email to verify your account."
            };
        }

        /// <summary>
        /// Verifies a user's email address using the verification token
        /// </summary>
        public async Task VerifyEmailAsync(string token)
        {
            var userAuth = await _userRepository.GetByVerificationTokenAsync(token);
            if (userAuth == null)
            {
                throw new ValidationException("Invalid verification token");
            }

            // Check if token is expired (24 hours)
            if (userAuth.EmailVerificationTokenCreatedAt.AddHours(24) < DateTime.UtcNow)
            {
                throw new ValidationException("Verification token has expired");
            }

            // Mark email as verified
            await _userRepository.VerifyEmailAsync(userAuth.UserId);
        }

        /// <summary>
        /// Refreshes an expired JWT token using a refresh token
        /// </summary>
        public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
        {
            var tokenInfo = await _userRepository.GetRefreshTokenInfoAsync(refreshToken);
            if (tokenInfo == null || tokenInfo.ExpiresAt < DateTime.UtcNow)
            {
                throw new UnauthorizedException("Invalid or expired refresh token");
            }

            var user = await _userRepository.GetByIdAsync(tokenInfo.UserId);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedException("User not found or inactive");
            }

            // Generate new tokens
            // TODO: Create UserWithAuth from User
            var userWithAuth = new UserWithAuth { User = user };
            var newToken = _jwtService.GenerateToken(userWithAuth);
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            
            // Invalidate old refresh token and store new one
            await _userRepository.InvalidateRefreshTokenAsync(refreshToken);
            await _userRepository.StoreRefreshTokenAsync(user.Id, newRefreshToken, newToken.ExpiresAt.AddDays(7));

            return new LoginResponse
            {
                Token = newToken.Token,
                RefreshToken = newRefreshToken,
                ExpiresAt = newToken.ExpiresAt,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email.Value,
                    SceneName = user.SceneName.Value,
                    Role = user.Role.ToString(),
                    IsActive = user.IsActive
                }
            };
        }

        private string GenerateVerificationToken()
        {
            return Guid.NewGuid().ToString("N");
        }
    }

    // Local UserAuthentication model for Auth feature that extends Core entity
    public class UserAuthenticationDto
    {
        public Guid UserId { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string? EmailVerificationToken { get; set; }
        public DateTime EmailVerificationTokenCreatedAt { get; set; } = DateTime.UtcNow;
        public string? PronouncedName { get; set; }
        public string? Pronouns { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    // Custom exceptions
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message) { }
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}