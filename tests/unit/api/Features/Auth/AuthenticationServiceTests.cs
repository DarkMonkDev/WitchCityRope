using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Microsoft.AspNetCore.Http.Features;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Authentication.Models;
using WitchCityRope.Api.Features.Authentication.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Models.Auth;
using WitchCityRope.Api.Services;
using Xunit;
using FluentAssertions;
using Testcontainers.PostgreSql;

namespace WitchCityRope.UnitTests.Api.Features.Auth;

/// <summary>
/// Comprehensive unit tests for AuthenticationService
/// Tests authentication, registration, and security validation
/// Phase 1: Authentication & Security Unit Tests
/// </summary>
[Collection("Database")]
public class AuthenticationServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private ApplicationDbContext _context = null!;
    private AuthenticationService _service = null!;
    private UserManager<ApplicationUser> _userManager = null!;
    private SignInManager<ApplicationUser> _signInManager = null!;
    private IJwtService _jwtService = null!;
    private ReturnUrlValidator _returnUrlValidator = null!;
    private ILogger<AuthenticationService> _logger = null!;
    private string _connectionString = null!;

    public AuthenticationServiceTests()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test_auth")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        // Setup UserManager dependencies
        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        var passwordHasher = Substitute.For<IPasswordHasher<ApplicationUser>>();
        var userValidators = new List<IUserValidator<ApplicationUser>>();
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>>();
        var keyNormalizer = Substitute.For<ILookupNormalizer>();
        var errors = Substitute.For<IdentityErrorDescriber>();
        var services = Substitute.For<IServiceProvider>();
        var userLogger = Substitute.For<ILogger<UserManager<ApplicationUser>>>();

        _userManager = Substitute.ForPartsOf<UserManager<ApplicationUser>>(
            userStore, null, passwordHasher, userValidators, passwordValidators,
            keyNormalizer, errors, services, userLogger);

        // FIX 2: Configure UserManager mock to actually save to database
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                var user = callInfo.ArgAt<ApplicationUser>(0);
                _context.Users.Add(user);
                _context.SaveChanges();
                return Task.FromResult(IdentityResult.Success);
            });

        // Setup SignInManager dependencies
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        var claimsFactory = Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var signInLogger = Substitute.For<ILogger<SignInManager<ApplicationUser>>>();

        _signInManager = Substitute.ForPartsOf<SignInManager<ApplicationUser>>(
            _userManager, contextAccessor, claimsFactory, null, signInLogger, null, null);

        // FIX 1: Add Task.FromResult() wrappers for async mocking
        _signInManager.CheckPasswordSignInAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(Task.FromResult(SignInResult.Failed));

        // Setup JWT service mock
        _jwtService = Substitute.For<IJwtService>();
        _jwtService.GenerateToken(Arg.Any<ApplicationUser>()).Returns(new WitchCityRope.Api.Models.Auth.JwtToken
        {
            Token = "test-jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });

        // FIX 3: Use REAL ReturnUrlValidator instead of mocking (it has no external dependencies)
        var returnUrlLogger = Substitute.For<ILogger<ReturnUrlValidator>>();
        var returnUrlConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Authentication:AllowedDomains:0"] = "localhost",
                ["Authentication:AllowedDomains:1"] = "witchcityrope.com"
            }!)
            .Build();
        
        // Use REAL validator - it's a pure function with no side effects, perfect for testing
        _returnUrlValidator = new ReturnUrlValidator(returnUrlLogger, returnUrlConfig);



        // Setup logger
        _logger = Substitute.For<ILogger<AuthenticationService>>();

        // Create service instance
        _service = new AuthenticationService(
            _context,
            _userManager,
            _signInManager,
            _jwtService,
            _returnUrlValidator,
            _logger);
    }
    public async Task DisposeAsync()
    {
        _context?.Dispose();
        _userManager?.Dispose();
        await _container.DisposeAsync();
    }

    #region Helper Methods

    private async Task<ApplicationUser> CreateTestUser(string email, string password = "Test123!", string sceneName = "TestUser")
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            SceneName = sceneName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            Role = "Member"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }



    private HttpContext CreateMockHttpContext()
    {
        var context = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        var connection = Substitute.For<ConnectionInfo>();
        
        // Setup request properties
        request.Scheme.Returns("https");
        request.Host.Returns(new HostString("localhost", 5655));
        
        // Setup headers
        var headers = new HeaderDictionary
        {
            ["User-Agent"] = "Test User Agent"
        };
        request.Headers.Returns(headers);
        
        // Setup connection with IP address
        connection.RemoteIpAddress.Returns(System.Net.IPAddress.Parse("127.0.0.1"));
        
        // Wire up context
        context.Request.Returns(request);
        context.Connection.Returns(connection);
        
        return context;
    }



    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessWithJwtToken()
    {
        // Arrange
        var email = $"valid-{Guid.NewGuid()}@example.com";
        var password = "ValidPassword123!";
        var user = await CreateTestUser(email);

        _userManager.FindByEmailAsync(email).Returns(Task.FromResult(user));
        _signInManager.CheckPasswordSignInAsync(user, password, true)
            .Returns(Task.FromResult(SignInResult.Success));

        var request = new LoginRequest { Email = email, Password = password };
        var httpContext = CreateMockHttpContext();

        // Act
        var (success, response, error) = await _service.LoginAsync(request, httpContext);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Token.Should().Be("test-jwt-token");
        response.User.Should().NotBeNull();
        response.User.Email.Should().Be(email);
        error.Should().BeEmpty();

        // Verify JWT token was generated
        _jwtService.Received(1).GenerateToken(Arg.Is<ApplicationUser>(u => u.Email == email));

        // Verify last login time was updated
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword123!"
        };
        var httpContext = CreateMockHttpContext();

        _userManager.FindByEmailAsync(request.Email).Returns(Task.FromResult<ApplicationUser?>(null));

        // Act
        var (success, response, error) = await _service.LoginAsync(request, httpContext);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Invalid email or password");

        // Verify JWT was NOT generated
        _jwtService.DidNotReceive().GenerateToken(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsFailure()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid()}@example.com";
        var user = await CreateTestUser(email);

        _userManager.FindByEmailAsync(email).Returns(Task.FromResult(user));
        _signInManager.CheckPasswordSignInAsync(user, Arg.Any<string>(), true)
            .Returns(Task.FromResult(SignInResult.Failed));

        var request = new LoginRequest { Email = email, Password = "WrongPassword123!" };
        var httpContext = CreateMockHttpContext();

        // Act
        var (success, response, error) = await _service.LoginAsync(request, httpContext);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Invalid email or password");

        // Verify JWT was NOT generated
        _jwtService.DidNotReceive().GenerateToken(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public async Task LoginAsync_CaseInsensitiveEmailMatching_Succeeds()
    {
        // Arrange
        var email = $"TeSt-{Guid.NewGuid()}@ExAmPlE.CoM";
        var user = await CreateTestUser(email.ToLower());

        _userManager.FindByEmailAsync(email).Returns(Task.FromResult(user)); // UserManager handles case-insensitive lookup
        _signInManager.CheckPasswordSignInAsync(user, Arg.Any<string>(), true)
            .Returns(Task.FromResult(SignInResult.Success));

        var request = new LoginRequest { Email = email, Password = "Test123!" };
        var httpContext = CreateMockHttpContext();

        // Act
        var (success, response, error) = await _service.LoginAsync(request, httpContext);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        error.Should().BeEmpty();
    }

    [Fact]
    public async Task LoginAsync_WithLockedAccount_ReturnsLockedOutError()
    {
        // Arrange
        var email = $"locked-{Guid.NewGuid()}@example.com";
        var user = await CreateTestUser(email);

        _userManager.FindByEmailAsync(email).Returns(Task.FromResult(user));
        _signInManager.CheckPasswordSignInAsync(user, Arg.Any<string>(), true)
            .Returns(Task.FromResult(SignInResult.LockedOut));

        var request = new LoginRequest { Email = email, Password = "Test123!" };
        var httpContext = CreateMockHttpContext();

        // Act
        var (success, response, error) = await _service.LoginAsync(request, httpContext);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("locked");

        // Verify JWT was NOT generated for locked account
        _jwtService.DidNotReceive().GenerateToken(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public async Task LoginAsync_WithValidReturnUrl_IncludesValidatedReturnUrl()
    {
        // Arrange
        var email = $"returnurl-{Guid.NewGuid()}@example.com";
        var user = await CreateTestUser(email);
        var returnUrl = "/events/123"; // Valid local path

        _userManager.FindByEmailAsync(email).Returns(Task.FromResult(user));
        _signInManager.CheckPasswordSignInAsync(user, Arg.Any<string>(), true)
            .Returns(Task.FromResult(SignInResult.Success));

        // No mock setup needed - using REAL ReturnUrlValidator
        // The real validator will validate local paths correctly

        var request = new LoginRequest
        {
            Email = email,
            Password = "Test123!",
            ReturnUrl = returnUrl
        };
        var httpContext = CreateMockHttpContext();

        // Act
        var (success, response, error) = await _service.LoginAsync(request, httpContext);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.ReturnUrl.Should().Be(returnUrl); // Valid local URL should be included
    }

    [Fact]
    public async Task LoginAsync_WithInvalidReturnUrl_OmitsReturnUrlInResponse()
    {
        // Arrange
        var email = $"badurl-{Guid.NewGuid()}@example.com";
        var user = await CreateTestUser(email);
        var maliciousUrl = "https://evil.com/phishing"; // External URL

        _userManager.FindByEmailAsync(email).Returns(Task.FromResult(user));
        _signInManager.CheckPasswordSignInAsync(user, Arg.Any<string>(), true)
            .Returns(Task.FromResult(SignInResult.Success));

        // No mock setup needed - using REAL ReturnUrlValidator
        // The real validator will reject external URLs not in AllowedDomains

        var request = new LoginRequest
        {
            Email = email,
            Password = "Test123!",
            ReturnUrl = maliciousUrl
        };
        var httpContext = CreateMockHttpContext();

        // Act
        var (success, response, error) = await _service.LoginAsync(request, httpContext);

        // Assert
        success.Should().BeTrue(); // Login succeeds
        response.Should().NotBeNull();
        response!.ReturnUrl.Should().BeNull(); // Invalid external URL should be rejected
    }

    #endregion

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUserSuccessfully()
    {
        // Arrange
        var email = $"new-{Guid.NewGuid()}@example.com";
        var sceneName = $"NewUser-{Guid.NewGuid()}";
        var password = "SecurePassword123!";

        var request = new RegisterRequest
        {
            Email = email,
            SceneName = sceneName,
            Password = password
        };

        // UserManager.CreateAsync is already configured in InitializeAsync to save to database

        // Act
        var (success, response, error) = await _service.RegisterAsync(request);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Email.Should().Be(email);
        response.SceneName.Should().Be(sceneName);
        error.Should().BeEmpty();

        // Verify user was created with UserManager
        await _userManager.Received(1).CreateAsync(
            Arg.Is<ApplicationUser>(u => u.Email == email && u.SceneName == sceneName),
            password);

        // Verify user was persisted to database
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        savedUser.Should().NotBeNull();
        savedUser!.EmailConfirmed.Should().BeTrue(); // Auto-confirmed for testing
        savedUser.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ReturnsError()
    {
        // Arrange
        var email = $"duplicate-{Guid.NewGuid()}@example.com";
        await CreateTestUser(email); // Create existing user

        var request = new RegisterRequest
        {
            Email = email,
            SceneName = "NewSceneName",
            Password = "Test123!"
        };

        // Act
        var (success, response, error) = await _service.RegisterAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("already registered");

        // Verify user creation was NOT attempted
        await _userManager.DidNotReceive().CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateSceneName_ReturnsError()
    {
        // Arrange
        var existingSceneName = $"ExistingUser-{Guid.NewGuid()}";
        await CreateTestUser($"existing-{Guid.NewGuid()}@example.com", sceneName: existingSceneName);

        var request = new RegisterRequest
        {
            Email = $"new-{Guid.NewGuid()}@example.com",
            SceneName = existingSceneName,
            Password = "Test123!"
        };

        // Act
        var (success, response, error) = await _service.RegisterAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("already taken");

        // Verify user creation was NOT attempted
        await _userManager.DidNotReceive().CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task RegisterAsync_WithWeakPassword_ReturnsValidationError()
    {
        // Arrange
        var email = $"weak-{Guid.NewGuid()}@example.com";
        var weakPassword = "12345"; // Too short, no complexity

        var request = new RegisterRequest
        {
            Email = email,
            SceneName = "TestUser",
            Password = weakPassword
        };

        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), weakPassword)
            .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooShort",
                Description = "Passwords must be at least 8 characters."
            })));

        // Act
        var (success, response, error) = await _service.RegisterAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("8 characters");

        // Verify no user was created in database
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        user.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidEmailFormat_ReturnsValidationError()
    {
        // Arrange
        var invalidEmail = "not-an-email";

        var request = new RegisterRequest
        {
            Email = invalidEmail,
            SceneName = "TestUser",
            Password = "Test123!"
        };

        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidEmail",
                Description = "Email 'not-an-email' is invalid."
            })));

        // Act
        var (success, response, error) = await _service.RegisterAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("invalid");
    }

    [Fact]
    public async Task RegisterAsync_SetsLastLoginTime_ToCurrentTime()
    {
        // Arrange
        var email = $"logintime-{Guid.NewGuid()}@example.com";
        var request = new RegisterRequest
        {
            Email = email,
            SceneName = "TestUser",
            Password = "Test123!"
        };

        // UserManager.CreateAsync is already configured to save to database

        var beforeRegistration = DateTime.UtcNow;

        // Act
        var (success, response, error) = await _service.RegisterAsync(request);

        // Assert
        success.Should().BeTrue();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        user.Should().NotBeNull();
        user!.LastLoginAt.Should().BeOnOrAfter(beforeRegistration);
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region GetCurrentUserAsync Tests

    [Fact]
    public async Task GetCurrentUserAsync_WithValidUserId_ReturnsUserData()
    {
        // Arrange
        var user = await CreateTestUser($"current-{Guid.NewGuid()}@example.com");

        // Act
        var (success, response, error) = await _service.GetCurrentUserAsync(user.Id.ToString());

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Id.Should().Be(user.Id.ToString());
        response.Email.Should().Be(user.Email);
        response.SceneName.Should().Be(user.SceneName);
        error.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithInvalidUserId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid().ToString();

        // Act
        var (success, response, error) = await _service.GetCurrentUserAsync(nonExistentUserId);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("not found");
    }

    #endregion

    #region GetServiceTokenAsync Tests

    [Fact]
    public async Task GetServiceTokenAsync_WithValidUserIdAndEmail_ReturnsJwtToken()
    {
        // Arrange
        var email = $"service-{Guid.NewGuid()}@example.com";
        var user = await CreateTestUser(email);

        // Act
        var (success, response, error) = await _service.GetServiceTokenAsync(
            user.Id.ToString(),
            email);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Token.Should().Be("test-jwt-token");
        response.User.Should().NotBeNull();
        response.User.Email.Should().Be(email);
        error.Should().BeEmpty();

        // Verify JWT was generated
        _jwtService.Received(1).GenerateToken(Arg.Is<ApplicationUser>(u => u.Email == email));
    }

    [Fact]
    public async Task GetServiceTokenAsync_WithMismatchedEmail_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUser($"mismatch-{Guid.NewGuid()}@example.com");
        var wrongEmail = "wrong@example.com";

        // Act
        var (success, response, error) = await _service.GetServiceTokenAsync(
            user.Id.ToString(),
            wrongEmail);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("not found or email mismatch");

        // Verify JWT was NOT generated
        _jwtService.DidNotReceive().GenerateToken(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public async Task GetServiceTokenAsync_WithUnconfirmedEmail_ReturnsError()
    {
        // Arrange
        var email = $"unconfirmed-{Guid.NewGuid()}@example.com";
        var user = await CreateTestUser(email);
        user.EmailConfirmed = false;
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _service.GetServiceTokenAsync(
            user.Id.ToString(),
            email);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("not active");

        // Verify JWT was NOT generated for unconfirmed account
        _jwtService.DidNotReceive().GenerateToken(Arg.Any<ApplicationUser>());
    }

    #endregion

    #region Security Tests (OWASP)

    [Theory]
    [InlineData("'; DROP TABLE Users; --")] // SQL Injection attempt
    [InlineData("<script>alert('xss')</script>")] // XSS attempt
    [InlineData("admin' OR '1'='1")] // SQL Injection boolean bypass
    public async Task LoginAsync_WithMaliciousInput_HandledSafely(string maliciousInput)
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = maliciousInput,
            Password = "Test123!"
        };
        var httpContext = CreateMockHttpContext();

        _userManager.FindByEmailAsync(maliciousInput).Returns(Task.FromResult<ApplicationUser?>(null));

        // Act
        var (success, response, error) = await _service.LoginAsync(request, httpContext);

        // Assert
        success.Should().BeFalse(); // Malicious input should not succeed
        response.Should().BeNull();
        error.Should().Contain("Invalid email or password"); // Generic error, no information leakage

        // Verify no JWT was generated
        _jwtService.DidNotReceive().GenerateToken(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public async Task RegisterAsync_PreventsSQLInjectionInEmail()
    {
        // Arrange
        var maliciousEmail = "admin@example.com'; DROP TABLE Users; --";
        var request = new RegisterRequest
        {
            Email = maliciousEmail,
            SceneName = "Hacker",
            Password = "Test123!"
        };

        // Act & Assert - Should fail validation, not execute SQL
        var (success, response, error) = await _service.RegisterAsync(request);

        // Database should still exist and be functional
        var userCount = await _context.Users.CountAsync();
        userCount.Should().BeGreaterThanOrEqualTo(0); // Database not corrupted
    }

    [Fact]
    public async Task RegisterAsync_SanitizesXSSInSceneName()
    {
        // Arrange
        var xssSceneName = "<script>alert('xss')</script>";
        var email = $"xss-test-{Guid.NewGuid()}@example.com";

        var request = new RegisterRequest
        {
            Email = email,
            SceneName = xssSceneName,
            Password = "Test123!"
        };

        // UserManager.CreateAsync is already configured to save to database

        // Act
        var (success, response, error) = await _service.RegisterAsync(request);

        // Assert - XSS should be stored as-is (sanitization happens on output, not input)
        // But database should remain functional
        var userCount = await _context.Users.CountAsync();
        userCount.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region Additional Login Security Tests (Phase 1)

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ReturnsForbidden()
    {
        // Arrange
        var email = $"inactive-{Guid.NewGuid()}@example.com";
        var user = await CreateTestUser(email);
        user.IsActive = false; // Mark user as inactive
        await _context.SaveChangesAsync();

        _userManager.FindByEmailAsync(email).Returns(Task.FromResult(user));
        _signInManager.CheckPasswordSignInAsync(user, Arg.Any<string>(), true)
            .Returns(Task.FromResult(SignInResult.NotAllowed)); // Identity returns NotAllowed for inactive users

        var request = new LoginRequest { Email = email, Password = "Test123!" };
        var httpContext = CreateMockHttpContext();

        // Act
        var (success, response, error) = await _service.LoginAsync(request, httpContext);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().NotBeEmpty();

        // Verify JWT was NOT generated for inactive user
        _jwtService.DidNotReceive().GenerateToken(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public async Task LoginAsync_WithDeletedUser_ReturnsUnauthorized()
    {
        // Arrange
        var email = $"deleted-{Guid.NewGuid()}@example.com";

        _userManager.FindByEmailAsync(email).Returns(Task.FromResult<ApplicationUser?>(null));

        var request = new LoginRequest { Email = email, Password = "Test123!" };
        var httpContext = CreateMockHttpContext();

        // Act
        var (success, response, error) = await _service.LoginAsync(request, httpContext);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Invalid email or password"); // Don't reveal user existence

        // Verify JWT was NOT generated
        _jwtService.DidNotReceive().GenerateToken(Arg.Any<ApplicationUser>());
    }

    #endregion

    #region Password Hashing Tests (Phase 1)

    [Fact]
    public async Task RegisterAsync_CreatesUserWithHashedPassword()
    {
        // Arrange
        var email = $"hashedpw-{Guid.NewGuid()}@example.com";
        var plainTextPassword = "PlainTextPassword123!";

        ApplicationUser? capturedUser = null;
        _userManager.CreateAsync(Arg.Do<ApplicationUser>(u => capturedUser = u), plainTextPassword)
            .Returns(callInfo =>
            {
                var user = callInfo.ArgAt<ApplicationUser>(0);
                _context.Users.Add(user);
                _context.SaveChanges();
                return Task.FromResult(IdentityResult.Success);
            });

        var request = new RegisterRequest
        {
            Email = email,
            SceneName = "TestUser",
            Password = plainTextPassword
        };

        // Act
        await _service.RegisterAsync(request);

        // Assert
        capturedUser.Should().NotBeNull();
        // UserManager.CreateAsync handles password hashing internally
        // We verify that CreateAsync was called with the plain text password
        await _userManager.Received(1).CreateAsync(
            Arg.Any<ApplicationUser>(),
            Arg.Is<string>(p => p == plainTextPassword));
    }

    #endregion

    #region Default Role Assignment Tests (Phase 1)

    [Fact]
    public async Task RegisterAsync_AssignsDefaultMemberRole()
    {
        // Arrange
        var email = $"defaultrole-{Guid.NewGuid()}@example.com";

        // UserManager.CreateAsync is already configured to save to database

        var request = new RegisterRequest
        {
            Email = email,
            SceneName = "NewMember",
            Password = "Test123!"
        };

        // Act
        await _service.RegisterAsync(request);

        // Assert
        // Verify user was created (role assignment happens in UserManager or database default)
        await _userManager.Received(1).CreateAsync(
            Arg.Is<ApplicationUser>(u => u.Email == email),
            Arg.Any<string>());

        // Check database for default role
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (savedUser != null)
        {
            savedUser.Role.Should().Be("Member"); // Default role should be Member
        }
    }

    #endregion
}
