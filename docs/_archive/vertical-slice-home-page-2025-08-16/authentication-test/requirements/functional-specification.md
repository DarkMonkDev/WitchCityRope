# Functional Specification: Authentication Vertical Slice Test
<!-- Last Updated: 2025-08-16 -->
<!-- Version: 1.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Draft -->

## Technical Overview
This functional specification defines the technical implementation approach for a throwaway authentication proof-of-concept that validates the Hybrid JWT + HttpOnly Cookies authentication pattern for the WitchCityRope React migration. This POC will prove React ↔ Web Service ↔ API Service authentication flows work correctly before committing to full implementation.

## Architecture

### Microservices Architecture
**CRITICAL**: This is a Web+API microservices architecture:
- **Web Service** (React + Vite): UI/Auth at http://localhost:5173
- **API Service** (Minimal API): Business logic at http://localhost:5655
- **Database** (PostgreSQL): localhost:5433
- **Pattern**: React → HTTP → Web Service → JWT → API Service → Database (NEVER React → Database directly)

### Authentication Flow Architecture
```
┌─────────────┐    HTTP/Cookies    ┌─────────────┐    JWT Bearer    ┌─────────────┐
│             │◄─────────────────► │             │◄──────────────► │             │
│   React     │                    │ Web Service │                 │ API Service │
│ Frontend    │                    │ (Auth+Proxy)│                 │ (Business)  │
│             │                    │             │                 │             │
└─────────────┘                    └─────────────┘                 └─────────────┘
      │                                   │                              │
      │ State Management                  │ Cookie Management            │ Data Access
      │ (Context + Hooks)                 │ (ASP.NET Identity)           │ (EF Core)
      │                                   │                              │
      ▼                                   ▼                              ▼
 ┌─────────────┐                  ┌─────────────┐              ┌─────────────┐
 │ Auth State  │                  │ HttpOnly    │              │ PostgreSQL  │
 │ React Ctx   │                  │ Cookies     │              │ Database    │
 └─────────────┘                  └─────────────┘              └─────────────┘
```

### Component Structure
```
/Features/Authentication/
├── Web/ (React Components)
│   ├── Pages/
│   │   ├── LoginPage.tsx
│   │   ├── RegisterPage.tsx
│   │   └── ProtectedWelcomePage.tsx
│   ├── Components/
│   │   ├── LoginForm.tsx
│   │   ├── RegisterForm.tsx
│   │   └── ProtectedRoute.tsx
│   ├── Hooks/
│   │   ├── useAuth.ts
│   │   └── useApi.ts
│   ├── Context/
│   │   └── AuthContext.tsx
│   └── Types/
│       └── auth.types.ts
├── API/ (C# Implementation)
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   └── ProtectedController.cs
│   ├── Services/
│   │   ├── IAuthService.cs
│   │   ├── AuthService.cs
│   │   ├── IJwtService.cs
│   │   └── JwtService.cs
│   ├── Models/
│   │   ├── User.cs
│   │   ├── RegisterDto.cs
│   │   ├── LoginDto.cs
│   │   └── UserDto.cs
│   └── Data/
│       └── AuthDbContext.cs
└── Database/
    └── Users table schema
```

### Service Architecture
- **React App**: UI components make HTTP calls to Web Service with cookie authentication
- **Web Service**: Handles authentication, generates JWT tokens for API service calls
- **API Service**: Business logic with JWT authentication from Web Service
- **No Direct Database Access**: React NEVER directly accesses database

## Data Models

### Database Schema
```sql
-- Users table for authentication
CREATE TABLE "Users" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Email" VARCHAR(254) NOT NULL UNIQUE,
    "SceneName" VARCHAR(50) NOT NULL UNIQUE,
    "PasswordHash" TEXT NOT NULL,
    "SecurityStamp" TEXT NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "IsEmailConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "LastLoginAt" TIMESTAMPTZ NULL
);

-- Indexes for performance
CREATE INDEX "IX_Users_Email" ON "Users" ("Email");
CREATE INDEX "IX_Users_SceneName" ON "Users" ("SceneName");
```

### DTOs and ViewModels
```csharp
// Registration DTO
public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
}

// Login DTO
public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// User response DTO
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

// Protected content response
public class ProtectedWelcomeDto
{
    public string Message { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
    public DateTime ServerTime { get; set; }
}
```

### TypeScript Interfaces
```typescript
// React TypeScript interfaces
export interface User {
  id: string;
  email: string;
  sceneName: string;
  createdAt: string;
  lastLoginAt?: string;
}

export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  sceneName: string;
}

export interface ProtectedWelcomeResponse {
  message: string;
  user: User;
  serverTime: string;
}
```

## API Specifications

