# Blazor Server Validation Best Practices for .NET 9

## Date: January 11, 2025
## Author: Claude (AI Assistant)

## Overview
This document outlines the best practices for implementing validation in a Blazor Server application using .NET 9, based on Microsoft's official guidance and community standards.

## Recommended Approach: Pure Blazor Validation

### Why Blazor Validation?
1. **Consistency**: Single validation system across the entire application
2. **No JavaScript Dependencies**: Reduces bundle size and complexity
3. **Server-Side Security**: Validation runs on the server, preventing client-side bypass
4. **Real-Time Feedback**: Instant validation as users type (with proper debouncing)
5. **Type Safety**: Strongly typed validation with compile-time checks
6. **Future-Proof**: Aligns with Microsoft's direction for Blazor

### Core Components

#### 1. EditForm Component
```razor
<EditForm Model="@model" OnValidSubmit="@HandleValidSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />
    
    <!-- Form fields here -->
    
    <button type="submit">Submit</button>
</EditForm>
```

#### 2. Data Annotations
```csharp
public class LoginModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
    public string Password { get; set; }
}
```

#### 3. Validation Components
- `<DataAnnotationsValidator />` - Enables data annotation validation
- `<ValidationSummary />` - Shows all validation errors
- `<ValidationMessage For="@(() => model.Email)" />` - Shows field-specific errors

### Advanced Validation Patterns

#### 1. Fluent Validation Integration
For complex validation scenarios:
```csharp
public class EventValidator : AbstractValidator<EventModel>
{
    public EventValidator()
    {
        RuleFor(x => x.StartDate)
            .GreaterThan(DateTime.Now)
            .WithMessage("Event must be in the future");
            
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date");
    }
}
```

#### 2. Custom Validation Components
```razor
@inherits InputBase<string>

<input @bind="CurrentValue" @oninput="OnInputChanged" />

@code {
    private async Task OnInputChanged(ChangeEventArgs e)
    {
        CurrentValue = e.Value?.ToString();
        
        // Custom validation logic
        if (!IsValidSceneName(CurrentValue))
        {
            EditContext.AddValidationMessage(FieldIdentifier, "Scene name contains invalid characters");
        }
    }
}
```

#### 3. Async Validation
```csharp
public class UniqueEmailAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var dbContext = validationContext.GetService<ApplicationDbContext>();
        var email = value as string;
        
        if (dbContext.Users.Any(u => u.Email == email))
        {
            return new ValidationResult("Email already exists");
        }
        
        return ValidationResult.Success;
    }
}
```

### Syncfusion Blazor Integration
Since WitchCityRope uses Syncfusion components:

```razor
<SfTextBox @bind-Value="model.Email" Placeholder="Email">
    <TextBoxFieldSettings Type="InputType.Email"></TextBoxFieldSettings>
</SfTextBox>
<ValidationMessage For="@(() => model.Email)" />
```

### Validation Timing Strategies

#### 1. On Field Change (Default)
```razor
<EditForm Model="@model" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    <!-- Validates as user types -->
</EditForm>
```

#### 2. On Focus Lost
```razor
<input @bind="model.Email" @bind:event="onchange" />
```

#### 3. On Submit Only
```razor
@code {
    private EditContext editContext;
    
    protected override void OnInitialized()
    {
        editContext = new EditContext(model);
        editContext.SetFieldCssClassProvider(new SubmitOnlyFieldCssClassProvider());
    }
}
```

### Error Display Best Practices

#### 1. Inline Validation Messages
```razor
<div class="form-group">
    <label>Email</label>
    <InputText @bind-Value="model.Email" class="form-control" />
    <ValidationMessage For="@(() => model.Email)" class="text-danger" />
</div>
```

#### 2. Custom Error Styling
```css
.validation-message {
    color: var(--wcr-color-error);
    font-size: 0.875rem;
    margin-top: 0.25rem;
}

.invalid {
    border-color: var(--wcr-color-error);
}
```

#### 3. Accessible Error Messages
```razor
<InputText @bind-Value="model.Email" 
           aria-invalid="@(editContext.GetValidationMessages(() => model.Email).Any())"
           aria-describedby="email-error" />
<div id="email-error" class="validation-message">
    <ValidationMessage For="@(() => model.Email)" />
</div>
```

