# WitchCityRope Validation Standards

## Overview

This document outlines the validation standards and components used throughout the WitchCityRope application. All forms should use these standardized components for consistent user experience and maintainability.

## Validation Components

### 1. WcrValidationSummary
Displays all validation errors in a centralized location.

```razor
<EditForm Model="model" OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    <WcrValidationSummary />
    <!-- Form fields -->
</EditForm>
```

### 2. WcrValidationMessage
Displays validation message for a specific field.

```razor
<WcrValidationMessage For="@(() => model.PropertyName)" />
```

### 3. Input Components

All input components follow a consistent pattern:
- Support for `@bind-Value`
- Built-in validation display
- Required field indicators
- Consistent styling
- Accessibility support

#### WcrInputText
```razor
<WcrInputText @bind-Value="model.Name" 
              Label="Name" 
              Placeholder="Enter your name"
              IsRequired="true" />
```

#### WcrInputEmail
```razor
<WcrInputEmail @bind-Value="model.Email" 
               Label="Email Address"
               CheckUniqueness="true"
               ExcludeUserId="currentUserId" />
```

#### WcrInputPassword
```razor
<WcrInputPassword @bind-Value="model.Password"
                  Label="Password"
                  ShowRequirements="true"
                  ShowToggle="true" />
```

#### WcrInputSelect
```razor
<WcrInputSelect @bind-Value="model.Role"
                Label="Role"
                IsRequired="true">
    <option value="">-- Select --</option>
    <option value="Member">Member</option>
    <option value="Admin">Admin</option>
</WcrInputSelect>
```

#### WcrInputTextArea
```razor
<WcrInputTextArea @bind-Value="model.Bio"
                  Label="Biography"
                  Rows="4"
                  MaxLength="500" />
```

#### WcrInputCheckbox
```razor
<WcrInputCheckbox @bind-Value="model.AgreeToTerms"
                  Label="I agree to the terms and conditions"
                  IsRequired="true" />
```

#### WcrInputNumber
```razor
<WcrInputNumber TValue="int" 
                @bind-Value="model.Age"
                Label="Age"
                Min="21"
                Max="100" />
```

#### WcrInputDate
```razor
<WcrInputDate TValue="DateTime" 
              @bind-Value="model.BirthDate"
              Label="Birth Date"
              Max="@DateTime.Today.AddYears(-21)" />
```

#### WcrInputRadio
```razor
<WcrInputRadio TValue="string" 
               @bind-Value="model.MembershipType"
               Label="Membership Type"
               Options="membershipOptions"
               Orientation="horizontal" />
```

## Validation Patterns

### 1. Basic Form Structure

```razor
@page "/example"
@using WitchCityRope.Web.Shared.Validation.Components
@using System.ComponentModel.DataAnnotations

<EditForm Model="model" OnValidSubmit="HandleValidSubmit">
    <DataAnnotationsValidator />
    
    <WcrInputText @bind-Value="model.Name" 
                  Label="Name" 
                  IsRequired="true" />
    
    <WcrInputEmail @bind-Value="model.Email" 
                   Label="Email" 
                   IsRequired="true" />
    
    <WcrValidationSummary />
    
    <button type="submit" class="btn btn-primary">Submit</button>
</EditForm>

@code {
    private ExampleModel model = new();
    
    public class ExampleModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, ErrorMessage = "Name must not exceed 50 characters")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }
    
    private async Task HandleValidSubmit()
    {
        // Process form submission
    }
}
```

### 2. Custom Validation

For complex validation scenarios, use the IValidationService:

```csharp
@inject IValidationService ValidationService

// In your component code:
private async Task ValidateEmail()
{
    var isUnique = await ValidationService.IsEmailUniqueAsync(model.Email);
    if (!isUnique)
    {
        // Handle duplicate email
    }
}
```

### 3. Password Validation

Password fields automatically show requirements when configured:

```razor
<WcrInputPassword @bind-Value="model.Password"
                  Label="Password"
                  ShowRequirements="true"
                  ShowToggle="true"
                  IsRequired="true" />
```

Requirements checked:
- Minimum 8 characters
- Contains uppercase letter
- Contains lowercase letter
- Contains number
- Contains special character

### 4. Async Validation

Some components support async validation (e.g., email uniqueness):

```razor
<WcrInputEmail @bind-Value="model.Email"
               Label="Email"
               CheckUniqueness="true"
               ExcludeUserId="@currentUserId" />
```

## Styling Guidelines

### CSS Classes

All validation components use consistent CSS classes:
- `.wcr-form-group` - Container for form field
- `.wcr-label` - Field label
- `.wcr-input` - Input element
- `.wcr-required` - Required indicator (*)
- `.wcr-field-validation` - Validation message
- `.has-error` - Error state class
- `.wcr-validation-summary` - Summary container

### Error States

Components automatically apply error styling when validation fails:
- Red border on input
- Red text for error messages
- Icon indicator for errors

### Responsive Design

All components are responsive by default:
- Full width on mobile
- Appropriate sizing on desktop
- Touch-friendly targets

## Best Practices

1. **Always use DataAnnotationsValidator** in your EditForm
2. **Use appropriate input types** (email, password, number, etc.)
3. **Provide clear labels** for all form fields
4. **Mark required fields** with IsRequired="true"
5. **Use placeholders** to provide input examples
6. **Implement async validation** for uniqueness checks
7. **Show validation summary** for complex forms
8. **Test on mobile devices** for touch interaction
9. **Ensure accessibility** with proper ARIA attributes
10. **Handle loading states** during async operations

## Migration Guide

When converting existing forms:

1. Replace HTML inputs with Wcr components
2. Update @bind to @bind-Value
3. Add appropriate validation attributes to model
4. Add DataAnnotationsValidator to EditForm
5. Replace custom validation display with WcrValidationMessage
6. Test all validation scenarios

## Examples

See these files for reference implementations:
- `/src/WitchCityRope.Web/Features/Auth/Pages/Login.razor`
- `/src/WitchCityRope.Web/Features/Auth/Pages/Register.razor`
- `/src/WitchCityRope.Web/Features/Vetting/Pages/VettingApplicationStandardized.razor`
- `/src/WitchCityRope.Web/Features/Test/Pages/ValidationTest.razor`

## Component API Reference

Each component supports these common parameters:
- `@bind-Value` - Two-way binding
- `Label` - Field label text
- `Placeholder` - Input placeholder
- `IsRequired` - Shows required indicator
- `IsDisabled` - Disables input
- `AdditionalAttributes` - Pass-through attributes

Component-specific parameters are documented in each component file.