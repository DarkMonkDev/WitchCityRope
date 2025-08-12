# Login Navigation Technical Design

## Architecture Overview

### Components Involved
1. **MainLayout.razor** - Primary layout with navigation
2. **IAuthService** - Authentication service interface
3. **UserDto** - User information including roles
4. **NavigationManager** - Blazor navigation service

### Current Implementation Analysis

#### MainLayout.razor Navigation Structure
```razor
@if (_currentUser != null)
{
    <!-- User dropdown menu -->
    <div class="user-menu-dropdown">
        <a href="/member/profile">My Profile</a>
        <a href="/member/tickets">My Tickets</a>
        <a href="/member/settings">Settings</a>
        @if (_currentUser.IsAdmin)
        {
            <a href="/admin">Admin Panel</a>
        }
        <button @onclick="Logout">Logout</button>
    </div>
}
else
{
    <a href="/auth/login" class="btn-primary">Login</a>
}
```

### Required Enhancements

#### 1. Navigation State Management
- Ensure `_currentUser` is properly populated after login
- Subscribe to authentication state changes
- Update navigation immediately without page refresh

#### 2. Role-Based Menu Items
Current roles and their navigation access:
- **Attendee**: Basic member features only
- **Member**: Full member features
- **Organizer**: Member features + event management
- **Moderator**: Member features + moderation tools
- **Administrator**: All features + admin panel

#### 3. Navigation Flow
```
Login → Auth Success → Update AuthState → Navigate to Dashboard → Update Nav Menu
```

### Implementation Details

#### Authentication State Updates
```csharp
protected override async Task OnInitializedAsync()
{
    _currentUser = await AuthService.GetCurrentUserAsync();
    AuthService.AuthenticationStateChanged += OnAuthStateChanged;
}

private async void OnAuthStateChanged()
{
    _currentUser = await AuthService.GetCurrentUserAsync();
    await InvokeAsync(StateHasChanged);
}
```

#### Navigation Menu Logic
```csharp
private bool ShowAdminLink => _currentUser?.IsAdmin == true;
private bool ShowModeratorTools => _currentUser?.Roles?.Contains("Moderator") == true;
private bool ShowOrganizerTools => _currentUser?.Roles?.Contains("Organizer") == true;
```

### Test Implementation Strategy

#### Integration Test Structure
```csharp
public class LoginNavigationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    
    [Theory]
    [InlineData("admin@witchcityrope.com", "Test123!", new[] { "Admin Panel" })]
    [InlineData("member@witchcityrope.com", "Test123!", new string[] { })]
    public async Task Login_ShowsCorrectNavigationItems(string email, string password, string[] expectedItems)
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act - Login
        var loginResponse = await LoginUser(client, email, password);
        
        // Act - Get navigation
        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        foreach (var item in expectedItems)
        {
            Assert.Contains(item, content);
        }
    }
}
```

### Performance Considerations
1. Cache user information to avoid repeated API calls
2. Use SignalR for real-time navigation updates
3. Minimize re-renders with proper state management

### Security Considerations
1. Verify role claims on both client and server
2. Never trust client-side role checks alone
3. Implement proper CSRF protection
4. Ensure secure cookie settings

### Mobile Responsiveness
- Hamburger menu for mobile devices
- Touch-friendly dropdown menus
- Proper viewport handling
- Consistent navigation across devices