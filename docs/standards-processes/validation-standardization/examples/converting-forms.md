# Converting Forms to Use Standardized Validation

## Example: Converting Profile.razor

### Before (Current Implementation)

```razor
<EditForm Model="@profile" OnValidSubmit="@SaveProfile">
    <div class="form-group">
        <label class="form-label">Scene Name</label>
        <SfTextBox @bind-Value="profile.SceneName" 
                  Placeholder="Your scene name"
                  CssClass="form-input"></SfTextBox>
        <span class="form-hint">This is how you'll be known in the community</span>
    </div>
</EditForm>
```

### After (With Standardized Validation)

```razor
<EditForm Model="@profile" OnValidSubmit="@SaveProfile">
    <DataAnnotationsValidator />
    <WcrValidationSummary />
    
    <WcrInputText @bind-Value="profile.SceneName"
                  Label="Scene Name"
                  IsRequired="true"
                  Placeholder="Your scene name"
                  HelpText="This is how you'll be known in the community"
                  ValidationFor="@(() => profile.SceneName)" />
</EditForm>
```

## Key Changes

1. **Add DataAnnotationsValidator**: This enables validation for the form
2. **Add WcrValidationSummary**: Shows all validation errors at the top
3. **Replace Syncfusion components**: Use our Wcr components for consistency
4. **Simplify markup**: Components handle all the wrapper divs and styling

## Model Requirements

The model needs validation attributes:

```csharp
public class ProfileViewModel
{
    [Required(ErrorMessage = "Scene name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Scene name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_'\.]+$", ErrorMessage = "Scene name contains invalid characters")]
    public string SceneName { get; set; } = string.Empty;
    
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Bio must be less than 500 characters")]
    public string? Bio { get; set; }
    
    // ... other properties
}
```

## Complete Form Example

```razor
@page "/profile"
@using WitchCityRope.Web.Shared.Validation.Components
@inject IValidationService ValidationService

<EditForm Model="@profile" OnValidSubmit="@SaveProfile">
    <DataAnnotationsValidator />
    
    <WcrValidationSummary Title="Please correct the following errors:" />
    
    <div class="form-section">
        <h3>Basic Information</h3>
        
        <WcrInputText @bind-Value="profile.SceneName"
                      Label="Scene Name"
                      IsRequired="true"
                      Placeholder="Your scene name"
                      HelpText="This is how you'll be known in the community" />
        
        <WcrInputEmail @bind-Value="profile.Email"
                       Label="Email Address"
                       IsRequired="true"
                       CheckUniqueness="true"
                       ExcludeUserId="@CurrentUserId"
                       HelpText="We'll never share your email" />
        
        <WcrInputTextArea @bind-Value="profile.Bio"
                          Label="Bio"
                          Placeholder="Tell us about yourself..."
                          MaxLength="500"
                          Rows="5"
                          HelpText="Share your experience and interests" />
        
        <WcrInputSelect @bind-Value="profile.Pronouns"
                        Label="Pronouns"
                        Placeholder="Select pronouns">
            <option value="she/her">she/her</option>
            <option value="he/him">he/him</option>
            <option value="they/them">they/them</option>
            <option value="other">Other</option>
        </WcrInputSelect>
    </div>
    
    <div class="form-actions">
        <button type="submit" class="btn btn-primary" disabled="@isSaving">
            @if (isSaving)
            {
                <span>Saving...</span>
            }
            else
            {
                <span>Save Changes</span>
            }
        </button>
        <button type="button" class="btn btn-secondary" @onclick="Cancel">
            Cancel
        </button>
    </div>
</EditForm>
```

## Benefits

1. **Consistent Validation**: All forms behave the same way
2. **Better UX**: Real-time validation with clear error messages
3. **Accessibility**: Proper ARIA attributes and keyboard navigation
4. **Less Code**: Components handle all the boilerplate
5. **Maintainable**: Changes to validation styling happen in one place
6. **Type-Safe**: Strong typing with validation attributes

## Migration Checklist

- [ ] Add validation attributes to model/DTO
- [ ] Add DataAnnotationsValidator to EditForm
- [ ] Replace input components with Wcr equivalents
- [ ] Add WcrValidationSummary if needed
- [ ] Test all validation scenarios
- [ ] Verify accessibility with screen reader
- [ ] Update any custom validation logic