### Authentication Endpoints
| Method | Path | Description | Request | Response | Authentication |
|--------|------|-------------|---------|----------|----------------|
| POST | /api/auth/register | Create new user account | RegisterDto | UserDto | None |
| POST | /api/auth/login | Authenticate user with cookie | LoginDto | UserDto | None |
| POST | /api/auth/logout | Clear authentication cookie | None | Success message | Cookie |
| GET | /api/auth/user | Get current user info | None | UserDto | Cookie |

### Protected Content Endpoints
| Method | Path | Description | Request | Response | Authentication |
|--------|------|-------------|---------|----------|----------------|
| GET | /api/protected/welcome | Get personalized welcome message | None | ProtectedWelcomeDto | JWT Bearer |

### API Response Formats
```json
// Success Response
{
  "success": true,
  "data": { /* response data */ },
  "message": "Operation completed successfully"
}

// Error Response
{
  "success": false,
  "error": "Error message",
  "details": "Detailed error information"
}
```

## Component Specifications

### React Components

#### AuthContext Provider
```typescript
// Authentication context for React state management
export const AuthContext = createContext<{
  authState: AuthState;
  login: (credentials: LoginRequest) => Promise<void>;
  register: (userData: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
} | null>(null);
```

#### LoginPage Component
- **Path**: `/login`
- **Authorization**: Public (redirects if authenticated)
- **Render Mode**: Client-side React
- **Key Features**: 
  - Email/password form with validation
  - Error handling and display
  - Redirect to protected page on success
  - Link to registration page

#### RegisterPage Component
- **Path**: `/register`
- **Authorization**: Public (redirects if authenticated)
- **Render Mode**: Client-side React
- **Key Features**:
  - Email/password/scene name form
  - Client-side validation (React Hook Form + Zod)
  - Server-side validation error display
  - Auto-login after successful registration

#### ProtectedWelcomePage Component
- **Path**: `/welcome`
- **Authorization**: Authenticated users only
- **Render Mode**: Client-side React
- **Key Features**:
  - Display "Welcome [SceneName]" message
  - Show user information from API
  - Logout button
  - Link to public events page

#### ProtectedRoute Component
```typescript
// Higher-order component for route protection
export const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { authState } = useAuth();
  
  if (authState.isLoading) {
    return <LoadingSpinner />;
  }
  
  if (!authState.isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  
  return <>{children}</>;
};
```

### State Management
- **Primary**: React Context + useReducer for authentication state
- **Local State**: useState for form state and UI state
- **Persistence**: Authentication state managed via HttpOnly cookies (no localStorage)
- **Updates**: useEffect hooks for API calls and state synchronization

## Integration Points

### Authentication System
- **Web Service**: ASP.NET Core Identity for user management
- **Cookie Management**: HttpOnly, Secure, SameSite=Strict cookies
- **JWT Generation**: Web Service generates JWT tokens for API service calls
- **Token Validation**: API Service validates JWT tokens from Web Service

### API Communication
- **Web ↔ React**: Fetch API with `credentials: 'include'` for cookie auth
- **Web ↔ API**: HttpClient with JWT Bearer token authentication
- **CORS**: Configured for React dev server (localhost:5173)

### Database Integration
- **ORM**: Entity Framework Core 9 with PostgreSQL
- **Connection**: PostgreSQL connection string with connection pooling
- **Migrations**: EF Core migrations for User table creation

## Security Requirements

### Authentication Security
- **Password Hashing**: ASP.NET Core Identity default (PBKDF2)
- **Cookie Security**: HttpOnly, Secure (HTTPS), SameSite=Strict
- **JWT Security**: HS256 signing, 1-hour expiration, secure secret key
- **Session Security**: 30-day cookie expiration with sliding renewal

### Input Validation
- **Client-side**: React Hook Form with Zod schemas
- **Server-side**: ASP.NET Core model validation and custom validators
- **SQL Injection**: Protected by EF Core parameterized queries
- **XSS Prevention**: HttpOnly cookies prevent client-side access

### OWASP Top 10 Compliance
- **A01 Broken Access Control**: Protected routes and API endpoints
- **A02 Cryptographic Failures**: Secure password hashing and JWT signing
- **A03 Injection**: EF Core parameterized queries
- **A07 Identification/Authentication Failures**: Strong password requirements

## Performance Requirements

### Response Time Targets
- Registration/Login: <2 seconds
- Protected page loads: <1 second
- API calls with JWT: <200ms
- Authentication state changes: <100ms

### Scalability Considerations
- Connection pooling for database connections
- JWT stateless authentication for API services
- Efficient React state management (Context + hooks)
- Optimized database queries with proper indexing

## Testing Requirements

### Unit Testing
- React components with React Testing Library
- Authentication hooks testing
- API service testing (AuthService, JwtService)
- Database context testing
- **Coverage Target**: 80% for critical authentication flows

