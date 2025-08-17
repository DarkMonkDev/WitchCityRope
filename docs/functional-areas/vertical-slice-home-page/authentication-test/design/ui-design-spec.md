# UI Design Specification: Authentication Vertical Slice Test
<!-- Last Updated: 2025-08-16 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Draft -->

## Overview
This document specifies the UI design for the authentication vertical slice test - a throwaway proof-of-concept to validate the Hybrid JWT + HttpOnly Cookies authentication pattern. The designs prioritize functionality over aesthetics to enable rapid implementation and testing.

## Design Principles for POC
- **Functionality over aesthetics**: Basic styling to validate UX flow
- **Rapid implementation**: Simple HTML/CSS patterns that translate easily to React
- **Testing focused**: All interactive elements have `data-testid` attributes
- **Minimal complexity**: No advanced animations or complex layouts
- **Authentication pattern validation**: Clear indication of protected vs public content

## Page Specifications

### 1. Login Page (`login-page-mockup.html`)

#### Purpose
Validate user authentication via email/password with HttpOnly cookie generation.

#### Layout
- **Container**: Centered form (400px max width)
- **Background**: Dark theme (#1a1a2e) consistent with Salem branding
- **Form**: Single column layout with clear field grouping

#### Components
- **Email Input**: Standard email validation
- **Password Input**: Masked text input
- **Submit Button**: Full-width primary action
- **Error Display**: Hidden by default, shows validation errors
- **Register Link**: Navigation to registration page

#### Validation States
- **Empty Fields**: Client-side required field validation
- **Invalid Credentials**: Server error display placeholder
- **Success**: Redirect indication to protected page

#### Data Test IDs
```html
data-testid="login-form"          // Form container
data-testid="email-input"         // Email field
data-testid="password-input"      // Password field
data-testid="login-button"        // Submit button
data-testid="login-error"         // Error message
data-testid="register-link"       // Link to registration
```

### 2. Registration Page (`register-page-mockup.html`)

#### Purpose
Validate new user registration with email, password, and scene name.

#### Layout
- **Container**: Centered form (400px max width)
- **Form Fields**: Email, Scene Name, Password with help text
- **Validation**: Client-side basic validation

#### Components
- **Email Input**: Unique email requirement
- **Scene Name Input**: Community display name (3-50 chars)
- **Password Input**: Strong password requirements
- **Help Text**: Field-specific guidance
- **Submit Button**: Account creation action
- **Login Link**: Navigation to login page

#### Business Rules Validation
- **Email**: Valid format, unique in system
- **Scene Name**: 3-50 characters, alphanumeric + spaces
- **Password**: 8+ chars, uppercase, lowercase, number, special char

#### Data Test IDs
```html
data-testid="register-form"       // Form container
data-testid="email-input"         // Email field
data-testid="scene-name-input"    // Scene name field
data-testid="password-input"      // Password field
data-testid="register-button"     // Submit button
data-testid="register-error"      // Error message
data-testid="login-link"          // Link to login
```

### 3. Protected Welcome Page (`protected-welcome-mockup.html`)

#### Purpose
Validate protected content access and display authenticated user information.

#### Layout
- **Header**: Protected page indicator
- **Welcome Section**: Personalized greeting with scene name
- **User Info Panel**: Account details from API
- **Actions**: Navigation and logout options
- **Server Time**: Real-time display showing API connectivity

#### Authentication Indicators
- **Protected Badge**: Clear indication of authentication requirement
- **Welcome Message**: Personalized with user's scene name
- **User Information**: Data fetched from protected API endpoint
- **Logout Function**: Session termination capability

#### API Data Display
```typescript
// Data from protected API endpoint
{
  message: "Welcome [SceneName]!",
  user: {
    sceneName: string,
    email: string,
    createdAt: string,
    lastLoginAt: string
  },
  serverTime: string
}
```

#### Data Test IDs
```html
data-testid="protected-indicator" // Protected page badge
data-testid="welcome-message"     // Welcome greeting
data-testid="user-scene-name"     // Scene name display
data-testid="user-email"          // Email display
data-testid="member-since"        // Registration date
data-testid="last-login"          // Last login time
data-testid="account-status"      // Account status
data-testid="events-link"         // Public events navigation
data-testid="profile-link"        // Profile editing link
data-testid="logout-button"       // Logout action
data-testid="server-time"         // Server time display
```

## Design System Elements

### Color Palette
```css
/* Primary Background */
--bg-primary: #1a1a2e;           /* Dark blue-black */
--bg-secondary: #16213e;         /* Lighter dark blue */
--bg-tertiary: #2d3748;          /* Gray for info panels */

/* Brand Colors */
--brand-primary: #6B46C1;        /* Purple - primary actions */
--brand-secondary: #553C9A;      /* Darker purple - hover states */

/* Text Colors */
--text-primary: #ffffff;         /* Primary text */
--text-secondary: #e2e8f0;       /* Secondary text */
--text-muted: #a0aec0;          /* Muted text and labels */

/* State Colors */
--success: #38a169;              /* Success states */
--error: #e53e3e;                /* Error states */
--border: #4a5568;               /* Form borders */
```

### Typography
```css
/* Font Stack */
font-family: Arial, sans-serif;

/* Heading Sizes */
h1: 24px, bold, #6B46C1
h3: 18px, normal, #e2e8f0

/* Body Text */
body: 14px, normal, #ffffff
labels: 14px, normal, #e2e8f0
help-text: 12px, normal, #a0aec0
```

### Form Elements
```css
/* Input Fields */
input {
  padding: 12px;
  border: 1px solid #4a5568;
  border-radius: 4px;
  background: #2d3748;
  color: white;
  font-size: 14px;
}

input:focus {
  border-color: #6B46C1;
  box-shadow: 0 0 0 3px rgba(107, 70, 193, 0.1);
}

/* Buttons */
.primary-button {
  background: #6B46C1;
  color: white;
  padding: 12px 24px;
  border-radius: 4px;
  font-size: 16px;
}

.primary-button:hover {
  background: #553C9A;
}
```

## Responsive Behavior

### Mobile Breakpoint (< 480px)
- Full-width forms maintain 20px side margins
- Stack action buttons vertically
- Reduce padding for smaller screens
- Maintain touch-friendly 44px minimum button height

### Tablet/Desktop (> 480px)
- Center forms with max-width constraints
- Horizontal button layouts where appropriate
- Increased padding for comfortable desktop use

## Accessibility Considerations

### Keyboard Navigation
- Tab order: Email → Password/Scene Name → Submit → Links
- Enter key submits forms
- Escape key closes error messages

### Screen Reader Support
- Form labels properly associated with inputs
- Error messages announced when displayed
- Required field indicators (*) with aria-labels
- Button and link purposes clearly described

### Color Contrast
- Text colors meet WCAG 2.1 AA standards (4.5:1 ratio)
- Error states use both color and text indicators
- Focus indicators visible for keyboard users

## Animation and Interaction

### Minimal Animation Approach
- **Hover States**: Subtle color transitions (0.2s ease)
- **Focus States**: Immediate border/shadow changes
- **Error Display**: Simple show/hide (no complex animations)
- **Form Submission**: Basic loading states

### State Transitions
```css
/* Hover transitions */
button {
  transition: background-color 0.2s ease;
}

/* Focus indicators */
input:focus {
  transition: border-color 0.1s ease, box-shadow 0.1s ease;
}
```

## Integration with React Implementation

### Component Mapping
```typescript
// Page components map to React Router routes
LoginPage → /login → LoginPage.tsx
RegisterPage → /register → RegisterPage.tsx
ProtectedWelcome → /welcome → ProtectedWelcomePage.tsx

// Form components become controlled React components
login-form → LoginForm.tsx (React Hook Form)
register-form → RegisterForm.tsx (React Hook Form)

// Layout containers become Chakra UI components
.form-container → Box with styling props
.welcome-container → VStack with Card styling
```

### State Management Integration
```typescript
// Authentication state from mockup user info
interface AuthState {
  user: {
    id: string;
    email: string;
    sceneName: string;
    createdAt: string;
    lastLoginAt: string;
  } | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}
```

### API Integration Points
```typescript
// Mock data becomes real API calls
POST /api/auth/register → RegisterDto
POST /api/auth/login → LoginDto  
GET /api/protected/welcome → ProtectedWelcomeDto
POST /api/auth/logout → Success message
```

## Performance Considerations

### Critical Rendering Path
- Inline critical CSS for form styling
- Defer non-essential JavaScript
- Optimize for < 2 second authentication flows
- Minimize layout shifts during error display

### Loading States
- Form submission: Disable button, show loading text
- Page transitions: Simple redirect without complex loaders
- API calls: Basic spinner for protected content loading

## Testing Integration

### E2E Test Compatibility
All `data-testid` attributes designed for Playwright E2E tests:

```typescript
// Registration flow test
await page.fill('[data-testid="email-input"]', 'test@example.com');
await page.fill('[data-testid="scene-name-input"]', 'TestUser');
await page.fill('[data-testid="password-input"]', 'TestPass123!');
await page.click('[data-testid="register-button"]');
await expect(page.locator('[data-testid="welcome-message"]')).toContainText('Welcome TestUser');
```

### Manual Testing Checklist
- [ ] All forms submit with valid data
- [ ] Error states display correctly
- [ ] Navigation links work between pages
- [ ] Responsive behavior on mobile/desktop
- [ ] Keyboard navigation functional
- [ ] Screen reader compatibility verified

## Implementation Priority

### Phase 1: Core Forms (High Priority)
1. Login form with validation
2. Registration form with business rules
3. Basic error handling

### Phase 2: Protected Content (High Priority)
1. Welcome page with user data display
2. Authentication state indicators
3. Logout functionality

### Phase 3: Polish (Low Priority)
1. Improved error messaging
2. Enhanced responsive behavior
3. Accessibility refinements

## Handoff Notes for React Developer

### Key Implementation Points
1. **Forms**: Use React Hook Form + Zod for validation
2. **Styling**: Convert CSS to Chakra UI components where possible
3. **State**: Implement authentication context matching mockup state
4. **API**: Map mockup data to actual API responses
5. **Routing**: Protected routes should redirect to login when unauthenticated

### Mock Data for Development
```typescript
// Test user for development
const mockUser = {
  id: "550e8400-e29b-41d4-a716-446655440000",
  email: "test@example.com",
  sceneName: "TestUser",
  createdAt: "2025-08-16T10:00:00Z",
  lastLoginAt: "2025-08-16T10:30:00Z"
};
```

### Security Implementation Notes
- HttpOnly cookies prevent XSS (no localStorage for auth tokens)
- CSRF protection via SameSite=Strict cookies
- Input validation on both client and server
- JWT tokens for service-to-service communication only

Remember: This is throwaway code designed to validate the authentication pattern. Focus on proving the concept works rather than production-quality implementation.