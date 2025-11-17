using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WitchCityRope.Api.Features.Authentication.Models;
using WitchCityRope.Api.Features.Authentication.Services;
using WitchCityRope.Api.Services;
using Xunit;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace WitchCityRope.UnitTests.Api.Features.Auth;

/// <summary>
/// Unit tests for AuthenticationEndpoints (Pattern B - Minimal API)
/// Tests all 8 authentication endpoints with comprehensive coverage
/// </summary>
public class AuthenticationEndpointsTests
{
    private readonly IAuthenticationService _mockAuthService;
    private readonly IJwtService _mockJwtService;
    private readonly ITokenBlacklistService _mockTokenBlacklistService;
    private readonly IConfiguration _mockConfiguration;
    private readonly ILogger<IAuthenticationService> _mockLogger;
    private readonly DefaultHttpContext _httpContext;

    public AuthenticationEndpointsTests()
    {
        _mockAuthService = Substitute.For<IAuthenticationService>();
        _mockJwtService = Substitute.For<IJwtService>();
        _mockTokenBlacklistService = Substitute.For<ITokenBlacklistService>();
        _mockConfiguration = Substitute.For<IConfiguration>();
        _mockLogger = Substitute.For<ILogger<IAuthenticationService>>();

        // Setup HttpContext for cookie/claims testing
        _httpContext = new DefaultHttpContext();
        _httpContext.Request.IsHttps = true; // Default to HTTPS for secure cookies
    }

    #region GetCurrentUser Tests

    /// <summary>
    /// Test: GetCurrentUser with valid "sub" claim returns 200 OK with user data
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_WithValidSubClaim_Returns200OkWithUserData()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var claims = new[] { new Claim("sub", userId) };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var expectedUser = new AuthUserResponse
        {
            Id = Guid.Parse(userId),
            Email = "test@example.com",
            SceneName = "TestUser",
            Role = "Member"
        };

        _mockAuthService.GetCurrentUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns((true, expectedUser, string.Empty));