### Integration Testing
- Complete authentication flow (Register → Login → Protected content)
- Cookie management and JWT token generation
- API authentication between services
- Database integration testing

### E2E Testing (Playwright)
```typescript
// E2E test scenarios
test('Complete authentication flow', async ({ page }) => {
  // Register new user
  await page.goto('/register');
  await page.fill('[data-testid="email-input"]', 'test@example.com');
  await page.fill('[data-testid="password-input"]', 'TestPass123!');
  await page.fill('[data-testid="scene-name-input"]', 'TestUser');
  await page.click('[data-testid="register-button"]');
  
  // Should be redirected to welcome page
  await expect(page).toHaveURL('/welcome');
  await expect(page.locator('[data-testid="welcome-message"]')).toContainText('Welcome TestUser');
  
  // Logout and verify redirect
  await page.click('[data-testid="logout-button"]');
  await expect(page).toHaveURL('/');
});
```

### Security Testing
- Authentication bypass testing
- Cookie security validation
- JWT token validation testing
- Input validation security testing

## Migration Requirements

### Database Migrations
```csharp
// EF Core migration for Users table
public partial class AddUsersTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                SceneName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: false),
                SecurityStamp = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                IsEmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                LastLoginAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });
    }
}
```

### Configuration Requirements
- JWT signing key configuration
- Database connection string
- CORS policy for React development
- Cookie security settings

## Dependencies

### NuGet Packages
```xml
<!-- API Service Dependencies -->
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
```

### NPM Packages
```json
{
  "dependencies": {
    "react-hook-form": "^7.48.0",
    "@hookform/resolvers": "^3.3.0",
    "zod": "^3.22.0",
    "react-router-dom": "^6.20.0"
  }
}
```

### External Services
- PostgreSQL database server
- No external authentication providers (OAuth deferred)

## Implementation Phases

### Phase 1: Database and API Foundation (Day 1 - Morning)
1. Create Users table with EF Core migration
2. Implement User entity and AuthDbContext
3. Create AuthService with ASP.NET Core Identity
4. Implement JwtService for token generation
5. Create AuthController with register/login/logout endpoints

### Phase 2: Protected API and JWT Flow (Day 1 - Afternoon)
1. Implement ProtectedController with JWT authentication
2. Create service-to-service JWT authentication flow
3. Add protected welcome endpoint returning user data
4. Test API endpoints with Postman/Swagger

### Phase 3: React Authentication Components (Day 2 - Morning)
1. Create AuthContext and useAuth hook
2. Implement LoginForm and RegisterForm components
3. Create LoginPage and RegisterPage
4. Implement ProtectedRoute component

### Phase 4: Integration and Testing (Day 2 - Afternoon)
1. Integrate React frontend with API endpoints
2. Test complete authentication flow
3. Implement ProtectedWelcomePage
4. Add E2E tests with Playwright
5. Security validation and OWASP compliance check

## Acceptance Criteria

### Technical Criteria
- [ ] User can register with email/password/scene name
- [ ] User can login and receive HttpOnly authentication cookie
- [ ] Protected welcome page accessible only when authenticated
- [ ] Public events page accessible without authentication
- [ ] Web Service authenticates API calls with JWT Bearer tokens
- [ ] User can logout and authentication state is cleared
- [ ] Authentication state persists across browser refreshes
- [ ] All security requirements met (HttpOnly cookies, JWT validation)
- [ ] Complete implementation in 1-2 days (throwaway quality)

### Architecture Validation Criteria
- [ ] React → Web Service cookie authentication works
- [ ] Web Service → API Service JWT authentication works
- [ ] Authentication state management works correctly in React
- [ ] Cookie security prevents XSS (HttpOnly, Secure, SameSite)
- [ ] API endpoints properly validate JWT tokens
- [ ] Error handling works for authentication failures
- [ ] No direct database access from React frontend

## Quality Checklist
- [ ] Aligns with business requirements for authentication POC
- [ ] Follows Web+API microservices architecture pattern
- [ ] Uses proven technologies (ASP.NET Identity, React Context)
- [ ] Implements security best practices (HttpOnly cookies, JWT)
- [ ] Performance targets realistic for POC (<2s auth, <200ms API)
- [ ] Testing approach covers critical authentication flows
- [ ] Integration points clearly defined (React ↔ Web ↔ API)
- [ ] Migration strategy documented (EF Core migrations)
- [ ] **Respects architecture boundaries**: React never accesses database directly

## Cleanup Requirements
After POC validation, the following cleanup is required:
1. Document lessons learned in appropriate lessons-learned files
2. Delete throwaway components if not suitable for production
3. Archive test user accounts created during validation
4. Update authentication patterns documentation
5. Plan production-quality implementation based on POC results

Remember: This is throwaway code designed to validate the authentication pattern works. Focus on proving the concept rather than production-quality implementation.