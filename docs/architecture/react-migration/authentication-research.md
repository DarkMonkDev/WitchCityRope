# React Authentication Research

*Generated on August 13, 2025*

## ⚠️ DECISION UPDATE - August 16, 2025 ⚠️

### FINAL DECISION: Hybrid JWT + HttpOnly Cookies with ASP.NET Core Identity

**After vertical slice testing and service-to-service authentication analysis, the final decision is:**
- **Hybrid JWT + HttpOnly Cookies** approach using ASP.NET Core Identity
- **Key Factor**: Service-to-service authentication requirement discovered during implementation
- **Cost**: $0 (completely free solution)
- **Implementation Speed**: Fastest to production-ready state
- **Reference**: See `/docs/functional-areas/vertical-slice-home-page/authentication-decision-report.md` for complete analysis

**This decision supersedes the original NextAuth.js recommendation below.**

---

## Initial Research - Superseded by Final Decision Above

*The research below represents initial analysis. The final implementation uses the Hybrid JWT + HttpOnly Cookies approach documented in the authentication decision report.*

## Overview
This document researches modern React authentication patterns, security best practices, and implementation strategies for the WitchCityRope migration from Blazor Server to React.

## Current WitchCityRope Authentication System

### Existing Implementation
- **Architecture**: JWT-based authentication with Identity Server integration
- **Pattern**: Web service → API endpoints → SignInManager → Cookies
- **Features**: 
  - Email/username and password authentication
  - Google OAuth integration (placeholder)
  - Two-factor authentication (2FA)
  - Remember Me functionality (30 days)
  - Password reset and management
  - Role-based access control (Admin, Teacher, Vetted Member, General Member, Guest)
  - Age verification (21+)
  - Account lockout and security features

### Current Security Measures
- JWT tokens with refresh token pattern
- Server-side authentication enforcement
- Role-based authorization attributes
- Secure API communication patterns
- Account lockout and brute force protection

## Modern React Authentication Patterns (2025)

### 1. Security-First Approach

#### HTTP-Only Cookies Pattern (Recommended)
**Current Best Practice**: As of 2025, storing tokens in browser local storage is **not considered secure**. The recommended approach uses:

- **HTTP-Only Cookies**: Tokens stored in secure, HTTP-only cookies
- **Short-lived Access Tokens**: 15-minute expiration times
- **Longer-lived Refresh Tokens**: 7-day expiration in HTTP-only cookies
- **Token Handler Pattern**: Backend components handle token security

#### Why Avoid Local Storage
- **XSS Vulnerability**: If an attacker can run JavaScript via XSS, they can access local storage
- **No Secure Browser Storage**: No truly secure way to store tokens in browser as of 2025
- **Industry Consensus**: Moving away from client-side token storage

### 2. Recommended Implementation Strategy

#### Token Handler Pattern
```typescript
// Secure pattern - tokens handled server-side
const authService = {
  login: async (credentials) => {
    // Server sets HTTP-only cookies
    const response = await fetch('/api/auth/login', {
      method: 'POST',
      credentials: 'include', // Include cookies
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(credentials)
    });
    return response.json();
  },
  
  refreshToken: async () => {
    // Automatic refresh via HTTP-only cookies
    const response = await fetch('/api/auth/refresh', {
      method: 'POST',
      credentials: 'include'
    });
    return response.ok;
  }
};
```

#### Protected Routes Implementation
```typescript
// Route protection with role-based access
const ProtectedRoute = ({ children, requiredRoles = [] }) => {
  const { user, isAuthenticated, hasRole } = useAuth();
  
  if (!isAuthenticated) {
    return <Navigate to="/login" />;
  }
  
  if (requiredRoles.length > 0 && !requiredRoles.some(role => hasRole(role))) {
    return <Navigate to="/unauthorized" />;
  }
  
  return children;
};
```

## Authentication Library Comparison (2025)

### 1. **NextAuth.js (Auth.js)** - Open Source Leader
**Pros:**
- Free and open-source
- Extensive provider support (Google, GitHub, etc.)
- Works with any database
- Flexible and customizable
- Large community

**Cons:**
- Original developer abandoned project (concern for long-term support)
- Configuration can be complex
- Documentation gaps for app router
- More setup required
- Need to build custom UI components

**Best For**: Projects requiring full control, budget constraints, custom requirements

