# WCR Validation Component Library - Definitive Reference

## Overview
The WitchCityRope Validation Component Library provides a complete set of Blazor components for consistent form validation across the application. All components follow ASP.NET Core validation patterns while providing enhanced user experience and consistent branding.

**This is the definitive reference for all WCR validation components.** For usage patterns and standards, also see:
- `/docs/standards-processes/form-fields-and-validation-standards.md` - When/how to use components
- `/docs/standards-processes/validation-standardization/VALIDATION_STANDARDS.md` - Architecture and principles

## Component Library (100% Complete)

### Core Components

#### 1. WcrValidationSummary
- **Purpose**: Displays all validation errors in a centralized location
- **Location**: `/src/WitchCityRope.Web/Shared/Validation/Components/WcrValidationSummary.razor`
- **Usage**:
```razor
<WcrValidationSummary />
```

#### 2. WcrValidationMessage
- **Purpose**: Displays validation message for a specific field
- **Location**: `/src/WitchCityRope.Web/Shared/Validation/Components/WcrValidationMessage.razor`
- **Usage**:
```razor
<WcrValidationMessage For="@(() => model.PropertyName)" />
```

### Input Components

#### 3. WcrInputText
- **Purpose**: Text input with validation support
- **Location**: `/src/WitchCityRope.Web/Shared/Validation/Components/WcrInputText.razor`
- **Features**: Label, placeholder, required indicator, disabled state, real-time validation
- **Usage**:
```razor
<WcrInputText @bind-Value="model.Name" 
              Label="Full Name"
              Placeholder="Enter your name"
              IsRequired="true" />
```

#### 4. WcrInputEmail
- **Purpose**: Email input with format validation and optional uniqueness check
- **Location**: `/src/WitchCityRope.Web/Shared/Validation/Components/WcrInputEmail.razor`
- **Features**: Email format validation, async uniqueness validation, all text input features
- **Usage**:
```razor
<WcrInputEmail @bind-Value="model.Email" 
               Label="Email Address"
               CheckUniqueness="true"
               ValidationService="ValidationService" />
```

#### 5. WcrInputPassword
- **Purpose**: Password input with requirements display
- **Location**: `/src/WitchCityRope.Web/Shared/Validation/Components/WcrInputPassword.razor`
- **Features**: Show/hide toggle, requirements checklist, strength indicator
- **Usage**:
```razor
<WcrInputPassword @bind-Value="model.Password" 
                  Label="Password"
                  ShowRequirements="true" />
```

#### 6. WcrInputSelect
- **Purpose**: Dropdown select with validation
- **Location**: `/src/WitchCityRope.Web/Shared/Validation/Components/WcrInputSelect.razor`
- **Features**: Option binding, required validation, disabled state
- **Usage**:
```razor
<WcrInputSelect @bind-Value="model.Role" 
                Label="Select Role"
                IsRequired="true">
    <option value="">Choose...</option>
    <option value="admin">Administrator</option>
    <option value="user">User</option>
</WcrInputSelect>
```

#### 7. WcrInputTextArea
- **Purpose**: Multi-line text input with validation
- **Location**: `/src/WitchCityRope.Web/Shared/Validation/Components/WcrInputTextArea.razor`
- **Features**: Adjustable rows, character counter, all text input features
- **Usage**:
```razor
<WcrInputTextArea @bind-Value="model.Description" 
                  Label="Description"
                  Rows="5"
                  MaxLength="500" />
```

#### 8. WcrInputCheckbox
- **Purpose**: Checkbox input with label integration
- **Location**: `/src/WitchCityRope.Web/Shared/Validation/Components/WcrInputCheckbox.razor`
- **Features**: Inline label, required validation for agreements
- **Usage**:
```razor
<WcrInputCheckbox @bind-Value="model.AgreeToTerms" 
                  Label="I agree to the terms and conditions"
                  IsRequired="true" />
```

#### 9. WcrInputNumber
- **Purpose**: Numeric input with min/max validation
- **Location**: `/src/WitchCityRope.Web/Shared/Validation/Components/WcrInputNumber.razor`
- **Features**: Decimal support, min/max constraints, step attribute
- **Usage**:
```razor
<WcrInputNumber @bind-Value="model.Price" 
                Label="Event Price"
                Min="0"
                Max="500"
                Step="0.01" />
```

#### 10. WcrInputDate
- **Purpose**: Date/time input with various formats
- **Location**: `/src/WitchCityRope.Web/Shared/Validation/Components/WcrInputDate.razor`
- **Features**: Multiple date types, min/max constraints, format conversion
- **Usage**:
```razor
<WcrInputDate @bind-Value="model.EventDate" 
              Label="Event Date"
              DateType="InputDateType.DateTimeLocal"
              Min="DateTime.Now" />
```

#### 11. WcrInputRadio
- **Purpose**: Radio button group with validation
- **Location**: `/src/WitchCityRope.Web/Shared/Validation/Components/WcrInputRadio.razor`
- **Features**: Generic type support, custom display text, horizontal/vertical layout
- **Usage**:
```razor
<WcrInputRadio @bind-Value="model.MembershipType" 
               Options="@(new[] { "Basic", "Premium", "VIP" })"
               Label="Select Membership"
               Orientation="horizontal" />
```