## Migration Strategy for Identity Pages

### Option 1: Convert to Blazor Components (Recommended)
- Replace Razor Pages with Blazor components
- Maintain same URLs using routing
- Consistent validation experience
- Full control over styling and behavior

### Option 2: Custom Identity UI Scaffolding
- Use .NET Identity scaffolding to generate Blazor components
- Customize generated components to match WitchCityRope design
- Built-in support for all Identity features

### Option 3: Hybrid Approach (Not Recommended)
- Keep Identity as Razor Pages
- Remove jQuery validation
- Rely on server-side validation only
- Less consistent user experience

## Implementation Guidelines

### 1. Validation Service
Create a centralized validation service:
```csharp
public interface IValidationService
{
    Task<bool> IsEmailUniqueAsync(string email, Guid? excludeUserId = null);
    Task<bool> IsSceneNameUniqueAsync(string sceneName, Guid? excludeUserId = null);
    ValidationResult ValidatePassword(string password);
}
```

### 2. Reusable Validation Components
```razor
<!-- SceneNameInput.razor -->
<div class="form-group">
    <label>@Label</label>
    <InputText @bind-Value="Value" class="form-control" />
    <ValidationMessage For="@ValidationFor" />
    @if (ShowHelpText)
    {
        <small class="form-text text-muted">@HelpText</small>
    }
</div>

@code {
    [Parameter] public string Label { get; set; }
    [Parameter] public string Value { get; set; }
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
    [Parameter] public Expression<Func<string>> ValidationFor { get; set; }
    [Parameter] public string HelpText { get; set; }
    [Parameter] public bool ShowHelpText { get; set; }
}
```

### 3. Form State Management
```csharp
public class FormStateService
{
    public bool IsSaving { get; private set; }
    public string LastError { get; private set; }
    
    public async Task<T> ExecuteWithLoadingAsync<T>(Func<Task<T>> action)
    {
        try
        {
            IsSaving = true;
            return await action();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            throw;
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

## Testing Validation

### 1. Unit Tests
```csharp
[Fact]
public void Email_Required_Validation()
{
    var model = new LoginModel { Email = "" };
    var context = new ValidationContext(model);
    var results = new List<ValidationResult>();
    
    var isValid = Validator.TryValidateObject(model, context, results, true);
    
    Assert.False(isValid);
    Assert.Contains(results, r => r.ErrorMessage.Contains("Email is required"));
}
```

### 2. Integration Tests
```csharp
[Fact]
public async Task LoginForm_InvalidEmail_ShowsError()
{
    var component = RenderComponent<LoginForm>();
    
    await component.Find("input[type='email']").ChangeAsync(new ChangeEventArgs 
    { 
        Value = "invalid-email" 
    });
    
    Assert.NotNull(component.Find(".validation-message"));
}
```

### 3. E2E Tests (Puppeteer)
```javascript
it('should show validation error for invalid email', async () => {
    await page.goto('http://localhost:8280/login');
    await page.type('#email', 'invalid-email');
    await page.click('#submit');
    
    const error = await page.$('.validation-message');
    expect(error).toBeTruthy();
    
    const errorText = await page.evaluate(el => el.textContent, error);
    expect(errorText).toContain('Invalid email address');
});
```

## Accessibility Considerations

1. **ARIA Attributes**: Use aria-invalid and aria-describedby
2. **Error Announcements**: Use live regions for screen reader users
3. **Focus Management**: Focus first error field on submission
4. **Color Independence**: Don't rely solely on color to indicate errors
5. **Clear Error Messages**: Be specific about what's wrong and how to fix it

## Performance Considerations

1. **Debounce Validation**: For async validation, debounce user input
2. **Lazy Validation**: Don't validate until user interacts with field
3. **Batch Validation**: Validate multiple fields together when related
4. **Cache Results**: Cache expensive validation results (e.g., uniqueness checks)

## Recommended Implementation Order

1. Create shared validation components
2. Implement validation service
3. Convert simplest Identity page (e.g., ForgotPassword)
4. Test thoroughly
5. Convert remaining Identity pages
6. Update all Blazor forms to use consistent patterns
7. Remove jQuery dependencies
8. Document standards for future development