### 2. **Clerk** - Premium Developer Experience
**Pros:**
- Beautiful pre-built UI components
- Comprehensive user management dashboard
- MFA, RBAC, email verification out-of-the-box
- Excellent documentation
- Strong funding and team focus ($25M funding)
- Fast implementation

**Cons:**
- Expensive at scale ($550/month for 10K MAU)
- Vendor lock-in concerns
- Less backend control
- Pricing can be prohibitive for larger applications

**Best For**: Rapid development, full-featured user management, budget available

### 3. **Auth0** - Enterprise Standard
**Pros:**
- Industry standard for enterprise
- Comprehensive feature set
- Strong security track record
- Extensive integrations
- Enterprise support

**Cons:**
- Very expensive (especially at scale)
- Complex pricing structure
- Overkill for smaller applications
- Recent security incidents (2 breaches in 12 months)

**Best For**: Large enterprise applications with significant budgets

### 4. **Supabase Auth** - Database-Integrated
**Pros:**
- Free tier available
- Integrates with PostgreSQL
- Row-level security support
- Open-source Firebase alternative
- Good documentation

**Cons:**
- Best when using Supabase ecosystem
- Less mature than alternatives
- Limited customization options
- Requires learning Supabase concepts

**Best For**: Projects already using Supabase, PostgreSQL-based applications

### 5. **Firebase Authentication** - Google Ecosystem
**Pros:**
- Easy email/password authentication
- Google integration
- Free tier available
- Reliable infrastructure

**Cons:**
- Difficult setup for multiple providers
- Vendor lock-in to Google ecosystem
- Limited customization
- Not ideal for complex authentication needs

**Best For**: Simple authentication needs, Google-centric applications

## Role-Based Access Control (RBAC) Implementation

### 2025 RBAC Best Practices

#### 1. Principle of Least Privilege
- Grant minimum permissions necessary
- Regular permission audits
- Automatic permission expiration

#### 2. External Authorization Systems
- Policy as Code approach
- Use specialized RBAC services (Permit.io, Oso)
- Separate authorization logic from application code

#### 3. React-Specific RBAC Patterns

##### Context API Pattern
```typescript
// AuthContext for role management
const AuthContext = createContext({
  user: null,
  permissions: [],
  hasPermission: (permission) => false,
  hasRole: (role) => false
});

// Permission-based component rendering
const PermissionGate = ({ permission, children, fallback = null }) => {
  const { hasPermission } = useAuth();
  return hasPermission(permission) ? children : fallback;
};
```

##### Higher-Order Component Pattern
```typescript
// HOC for role-based component access
const withRole = (WrappedComponent, requiredRoles) => {
  return function WithRoleComponent(props) {
    const { hasRole } = useAuth();
    
    if (!requiredRoles.some(role => hasRole(role))) {
      return <Unauthorized />;
    }
    
    return <WrappedComponent {...props} />;
  };
};
```

### Current WCR Roles to Migrate
1. **Administrator/Admin**: Full system access
2. **Teacher**: Event management, member interaction
3. **Vetted Member**: Access to vetted events, enhanced features
4. **General Member**: Basic member features
5. **Guest/Attendee**: Public access, basic registration

## Security Considerations for WitchCityRope

### 1. Age Verification (21+)
- Implement robust age verification system
- Maintain compliance with community standards
- Audit trail for age verification

### 2. Sensitive Community Data
- Protect member privacy and scene names
- Secure handling of vetting applications
- Incident report confidentiality
- Payment information security

### 3. Two-Factor Authentication
- Maintain current 2FA implementation
- Consider modern alternatives (WebAuthn, FIDO2)
- SMS and authenticator app support

### 4. Session Management
- Implement secure session handling
- Concurrent session management
- Session timeout policies
- Remember me functionality (current: 30 days)

## Recommended Authentication Architecture for WitchCityRope

### Option A: NextAuth.js + Custom Implementation (Recommended)
**Rationale**: Provides maximum control and customization needed for community features

**Architecture:**
```
React App → NextAuth.js → Custom API → JWT with HTTP-Only Cookies
```

**Implementation:**
- NextAuth.js for core authentication flows
- Custom role management system
- HTTP-only cookie token storage
- Integration with existing .NET API
- Custom UI components matching WCR design

**Pros:**
- Full control over authentication flows
- Cost-effective (open source)
- Integrates well with existing API
- Customizable for community-specific needs
- Maintains security best practices