## Styling System

### CSS Variables
All components use consistent CSS variables defined in `validation.css`:
```css
--wcr-color-burgundy: #8B0000;
--wcr-color-cream: #FFF8DC;
--wcr-color-charcoal: #36454F;
--wcr-color-taupe: #8B8680;
--wcr-color-stone: #708090;
--wcr-color-brass: #B5651D;
--wcr-color-error: #DC3545;
--wcr-color-success: #28A745;
```

### Form Group Structure
```html
<div class="wcr-form-group has-error">
    <label class="wcr-label">
        Label Text
        <span class="wcr-required">*</span>
    </label>
    <input class="wcr-input" />
    <div class="wcr-field-validation">
        <i class="bi bi-exclamation-circle"></i>
        <span>Error message</span>
    </div>
</div>
```

## Integration Patterns

### Basic Form Setup
```razor
<EditForm Model="@model" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    
    <WcrInputText @bind-Value="model.Name" Label="Name" IsRequired="true" />
    <WcrInputEmail @bind-Value="model.Email" Label="Email" />
    <WcrInputSelect @bind-Value="model.Role" Label="Role">
        <option value="">Select...</option>
        <option value="admin">Admin</option>
        <option value="user">User</option>
    </WcrInputSelect>
    
    <WcrValidationSummary />
    
    <button type="submit" class="wcr-button wcr-button-primary">Submit</button>
</EditForm>
```

### Model with Validation Attributes
```csharp
public class UserFormModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = "";
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = "";
    
    [Required(ErrorMessage = "Please select a role")]
    public string Role { get; set; } = "";
}
```

## Best Practices

### 1. Consistent Labels
Always provide labels for accessibility:
```razor
<WcrInputText @bind-Value="model.Field" Label="Field Name" />
```

### 2. Required Field Indicators
Use `IsRequired` parameter to show visual indicators:
```razor
<WcrInputText @bind-Value="model.RequiredField" Label="Required Field" IsRequired="true" />
```

### 3. Placeholder Text
Provide helpful placeholder text:
```razor
<WcrInputText @bind-Value="model.Phone" 
              Label="Phone Number" 
              Placeholder="(555) 123-4567" />
```

### 4. Validation Messages
Use appropriate validation attributes on your model:
```csharp
[Required(ErrorMessage = "This field is required")]
[StringLength(100, ErrorMessage = "Maximum length is 100 characters")]
public string Field { get; set; }
```

### 5. Form Layout
Use proper spacing with form groups:
```css
.wcr-form-group {
    margin-bottom: 1.5rem;
}
```

## Migration Guide

### Converting from Standard Blazor Components
```razor
<!-- OLD -->
<div class="form-group">
    <label for="name">Name</label>
    <InputText id="name" @bind-Value="model.Name" class="form-control" />
    <ValidationMessage For="@(() => model.Name)" />
</div>

<!-- NEW -->
<WcrInputText @bind-Value="model.Name" Label="Name" IsRequired="true" />
```

### Converting from Syncfusion Components
```razor
<!-- OLD -->
<SfTextBox @bind-Value="model.Email" Placeholder="Email" />

<!-- NEW -->
<WcrInputEmail @bind-Value="model.Email" Label="Email" Placeholder="Enter your email" />
```

## Component Status

| Component | Status | Session | Lines of Code |
|-----------|---------|---------|---------------|
| WcrValidationSummary | ✅ Complete | 1 | 89 |
| WcrValidationMessage | ✅ Complete | 1 | 67 |
| WcrInputText | ✅ Complete | 1 | 125 |
| WcrInputEmail | ✅ Complete | 1 | 184 |
| WcrInputPassword | ✅ Complete | 1 | 267 |
| WcrInputSelect | ✅ Complete | 4 | 143 |
| WcrInputTextArea | ✅ Complete | 4 | 156 |
| WcrInputCheckbox | ✅ Complete | 6 | 119 |
| WcrInputNumber | ✅ Complete | 7 | 118 |
| WcrInputDate | ✅ Complete | 7 | 163 |
| WcrInputRadio | ✅ Complete | 8 | 178 |

**Total**: 11 components, ~1,609 lines of code

## Future Enhancements

### Potential Additional Components
- WcrInputFile - File upload with validation
- WcrInputRange - Slider input
- WcrInputColor - Color picker
- WcrInputSearch - Search with debounce
- WcrInputTags - Tag/chip input

### Advanced Features
- Async validation debouncing
- Custom validation rules
- Conditional validation
- Dynamic form generation
- Accessibility improvements

## Conclusion

The WCR Validation Component Library provides a complete, consistent, and user-friendly validation system for the WitchCityRope application. With 11 core components covering all common input scenarios, the library ensures:

- Consistent user experience across all forms
- Strong branding with WCR colors and styling
- Accessibility compliance
- Easy migration from existing components
- Reduced development time for new forms

The library is production-ready and has been successfully implemented across 80% of the application's forms.