# WitchCityRope React Forms and Validation Requirements

<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Overview

This document consolidates all business requirements, validation rules, and accessibility standards for forms in the WitchCityRope React application. These requirements are extracted from the legacy Blazor implementation and adapted for React + TypeScript.

**Note**: This document focuses on **WHAT** needs to be validated and **WHY**. For **HOW** to implement forms in React, see [Forms Standardization](/docs/standards-processes/forms-standardization.md).

## Business Validation Rules

### 1. Email Validation

**Purpose**: Ensure valid email addresses for authentication and communication

**Requirements**:
- **Format**: Must match standard email regex pattern
- **Uniqueness**: Must be unique across all users (async validation)
- **Required**: Email is required for all user accounts
- **Case Handling**: Store as lowercase, accept any case input

**Error Messages**:
- Empty: "Email is required"
- Invalid format: "Please enter a valid email address"
- Already exists: "An account with this email already exists"

**Implementation Notes**:
- Async validation should debounce (500ms delay)
- Show loading indicator during uniqueness check
- Exclude current user when checking uniqueness (for profile updates)

### 2. Scene Name Validation

**Purpose**: Unique identifier for community members within events and interactions

**Requirements**:
- **Length**: 2-50 characters
- **Characters**: Letters, numbers, hyphens, underscores only
- **Uniqueness**: Must be unique across all users (async validation)
- **Required**: Scene name is required for all user accounts
- **Case Sensitivity**: Case-insensitive uniqueness check

**Regex Pattern**: `^[a-zA-Z0-9_-]+$`

**Error Messages**:
- Empty: "Scene name is required"
- Too short: "Scene name must be at least 2 characters"
- Too long: "Scene name must not exceed 50 characters"
- Invalid characters: "Scene name can only contain letters, numbers, hyphens, and underscores"
- Already exists: "This scene name is already taken"

### 3. Password Validation

**Purpose**: Ensure strong passwords for account security

**Requirements**:
- **Minimum Length**: 8 characters
- **Character Types**: Must contain all of:
  - At least one uppercase letter (A-Z)
  - At least one lowercase letter (a-z)
  - At least one digit (0-9)
  - At least one special character (@$!%*?&)

**Display Requirements**:
- Show password requirements BEFORE user starts typing
- Use visual indicators (checkmarks) as requirements are met
- Include show/hide password toggle
- Real-time validation feedback

**Error Messages**:
- Empty: "Password is required"
- Too short: "Password must be at least 8 characters"
- Missing uppercase: "Password must contain at least one uppercase letter"
- Missing lowercase: "Password must contain at least one lowercase letter"
- Missing digit: "Password must contain at least one number"
- Missing special: "Password must contain at least one special character (@$!%*?&)"

### 4. Password Confirmation

**Purpose**: Prevent password entry errors

**Requirements**:
- **Matching**: Must exactly match the password field
- **Real-time**: Validate as user types in confirmation field
- **Required**: Required when password is being set/changed

**Error Messages**:
- Empty: "Please confirm your password"
- Mismatch: "Passwords don't match"

### 5. Phone Number Validation

**Purpose**: Valid contact information for emergencies and communications

**Requirements**:
- **Format**: US phone numbers in various formats
- **Pattern**: Allow (555) 123-4567, 555-123-4567, 5551234567, +1-555-123-4567
- **Normalization**: Store in standard format
- **Optional**: Phone numbers are optional unless specified otherwise

**Regex Pattern**: `^\+?1?[0-9]{10,14}$` (flexible for various formats)

**Error Messages**:
- Invalid format: "Please enter a valid phone number"

### 6. Emergency Contact Information

**Purpose**: Required safety information for event participation

**Required Fields**:
- **Name**: Required, 1-100 characters
- **Phone**: Required, valid phone number
- **Relationship**: Required, 1-50 characters

**Error Messages**:
- Name empty: "Emergency contact name is required"
- Phone empty: "Emergency contact phone number is required"
- Phone invalid: "Please enter a valid emergency contact phone number"
- Relationship empty: "Please specify your relationship to the emergency contact"

### 7. Event Registration Validation

**Purpose**: Ensure complete and safe event participation

**Required Fields**:
- **Event Selection**: Must select valid event
- **Attendee Type**: Member, Guest, or other defined types
- **Emergency Contact**: Complete emergency contact information
- **Waiver Agreement**: Must explicitly agree to waiver terms

**Optional Fields**:
- **Medical Information**: Free text, 0-1000 characters
- **Dietary Restrictions**: Free text, 0-500 characters
- **Special Requests**: Free text, 0-500 characters

**Error Messages**:
- No event: "Please select an event"
- No attendee type: "Please select your attendee type"
- No waiver: "You must agree to the waiver terms to register"

## Form Field Standards

### 1. Required Field Indicators

**Visual Indicator**: Red asterisk (*) after field label
**ARIA Support**: `aria-required="true"` on input elements
**Styling**: Use consistent red color for required indicators

### 2. Field Labels

**Pattern**: Use descriptive, clear labels
**Capitalization**: Sentence case ("Email address" not "EMAIL ADDRESS")
**Consistency**: Use same label text for same fields across forms

**Standard Labels**:
- "Email address"
- "Scene name"
- "Password"
- "Confirm password"
- "Phone number"
- "Emergency contact name"
- "Emergency contact phone"
- "Relationship to emergency contact"

### 3. Placeholder Text

**Purpose**: Provide input format examples
**Pattern**: Use realistic examples, not instructions

**Standard Placeholders**:
- Email: "your.email@example.com"
- Scene name: "YourSceneName"
- Phone: "(555) 123-4567"
- Emergency contact: "Jane Smith"

### 4. Help Text

**Purpose**: Provide additional guidance when needed
**Placement**: Below input field, above error messages
**Styling**: Smaller, muted text color

