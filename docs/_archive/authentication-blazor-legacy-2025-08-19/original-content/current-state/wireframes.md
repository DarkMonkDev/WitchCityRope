# Authentication System - Wireframes
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 2.0 -->
<!-- Owner: Authentication Team -->
<!-- Status: Active -->

## Overview
This document references the visual wireframes for authentication-related pages and components.

## Wireframe Files

### Login Page
**File**: [wireframes/auth-login-register-visual.html](../wireframes/auth-login-register-visual.html)
- Email/username input field
- Password field with show/hide toggle
- Remember me checkbox
- Login button
- Links to register and forgot password
- Social login buttons (not implemented)

### Registration Page  
**File**: [wireframes/auth-login-register-visual.html](../wireframes/auth-login-register-visual.html)
- Email input with validation
- Scene name (unique username)
- Password with complexity indicator
- Confirm password
- Date of birth (age verification)
- Legal name (optional, encrypted)
- Terms acceptance checkbox
- Register button

### Password Reset
**File**: [wireframes/auth-password-reset-visual.html](../wireframes/auth-password-reset-visual.html)
- Email input
- Submit button
- Success/error messages
- Link back to login

### Two-Factor Setup
**File**: [wireframes/auth-2fa-setup-visual.html](../wireframes/auth-2fa-setup-visual.html)
- QR code display
- Manual entry code
- Verification code input
- Backup codes display
- Enable/disable buttons

### Two-Factor Entry
**File**: [wireframes/auth-2fa-entry-visual.html](../wireframes/auth-2fa-entry-visual.html)
- 6-digit code input
- Remember device checkbox
- Use backup code link
- Verify button

## Component Patterns

### Form Layout
- Labels above inputs
- Required fields marked with *
- Help text below inputs
- Error messages below fields
- Submit button right-aligned

### Error Display
- Red text color (#dc3545)
- Icon before message
- Field-level and form-level errors
- Clear, actionable messages

### Success States
- Green color (#28a745)
- Checkmark icon
- Auto-redirect after success
- Clear success messages

### Mobile Responsiveness
- Single column layout
- Touch-friendly input sizes
- Larger tap targets (44px minimum)
- Simplified navigation

## User Interface Elements

### Input Fields
```html
<div class="form-group">
    <label for="email">Email Address *</label>
    <input type="email" 
           class="form-control" 
           id="email"
           data-testid="email-input"
           required>
    <small class="form-text text-muted">
        We'll never share your email
    </small>
</div>
```

### Password Field
```html
<div class="form-group">
    <label for="password">Password *</label>
    <div class="input-group">
        <input type="password" 
               class="form-control" 
               id="password"
               data-testid="password-input">
        <button type="button" 
                class="btn btn-outline-secondary"
                data-testid="toggle-password">
            Show
        </button>
    </div>
    <div class="password-strength">
        <!-- Strength indicator -->
    </div>
</div>
```

### Buttons
- Primary action: `btn btn-primary`
- Secondary action: `btn btn-outline-secondary`
- Danger action: `btn btn-danger`
- Disabled state during submission

## Accessibility Features

### ARIA Labels
- All form inputs have labels
- Error messages linked to inputs
- Loading states announced
- Success/error alerts

### Keyboard Navigation
- Tab order follows visual flow
- Enter submits forms
- Escape closes modals
- Focus visible indicators

### Screen Reader Support
- Descriptive labels
- Error announcements
- State changes communicated
- Help text associated

## Visual Design Notes

### Typography
- Headers: 24px, bold
- Labels: 14px, medium
- Input text: 16px, regular
- Help text: 12px, muted

### Spacing
- Form groups: 16px margin
- Label to input: 4px
- Input to help: 4px
- Between sections: 32px

### Colors
- Primary: #6f42c1 (purple)
- Success: #28a745 (green)
- Danger: #dc3545 (red)
- Muted: #6c757d (gray)

## Implementation Notes

### Responsive Breakpoints
- Mobile: < 768px (single column)
- Tablet: 768-1024px (adjusted padding)
- Desktop: > 1024px (centered form)

### Form Width
- Max width: 400px on desktop
- Full width on mobile
- Centered in viewport

### Validation
- Client-side for immediate feedback
- Server-side for security
- Inline error messages
- Summary at form top

---

*These wireframes represent the current implementation. For the technical implementation, see [functional-design.md](functional-design.md)*