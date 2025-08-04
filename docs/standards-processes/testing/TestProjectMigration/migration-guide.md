# Test Migration Guide - Step by Step Instructions

## Phase 1: Foundation Setup

### Step 1.1: Create Test Infrastructure

Create the following base classes in `Tests.Common` project:

#### 1. IdentityTestBase.cs
```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Tests.Common.Identity
{
    public abstract class IdentityTestBase : IDisposable
    {
        protected WitchCityRopeIdentityDbContext DbContext { get; }
        protected Mock<UserManager<WitchCityRopeUser>> UserManagerMock { get; }
        protected Mock<WitchCityRopeSignInManager> SignInManagerMock { get; }
        
        protected IdentityTestBase()
        {
            var options = new DbContextOptionsBuilder<WitchCityRopeIdentityDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            DbContext = new WitchCityRopeIdentityDbContext(options);
            UserManagerMock = MockIdentityFactory.CreateUserManager();
            SignInManagerMock = MockIdentityFactory.CreateSignInManager(UserManagerMock.Object);
        }
        
        public void Dispose()
        {
            DbContext?.Dispose();
        }
    }
}
```

#### 2. MockIdentityFactory.cs
```csharp
public static class MockIdentityFactory
{
    public static Mock<UserManager<WitchCityRopeUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<WitchCityRopeUser>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        var passwordHasher = new Mock<IPasswordHasher<WitchCityRopeUser>>();
        var userValidators = new List<IUserValidator<WitchCityRopeUser>>();
        var passwordValidators = new List<IPasswordValidator<WitchCityRopeUser>>();
        var keyNormalizer = new Mock<ILookupNormalizer>();
        var errors = new Mock<IdentityErrorDescriber>();
        var services = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<UserManager<WitchCityRopeUser>>>();
        
        var userManager = new Mock<UserManager<WitchCityRopeUser>>(
            store.Object, options.Object, passwordHasher.Object,
            userValidators, passwordValidators, keyNormalizer.Object,
            errors.Object, services.Object, logger.Object);
            
        return userManager;
    }
}
```

### Step 1.2: Fix Compilation Errors

#### API Tests - Namespace Updates
In each test file, update the following:

```csharp
// OLD
using WitchCityRope.Core.Entities;
using WitchCityRope.Infrastructure.Data;

// NEW
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Api.Features.Auth.Models;
using WitchCityRope.Api.Features.Auth.Services;
```

#### DbContext References
Find and replace in all test files:
- `WitchCityRopeDbContext` → `WitchCityRopeIdentityDbContext`

## Phase 2: API Test Migration

### Step 2.1: Update AuthServiceTests.cs

```csharp
[Fact]
public async Task LoginAsync_ValidCredentials_ReturnsJwtToken()
{
    // Arrange
    var email = "test@example.com";
    var password = "Test123!";
    var user = new UserBuilder()
        .WithEmail(email)
        .WithSceneName("TestUser")
        .Build();
        
    UserManagerMock.Setup(x => x.FindByEmailAsync(email))
        .ReturnsAsync(user);
        
    SignInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, false, true))
        .ReturnsAsync(SignInResult.Success);
        
    _jwtServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<WitchCityRopeUser>()))
        .Returns("mock-jwt-token");
        
    // Act
    var result = await _authService.LoginAsync(email, password);
    
    // Assert
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();
    result.AccessToken.Should().Be("mock-jwt-token");
}

[Fact]
public async Task LoginAsync_AccountLocked_ReturnsLockedOutResult()
{
    // Arrange
    var email = "locked@example.com";
    var user = new UserBuilder().WithEmail(email).Build();
    
    UserManagerMock.Setup(x => x.FindByEmailAsync(email))
        .ReturnsAsync(user);
        
    SignInManagerMock.Setup(x => x.CheckPasswordSignInAsync(
        It.IsAny<WitchCityRopeUser>(), It.IsAny<string>(), false, true))
        .ReturnsAsync(SignInResult.LockedOut);
        
    // Act
    var result = await _authService.LoginAsync(email, "any-password");
    
    // Assert
    result.IsSuccess.Should().BeFalse();
    result.ErrorMessage.Should().Contain("locked");
}
```

### Step 2.2: Update Controller Integration Tests

Create a custom WebApplicationFactory:

```csharp
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> 
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real database context
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<WitchCityRopeIdentityDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);
                
            // Add in-memory database
            services.AddDbContext<WitchCityRopeIdentityDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });
            
            // Add test authentication
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });
        });
    }
}
```

## Phase 3: Web Test Migration

### Step 3.1: Remove Obsolete Component Tests

Delete or archive these files:
- `LoginComponentTests.cs`
- `RegisterComponentTests.cs`
- `TwoFactorAuthComponentTests.cs`
- `MainNavTests.cs`

### Step 3.2: Create Identity Page Integration Tests

Create `IdentityPagesTests.cs`:

