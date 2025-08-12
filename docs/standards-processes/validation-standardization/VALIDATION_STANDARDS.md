# Witch City Rope Validation Standards

## Overview

This document outlines the standardized validation approach for the Witch City Rope application. All forms should follow these standards to ensure consistency, maintainability, and a superior user experience.

## Core Principles

1. **Server-Side First**: All validation logic runs on the server for security and consistency
2. **Real-Time Feedback**: Provide immediate validation feedback as users interact with forms
3. **Accessibility**: All validation messages are accessible to screen readers
4. **Visual Consistency**: Unified styling for all validation states across the application
5. **Progressive Enhancement**: Forms work without JavaScript but provide enhanced experience with it

## Architecture

### Validation Stack

```
┌─────────────────────────────────┐
│   Blazor Components (UI Layer)  │
├─────────────────────────────────┤
│   Custom Validation Components   │
│   (WcrInput*, WcrSelect, etc.)  │
├─────────────────────────────────┤
│   EditForm + DataAnnotations    │
├─────────────────────────────────┤
│   IValidationService (Backend)   │
├─────────────────────────────────┤
│   Database Constraints          │
└─────────────────────────────────┘
```

### Key Components

1. **IValidationService** (`/src/WitchCityRope.Web/Shared/Validation/Services/IValidationService.cs`)
   - Centralized validation logic
   - Async validation methods (email uniqueness, scene name uniqueness)
   - Password complexity validation

2. **Custom Input Components** (`/src/WitchCityRope.Web/Shared/Validation/Components/`)
   - WcrInputText
   - WcrInputEmail
   - WcrInputPassword
   - WcrInputTextArea
   - WcrSelect
   - WcrValidationSummary

3. **Validation CSS** (`/src/WitchCityRope.Web/wwwroot/css/validation.css`)
   - Consistent error styling
   - Animation and transitions
   - Dark mode support

## Implementation Guidelines

### 1. Form Structure

Every form should use Blazor's EditForm with DataAnnotationsValidator:

```razor
<EditForm Model="model" OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    
    <!-- Form fields -->
    
    <WcrValidationSummary />
    
    <button type="submit">Submit</button>
</EditForm>
```

### 2. Model Validation

Use DataAnnotations on your model properties:

```csharp
public class LoginModel
{
    [Required(ErrorMessage = "Email or scene name is required")]
    public string EmailOrSceneName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
```

### 3. Custom Validation Components

Always use the custom WCR validation components instead of standard HTML inputs.

**Reference**: See `/docs/standards-processes/validation-standardization/VALIDATION_COMPONENT_LIBRARY.md` for complete component documentation and usage examples.

### 4. Async Validation

For database validation (email/scene name uniqueness):

```csharp
@inject IValidationService ValidationService

private async Task ValidateEmail()
{
    var isUnique = await ValidationService.IsEmailUniqueAsync(
        model.Email, 
        excludeUserId: currentUser?.Id
    );
    
    if (!isUnique)
    {
        // Handle duplicate email
    }
}
```

### 5. Password Validation

Password requirements are automatically displayed by WcrInputPassword:

```csharp
// Password must meet these requirements:
// - At least 8 characters
// - Contains uppercase letter
// - Contains lowercase letter
// - Contains number
// - Contains special character
```

### 6. Error Display

Validation errors are shown in three ways:

1. **Field-level errors**: Displayed under each input
2. **Validation summary**: Shows all errors at once
3. **Custom errors**: For server-side validation failures

```razor
<!-- Field automatically shows its validation error -->
<WcrInputEmail @bind-Value="model.Email" />

<!-- Summary shows all errors -->
<WcrValidationSummary />

<!-- Custom server errors -->
@if (!string.IsNullOrEmpty(serverError))
{
    <div class="validation-errors">
        <ul>
            <li>@serverError</li>
        </ul>
    </div>
}
```

## Styling Guidelines

### CSS Classes

- `.wcr-input` - Base input styling
- `.wcr-input-error` - Input with validation error
- `.wcr-field-validation` - Field-level error message
- `.wcr-validation-summary` - Validation summary container
- `.validation-errors` - Custom error container

### Error States

```css
/* Input error state */
.wcr-input-error {
    border-color: #dc3545;
    background-color: rgba(220, 53, 69, 0.05);
}

/* Error message */
.wcr-field-validation {
    color: #dc3545;
    font-size: 0.875rem;
    margin-top: 0.25rem;
}
```

### Animation

All validation messages fade in/out smoothly:

