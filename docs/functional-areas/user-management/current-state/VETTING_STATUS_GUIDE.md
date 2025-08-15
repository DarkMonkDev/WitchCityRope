# Vetting Status Implementation Guide

This guide explains how to properly check a user's vetting status in the WitchCityRope application.

## Background

The application tracks whether users have been "vetted" (approved to attend social events). This status is stored in the database and included in authentication claims.

## How Vetting Status Works

1. **Database Storage**: The `IsVetted` property is stored on the `User` entity in the database.

2. **Authentication Claims**: When a user logs in, the `IsVetted` status is added as a claim by `WitchCityRopeSignInManager`:
   ```csharp
   identity.AddClaim(new Claim("IsVetted", user.IsVetted.ToString()));
   ```

3. **No "VettedMember" Role**: There is NO role called "VettedMember". Vetting status is tracked as a boolean property/claim, not as a role.

## How to Check Vetting Status

### In Blazor Components

Use the provided extension methods from `WitchCityRope.Web.Extensions.ClaimsPrincipalExtensions`:

```csharp
@using WitchCityRope.Web.Extensions

@code {
    private bool isVetted = false;
    
    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
        // Simple check using extension method
        isVetted = user.IsVetted();
    }
}
```

### Manual Claim Check (if not using extensions)

```csharp
var isVettedClaim = user.FindFirst("IsVetted");
if (isVettedClaim != null && bool.TryParse(isVettedClaim.Value, out var vettedValue))
{
    isVetted = vettedValue;
}
```

### In API Controllers

The same approach works in API controllers:

```csharp
[HttpGet]
public IActionResult GetSocialEvents()
{
    var isVetted = User.IsVetted(); // Using extension method
    
    if (!isVetted)
    {
        return Forbid("User must be vetted to access social events");
    }
    
    // ... rest of the logic
}
```

## Getting Detailed Vetting Status

To get the full vetting application status (NotApplied, Pending, Approved, Rejected), call the Dashboard API:

```csharp
var dashboardData = await ApiClient.GetAsync<DashboardDto>($"api/dashboard/{userId}");
var vettingStatus = dashboardData.VettingStatus; // This is an enum with detailed status
```

## Common Mistakes to Avoid

1. **DON'T** check for a "VettedMember" role - it doesn't exist
2. **DON'T** assume all authenticated users are vetted
3. **DO** handle the case where the claim might be missing (for older sessions)
4. **DO** use the extension methods for cleaner code

## Extension Methods Available

The `ClaimsPrincipalExtensions` class provides these helpful methods:

- `IsVetted()` - Returns true if the user is vetted
- `GetUserId()` - Returns the user's GUID or null
- `GetSceneName()` - Returns the user's scene name
- `GetDisplayName()` - Returns the user's display name (with fallbacks)
- `GetUserRole()` - Returns the user's role claim

## Example: Events Page Implementation

Here's how the Events page properly checks vetting status to show/hide social events:

```csharp
// Check if user is vetted
isVetted = user.IsVetted();

// Get detailed vetting status from API
var userId = user.GetUserId();
if (userId.HasValue)
{
    var dashboardData = await ApiClient.GetAsync<DashboardDto>($"api/dashboard/{userId.Value}");
    vettingStatus = MapVettingStatus(dashboardData.VettingStatus);
    
    // Show message for non-vetted users
    showVettingMessage = !isVetted && 
        (vettingStatus == VettingStatus.NotApplied || vettingStatus == VettingStatus.Pending);
}

// In the UI, disable RSVP for social events if not vetted
@if (evt.IsSocialEvent && !isVetted)
{
    <button disabled>Vetting Required</button>
}
```

## Testing

When testing vetting functionality:

1. Create test users with `IsVetted = true` and `IsVetted = false`
2. Verify the claim is properly set after login
3. Test that social event access is properly restricted
4. Test that the UI properly shows/hides vetting-related messages