```csharp
public class IdentityPagesTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public IdentityPagesTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task Login_Get_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/Identity/Account/Login");
        
        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.ToString()
            .Should().Contain("text/html");
    }
    
    [Fact]
    public async Task Login_Post_ValidCredentials_RedirectsToHomePage()
    {
        // Arrange
        var loginPage = await _client.GetAsync("/Identity/Account/Login");
        loginPage.EnsureSuccessStatusCode();
        
        var token = ExtractAntiForgeryToken(await loginPage.Content.ReadAsStringAsync());
        
        // Seed test user
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
            var user = new WitchCityRopeUser 
            { 
                UserName = "test@example.com",
                Email = "test@example.com",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "Test123!");
        }
        
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Input.Email", "test@example.com"),
            new KeyValuePair<string, string>("Input.Password", "Test123!"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });
        
        // Act
        var response = await _client.PostAsync("/Identity/Account/Login", formData);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.ToString().Should().Be("/");
    }
}
```

### Step 3.3: Update Service Tests

Update `AuthServiceTests.cs` in Web.Tests:

```csharp
public class SimplifiedIdentityAuthServiceTests : IdentityTestBase
{
    private readonly SimplifiedIdentityAuthService _authService;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<NavigationManager> _navigationMock;
    
    public SimplifiedIdentityAuthServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _navigationMock = new Mock<NavigationManager>();
        
        _authService = new SimplifiedIdentityAuthService(
            SignInManagerMock.Object,
            UserManagerMock.Object,
            _httpContextAccessorMock.Object,
            _navigationMock.Object);
    }
    
    [Fact]
    public async Task GetCurrentUserAsync_AuthenticatedUser_ReturnsUserInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder()
            .WithId(userId)
            .WithEmail("test@example.com")
            .WithSceneName("TestScene")
            .Build();
            
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _httpContextAccessorMock.Setup(x => x.HttpContext.User)
            .Returns(principal);
            
        UserManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
            
        // Act
        var result = await _authService.GetCurrentUserAsync();
        
        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
        result.SceneName.Should().Be("TestScene");
    }
}
```

## Phase 4: Coverage & Quality

### Step 4.1: Run Coverage Analysis

```bash
# Install coverage tools
dotnet tool install --global coverlet.console
dotnet tool install --global dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutput=./TestResults/ /p:CoverletOutputFormat=cobertura

# Generate HTML report
reportgenerator -reports:"./TestResults/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

### Step 4.2: Identify Coverage Gaps

Focus on these areas for additional tests:
1. **Authentication edge cases**
   - Email vs SceneName login
   - Lockout scenarios
   - Password reset flows
   
2. **Authorization policies**
   - Role-based access
   - Vetting status checks
   - Claim-based authorization

3. **Token management**
   - JWT expiration
   - Refresh token flows
   - Token validation

### Step 4.3: Write Additional Tests

Example of edge case test:

```csharp
[Fact]
public async Task Login_WithSceneName_AuthenticatesSuccessfully()
{
    // Arrange
    var sceneName = "UniqueScene";
    var user = new UserBuilder()
        .WithSceneName(sceneName)
        .WithEmail("test@example.com")
        .Build();
        
    // Mock the custom SignInManager method
    SignInManagerMock.Setup(x => x.PasswordSignInByEmailOrSceneNameAsync(
        sceneName, "Test123!", false, true))
        .ReturnsAsync(SignInResult.Success);
        
    // Act
    var result = await _authService.LoginAsync(sceneName, "Test123!");
    
    // Assert
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();
}
```

## Common Issues & Solutions

### Issue 1: UserManager Mock Setup
**Problem**: Complex UserManager methods are hard to mock
**Solution**: Create extension methods for common setups

```csharp
public static class UserManagerMockExtensions
{
    public static void SetupSuccessfulUserCreation(
        this Mock<UserManager<WitchCityRopeUser>> mock)
    {
        mock.Setup(x => x.CreateAsync(It.IsAny<WitchCityRopeUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
            
        mock.Setup(x => x.AddToRoleAsync(It.IsAny<WitchCityRopeUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
    }
}
```

### Issue 2: DbContext Disposal
**Problem**: Tests fail due to disposed context
**Solution**: Use proper test lifecycle

```csharp
public class ServiceTests : IdentityTestBase, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await DbContext.Database.EnsureCreatedAsync();
    }
    
    public async Task DisposeAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
    }
}
```

### Issue 3: Antiforgery Token Extraction
**Problem**: Identity pages require antiforgery tokens
**Solution**: Helper method to extract tokens

```csharp
private string ExtractAntiForgeryToken(string html)
{
    var match = Regex.Match(html, @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
    return match.Success ? match.Groups[1].Value : string.Empty;
}
```

## Verification Checklist

- [ ] All test projects compile without errors
- [ ] API tests pass (115 tests)
- [ ] Web tests pass
- [ ] Integration tests verify auth flows
- [ ] Coverage report shows 80%+
- [ ] Performance tests pass
- [ ] No hardcoded test data
- [ ] Proper test isolation
- [ ] Documentation updated