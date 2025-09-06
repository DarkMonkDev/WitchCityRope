using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using WitchCityRope.Api.Features.Auth.Models;
using WitchCityRope.Api.Features.Auth.Services;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Tests.Common.Builders;

namespace WitchCityRope.Api.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtServiceMock = new Mock<IJwtService>();
        _emailServiceMock = new Mock<IEmailService>();
        _encryptionServiceMock = new Mock<IEncryptionService>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtServiceMock.Object,
            _emailServiceMock.Object,
            _encryptionServiceMock.Object);
    }

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WhenValidCredentials_ShouldReturnSuccessfulLoginResponse()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "Password123!" };
        var user = CreateValidUserWithAuth();
        var token = new JwtToken { Token = "jwt-token", ExpiresAt = DateTime.UtcNow.AddHours(1) };
        var refreshToken = "refresh-token";

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);
        _jwtServiceMock.Setup(x => x.GenerateToken(user))
            .Returns(token);
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be(token.Token);
        result.RefreshToken.Should().Be(refreshToken);
        result.User.Email.Should().Be(user.Email.Value);
        result.User.SceneName.Should().Be(user.SceneName.Value);

        _userRepositoryMock.Verify(x => x.StoreRefreshTokenAsync(
            user.Id, refreshToken, It.IsAny<DateTime>()), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateLastLoginAsync(user.Id), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest { Email = "nonexistent@example.com", Password = "Password123!" };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync((UserWithAuth?)null);

        // Act & Assert
        await _authService.Invoking(x => x.LoginAsync(request))
            .Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WhenInvalidPassword_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "WrongPassword!" };
        var user = CreateValidUserWithAuth();

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(false);

        // Act & Assert
        await _authService.Invoking(x => x.LoginAsync(request))
            .Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WhenAccountNotActive_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "Password123!" };
        var user = CreateValidUserWithAuth(isActive: false);

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);

        // Act & Assert
        await _authService.Invoking(x => x.LoginAsync(request))
            .Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Account is not active");
    }

    [Fact]
    public async Task LoginAsync_WhenEmailNotVerified_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "Password123!" };
        var user = CreateValidUserWithAuth(emailVerified: false);

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);

        // Act & Assert
        await _authService.Invoking(x => x.LoginAsync(request))
            .Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Please verify your email address before logging in");
    }

    #endregion

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WhenValidRequest_ShouldCreateUserAndSendVerificationEmail()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "new@example.com",
            Password = "SecurePassword123!",
            SceneName = "NewUser",
            LegalName = "John Doe",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            PronouncedName = "John",
            Pronouns = "he/him"
        };

        var encryptedLegalName = "encrypted-legal-name";

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync((UserWithAuth?)null);
        _userRepositoryMock.Setup(x => x.IsSceneNameTakenAsync(request.SceneName))
            .ReturnsAsync(false);
        _encryptionServiceMock.Setup(x => x.Encrypt(request.LegalName))
            .Returns(encryptedLegalName);
        _passwordHasherMock.Setup(x => x.HashPassword(request.Password))
            .Returns("hashed-password");
        _emailServiceMock.Setup(x => x.SendVerificationEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(request.Email);
        result.EmailVerificationSent.Should().BeTrue();
        result.Message.Should().Contain("Registration successful");

        _userRepositoryMock.Verify(x => x.CreateAsync(
            It.Is<User>(u => u.Email.Value == request.Email && 
                            u.SceneName.Value == request.SceneName),
            It.Is<UserAuthentication>(ua => ua.PasswordHash == "hashed-password" &&
                                           ua.EmailVerified == false &&
                                           ua.PronouncedName == request.PronouncedName &&
                                           ua.Pronouns == request.Pronouns)),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenUnder21_ShouldThrowValidationException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "young@example.com",
            Password = "Password123!",
            SceneName = "YoungUser",
            LegalName = "Jane Doe",
            DateOfBirth = DateTime.UtcNow.AddYears(-20) // Under 21
        };

        // Act & Assert
        await _authService.Invoking(x => x.RegisterAsync(request))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("Must be at least 21 years old to register");
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldThrowConflictException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "Password123!",
            SceneName = "NewUser",
            LegalName = "John Doe",
            DateOfBirth = DateTime.UtcNow.AddYears(-25)
        };

        var existingUser = CreateValidUserWithAuth();
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        // Act & Assert
        await _authService.Invoking(x => x.RegisterAsync(request))
            .Should().ThrowAsync<ConflictException>()
            .WithMessage("Email address is already registered");
    }

    [Fact]
    public async Task RegisterAsync_WhenSceneNameTaken_ShouldThrowConflictException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "new@example.com",
            Password = "Password123!",
            SceneName = "TakenName",
            LegalName = "John Doe",
            DateOfBirth = DateTime.UtcNow.AddYears(-25)
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync((UserWithAuth?)null);
        _userRepositoryMock.Setup(x => x.IsSceneNameTakenAsync(request.SceneName))
            .ReturnsAsync(true);

        // Act & Assert
        await _authService.Invoking(x => x.RegisterAsync(request))
            .Should().ThrowAsync<ConflictException>()
            .WithMessage("Scene name is already taken");
    }

    #endregion

    #region VerifyEmailAsync Tests

    [Fact]
    public async Task VerifyEmailAsync_WhenValidToken_ShouldVerifyEmail()
    {
        // Arrange
        var token = "valid-token";
        var userAuth = new UserAuthentication
        {
            UserId = Guid.NewGuid(),
            EmailVerificationToken = token,
            EmailVerificationTokenCreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _userRepositoryMock.Setup(x => x.GetByVerificationTokenAsync(token))
            .ReturnsAsync(userAuth);

        // Act
        var result = await _authService.VerifyEmailAsync(token);

        // Assert
        result.Should().BeTrue();
        _userRepositoryMock.Verify(x => x.VerifyEmailAsync(userAuth.UserId), Times.Once);
    }

    [Fact]
    public async Task VerifyEmailAsync_WhenInvalidToken_ShouldThrowValidationException()
    {
        // Arrange
        var token = "invalid-token";
        _userRepositoryMock.Setup(x => x.GetByVerificationTokenAsync(token))
            .ReturnsAsync((UserAuthentication?)null);

        // Act & Assert
        await _authService.Invoking(x => x.VerifyEmailAsync(token))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("Invalid verification token");
    }

    [Fact]
    public async Task VerifyEmailAsync_WhenTokenExpired_ShouldThrowValidationException()
    {
        // Arrange
        var token = "expired-token";
        var userAuth = new UserAuthentication
        {
            UserId = Guid.NewGuid(),
            EmailVerificationToken = token,
            EmailVerificationTokenCreatedAt = DateTime.UtcNow.AddHours(-25) // Over 24 hours old
        };

        _userRepositoryMock.Setup(x => x.GetByVerificationTokenAsync(token))
            .ReturnsAsync(userAuth);

        // Act & Assert
        await _authService.Invoking(x => x.VerifyEmailAsync(token))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("Verification token has expired");
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_WhenValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange
        var refreshToken = "valid-refresh-token";
        var userId = Guid.NewGuid();
        var tokenInfo = new RefreshTokenInfo
        {
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        var user = CreateValidUserWithAuth(userId: userId);

        var newToken = new JwtToken { Token = "new-jwt-token", ExpiresAt = DateTime.UtcNow.AddHours(1) };
        var newRefreshToken = "new-refresh-token";

        _userRepositoryMock.Setup(x => x.GetRefreshTokenInfoAsync(refreshToken))
            .ReturnsAsync(tokenInfo);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _jwtServiceMock.Setup(x => x.GenerateToken(It.IsAny<UserWithAuth>()))
            .Returns(newToken);
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns(newRefreshToken);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be(newToken.Token);
        result.RefreshToken.Should().Be(newRefreshToken);

        _userRepositoryMock.Verify(x => x.InvalidateRefreshTokenAsync(refreshToken), Times.Once);
        _userRepositoryMock.Verify(x => x.StoreRefreshTokenAsync(
            userId, newRefreshToken, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenInvalidToken_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var refreshToken = "invalid-refresh-token";
        _userRepositoryMock.Setup(x => x.GetRefreshTokenInfoAsync(refreshToken))
            .ReturnsAsync((RefreshTokenInfo?)null);

        // Act & Assert
        await _authService.Invoking(x => x.RefreshTokenAsync(refreshToken))
            .Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid or expired refresh token");
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenExpired_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var refreshToken = "expired-refresh-token";
        var tokenInfo = new RefreshTokenInfo
        {
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired
        };

        _userRepositoryMock.Setup(x => x.GetRefreshTokenInfoAsync(refreshToken))
            .ReturnsAsync(tokenInfo);

        // Act & Assert
        await _authService.Invoking(x => x.RefreshTokenAsync(refreshToken))
            .Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid or expired refresh token");
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenUserNotFound_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var refreshToken = "valid-refresh-token";
        var userId = Guid.NewGuid();
        var tokenInfo = new RefreshTokenInfo
        {
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        _userRepositoryMock.Setup(x => x.GetRefreshTokenInfoAsync(refreshToken))
            .ReturnsAsync(tokenInfo);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await _authService.Invoking(x => x.RefreshTokenAsync(refreshToken))
            .Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("User not found or inactive");
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenUserInactive_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var refreshToken = "valid-refresh-token";
        var userId = Guid.NewGuid();
        var tokenInfo = new RefreshTokenInfo
        {
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        var user = CreateValidUserWithAuth(userId: userId, isActive: false);

        _userRepositoryMock.Setup(x => x.GetRefreshTokenInfoAsync(refreshToken))
            .ReturnsAsync(tokenInfo);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act & Assert
        await _authService.Invoking(x => x.RefreshTokenAsync(refreshToken))
            .Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("User not found or inactive");
    }

    #endregion

    #region Helper Methods

    private UserWithAuth CreateValidUserWithAuth(
        Guid? userId = null, 
        bool isActive = true, 
        bool emailVerified = true,
        string email = "test@example.com")
    {
        // Create a proper User entity first
        var user = new User(
            encryptedLegalName: "encrypted-legal-name",
            sceneName: SceneName.Create("TestUser"),
            email: EmailAddress.Create(email),
            dateOfBirth: DateTime.UtcNow.AddYears(-25),
            role: UserRole.Member
        );

        // Set the properties that tests expect if needed
        if (userId.HasValue)
        {
            // For tests, we need to set the ID via reflection since it's private set
            var idProperty = typeof(User).GetProperty(nameof(User.Id));
            idProperty?.SetValue(user, userId.Value);
        }
        
        if (!isActive)
        {
            // For tests, set IsActive via reflection
            var isActiveProperty = typeof(User).GetProperty(nameof(User.IsActive));
            isActiveProperty?.SetValue(user, isActive);
        }

        // Create UserWithAuth with the User entity
        var userWithAuth = new UserWithAuth
        {
            User = user,
            PasswordHash = "hashed-password",
            EmailVerified = emailVerified,
            PronouncedName = "Test",
            Pronouns = "they/them",
            LastLoginAt = DateTime.UtcNow.AddDays(-1)
        };

        return userWithAuth;
    }

    #endregion
}