**Cons:**
- More development time required
- Need to build custom UI components
- Ongoing maintenance responsibility

### Option B: Clerk + Custom Integration (Alternative)
**Rationale**: Faster implementation with professional UI components

**Architecture:**
```
React App → Clerk Components → Clerk API → Webhook Integration → Custom API
```

**Implementation:**
- Clerk for authentication and user management
- Custom role mapping for WCR-specific roles
- Webhook integration with existing API
- Custom styling to match WCR design

**Pros:**
- Rapid implementation
- Professional UI components
- Built-in user management
- MFA and advanced features included

**Cons:**
- Higher cost at scale
- Vendor dependency
- Limited customization for community features
- Integration complexity with existing API

## Migration Strategy Recommendations

### Phase 1: Authentication Foundation
1. **Choose Authentication Library**: NextAuth.js recommended
2. **Implement Core Auth Flows**: Login, logout, registration
3. **JWT Token Management**: HTTP-only cookies pattern
4. **Basic Role System**: Map existing roles to React implementation

### Phase 2: Advanced Features
1. **Two-Factor Authentication**: Maintain current 2FA system
2. **Social Authentication**: Implement Google OAuth
3. **Password Management**: Reset, change, strength validation
4. **Session Management**: Remember me, concurrent sessions

### Phase 3: Community-Specific Features
1. **Age Verification**: 21+ requirement implementation
2. **Vetting System Integration**: Application and approval flows
3. **Scene Name Management**: Privacy and identity features
4. **Incident Report Security**: Confidential reporting system

### Phase 4: Enhanced Security
1. **Advanced RBAC**: Granular permissions system
2. **Audit Trails**: Comprehensive logging
3. **Security Monitoring**: Anomaly detection
4. **Compliance Features**: Privacy protection enhancements

## Technical Implementation Details

### API Integration Pattern
```typescript
// Axios interceptor for automatic token handling
api.interceptors.request.use((config) => {
  // Tokens handled automatically via HTTP-only cookies
  config.withCredentials = true;
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Attempt token refresh
      const refreshed = await authService.refreshToken();
      if (refreshed) {
        // Retry original request
        return api.request(error.config);
      } else {
        // Redirect to login
        authService.logout();
      }
    }
    return Promise.reject(error);
  }
);
```

### Form Integration
```typescript
// React Hook Form integration with validation
const LoginForm = () => {
  const { login } = useAuth();
  const { register, handleSubmit, formState: { errors } } = useForm();
  
  const onSubmit = async (data) => {
    try {
      await login(data);
      // Handle successful login
    } catch (error) {
      // Handle login error
    }
  };
  
  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <input 
        {...register('email', { required: 'Email is required' })}
        type="email"
      />
      {errors.email && <span>{errors.email.message}</span>}
      {/* More form fields */}
    </form>
  );
};
```

## Security Checklist for Implementation

### Must-Have Security Features
- [ ] HTTP-only cookie token storage
- [ ] Short-lived access tokens (15 minutes)
- [ ] Automatic token refresh
- [ ] CSRF protection
- [ ] Rate limiting on auth endpoints
- [ ] Account lockout protection
- [ ] Password strength requirements
- [ ] Two-factor authentication
- [ ] Secure password reset flows
- [ ] Session management
- [ ] Role-based access control
- [ ] Audit logging
- [ ] Age verification (21+)
- [ ] Privacy protection for sensitive data

### Nice-to-Have Enhancements
- [ ] WebAuthn/FIDO2 support
- [ ] Device management
- [ ] Login notification system
- [ ] Advanced anomaly detection
- [ ] Single sign-on (SSO) options
- [ ] Progressive enhancement
- [ ] Offline authentication handling

## Conclusion

For the WitchCityRope migration, **NextAuth.js with custom implementation** is recommended as the primary approach, providing the flexibility needed for community-specific features while maintaining modern security practices. The architecture should prioritize HTTP-only cookie token storage and follow the token handler pattern for maximum security.

The migration should be implemented in phases, starting with core authentication and gradually adding community-specific features. Security must be the top priority throughout the implementation, with particular attention to protecting sensitive community data and maintaining compliance with age verification requirements.

Key success factors include:
1. Following 2025 security best practices (HTTP-only cookies, short token lifespans)
2. Maintaining feature parity with current Blazor implementation
3. Implementing robust RBAC for community roles
4. Ensuring seamless user experience during migration
5. Providing comprehensive audit trails for compliance