# Test Project Migration - Technical Design

## Architecture Overview

### Test Infrastructure Design

```
TestProjectMigration/
├── Shared Test Infrastructure/
│   ├── IdentityTestBase.cs
│   ├── MockIdentityFactory.cs
│   ├── TestAuthenticationHandler.cs
│   └── TestDataBuilders/
│       ├── UserBuilder.cs
│       ├── RoleBuilder.cs
│       └── ClaimsBuilder.cs
├── API Tests/
│   ├── Auth/
│   ├── Services/
│   └── Integration/
└── Web Tests/
    ├── Services/
    ├── Components/
    └── Integration/
```

## Core Test Patterns

### 1. Identity Mocking Pattern

```csharp
public class IdentityTestBase : IDisposable
{
    protected Mock<UserManager<WitchCityRopeUser>> UserManagerMock { get; }
    protected Mock<SignInManager<WitchCityRopeUser>> SignInManagerMock { get; }
    protected WitchCityRopeIdentityDbContext DbContext { get; }
    
    public IdentityTestBase()
    {
        var options = new DbContextOptionsBuilder<WitchCityRopeIdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        DbContext = new WitchCityRopeIdentityDbContext(options);
        
        UserManagerMock = MockIdentityFactory.CreateUserManager();
        SignInManagerMock = MockIdentityFactory.CreateSignInManager(UserManagerMock.Object);
    }
}
```

### 2. Mock Factory Implementation

```csharp
public static class MockIdentityFactory
{
    public static Mock<UserManager<WitchCityRopeUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<WitchCityRopeUser>>();
        var userManager = new Mock<UserManager<WitchCityRopeUser>>(
            store.Object, null, null, null, null, null, null, null, null);
            
        // Setup common behaviors
        userManager.Setup(x => x.CreateAsync(It.IsAny<WitchCityRopeUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
            
        userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((string email) => new UserBuilder().WithEmail(email).Build());
            
        return userManager;
    }
    
    public static Mock<WitchCityRopeSignInManager> CreateSignInManager(
        UserManager<WitchCityRopeUser> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<WitchCityRopeUser>>();
        var signInManager = new Mock<WitchCityRopeSignInManager>(
            userManager, contextAccessor.Object, claimsFactory.Object,
            null, null, null, null);
            
        return signInManager;
    }
}
```

### 3. Test Data Builders

```csharp
public class UserBuilder
{
    private WitchCityRopeUser _user = new();
    
    public UserBuilder WithSceneName(string sceneName)
    {
        _user.SceneNameValue = sceneName;
        return this;
    }
    
    public UserBuilder WithVettingStatus(bool isVetted)
    {
        _user.IsVetted = isVetted;
        return this;
    }
    
    public UserBuilder WithRole(string role)
    {
        _user.UserRole = role;
        return this;
    }
    
    public WitchCityRopeUser Build() => _user;
}
```

## API Test Migration Patterns

### 1. Authentication Service Tests

```csharp
public class AuthServiceTests : IdentityTestBase
{
    private readonly IAuthService _authService;
    private readonly Mock<IJwtService> _jwtServiceMock;
    
    public AuthServiceTests()
    {
        _jwtServiceMock = new Mock<IJwtService>();
        _authService = new AuthService(
            UserManagerMock.Object,
            SignInManagerMock.Object,
            _jwtServiceMock.Object);
    }
    
    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@example.com")
            .WithSceneName("TestUser")
            .Build();
            
        UserManagerMock.Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
            
        SignInManagerMock.Setup(x => x.CheckPasswordSignInAsync(
            user, "Test123!", false, true))
            .ReturnsAsync(SignInResult.Success);
            
        // Act
        var result = await _authService.LoginAsync("test@example.com", "Test123!");
        
        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }
}
```

### 2. Integration Test Pattern

```csharp
public class AuthControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    
    public AuthControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove real services
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(UserManager<WitchCityRopeUser>));
                if (descriptor != null) services.Remove(descriptor);
                
                // Add test services
                services.AddSingleton(MockIdentityFactory.CreateUserManager().Object);
            });
        });
    }
}
```

## Web Test Migration Patterns

### 1. Service Tests

