# Technical Context - WitchCityRope React Migration

**Last Updated**: August 14, 2025  
**Purpose**: Provide complete technical understanding of current and target systems  
**Audience**: New developers who need comprehensive technical background  

---

## 🏗️ Current Architecture Overview

### System Components & Ports

**Current WitchCityRope Architecture** (Blazor Server):
```
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│   Blazor Server     │    │    .NET API         │    │   PostgreSQL DB     │
│   (Port 5651)      │────│   (Port 5653)      │────│   (Port 5433)      │
│                     │    │                     │    │                     │
│ • UI Components     │    │ • REST Endpoints    │    │ • User Data         │
│ • SignalR Client    │    │ • Business Logic    │    │ • Event Data        │
│ • Authentication    │    │ • JWT Services      │    │ • Registration Data │
│ • Form Handling     │    │ • Entity Framework  │    │ • Payment Records   │
└─────────────────────┘    └─────────────────────┘    └─────────────────────┘
```

**Network Architecture**:
- **Web Service**: `http://localhost:5651` (Blazor Server UI)
- **API Service**: `http://localhost:5653` (Business logic + data access)
- **Database**: `localhost:5433` (PostgreSQL with full schema)
- **Pattern**: Blazor → SignalR → API → Database
- **Docker**: Development environment with `docker-compose`

### Current Technology Stack

**Backend (.NET 10)**:
```csharp
// Core Framework
- .NET 10 (LTS release)
- ASP.NET Core (Web + API)
- Entity Framework Core 8.0
- PostgreSQL with Npgsql driver

// Authentication & Security  
- ASP.NET Core Identity
- JWT Bearer authentication
- Role-based authorization
- HTTPS/TLS enforcement

// UI Framework (Current)
- Blazor Server with SignalR
- Syncfusion component library
- Custom CSS with design system
- Server-side rendering with client interaction

// Development Tools
- Docker Compose for local development
- Hot reload for Blazor components
- Entity Framework migrations
- Integrated testing with xUnit
```

**Database Schema** (PostgreSQL):
```sql
-- Core Tables (23 total)
Tables:
├── AspNetUsers (Identity system)
├── AspNetRoles (Role management)
├── Events (Community events)
├── Registrations (Event registrations) 
├── Payments (Payment tracking)
├── VettingApplications (Member vetting)
├── IncidentReports (Safety reports)
├── Settings (System configuration)
└── ... (15 additional supporting tables)

-- Key Relationships
Events → Registrations (1:N)
Users → Registrations (1:N)
Users → VettingApplications (1:N)
Events → Payments (1:N)
Users → IncidentReports (1:N)

-- Storage Patterns
- User PII encrypted at rest
- File uploads stored in filesystem
- Session data in database
- Audit logs for administrative actions
```

### Business Logic Inventory