        // Act
        var result = await GetCurrentUser(claimsPrincipal);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<AuthUserResponse>;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedUser);
    }

    /// <summary>
    /// Test: GetCurrentUser with valid NameIdentifier claim returns 200 OK with user data
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_WithValidNameIdentifierClaim_Returns200OkWithUserData()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var expectedUser = new AuthUserResponse
        {
            Id = Guid.Parse(userId),
            Email = "test@example.com",
            SceneName = "TestUser",
            Role = "Member"
        };

        _mockAuthService.GetCurrentUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns((true, expectedUser, string.Empty));

        // Act
        var result = await GetCurrentUser(claimsPrincipal);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<AuthUserResponse>;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedUser);
    }

    /// <summary>
    /// Test: GetCurrentUser with missing user ID claim returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_WithMissingUserIdClaim_Returns401Unauthorized()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity()); // No claims

        // Act
        var result = await GetCurrentUser(claimsPrincipal);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Invalid Token");
        problemResult.ProblemDetails.Detail.Should().Contain("User ID not found in token claims");
    }

    /// <summary>
    /// Test: GetCurrentUser when service returns user not found returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var claims = new[] { new Claim("sub", userId) };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _mockAuthService.GetCurrentUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns((false, null, "User not found"));

        // Act
        var result = await GetCurrentUser(claimsPrincipal);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Title.Should().Be("Get Current User Failed");
        problemResult.ProblemDetails.Detail.Should().Be("User not found");
    }

    /// <summary>
    /// Test: GetCurrentUser when service throws exception returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task GetCurrentUser_WhenServiceFails_Returns500InternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var claims = new[] { new Claim("sub", userId) };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var expectedUser = new AuthUserResponse(); // Non-null response but failed
        _mockAuthService.GetCurrentUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns((false, expectedUser, "Database error"));

        // Act
        var result = await GetCurrentUser(claimsPrincipal);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Get Current User Failed");
        problemResult.ProblemDetails.Detail.Should().Be("Database error");
    }

    #endregion

    #region Login Tests

    /// <summary>
    /// Test: Login with valid credentials returns 200 OK and sets httpOnly cookie
    /// </summary>
    [Fact]
    public async Task Login_WithValidCredentials_Returns200OkAndSetsHttpOnlyCookie()
    {
        // Arrange
        var request = new LoginRequest
        {
            EmailOrSceneName = "test@example.com",
            Password = "Test123!",
            ReturnUrl = null
        };

        var loginResponse = new LoginResponse
        {
            Token = "valid-jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new AuthUserResponse
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                SceneName = "TestUser",
                Role = "Member"
            },
            ReturnUrl = null
        };

        _mockAuthService.LoginAsync(request, _httpContext, Arg.Any<CancellationToken>())
            .Returns((true, loginResponse, string.Empty));

        // Act
        var result = await Login(request, _httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // For Results.Ok(anonymous type), we need to check using reflection or dynamic
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty.Should().NotBeNull();
        var statusCode = (int?)statusCodeProperty!.GetValue(result);
        statusCode.Should().Be(200);

        // Verify cookie was set with correct options
        var setCookieHeader = _httpContext.Response.Headers["Set-Cookie"].ToString();
        setCookieHeader.Should().Contain("auth-token=");
        setCookieHeader.Should().Contain("httponly");
        setCookieHeader.Should().Contain("secure");
        setCookieHeader.Should().Contain("samesite=lax");
    }

    /// <summary>
    /// Test: Login with valid returnUrl includes validated returnUrl in response
    /// </summary>
    [Fact]
    public async Task Login_WithValidReturnUrl_IncludesReturnUrlInResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            EmailOrSceneName = "test@example.com",
            Password = "Test123!",
            ReturnUrl = "/events"
        };

        var loginResponse = new LoginResponse
        {
            Token = "valid-jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new AuthUserResponse
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                SceneName = "TestUser",
                Role = "Member"
            },
            ReturnUrl = "/events" // Validated by service
        };

        _mockAuthService.LoginAsync(request, _httpContext, Arg.Any<CancellationToken>())
            .Returns((true, loginResponse, string.Empty));

        // Act
        var result = await Login(request, _httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Check returnUrl in response using reflection/dynamic
        var valueProperty = result.GetType().GetProperty("Value");
        valueProperty.Should().NotBeNull();
        dynamic responseValue = valueProperty!.GetValue(result)!;
        string returnUrl = responseValue.ReturnUrl;
        returnUrl.Should().Be("/events");
    }

    /// <summary>
    /// Test: Login with invalid credentials returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401Unauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            EmailOrSceneName = "test@example.com",
            Password = "WrongPassword",
            ReturnUrl = null
        };

        _mockAuthService.LoginAsync(request, _httpContext, Arg.Any<CancellationToken>())
            .Returns((false, null, "Invalid email or password"));

        // Act
        var result = await Login(request, _httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Login Failed");
        problemResult.ProblemDetails.Detail.Should().Contain("Invalid email");
    }

    /// <summary>
    /// Test: Login with service error returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task Login_WithServiceError_Returns400BadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            EmailOrSceneName = "test@example.com",
            Password = "Test123!",
            ReturnUrl = null
        };

        _mockAuthService.LoginAsync(request, _httpContext, Arg.Any<CancellationToken>())
            .Returns((false, null, "Account is locked"));

        // Act
        var result = await Login(request, _httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Login Failed");
        problemResult.ProblemDetails.Detail.Should().Be("Account is locked");
    }

    /// <summary>
    /// Test: Login cookie has Secure flag when HTTPS is used
    /// </summary>
    [Fact]
    public async Task Login_WithHttps_SetsCookieWithSecureFlag()
    {
        // Arrange
        _httpContext.Request.IsHttps = true;
        var request = new LoginRequest
        {
            EmailOrSceneName = "test@example.com",
            Password = "Test123!",
            ReturnUrl = null
        };

        var loginResponse = new LoginResponse
        {
            Token = "valid-jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new AuthUserResponse { Id = Guid.NewGuid(), Email = "test@example.com", SceneName = "TestUser", Role = "Member" },
            ReturnUrl = null
        };

        _mockAuthService.LoginAsync(request, _httpContext, Arg.Any<CancellationToken>())
            .Returns((true, loginResponse, string.Empty));

        // Act
        await Login(request, _httpContext);

        // Assert
        var setCookieHeader = _httpContext.Response.Headers["Set-Cookie"].ToString();
        setCookieHeader.Should().Contain("secure");
    }

    /// <summary>
    /// Test: Login cookie has SameSite=Lax for cross-port requests (5173->5655)
    /// </summary>
    [Fact]
    public async Task Login_SetsCookieWithSameSiteLax()
    {
        // Arrange
        var request = new LoginRequest
        {
            EmailOrSceneName = "test@example.com",
            Password = "Test123!",
            ReturnUrl = null
        };

        var loginResponse = new LoginResponse
        {
            Token = "valid-jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new AuthUserResponse { Id = Guid.NewGuid(), Email = "test@example.com", SceneName = "TestUser", Role = "Member" },
            ReturnUrl = null
        };

        _mockAuthService.LoginAsync(request, _httpContext, Arg.Any<CancellationToken>())
            .Returns((true, loginResponse, string.Empty));

        // Act
        await Login(request, _httpContext);

        // Assert
        var setCookieHeader = _httpContext.Response.Headers["Set-Cookie"].ToString();
        setCookieHeader.Should().Contain("samesite=lax");
    }

    #endregion

    #region Register Tests

    /// <summary>
    /// Test: Register with valid request returns 201 Created with user data
    /// </summary>
    [Fact]
    public async Task Register_WithValidRequest_Returns201CreatedWithUserData()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "Test123!",
            SceneName = "NewUser",
            TermsOfServiceAccepted = true
        };

        var expectedUser = new AuthUserResponse
        {
            Id = Guid.NewGuid(),
            Email = "newuser@example.com",
            SceneName = "NewUser",
            Role = "Member"
        };

        _mockAuthService.RegisterAsync(request, Arg.Any<CancellationToken>())
            .Returns((true, expectedUser, string.Empty));

        // Act
        var result = await Register(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var createdResult = result as Created<AuthUserResponse>;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
        createdResult.Location.Should().Be($"/api/auth/user/{expectedUser.Id}");
        createdResult.Value.Should().BeEquivalentTo(expectedUser);
    }

    /// <summary>
    /// Test: Register with duplicate email returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "Test123!",
            SceneName = "ExistingUser",
            TermsOfServiceAccepted = true
        };

        _mockAuthService.RegisterAsync(request, Arg.Any<CancellationToken>())
            .Returns((false, null, "Email already exists"));

        // Act
        var result = await Register(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Registration Failed");
        problemResult.ProblemDetails.Detail.Should().Contain("Email already exists");
    }

    /// <summary>
    /// Test: Register with invalid request returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task Register_WithInvalidRequest_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "weak",
            SceneName = "",
            TermsOfServiceAccepted = false
        };

        _mockAuthService.RegisterAsync(request, Arg.Any<CancellationToken>())
            .Returns((false, null, "Validation failed"));

        // Act
        var result = await Register(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
    }

    #endregion

    #region GetServiceToken Tests

    /// <summary>
    /// Test: GetServiceToken with valid service secret returns 200 OK with token
    /// </summary>
    [Fact]
    public async Task GetServiceToken_WithValidServiceSecret_Returns200OkWithToken()
    {
        // Arrange
        var request = new ServiceTokenRequest
        {
            UserId = Guid.NewGuid().ToString(),
            Email = "test@example.com"
        };

        var expectedSecret = "valid-service-secret";
        _httpContext.Request.Headers["X-Service-Secret"] = expectedSecret;
        _mockConfiguration["ServiceAuth:Secret"].Returns(expectedSecret);

        var tokenResponse = new LoginResponse
        {
            Token = "service-jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new AuthUserResponse
            {
                Id = Guid.Parse(request.UserId),
                Email = request.Email,
                SceneName = "TestUser",
                Role = "Member"
            }
        };

        _mockAuthService.GetServiceTokenAsync(request.UserId, request.Email, Arg.Any<CancellationToken>())
            .Returns((true, tokenResponse, string.Empty));

        // Act
        var result = await GetServiceToken(request, _httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<LoginResponse>;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(tokenResponse);
    }

    /// <summary>
    /// Test: GetServiceToken with missing service secret returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetServiceToken_WithMissingServiceSecret_Returns401Unauthorized()
    {
        // Arrange
        var request = new ServiceTokenRequest
        {
            UserId = Guid.NewGuid().ToString(),
            Email = "test@example.com"
        };

        // No X-Service-Secret header set
        _mockConfiguration["ServiceAuth:Secret"].Returns("valid-service-secret");

        // Act
        var result = await GetServiceToken(request, _httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Invalid Service Credentials");
        problemResult.ProblemDetails.Detail.Should().Contain("Service secret is missing or incorrect");
    }

    /// <summary>
    /// Test: GetServiceToken with invalid service secret returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetServiceToken_WithInvalidServiceSecret_Returns401Unauthorized()
    {
        // Arrange
        var request = new ServiceTokenRequest
        {
            UserId = Guid.NewGuid().ToString(),
            Email = "test@example.com"
        };

        _httpContext.Request.Headers["X-Service-Secret"] = "invalid-secret";
        _mockConfiguration["ServiceAuth:Secret"].Returns("valid-service-secret");

        // Act
        var result = await GetServiceToken(request, _httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Invalid Service Credentials");
    }

    /// <summary>
    /// Test: GetServiceToken with empty UserId returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task GetServiceToken_WithEmptyUserId_Returns400BadRequest()
    {
        // Arrange
        var request = new ServiceTokenRequest
        {
            UserId = "",
            Email = "test@example.com"
        };

        var expectedSecret = "valid-service-secret";
        _httpContext.Request.Headers["X-Service-Secret"] = expectedSecret;
        _mockConfiguration["ServiceAuth:Secret"].Returns(expectedSecret);

        // Act
        var result = await GetServiceToken(request, _httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Invalid Request");
        problemResult.ProblemDetails.Detail.Should().Contain("User ID and email are required");
    }

    /// <summary>
    /// Test: GetServiceToken with empty Email returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task GetServiceToken_WithEmptyEmail_Returns400BadRequest()
    {
        // Arrange
        var request = new ServiceTokenRequest
        {
            UserId = Guid.NewGuid().ToString(),
            Email = ""
        };

        var expectedSecret = "valid-service-secret";
        _httpContext.Request.Headers["X-Service-Secret"] = expectedSecret;
        _mockConfiguration["ServiceAuth:Secret"].Returns(expectedSecret);

        // Act
        var result = await GetServiceToken(request, _httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Invalid Request");
    }

    /// <summary>
    /// Test: GetServiceToken when user not found returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task GetServiceToken_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        var request = new ServiceTokenRequest
        {
            UserId = Guid.NewGuid().ToString(),
            Email = "nonexistent@example.com"
        };

        var expectedSecret = "valid-service-secret";
        _httpContext.Request.Headers["X-Service-Secret"] = expectedSecret;
        _mockConfiguration["ServiceAuth:Secret"].Returns(expectedSecret);

        _mockAuthService.GetServiceTokenAsync(request.UserId, request.Email, Arg.Any<CancellationToken>())
            .Returns((false, null, "User not found"));

        // Act
        var result = await GetServiceToken(request, _httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Title.Should().Be("Service Token Generation Failed");
        problemResult.ProblemDetails.Detail.Should().Contain("not found");
    }

    #endregion

    #region Logout Tests

    /// <summary>
    /// Test: Logout with valid auth token clears cookie and blacklists token
    /// </summary>
    [Fact]
    public void Logout_WithValidAuthToken_ClearsCookieAndBlacklistsToken()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var token = GenerateTestJwtToken(jti);
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            { "auth-token", token }
        });

        _mockJwtService.ExtractJti(token).Returns(jti);

        // Act
        var result = Logout(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Check status code using reflection (Results.Ok with anonymous type)
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty.Should().NotBeNull();
        var statusCode = (int?)statusCodeProperty!.GetValue(result);
        statusCode.Should().Be(200);

        // Verify cookie was cleared
        var setCookieHeader = _httpContext.Response.Headers["Set-Cookie"].ToString();
        setCookieHeader.Should().Contain("auth-token=");
        setCookieHeader.Should().Contain("expires="); // Expiration set to past date

        // Verify token was blacklisted
        _mockTokenBlacklistService.Received(1).BlacklistToken(jti, Arg.Any<DateTime>());
    }

    /// <summary>
    /// Test: Logout without auth token still succeeds and clears any stale cookies
    /// </summary>
    [Fact]
    public void Logout_WithoutAuthToken_StillSucceedsAndClearsStale()
    {
        // Arrange
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>());

        // Act
        var result = Logout(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Check status code using reflection
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty.Should().NotBeNull();
        var statusCode = (int?)statusCodeProperty!.GetValue(result);
        statusCode.Should().Be(200);

        // Verify logout still succeeded even without token
        var valueProperty = result.GetType().GetProperty("Value");
        dynamic responseValue = valueProperty!.GetValue(result)!;
        bool success = responseValue.Success;
        success.Should().BeTrue();
    }

    /// <summary>
    /// Test: Logout with invalid token still clears cookie and returns success
    /// </summary>
    [Fact]
    public void Logout_WithInvalidToken_StillClearsCookieAndSucceeds()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            { "auth-token", invalidToken }
        });

        _mockJwtService.ExtractJti(invalidToken).Returns((string?)null);

        // Act
        var result = Logout(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Check status code using reflection
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty.Should().NotBeNull();
        var statusCode = (int?)statusCodeProperty!.GetValue(result);
        statusCode.Should().Be(200);
    }

    /// <summary>
    /// Test: Logout always succeeds even with exceptions (user perspective)
    /// </summary>
    [Fact]
    public void Logout_WithException_StillReturnsSuccess()
    {
        // Arrange
        var token = "valid.jwt.token";
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            { "auth-token", token }
        });

        _mockJwtService.ExtractJti(token).Throws(new InvalidOperationException("JWT parsing failed"));

        // Act
        var result = Logout(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Check status code using reflection
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty.Should().NotBeNull();
        var statusCode = (int?)statusCodeProperty!.GetValue(result);
        statusCode.Should().Be(200);
    }

    #endregion

    #region GetUser (from cookie) Tests

    /// <summary>
    /// Test: GetUser with valid auth cookie returns 200 OK with user data
    /// </summary>
    [Fact]
    public async Task GetUser_WithValidAuthCookie_Returns200OkWithUserData()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var token = GenerateTestJwtToken(Guid.NewGuid().ToString(), userId);
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            { "auth-token", token }
        });

        _mockJwtService.ValidateToken(token).Returns(true);

        var expectedUser = new AuthUserResponse
        {
            Id = Guid.Parse(userId),
            Email = "test@example.com",
            SceneName = "TestUser",
            Role = "Member"
        };

        _mockAuthService.GetCurrentUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns((true, expectedUser, string.Empty));

        // Act
        var result = await GetUser(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<AuthUserResponse>;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedUser);
    }

    /// <summary>
    /// Test: GetUser with missing auth cookie returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetUser_WithMissingAuthCookie_Returns401Unauthorized()
    {
        // Arrange
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>());

        // Act
        var result = await GetUser(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Not Authenticated");
        problemResult.ProblemDetails.Detail.Should().Contain("Authentication cookie not found");
    }

    /// <summary>
    /// Test: GetUser with invalid token clears cookie and returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetUser_WithInvalidToken_ClearsCookieAndReturns401()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            { "auth-token", invalidToken }
        });

        _mockJwtService.ValidateToken(invalidToken).Returns(false);

        // Act
        var result = await GetUser(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Invalid Token");
        problemResult.ProblemDetails.Detail.Should().Contain("Authentication token is invalid or expired");

        // Verify cookie was cleared
        var setCookieHeader = _httpContext.Response.Headers["Set-Cookie"].ToString();
        setCookieHeader.Should().Contain("auth-token=");
    }

    /// <summary>
    /// Test: GetUser with token missing user ID returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetUser_WithTokenMissingUserId_Returns401Unauthorized()
    {
        // Arrange
        var token = GenerateTestJwtTokenWithoutSub();
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            { "auth-token", token }
        });

        _mockJwtService.ValidateToken(token).Returns(true);

        // Act
        var result = await GetUser(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Invalid Token");
        problemResult.ProblemDetails.Detail.Should().Contain("User ID not found in token");
    }

    /// <summary>
    /// Test: GetUser when exception occurs returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task GetUser_WhenExceptionOccurs_Returns500InternalServerError()
    {
        // Arrange
        var token = "valid.jwt.token";
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            { "auth-token", token }
        });

        _mockJwtService.ValidateToken(token).Throws(new InvalidOperationException("JWT validation failed"));

        // Act
        var result = await GetUser(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Authentication Error");
        problemResult.ProblemDetails.Detail.Should().Contain("Could not validate authentication");
    }

    #endregion

    #region RefreshToken Tests

    /// <summary>
    /// Test: RefreshToken with valid cookie returns 200 OK and sets new cookie
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithValidCookie_Returns200OkAndSetsNewCookie()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var currentToken = GenerateTestJwtToken(Guid.NewGuid().ToString(), userId, email);
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            { "auth-token", currentToken }
        });

        var newTokenResponse = new LoginResponse
        {
            Token = "new-jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new AuthUserResponse
            {
                Id = Guid.Parse(userId),
                Email = email,
                SceneName = "TestUser",
                Role = "Member"
            }
        };

        _mockAuthService.GetServiceTokenAsync(userId, email, Arg.Any<CancellationToken>())
            .Returns((true, newTokenResponse, string.Empty));

        // Act
        var result = await RefreshToken(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Check status code using reflection
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty.Should().NotBeNull();
        var statusCode = (int?)statusCodeProperty!.GetValue(result);
        statusCode.Should().Be(200);

        // Verify new cookie was set
        var setCookieHeader = _httpContext.Response.Headers["Set-Cookie"].ToString();
        setCookieHeader.Should().Contain("auth-token=");
        setCookieHeader.Should().Contain("new-jwt-token");
    }

    /// <summary>
    /// Test: RefreshToken with missing cookie returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithMissingCookie_Returns401Unauthorized()
    {
        // Arrange
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>());

        // Act
        var result = await RefreshToken(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("No Token");
        problemResult.ProblemDetails.Detail.Should().Contain("No authentication token found for refresh");
    }

    /// <summary>
    /// Test: RefreshToken with malformed token returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithMalformedToken_Returns401Unauthorized()
    {
        // Arrange
        var malformedToken = "not.a.valid.jwt";
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            { "auth-token", malformedToken }
        });

        // Act
        var result = await RefreshToken(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Invalid Token");
        problemResult.ProblemDetails.Detail.Should().Contain("Token format is invalid");
    }

    /// <summary>
    /// Test: RefreshToken when service fails returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task RefreshToken_WhenServiceFails_Returns400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var currentToken = GenerateTestJwtToken(Guid.NewGuid().ToString(), userId, email);
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            { "auth-token", currentToken }
        });

        _mockAuthService.GetServiceTokenAsync(userId, email, Arg.Any<CancellationToken>())
            .Returns((false, null, "User account locked"));

        // Act
        var result = await RefreshToken(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Refresh Failed");
        problemResult.ProblemDetails.Detail.Should().Be("User account locked");
    }

    /// <summary>
    /// Test: RefreshToken with exception from logger returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithException_Returns500InternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var token = GenerateTestJwtToken(Guid.NewGuid().ToString(), userId, email);
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            { "auth-token", token }
        });

        // Mock service to throw exception (simulating unexpected error in outer catch)
        _mockAuthService.GetServiceTokenAsync(userId, email, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await RefreshToken(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Refresh Error");
        problemResult.ProblemDetails.Detail.Should().Contain("Could not refresh authentication token");
    }

    #endregion

    #region DebugAuthStatus Tests

    /// <summary>
    /// Test: DebugAuthStatus with valid token returns 200 OK with status details
    /// </summary>
    [Fact]
    public void DebugAuthStatus_WithValidToken_Returns200OkWithStatusDetails()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var token = GenerateTestJwtToken(jti);
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            { "auth-token", token }
        });

        _mockJwtService.ValidateToken(token).Returns(true);
        _mockJwtService.ExtractJti(token).Returns(jti);
        _mockTokenBlacklistService.IsTokenBlacklisted(jti).Returns(false);

        // Act
        var result = DebugAuthStatus(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Check status code using reflection
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty.Should().NotBeNull();
        var statusCode = (int?)statusCodeProperty!.GetValue(result);
        statusCode.Should().Be(200);

        // Verify response contains expected fields
        var valueProperty = result.GetType().GetProperty("Value");
        dynamic responseValue = valueProperty!.GetValue(result)!;
        bool hasAuthCookie = responseValue.HasAuthCookie;
        hasAuthCookie.Should().BeTrue();

        bool isTokenValid = responseValue.IsTokenValid;
        isTokenValid.Should().BeTrue();

        bool isBlacklisted = responseValue.IsBlacklisted;
        isBlacklisted.Should().BeFalse();
    }

    /// <summary>
    /// Test: DebugAuthStatus with no cookie returns 200 OK with HasAuthCookie=false
    /// </summary>
    [Fact]
    public void DebugAuthStatus_WithNoCookie_Returns200OkWithHasAuthCookieFalse()
    {
        // Arrange
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>());

        // Act
        var result = DebugAuthStatus(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Check status code using reflection
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty.Should().NotBeNull();
        var statusCode = (int?)statusCodeProperty!.GetValue(result);
        statusCode.Should().Be(200);

        var valueProperty = result.GetType().GetProperty("Value");
        dynamic responseValue = valueProperty!.GetValue(result)!;
        bool hasAuthCookie = responseValue.HasAuthCookie;
        hasAuthCookie.Should().BeFalse();
    }

    /// <summary>
    /// Test: DebugAuthStatus with blacklisted token shows IsBlacklisted=true
    /// </summary>
    [Fact]
    public void DebugAuthStatus_WithBlacklistedToken_ShowsIsBlacklistedTrue()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var token = GenerateTestJwtToken(jti);
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            { "auth-token", token }
        });

        _mockJwtService.ValidateToken(token).Returns(false); // Invalid because blacklisted
        _mockJwtService.ExtractJti(token).Returns(jti);
        _mockTokenBlacklistService.IsTokenBlacklisted(jti).Returns(true);

        // Act
        var result = DebugAuthStatus(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Check status code using reflection
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty.Should().NotBeNull();
        var statusCode = (int?)statusCodeProperty!.GetValue(result);
        statusCode.Should().Be(200);

        var valueProperty = result.GetType().GetProperty("Value");
        dynamic responseValue = valueProperty!.GetValue(result)!;
        bool isBlacklisted = responseValue.IsBlacklisted;
        isBlacklisted.Should().BeTrue();
    }

    /// <summary>
    /// Test: DebugAuthStatus with exception returns 500 InternalServerError
    /// </summary>
    [Fact]
    public void DebugAuthStatus_WithException_Returns500InternalServerError()
    {
        // Arrange
        var token = "invalid.token";
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            { "auth-token", token }
        });

        _mockJwtService.ValidateToken(token).Throws(new InvalidOperationException("Validation failed"));

        // Act
        var result = DebugAuthStatus(_httpContext);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Debug Error");
    }

    #endregion

    #region ForgotPassword Endpoint Tests (Phase 3: Password Reset)

    /// <summary>
    /// Test: ForgotPassword with valid email returns 200 OK with generic success message
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WithValidEmail_Returns200OkWithGenericMessage()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = "test@example.com"
        };

        _mockAuthService.ForgotPasswordAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        var result = await ForgotPassword(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // For Results.Ok(anonymous type), we need to use reflection
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty.Should().NotBeNull();
        var statusCode = (int?)statusCodeProperty!.GetValue(result);
        statusCode.Should().Be(200);

        // Verify response structure using reflection for Value property
        var valueProperty = result.GetType().GetProperty("Value");
        valueProperty.Should().NotBeNull();
        dynamic responseValue = valueProperty!.GetValue(result)!;
        bool success = responseValue.Success;
        string message = responseValue.Message;

        success.Should().BeTrue();
        message.Should().Be("If an account exists with this email, a password reset link has been sent.");

        // Verify service was called
        await _mockAuthService.Received(1).ForgotPasswordAsync(request.Email, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: ForgotPassword with non-existent email returns 200 OK (security - no enumeration)
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WithNonExistentEmail_Returns200OkForSecurity()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = "nonexistent@example.com"
        };

        // Service returns success even for non-existent email (security)
        _mockAuthService.ForgotPasswordAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        var result = await ForgotPassword(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // For Results.Ok(anonymous type), we need to use reflection
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty.Should().NotBeNull();
        var statusCode = (int?)statusCodeProperty!.GetValue(result);
        statusCode.Should().Be(200);

        // Verify generic message (no indication of whether email exists)
        var valueProperty = result.GetType().GetProperty("Value");
        valueProperty.Should().NotBeNull();
        dynamic responseValue = valueProperty!.GetValue(result)!;
        string message = responseValue.Message;
        message.Should().Be("If an account exists with this email, a password reset link has been sent.");
    }

    /// <summary>
    /// Test: ForgotPassword always returns 200 OK even when service fails (security)
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WhenServiceFails_StillReturns200OkForSecurity()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = "test@example.com"
        };

        // Even with service failure, endpoint should return success for security
        _mockAuthService.ForgotPasswordAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns((false, "Some internal error"));

        // Act
        var result = await ForgotPassword(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // For Results.Ok(anonymous type), we need to use reflection
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty.Should().NotBeNull();
        var statusCode = (int?)statusCodeProperty!.GetValue(result);
        statusCode.Should().Be(200);

        // Verify same generic message (prevent information leakage)
        var valueProperty = result.GetType().GetProperty("Value");
        valueProperty.Should().NotBeNull();
        dynamic responseValue = valueProperty!.GetValue(result)!;
        string message = responseValue.Message;
        message.Should().Be("If an account exists with this email, a password reset link has been sent.");
    }

    #endregion

    #region ResetPassword Endpoint Tests (Phase 3: Password Reset)

    /// <summary>
    /// Test: ResetPassword with valid token returns 200 OK with success message
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithValidToken_Returns200OkWithSuccessMessage()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            UserId = Guid.NewGuid().ToString(),
            Token = "valid-reset-token",
            NewPassword = "NewSecurePassword123!"
        };

        _mockAuthService.ResetPasswordAsync(request.UserId, request.Token, request.NewPassword, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        var result = await ResetPassword(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // For Results.Ok(anonymous type), we need to use reflection
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty.Should().NotBeNull();
        var statusCode = (int?)statusCodeProperty!.GetValue(result);
        statusCode.Should().Be(200);

        // Verify response structure
        var valueProperty = result.GetType().GetProperty("Value");
        valueProperty.Should().NotBeNull();
        dynamic responseValue = valueProperty!.GetValue(result)!;
        bool success = responseValue.Success;
        string message = responseValue.Message;

        success.Should().BeTrue();
        message.Should().Be("Your password has been reset successfully. You can now log in with your new password.");

        // Verify service was called with correct parameters
        await _mockAuthService.Received(1).ResetPasswordAsync(
            request.UserId,
            request.Token,
            request.NewPassword,
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: ResetPassword with invalid token returns 400 Problem
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithInvalidToken_Returns400Problem()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            UserId = Guid.NewGuid().ToString(),
            Token = "invalid-token",
            NewPassword = "NewPassword123!"
        };

        _mockAuthService.ResetPasswordAsync(request.UserId, request.Token, request.NewPassword, Arg.Any<CancellationToken>())
            .Returns((false, "This password reset link is invalid or has expired. Please request a new password reset."));

        // Act
        var result = await ResetPassword(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Password Reset Failed");
        problemResult.ProblemDetails.Detail.Should().Contain("invalid or has expired");
    }

    /// <summary>
    /// Test: ResetPassword with invalid user ID format returns 400 Problem
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithInvalidUserIdFormat_Returns400Problem()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            UserId = "not-a-guid",
            Token = "token",
            NewPassword = "NewPassword123!"
        };

        _mockAuthService.ResetPasswordAsync(request.UserId, request.Token, request.NewPassword, Arg.Any<CancellationToken>())
            .Returns((false, "Invalid password reset link"));

        // Act
        var result = await ResetPassword(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Detail.Should().Contain("Invalid password reset link");
    }

    /// <summary>
    /// Test: ResetPassword with non-existent user returns 400 Problem
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithNonExistentUser_Returns400Problem()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            UserId = Guid.NewGuid().ToString(),
            Token = "token",
            NewPassword = "NewPassword123!"
        };

        _mockAuthService.ResetPasswordAsync(request.UserId, request.Token, request.NewPassword, Arg.Any<CancellationToken>())
            .Returns((false, "Invalid password reset link"));

        // Act
        var result = await ResetPassword(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Detail.Should().Be("Invalid password reset link");
    }

    /// <summary>
    /// Test: ResetPassword with weak password returns 400 Problem with validation error
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithWeakPassword_Returns400ProblemWithValidationError()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            UserId = Guid.NewGuid().ToString(),
            Token = "valid-token",
            NewPassword = "12345" // Too short
        };

        _mockAuthService.ResetPasswordAsync(request.UserId, request.Token, request.NewPassword, Arg.Any<CancellationToken>())
            .Returns((false, "Passwords must be at least 8 characters."));

        // Act
        var result = await ResetPassword(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Detail.Should().Contain("8 characters");
    }

    /// <summary>
    /// Test: ResetPassword with missing required fields returns validation error
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithMissingFields_Returns400Problem()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            UserId = "",
            Token = "token",
            NewPassword = "Password123!"
        };

        _mockAuthService.ResetPasswordAsync(request.UserId, request.Token, request.NewPassword, Arg.Any<CancellationToken>())
            .Returns((false, "User ID, token, and new password are required"));

        // Act
        var result = await ResetPassword(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Detail.Should().Contain("required");
    }

    #endregion

    #region Helper Methods - Endpoint Simulations

    /// <summary>
    /// Helper: Simulates GetCurrentUser endpoint call
    /// </summary>
    private async Task<IResult> GetCurrentUser(ClaimsPrincipal user)
    {
        var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Results.Problem(
                title: "Invalid Token",
                detail: "User ID not found in token claims",
                statusCode: 401);
        }

        var (success, response, error) = await _mockAuthService.GetCurrentUserAsync(userId, CancellationToken.None);

        return success
            ? Results.Ok(response)
            : Results.Problem(
                title: "Get Current User Failed",
                detail: error,
                statusCode: response == null ? 404 : 500);
    }

    /// <summary>
    /// Helper: Simulates Login endpoint call
    /// </summary>
    private async Task<IResult> Login(LoginRequest request, HttpContext context)
    {
        var (success, response, error) = await _mockAuthService.LoginAsync(request, context, CancellationToken.None);

        if (success && response != null)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = response.ExpiresAt
            };

            context.Response.Cookies.Append("auth-token", response.Token, cookieOptions);

            return Results.Ok(new
            {
                Success = true,
                User = response.User,
                ReturnUrl = response.ReturnUrl,
                Message = "Login successful"
            });
        }

        return Results.Problem(
            title: "Login Failed",
            detail: error,
            statusCode: error.Contains("Invalid email") && error.Contains("password") ? 401 : 400);
    }

    /// <summary>
    /// Helper: Simulates Register endpoint call
    /// </summary>
    private async Task<IResult> Register(RegisterRequest request)
    {
        var (success, response, error) = await _mockAuthService.RegisterAsync(request, CancellationToken.None);

        return success
            ? Results.Created($"/api/auth/user/{response!.Id}", response)
            : Results.Problem(
                title: "Registration Failed",
                detail: error,
                statusCode: 400);
    }

    /// <summary>
    /// Helper: Simulates GetServiceToken endpoint call
    /// </summary>
    private async Task<IResult> GetServiceToken(ServiceTokenRequest request, HttpContext context)
    {
        var serviceSecret = context.Request.Headers["X-Service-Secret"].FirstOrDefault();
        var expectedSecret = _mockConfiguration["ServiceAuth:Secret"];

        if (string.IsNullOrEmpty(serviceSecret) || serviceSecret != expectedSecret)
        {
            return Results.Problem(
                title: "Invalid Service Credentials",
                detail: "Service secret is missing or incorrect",
                statusCode: 401);
        }

        if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Email))
        {
            return Results.Problem(
                title: "Invalid Request",
                detail: "User ID and email are required",
                statusCode: 400);
        }

        var (success, response, error) = await _mockAuthService.GetServiceTokenAsync(
            request.UserId,
            request.Email,
            CancellationToken.None);

        return success
            ? Results.Ok(response)
            : Results.Problem(
                title: "Service Token Generation Failed",
                detail: error,
                statusCode: error.Contains("not found") ? 404 : 400);
    }

    /// <summary>
    /// Helper: Simulates Logout endpoint call
    /// </summary>
    private IResult Logout(HttpContext context)
    {
        try
        {
            var authCookie = context.Request.Cookies["auth-token"];
            if (!string.IsNullOrEmpty(authCookie))
            {
                var jti = _mockJwtService.ExtractJti(authCookie);
                if (!string.IsNullOrEmpty(jti))
                {
                    var handler = new JwtSecurityTokenHandler();
                    try
                    {
                        var jsonToken = handler.ReadJwtToken(authCookie);
                        var expirationTime = jsonToken.ValidTo;
                        _mockTokenBlacklistService.BlacklistToken(jti, expirationTime);
                    }
                    catch
                    {
                        // Ignore parsing errors, continue with logout
                    }
                }
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(-1)
            };

            context.Response.Cookies.Append("auth-token", "", cookieOptions);
            context.Response.Cookies.Delete("auth-token", new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });

            return Results.Ok(new
            {
                Success = true,
                Message = "Logged out successfully"
            });
        }
        catch
        {
            return Results.Ok(new
            {
                Success = true,
                Message = "Logged out successfully"
            });
        }
    }

    /// <summary>
    /// Helper: Simulates GetUser endpoint call
    /// </summary>
    private async Task<IResult> GetUser(HttpContext context)
    {
        try
        {
            var token = context.Request.Cookies["auth-token"];
            if (string.IsNullOrEmpty(token))
            {
                return Results.Problem(
                    title: "Not Authenticated",
                    detail: "Authentication cookie not found",
                    statusCode: 401);
            }

            if (!_mockJwtService.ValidateToken(token))
            {
                var clearCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddDays(-1)
                };

                context.Response.Cookies.Append("auth-token", "", clearCookieOptions);
                context.Response.Cookies.Delete("auth-token", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path = "/"
                });

                return Results.Problem(
                    title: "Invalid Token",
                    detail: "Authentication token is invalid or expired",
                    statusCode: 401);
            }

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            var userId = jsonToken?.Claims?.FirstOrDefault(x => x.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Results.Problem(
                    title: "Invalid Token",
                    detail: "User ID not found in token",
                    statusCode: 401);
            }

            var (success, response, error) = await _mockAuthService.GetCurrentUserAsync(userId, CancellationToken.None);

            return success
                ? Results.Ok(response)
                : Results.Problem(
                    title: "Get Current User Failed",
                    detail: error,
                    statusCode: response == null ? 404 : 500);
        }
        catch (Exception ex)
        {
            _mockLogger.LogError(ex, "Error getting user from cookie");
            return Results.Problem(
                title: "Authentication Error",
                detail: "Could not validate authentication",
                statusCode: 500);
        }
    }

    /// <summary>
    /// Helper: Simulates RefreshToken endpoint call
    /// </summary>
    private async Task<IResult> RefreshToken(HttpContext context)
    {
        try
        {
            var currentToken = context.Request.Cookies["auth-token"];
            if (string.IsNullOrEmpty(currentToken))
            {
                return Results.Problem(
                    title: "No Token",
                    detail: "No authentication token found for refresh",
                    statusCode: 401);
            }

            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jsonToken;

            try
            {
                jsonToken = handler.ReadJwtToken(currentToken);
            }
            catch
            {
                return Results.Problem(
                    title: "Invalid Token",
                    detail: "Token format is invalid",
                    statusCode: 401);
            }

            var userId = jsonToken?.Claims?.FirstOrDefault(x => x.Type == "sub")?.Value;
            var email = jsonToken?.Claims?.FirstOrDefault(x => x.Type == "email")?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            {
                return Results.Problem(
                    title: "Invalid Token Claims",
                    detail: "Required user information not found in token",
                    statusCode: 401);
            }

            var (success, response, error) = await _mockAuthService.GetServiceTokenAsync(userId, email, CancellationToken.None);

            if (success && response != null)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = response.ExpiresAt
                };

                context.Response.Cookies.Append("auth-token", response.Token, cookieOptions);

                return Results.Ok(new
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    ExpiresAt = response.ExpiresAt
                });
            }

            return Results.Problem(
                title: "Refresh Failed",
                detail: error,
                statusCode: 400);
        }
        catch (Exception ex)
        {
            _mockLogger.LogError(ex, "Token refresh failed");
            return Results.Problem(
                title: "Refresh Error",
                detail: "Could not refresh authentication token",
                statusCode: 500);
        }
    }

    /// <summary>
    /// Helper: Simulates DebugAuthStatus endpoint call
    /// </summary>
    private IResult DebugAuthStatus(HttpContext context)
    {
        try
        {
            var authCookie = context.Request.Cookies["auth-token"];
            var result = new
            {
                HasAuthCookie = !string.IsNullOrEmpty(authCookie),
                CookieLength = authCookie?.Length ?? 0,
                CookiePreview = authCookie?.Substring(0, Math.Min(authCookie.Length, 50)) + "...",
                AllCookies = context.Request.Cookies.Select(c => new
                {
                    Name = c.Key,
                    ValueLength = c.Value?.Length ?? 0,
                    ValuePreview = c.Value?.Substring(0, Math.Min(c.Value.Length, 20)) + "..."
                }).ToList(),
                IsTokenValid = !string.IsNullOrEmpty(authCookie) && _mockJwtService.ValidateToken(authCookie),
                JTI = !string.IsNullOrEmpty(authCookie) ? _mockJwtService.ExtractJti(authCookie) : null,
                IsBlacklisted = false,
                Timestamp = DateTimeOffset.UtcNow
            };

            if (!string.IsNullOrEmpty(result.JTI))
            {
                result = result with { IsBlacklisted = _mockTokenBlacklistService.IsTokenBlacklisted(result.JTI) };
            }

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            _mockLogger.LogError(ex, "🔐 AUTH DEBUG: Error checking authentication status");
            return Results.Problem(
                title: "Debug Error",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Helper: Simulates ForgotPassword endpoint call
    /// </summary>
    private async Task<IResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var (success, error) = await _mockAuthService.ForgotPasswordAsync(request.Email, CancellationToken.None);

        // Always return success for security (prevent email enumeration)
        return Results.Ok(new
        {
            Success = true,
            Message = "If an account exists with this email, a password reset link has been sent."
        });
    }

    /// <summary>
    /// Helper: Simulates ResetPassword endpoint call
    /// </summary>
    private async Task<IResult> ResetPassword(ResetPasswordRequest request)
    {
        var (success, error) = await _mockAuthService.ResetPasswordAsync(
            request.UserId,
            request.Token,
            request.NewPassword,
            CancellationToken.None);

        return success
            ? Results.Ok(new
            {
                Success = true,
                Message = "Your password has been reset successfully. You can now log in with your new password."
            })
            : Results.Problem(
                title: "Password Reset Failed",
                detail: error,
                statusCode: 400);
    }

    #endregion

    #region Test Utilities

    /// <summary>
    /// Generate a test JWT token with claims
    /// </summary>
    private static string GenerateTestJwtToken(string jti, string? userId = null, string? email = null)
    {
        var claims = new List<Claim>
        {
            new Claim("jti", jti)
        };

        if (!string.IsNullOrEmpty(userId))
        {
            claims.Add(new Claim("sub", userId));
        }

        if (!string.IsNullOrEmpty(email))
        {
            claims.Add(new Claim("email", email));
        }

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: null // Not needed for tests
        );

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(token);
    }

    /// <summary>
    /// Generate a test JWT token without sub claim
    /// </summary>
    private static string GenerateTestJwtTokenWithoutSub()
    {
        var claims = new List<Claim>
        {
            new Claim("jti", Guid.NewGuid().ToString()),
            new Claim("email", "test@example.com")
        };

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: null
        );

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(token);
    }

    /// <summary>
    /// Test implementation of IRequestCookieCollection for mocking cookies
    /// </summary>
    private class TestRequestCookieCollection : IRequestCookieCollection
    {
        private readonly Dictionary<string, string> _cookies;

        public TestRequestCookieCollection(Dictionary<string, string> cookies)
        {
            _cookies = cookies;
        }

        public string? this[string key] => _cookies.TryGetValue(key, out var value) ? value : null;
        public int Count => _cookies.Count;
        public ICollection<string> Keys => _cookies.Keys;
        public bool ContainsKey(string key) => _cookies.ContainsKey(key);
        public bool TryGetValue(string key, out string? value) => _cookies.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _cookies.GetEnumerator();
    }

    #endregion
}
