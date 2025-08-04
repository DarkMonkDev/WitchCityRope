# Identity Test Infrastructure

This folder contains test infrastructure classes for testing ASP.NET Core Identity functionality in the WitchCityRope application.

## Core Classes

### IdentityTestBase
Base class that provides pre-configured mocks for Identity services:
- `UserManagerMock` - Mock for UserManager<WitchCityRopeUser>
- `SignInManagerMock` - Mock for SignInManager<WitchCityRopeUser>
- `RoleManagerMock` - Mock for RoleManager<WitchCityRopeRole>
- Helper methods for common Identity operations

### MockIdentityFactory
Factory class for creating Identity-related mocks and test data:
- `CreateUserManagerMock()` - Creates a configured UserManager mock
- `CreateSignInManagerMock()` - Creates a configured SignInManager mock
- `CreateRoleManagerMock()` - Creates a configured RoleManager mock
- `CreateTestUser()` - Creates a test WitchCityRopeUser
- `CreateTestRole()` - Creates a test WitchCityRopeRole

### IdentityUserBuilder
Fluent builder for creating WitchCityRopeUser instances for testing:
- Extends the core UserBuilder with Identity-specific properties
- Supports common scenarios like vetted users, locked accounts, etc.
- Can build multiple users with variations

### IdentityTestHelpers
Helper methods for common Identity testing scenarios:
- `CreateAuthenticationContext()` - Sets up complete auth context
- `SimulateSuccessfulLogin()` - Simulates login flow
- `SetupEmailConfirmation()` - Sets up email confirmation
- `SetupPasswordReset()` - Sets up password reset flow

## Usage Examples

### Basic User Creation Test
```csharp
public class MyTest : IdentityTestBase
{
    [Fact]
    public async Task CreateUser_Success()
    {
        // Arrange
        var user = new IdentityUserBuilder()
            .WithSceneName("TestUser")
            .WithEmail("test@example.com")
            .AsOrganizer()
            .AsVetted()
            .Build();

        SetupSuccessfulUserCreation(user);

        // Act
        var result = await UserManagerMock.Object.CreateAsync(user, "Test123!");

        // Assert
        result.Should().Be(IdentityResult.Success);
        VerifyUserCreated(user, "Test123!");
    }
}
```

### Login Flow Test
```csharp
[Fact]
public async Task Login_WithValidCredentials_Success()
{
    // Arrange
    var email = "user@example.com";
    var password = "Test123!";
    var user = MockIdentityFactory.CreateTestUser(email: email);

    SetupFindByEmail(email, user);
    SetupPasswordCheck(user, password, true);
    SetupPasswordSignIn(email, password, SignInResult.Success);

    // Act
    var result = await SignInManagerMock.Object.PasswordSignInAsync(
        email, password, false, false);

    // Assert
    result.Should().Be(SignInResult.Success);
}
```

### Using MockIdentityFactory
```csharp
// Create a user with specific properties
var user = MockIdentityFactory.CreateTestUser(
    sceneName: "RopeExpert",
    email: "expert@example.com",
    role: UserRole.Organizer,
    isVetted: true);

// Create a UserManager with pre-configured user
var userManager = MockIdentityFactory.CreateUserManagerWithUser(user);

// Create standard roles
var roles = MockIdentityFactory.CreateStandardRoles();
```

### Using IdentityTestHelpers
```csharp
// Create complete authentication context
var context = IdentityTestHelpers.CreateAuthenticationContext();

// Simulate successful login
await IdentityTestHelpers.SimulateSuccessfulLogin(
    context.UserManager,
    context.SignInManager,
    "user@example.com",
    "password");

// Validate password
var errors = IdentityTestHelpers.ValidatePassword("weak");
```

## Common Test Scenarios

### Testing Role-Based Authorization
```csharp
var user = new IdentityUserBuilder().AsOrganizer().Build();
SetupUserInRole(user, "Organizer", true);
SetupGetUserRoles(user, "Organizer", "Member");

var isOrganizer = await UserManagerMock.Object.IsInRoleAsync(user, "Organizer");
```

### Testing Account Lockout
```csharp
var user = new IdentityUserBuilder()
    .AsLockedOut(TimeSpan.FromMinutes(30))
    .Build();

SetupCheckPasswordSignIn(user, "password", SignInResult.LockedOut);
```

### Testing Email Confirmation
```csharp
var user = new IdentityUserBuilder().WithUnconfirmedEmail().Build();
var token = await IdentityTestHelpers.SetupEmailConfirmation(UserManagerMock, user);
var result = await UserManagerMock.Object.ConfirmEmailAsync(user, token);
```

## Best Practices

1. **Inherit from IdentityTestBase** for tests that need Identity mocks
2. **Use builders** for creating test data with specific characteristics
3. **Use factory methods** for common scenarios
4. **Use helper methods** for complex flows like authentication
5. **Always verify** the operations you expect to be called

## Integration with Other Test Projects

Both API and Web test projects can use these infrastructure classes:

```csharp
// In API tests
public class AuthControllerTests : IdentityTestBase
{
    // Your API controller tests here
}

// In Web tests  
public class AccountControllerTests : IdentityTestBase
{
    // Your MVC controller tests here
}
```