**Core Domain Models** (~15,000 lines of C#):
```csharp
// User Management
public class ApplicationUser : IdentityUser
{
    public string? SceneName { get; set; }
    public string? EncryptedLegalName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public UserRole Role { get; set; }
    // ... additional properties for vetting, preferences, etc.
}

// Event Management  
public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartDateTime { get; set; }
    public decimal? TicketPrice { get; set; }
    public EventStatus Status { get; set; }
    // ... scheduling, capacity, requirements
}

// Registration System
public class Registration  
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string UserId { get; set; }
    public RegistrationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    // ... payment tracking, special requirements
}
```

**Business Services** (~25,000 lines of C#):
```csharp
// Key Service Classes
- UserManagementService (registration, profile management)
- EventService (CRUD, scheduling, capacity management)  
- RegistrationService (event signup, waitlist management)
- PaymentService (Stripe/PayPal integration)
- VettingService (application processing, approval workflows)
- IncidentService (safety report processing)
- NotificationService (email, in-app notifications)
- AuditService (administrative action logging)
```

**Complex Workflows**:
1. **User Vetting Process**: Multi-step application with file uploads, admin review, approval workflow
2. **Event Registration**: Capacity management, waitlists, payment processing, special requirements
3. **Payment Processing**: Stripe integration, partial payments, refunds, financial reporting
4. **Incident Management**: Safety reports, investigation workflow, resolution tracking
5. **Admin Operations**: User management, event oversight, system configuration

---

## 🎯 Target Architecture Overview

### New React System Design

**Target WitchCityRope-React Architecture**:
```
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│   React SPA         │    │   .NET API          │    │   PostgreSQL DB     │
│   (Port 3000)      │────│   (Port 5653)      │────│   (Port 5433)      │
│                     │    │   (Ported + Enhanced)   │   (Same Schema)     │
│ • React Components  │    │                     │    │                     │
│ • Client State      │    │ • REST Endpoints    │    │ • Same Data Model   │
│ • JWT Tokens        │    │ • Business Logic    │    │ • Existing Records  │
│ • API Integration   │    │ • Enhanced CORS     │    │ • No Data Migration │
└─────────────────────┘    └─────────────────────┘    └─────────────────────┘
```

**New Technology Stack**:
```typescript
// Frontend (React 18 + TypeScript)
- React 18 with concurrent features
- TypeScript strict mode
- Vite for build tooling
- React Router v7 for client-side routing

// State Management
- Zustand for client state (auth, UI)
- TanStack Query for server state (API data)
- React Hook Form for form state
- Zod for validation schemas

// UI Framework
- Chakra UI for component library
- Tailwind CSS for utility styling
- Lucide React for icons
- Framer Motion for animations

// HTTP & Integration
- Axios for HTTP client
- JWT for authentication tokens  
- Server-Sent Events for real-time features
- Progressive Web App capabilities
```

### Repository Structure (Monorepo)

**WitchCityRope-React Structure**:
```
WitchCityRope-React/
├── apps/
│   ├── web/                    # React frontend application
│   │   ├── src/
│   │   │   ├── components/     # React components  
│   │   │   ├── pages/          # Page components
│   │   │   ├── hooks/          # Custom React hooks
│   │   │   ├── services/       # API integration services
│   │   │   ├── store/          # State management (Zustand)
│   │   │   ├── types/          # TypeScript type definitions
│   │   │   └── utils/          # Utility functions
│   │   ├── public/             # Static assets
│   │   └── package.json        # React app dependencies
│   └── api/                    # .NET API application  
│       ├── Controllers/        # API controllers (ported)
│       ├── Services/          # Business services (ported)
│       ├── Models/            # Data models (ported)
│       └── Program.cs         # API configuration
├── packages/
│   ├── domain/                # C# domain models
│   │   ├── Entities/         # Core business entities
│   │   ├── ValueObjects/     # Domain value objects  
│   │   ├── Enums/           # Shared enumerations
│   │   └── Interfaces/      # Domain interfaces
│   ├── contracts/            # API contracts (DTOs)
│   │   ├── DTOs/            # Data transfer objects
│   │   ├── Requests/        # API request models
│   │   ├── Responses/       # API response models
│   │   └── Validation/      # FluentValidation rules
│   ├── shared-types/        # TypeScript types (generated from C#)
│   │   ├── models/         # TS interfaces from C# DTOs
│   │   ├── enums/          # TS enums from C# enums  
│   │   └── api/            # API client type definitions
│   └── ui/                  # Shared React component library
│       ├── components/      # Reusable UI components
│       ├── hooks/          # Reusable React hooks
│       └── themes/         # Design system and themes
├── tests/
│   ├── unit/               # Unit tests (Jest/Vitest)
│   ├── integration/        # API integration tests
│   ├── e2e/               # End-to-end tests (Playwright)
│   └── performance/       # Performance tests
├── docs/                  # Complete documentation system (migrated)
├── .claude/              # AI workflow orchestration (migrated)
├── infrastructure/       # Docker, Kubernetes, Terraform configs
├── scripts/              # Build and deployment scripts
└── package.json          # Root monorepo configuration
```

---

## 📊 API Layer Migration Analysis

### API Portability Assessment

**Comprehensive Analysis Results**:
```
Total Controllers Analyzed: 12
✅ Directly Portable: 11 controllers (91.7%)
⚠️ Minor Modifications: 1 controller (8.3%)
❌ Blazor Dependencies: 3 specific dependencies only

Portability Breakdown:
├── AuthController: ⚠️ Remove service token endpoint
├── EventsController: ✅ Direct port
├── UsersController: ✅ Direct port  
├── AdminController: ✅ Direct port
├── PaymentController: ✅ Direct port
├── RegistrationController: ✅ Direct port
├── VettingController: ✅ Direct port
├── IncidentController: ✅ Direct port
├── NotificationController: ✅ Direct port
├── ReportingController: ✅ Direct port
├── SettingsController: ✅ Direct port
└── HealthController: ✅ Direct port
```

**Business Logic Portability** (~99,000 lines):
```
Service Layer Analysis:
✅ UserManagementService: 95% portable
  ├── User registration/login: Direct port
  ├── Profile management: Direct port
  ├── Role assignment: Direct port
  └── Password reset: Minor API changes

✅ EventService: 98% portable  
  ├── Event CRUD operations: Direct port
  ├── Schedule management: Direct port
  ├── Capacity tracking: Direct port
  └── Event publishing: Direct port

✅ PaymentService: 90% portable
  ├── Stripe integration: Direct port
  ├── Payment processing: Direct port
  ├── Refund handling: Direct port
  └── Financial reporting: Minor changes for UI

✅ VettingService: 95% portable
  ├── Application processing: Direct port
  ├── File upload handling: Direct port
  ├── Approval workflows: Direct port
  └── Status notifications: Direct port

Overall Business Logic: 95%+ directly portable
```

### Database Schema Compatibility

**No Database Changes Required**:
```sql
-- Current Schema Fully Compatible
PostgreSQL Schema (23 tables):
├── All tables compatible with both Blazor and React
├── Identity system works with JWT authentication  
├── Existing data preserved completely
├── No schema migrations required
└── Same Entity Framework models

-- Connection String Unchanged
Host=localhost;Port=5433;Database=witchcityrope;Username=witchcityrope;Password=***
```

### Identified Dependencies to Remove

**Blazor-Specific Dependencies** (3 total):
```csharp
// 1. Syncfusion License (UI library)
// In Program.cs - REMOVE
SyncfusionLicenseProvider.RegisterLicense("license-key-here");

// 2. SignalR Hub Configuration (if not needed for React)  
// In Program.cs - EVALUATE/REPLACE
builder.Services.AddSignalR();
app.MapHub<NotificationHub>("/notificationHub");

// 3. Service Token Endpoint (Blazor-specific auth)
// In AuthController.cs - REMOVE
[HttpPost("service-token")]
public async Task<IActionResult> GetServiceToken() { ... }
```

**Required API Enhancements for React**:
```csharp
// Enhanced CORS Configuration
services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", builder =>
    {
        builder
            .WithOrigins("http://localhost:3000", "https://app.witchcityrope.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Additional Response Headers for React
services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// OpenAPI/Swagger for React Development
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WitchCityRope API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
});
```

---

## 🔐 Authentication & Security Architecture

### Current Authentication System

**Blazor Server Authentication**:
```csharp
// Current Implementation
- ASP.NET Core Identity for user management
- JWT tokens for API authentication  
- Cookie-based sessions for Blazor Server
- Role-based authorization with custom roles
- HTTPS enforcement throughout
- CSRF protection via anti-forgery tokens

// Role System (5 roles)
public enum UserRole
{
    Guest = 0,        // Unverified attendees
    Member = 1,       // Basic community members  
    VettedMember = 2, // Verified community members
    Teacher = 3,      // Workshop instructors
    Admin = 4         // System administrators
}
```

**Current Security Patterns**:
```csharp
// Authorization Patterns
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase { ... }

[Authorize(Policy = "VettedMemberOnly")]
public async Task<IActionResult> VettedContent() { ... }

// Custom Authorization Policies
services.AddAuthorization(options =>
{
    options.AddPolicy("VettedMemberOnly", policy =>
        policy.RequireRole("VettedMember", "Teacher", "Admin"));
        
    options.AddPolicy("TeacherOrAdmin", policy =>
        policy.RequireRole("Teacher", "Admin"));
});
```

### Target Authentication System

**React Authentication Pattern**:
```typescript
// JWT-Based Authentication for React
interface AuthState {
  user: User | null;
  token: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}

// Authentication Hook
export const useAuth = () => {
  const [state, setState] = useAuthStore();

  const login = async (credentials: LoginCredentials) => {
    const response = await api.post('/auth/login', credentials);
    const { token, refreshToken, user } = response.data;
    
    // Secure token storage
    localStorage.setItem('auth-token', token);
    localStorage.setItem('refresh-token', refreshToken);
    
    setState({ user, token, refreshToken, isAuthenticated: true });
  };
  
  return { ...state, login, logout };
};

// Protected Route Component
export const ProtectedRoute = ({ requiredRole, children }) => {
  const { user, isAuthenticated } = useAuth();
  
  if (!isAuthenticated) return <Navigate to="/login" />;
  if (requiredRole && !user.roles.includes(requiredRole)) {
    return <Navigate to="/unauthorized" />;
  }
  
  return children;
};
```

**API Integration Security**:
```typescript
// Axios Interceptors for JWT
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('auth-token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  }
);

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Attempt token refresh
      const refreshToken = localStorage.getItem('refresh-token');
      if (refreshToken) {
        try {
          const response = await api.post('/auth/refresh', { refreshToken });
          const { token } = response.data;
          localStorage.setItem('auth-token', token);
          return api.request(error.config);
        } catch {
          // Refresh failed, redirect to login
          localStorage.clear();
          window.location.href = '/login';
        }
      }
    }
    return Promise.reject(error);
  }
);
```

---

## 🏃‍♂️ Performance Characteristics

### Current System Performance

**Blazor Server Metrics** (baseline measurements):
```
Page Load Performance:
├── Initial Load: 2.5-4.0 seconds
├── Navigation: 0.5-1.5 seconds (SignalR roundtrip)
├── Form Submission: 1.0-2.0 seconds
└── Bundle Size: ~150KB initial + SignalR overhead

Memory Usage:
├── Server Memory: 150-300MB per active session
├── Client Memory: 50-100MB (browser)
├── Database Connections: Pool of 20-50 connections
└── SignalR Connections: 1 per active user

Scalability Limits:
├── Concurrent Users: ~200-500 (server CPU bound)
├── Memory Per User: ~2-5MB server-side
├── Network: Persistent WebSocket connections
└── Database: Shared connection pool
```

### Target System Performance

**React Performance Targets**:
```
Performance Improvements Expected:
├── Page Load: <2.0 seconds (50%+ improvement)
├── Navigation: <0.2 seconds (client-side routing)
├── Form Interaction: Real-time validation
├── Bundle Size: <300KB initial (gzipped)

Resource Efficiency:
├── Server Memory: Stateless API (90% reduction)
├── Client Memory: 100-200MB (rich client app)
├── Database: Connection pooling optimized
├── Network: HTTP requests instead of persistent connections

Scalability Improvements:
├── Concurrent Users: 1000+ (API stateless)
├── CDN Cacheable: Static assets served from CDN
├── Horizontal Scaling: API instances scale independently
├── Database: Read replicas for query performance
```

**Performance Monitoring Strategy**:
```typescript
// Performance Monitoring Hooks
export const usePagePerformance = (pageName: string) => {
  useEffect(() => {
    const observer = new PerformanceObserver((list) => {
      const entries = list.getEntries();
      analytics.track('page_performance', {
        page: pageName,
        loadTime: entries[0].duration,
        firstContentfulPaint: entries[0].firstContentfulPaint
      });
    });
    
    observer.observe({ entryTypes: ['navigation', 'paint'] });
    return () => observer.disconnect();
  }, [pageName]);
};

// Bundle Size Monitoring
export const bundleAnalyzer = {
  initialBundle: '< 300KB target',
  asyncChunks: 'Code splitting by route',
  vendorChunks: 'Separate vendor bundle',
  monitoring: 'Bundle analyzer in CI/CD'
};
```

---

## 🔌 Integration Points & Dependencies

### External Service Integrations

**Payment Processing**:
```csharp
// Current: Stripe Integration (Preserve)
services.Configure<StripeSettings>(configuration.GetSection("Stripe"));
services.AddTransient<IPaymentService, StripePaymentService>();

// PayPal Integration (Secondary)
services.Configure<PayPalSettings>(configuration.GetSection("PayPal"));
services.AddTransient<IPayPalService, PayPalService>();
```

**Email Service**:
```csharp
// Current: SendGrid Integration (Preserve)
services.Configure<SendGridSettings>(configuration.GetSection("SendGrid"));
services.AddTransient<IEmailService, SendGridEmailService>();

// Email Templates (Migrate to React-friendly format)
- Welcome emails
- Password reset emails  
- Event notifications
- Vetting status updates
- Administrative notifications
```

**File Storage**:
```csharp
// Current: Local filesystem storage
// Consider: AWS S3, Azure Blob, or local NAS for production

public interface IFileStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string folder);
    Task<byte[]> GetFileAsync(string filePath);
    Task DeleteFileAsync(string filePath);
}
```

### Development Environment Dependencies

**Required Infrastructure**:
```yaml
# Docker Compose Services
services:
  postgres:
    image: postgres:15
    ports: ["5433:5432"]
    
  redis: # Optional for caching
    image: redis:7-alpine
    ports: ["6379:6379"]
    
  maildev: # Email testing
    image: maildev/maildev
    ports: ["1080:1080", "1025:1025"]
```

**Development Tools**:
```json
// Package.json (Root)
{
  "devDependencies": {
    "turbo": "^1.10.0",
    "typescript": "^5.0.0",
    "@types/node": "^20.0.0",
    "eslint": "^8.45.0",
    "prettier": "^3.0.0",
    "husky": "^8.0.0",
    "lint-staged": "^13.0.0"
  }
}

// Development Scripts
{
  "scripts": {
    "dev": "turbo run dev",
    "build": "turbo run build", 
    "test": "turbo run test",
    "lint": "turbo run lint",
    "type-check": "turbo run type-check"
  }
}
```

---

## 🧪 Testing Architecture

### Current Testing Infrastructure

**Existing Test Suite** (Preserve patterns):
```csharp
// Unit Tests (.NET)
- WitchCityRope.Core.Tests
- WitchCityRope.Api.Tests  
- WitchCityRope.Infrastructure.Tests

// Integration Tests
- WitchCityRope.IntegrationTests
- Database integration with test containers
- API endpoint testing
- Authentication flow testing

// E2E Tests  
- Playwright tests (180+ tests currently)
- User workflow automation
- Cross-browser testing
- Mobile responsiveness testing
```

### Target Testing Strategy

**React Testing Architecture**:
```typescript
// Unit Testing (Vitest + Testing Library)
├── Component testing: @testing-library/react
├── Hook testing: @testing-library/react-hooks
├── Utility testing: Pure function testing
└── Store testing: Zustand store testing

// Integration Testing
├── API integration: Mock Service Worker (MSW)
├── Form workflows: Complete form submission flows
├── Authentication: Login/logout/refresh token flows
└── Component integration: Multi-component workflows

// E2E Testing (Playwright)
├── User registration and login flows
├── Event creation and registration workflows  
├── Payment processing end-to-end
├── Admin panel functionality
├── Mobile responsive testing

// Performance Testing
├── Bundle size monitoring
├── Page load performance
├── Memory leak detection
└── Network request optimization
```

**Testing Configuration**:
```typescript
// Vitest Configuration
export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    coverage: {
      reporter: ['text', 'json', 'html'],
      threshold: {
        global: {
          branches: 90,
          functions: 90,
          lines: 90,
          statements: 90
        }
      }
    }
  }
});

// Playwright Configuration  
export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: true,
  retries: 1,
  workers: 4,
  reporter: [['html'], ['json', { outputFile: 'test-results.json' }]],
  use: {
    baseURL: 'http://localhost:3000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure'
  },
  projects: [
    { name: 'chromium', use: devices['Desktop Chrome'] },
    { name: 'firefox', use: devices['Desktop Firefox'] },
    { name: 'webkit', use: devices['Desktop Safari'] },
    { name: 'mobile', use: devices['iPhone 13'] }
  ]
});
```

---

## 🚀 Deployment & DevOps

### Current Deployment

**Production Infrastructure** (preserve where possible):
```bash
# Current Deployment Pattern
├── Docker containers for web + api + database
├── Nginx reverse proxy
├── SSL/TLS certificates (Let's Encrypt)
├── PostgreSQL database with backups
└── Monitoring and logging infrastructure
```

### Target Deployment Strategy

**React Deployment Architecture**:
```dockerfile
# Multi-stage Docker build
FROM node:18-alpine AS web-build
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production
COPY . .
RUN npm run build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api-runtime
WORKDIR /app
COPY --from=api-build /app/publish .
EXPOSE 5653
ENTRYPOINT ["dotnet", "WitchCityRope.Api.dll"]

FROM nginx:alpine AS web-runtime  
COPY --from=web-build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
```

**Production Configuration**:
```yaml
# Docker Compose Production
version: '3.8'
services:
  web:
    build: ./apps/web
    ports: ["80:80", "443:443"]
    volumes:
      - ./ssl:/etc/ssl/certs
    depends_on: [api]
    
  api:
    build: ./apps/api
    ports: ["5653:80"]
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
    depends_on: [postgres]
    
  postgres:
    image: postgres:15
    volumes:
      - postgres_data:/var/lib/postgresql/data
    environment:
      - POSTGRES_DB=witchcityrope_prod
      - POSTGRES_USER=${DB_USER}
      - POSTGRES_PASSWORD=${DB_PASSWORD}
```

---

## 📋 Migration Checklist Summary

### Critical Technical Prerequisites

**Week 1 Readiness Checklist**:
- [ ] Understanding of current Blazor Server architecture
- [ ] Familiarity with .NET API structure and business logic
- [ ] PostgreSQL schema knowledge and Entity Framework patterns
- [ ] Authentication system understanding (JWT + Identity)
- [ ] React/TypeScript development environment setup
- [ ] Docker development environment operational

**API Migration Readiness**:
- [ ] Current API endpoints documented and tested
- [ ] Business logic dependencies mapped
- [ ] Database connection strings and configuration
- [ ] Authentication patterns understood
- [ ] External service integrations identified

**React Development Readiness**:
- [ ] Component library decisions implemented
- [ ] State management patterns established
- [ ] Form handling and validation ready
- [ ] API integration layer designed
- [ ] Testing strategy and tools configured

**Quality Assurance Readiness**:
- [ ] Testing infrastructure migration plan
- [ ] Performance monitoring strategy
- [ ] Security review checklist
- [ ] Accessibility compliance plan
- [ ] Browser compatibility testing plan

This technical context provides comprehensive understanding of both current and target systems, enabling informed development decisions and successful migration execution.