```csharp
public class SimplifiedIdentityAuthServiceTests : IdentityTestBase
{
    private readonly SimplifiedIdentityAuthService _authService;
    private readonly Mock<NavigationManager> _navigationMock;
    
    [Fact]
    public async Task LoginAsync_Success_NavigatesToReturnUrl()
    {
        // Arrange
        SignInManagerMock.Setup(x => x.PasswordSignInByEmailOrSceneNameAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(SignInResult.Success);
            
        // Act
        var result = await _authService.LoginAsync("test@example.com", "Test123!");
        
        // Assert
        result.Succeeded.Should().BeTrue();
        _navigationMock.Verify(x => x.NavigateTo(It.IsAny<string>(), true), Times.Once);
    }
}
```

### 2. Blazor Component Tests

```csharp
public class AuthorizedComponentTests : ComponentTestBase
{
    [Fact]
    public void Component_RequiresAuthentication_ShowsCorrectContent()
    {
        // Arrange
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("TestUser");
        authContext.SetRoles("Member");
        authContext.SetClaims(("IsVetted", "true"));
        
        // Act
        var component = RenderComponent<MemberDashboard>();
        
        // Assert
        component.Find(".welcome-message").TextContent
            .Should().Contain("Welcome, TestUser");
    }
}
```

## Testing ASP.NET Core Identity Pages

### Integration Tests for Identity UI

```csharp
public class IdentityPagesIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task LoginPage_ValidCredentials_RedirectsToDashboard()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginPage = await client.GetAsync("/Identity/Account/Login");
        loginPage.EnsureSuccessStatusCode();
        
        var token = ExtractAntiForgeryToken(await loginPage.Content.ReadAsStringAsync());
        
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Input.Email", "test@example.com"),
            new KeyValuePair<string, string>("Input.Password", "Test123!"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });
        
        // Act
        var response = await client.PostAsync("/Identity/Account/Login", formData);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.ToString().Should().Be("/");
    }
}
```

## Test Data Management

### 1. Seed Data for Tests

```csharp
public static class TestDataSeeder
{
    public static async Task SeedTestUsers(WitchCityRopeIdentityDbContext context)
    {
        var users = new[]
        {
            new UserBuilder().WithEmail("admin@test.com").WithRole("Administrator").Build(),
            new UserBuilder().WithEmail("member@test.com").WithRole("Member").Build(),
            new UserBuilder().WithEmail("vetted@test.com").WithRole("Member").WithVettingStatus(true).Build()
        };
        
        context.Users.AddRange(users);
        await context.SaveChangesAsync();
    }
}
```

### 2. Test Authentication Handler

```csharp
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim("SceneName", "TestScene"),
            new Claim(ClaimTypes.Role, "Member")
        };
        
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

## Performance Testing

```csharp
[Collection("Performance")]
public class AuthPerformanceTests
{
    [Fact]
    public async Task Login_Performance_Under100ms()
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Perform login operation
        var result = await _authService.LoginAsync("test@example.com", "Test123!");
        
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }
}
```

## Coverage Requirements

### Minimum Coverage Targets
- **Controllers**: 90% coverage
- **Services**: 85% coverage  
- **Core Logic**: 95% coverage
- **Integration**: 80% coverage
- **Overall**: 80%+ coverage

### Coverage Tools Configuration

```xml
<PropertyGroup>
  <CollectCoverage>true</CollectCoverage>
  <CoverletOutputFormat>cobertura</CoverletOutputFormat>
  <CoverletOutput>./TestResults/</CoverletOutput>
  <ExcludeByAttribute>GeneratedCodeAttribute,CompilerGeneratedAttribute</ExcludeByAttribute>
</PropertyGroup>
```

## Migration Checklist

- [ ] Create base test infrastructure classes
- [ ] Implement mock factories for Identity services
- [ ] Create test data builders
- [ ] Migrate API auth service tests
- [ ] Migrate API controller tests
- [ ] Remove obsolete Web auth component tests
- [ ] Create Identity page integration tests
- [ ] Update Web service tests
- [ ] Add performance tests
- [ ] Verify 80%+ coverage
- [ ] Document test patterns