```css
.wcr-field-validation {
    animation: validationFadeIn 0.3s ease-out;
}

@keyframes validationFadeIn {
    from { opacity: 0; transform: translateY(-5px); }
    to { opacity: 1; transform: translateY(0); }
}
```

## Best Practices

### DO:
- ✅ Use server-side validation for all security-critical checks
- ✅ Provide immediate feedback on blur/change events
- ✅ Show password requirements before user starts typing
- ✅ Use consistent error messaging across the application
- ✅ Make all validation messages accessible to screen readers
- ✅ Test validation with keyboard-only navigation
- ✅ Handle loading states during async validation

### DON'T:
- ❌ Rely solely on client-side validation
- ❌ Use generic error messages ("Invalid input")
- ❌ Block form submission without clear error messages
- ❌ Validate on every keystroke (causes poor performance)
- ❌ Show multiple errors for the same field
- ❌ Use color alone to indicate errors

## Testing Validation

### Manual Testing Checklist

1. **Empty Form Submission**
   - [ ] All required fields show errors
   - [ ] Validation summary displays
   - [ ] Submit button remains enabled

2. **Invalid Input**
   - [ ] Email format validation works
   - [ ] Password requirements display
   - [ ] Error messages are specific and helpful

3. **Async Validation**
   - [ ] Duplicate email check works
   - [ ] Loading state shows during check
   - [ ] Error appears after check completes

4. **Accessibility**
   - [ ] Tab navigation works
   - [ ] Screen reader announces errors
   - [ ] Error messages are associated with inputs

### Automated Testing

Use Puppeteer for end-to-end validation testing:

```javascript
// Test empty form submission
await page.click('button[type="submit"]');
await page.waitForSelector('.wcr-validation-summary');

// Test invalid email
await page.type('input[type="email"]', 'invalid-email');
await page.keyboard.press('Tab');
await page.waitForSelector('.wcr-field-validation');

// Verify error message
const errorText = await page.$eval('.wcr-field-validation', el => el.textContent);
expect(errorText).toContain('valid email');
```

## Migration Guide

### Converting Existing Forms

1. **Replace jQuery Validation**
   ```html
   <!-- OLD -->
   <input asp-for="Email" class="form-control" />
   <span asp-validation-for="Email"></span>
   
   <!-- NEW -->
   <WcrInputEmail @bind-Value="model.Email" Label="EMAIL*" />
   ```

2. **Update Models**
   - Add DataAnnotations
   - Create nested InputModel if needed
   - Implement IValidatableObject for complex validation

3. **Remove Client Scripts**
   - Remove jquery.validate references
   - Remove validation partial views
   - Remove custom validation JavaScript

4. **Test Thoroughly**
   - Test all validation scenarios
   - Verify async validation works
   - Check accessibility

## Examples

### Simple Login Form

```razor
@page "/login"
@using System.ComponentModel.DataAnnotations

<EditForm Model="loginModel" OnValidSubmit="HandleLogin">
    <DataAnnotationsValidator />
    
    <div class="form-group">
        <WcrInputText @bind-Value="loginModel.EmailOrSceneName" 
                      Placeholder="Email or Scene Name"
                      Label="EMAIL OR SCENE NAME*" />
    </div>
    
    <div class="form-group">
        <WcrInputPassword @bind-Value="loginModel.Password" 
                          Placeholder="Password"
                          Label="PASSWORD*" />
    </div>
    
    <WcrValidationSummary />
    
    <button type="submit" class="btn-primary">Sign In</button>
</EditForm>

@code {
    private LoginModel loginModel = new();
    
    public class LoginModel
    {
        [Required(ErrorMessage = "Email or scene name is required")]
        public string EmailOrSceneName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }
    
    private async Task HandleLogin()
    {
        // Login logic here
    }
}
```

### Complex Registration Form

See `/src/WitchCityRope.Web/Features/Auth/Pages/Register.razor` for a complete example with:
- Multiple validation types
- Async uniqueness checks
- Password requirements
- Custom validation logic
- Server-side error handling

## Support

For questions about validation implementation:
1. Check existing implementations in `/src/WitchCityRope.Web/Features/Auth/Pages/`
2. Review the validation components source code
3. Consult the Blazor documentation for EditForm
4. Ask the development team for guidance

## Future Enhancements

- [ ] Add WcrCheckbox component
- [ ] Add WcrRadioGroup component  
- [ ] Add WcrDatePicker component
- [ ] Add WcrNumberInput component
- [ ] Implement client-side validation for offline scenarios
- [ ] Add validation telemetry for error tracking
- [ ] Create validation analyzer for build-time checks