**Standard Help Text**:
- Scene name: "This is how you'll be known in the community"
- Password: "Must contain uppercase, lowercase, number, and special character"

## Error Message Standards

### 1. Error Display Patterns

**Field-Level Errors**: Show directly under each field
**Form-Level Errors**: Show at top of form in summary
**Server Errors**: Handle gracefully with user-friendly messages

### 2. Error Message Tone

**Style**: Helpful, not accusatory
**Language**: Clear, specific guidance on how to fix
**Avoid**: Generic messages like "Invalid input"

### 3. Error Styling

**Color**: Consistent red for all errors
**Icons**: Use warning icons for visual reinforcement
**Animation**: Smooth fade-in for new errors

## Accessibility Requirements

### 1. ARIA Attributes

**Required Attributes**:
- `aria-required="true"` for required fields
- `aria-invalid="true"` when field has errors
- `aria-describedby` linking to error messages and help text

### 2. Screen Reader Support

**Error Announcements**: Use `role="alert"` and `aria-live="polite"`
**Field Associations**: Proper label-input associations
**Focus Management**: Focus first error field on form submission

### 3. Keyboard Navigation

**Tab Order**: Logical tab sequence through form
**Enter Key**: Submit form when appropriate
**Escape Key**: Cancel/close form modals

### 4. Visual Accessibility

**Color Independence**: Don't rely solely on color for error states
**Contrast**: Ensure sufficient color contrast for all text
**Focus Indicators**: Clear visual focus indicators

## Loading and State Management

### 1. Form Submission States

**Loading State**: Disable form and show loading indicator
**Success State**: Clear success message with next steps
**Error State**: Preserve form data, show errors

### 2. Async Validation States

**Validation Loading**: Show spinner during async checks
**Debouncing**: 500ms delay before triggering async validation
**Error Handling**: Graceful degradation when validation service unavailable

### 3. Form Persistence

**Draft Saving**: Consider auto-save for long forms
**Navigation Warnings**: Warn before leaving unsaved forms
**Error Recovery**: Preserve user input when possible

## Business Rules from Legacy System

### 1. User Registration Rules

- Email and scene name must both be unique
- Password must meet all complexity requirements
- All fields are required (no optional registration fields)
- Terms of service agreement is required

### 2. Event Registration Rules

- Must be logged in to register for events
- Emergency contact information is mandatory
- Waiver agreement is mandatory
- Medical information is optional but recommended
- Registration capacity limits must be enforced

### 3. Profile Update Rules

- Cannot change email to one that already exists (except current)
- Cannot change scene name to one that already exists (except current)
- Password changes require current password confirmation
- Profile updates require re-authentication for sensitive changes

### 4. Safety and Consent Rules

- All event registrations require waiver acceptance
- Emergency contact information cannot be skipped
- Medical information should be encouraged but not required
- Privacy settings must be explicitly chosen

## Integration Requirements

### 1. Backend API Integration

**Validation Endpoints**:
- `POST /api/validation/email-unique` - Check email uniqueness
- `POST /api/validation/scene-name-unique` - Check scene name uniqueness
- `GET /api/validation/password-strength` - Validate password strength

**Error Response Format**: Standardized error responses from backend
**Authentication**: Validation endpoints may require authentication

### 2. Success Actions

**Registration Success**: Redirect to email verification page
**Login Success**: Redirect to intended destination or dashboard
**Profile Update**: Show success message, update navigation if needed
**Event Registration**: Show confirmation, send confirmation email

## Testing Requirements

### 1. Validation Testing

**Unit Tests**: Test all validation rules with edge cases
**Integration Tests**: Test form submission end-to-end
**Accessibility Tests**: Automated accessibility testing

### 2. User Experience Testing

**Usability Tests**: Test with actual community members
**Mobile Testing**: Ensure forms work well on mobile devices
**Screen Reader Testing**: Test with actual screen reader users

### 3. Performance Testing

**Load Testing**: Test async validation under load
**Network Testing**: Test with slow/unreliable connections
**Error Testing**: Test all error scenarios

## Migration Notes

### 1. Preserved from Blazor

These business requirements are preserved exactly from the Blazor implementation:
- All validation rules and error messages
- Accessibility requirements
- Required field definitions
- Business logic patterns

### 2. Removed Blazor Specifics

These technical details were Blazor-specific and not carried forward:
- EditForm component usage
- DataAnnotationsValidator patterns
- WCR Blazor component library
- Blazor-specific CSS classes
- ASP.NET Core validation attributes

### 3. React Adaptations

These areas need React-specific implementation:
- React Hook Form + Zod validation schemas
- React component patterns for form fields
- React error handling and state management
- React-specific accessibility implementations

## Related Documentation

- [Forms Standardization](/docs/standards-processes/forms-standardization.md) - React implementation patterns
- [Blazor Legacy Archive](/docs/_archive/blazor-legacy/forms-validation/) - Original Blazor documentation
- [Authentication Patterns](/docs/functional-areas/authentication/) - Auth-specific form requirements
- [Frontend Lessons Learned](/docs/lessons-learned/frontend-lessons-learned.md) - React implementation learnings

## Future Enhancements

### 1. Advanced Validation

- Multi-step form validation
- Conditional field validation
- Cross-field dependency validation
- Real-time password strength scoring

### 2. User Experience

- Form auto-completion
- Smart field suggestions
- Progressive form disclosure
- Improved mobile experience

### 3. Accessibility

- Voice input support
- Enhanced screen reader support
- Improved keyboard shortcuts
- Better error recovery

---

*This document represents the complete business requirements for forms and validation, extracted from the successful Blazor implementation and prepared for React migration. All validation rules, error messages, and accessibility standards should be preserved in the React implementation.*