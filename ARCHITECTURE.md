# 🏗️ WitchCityRope Architecture Documentation

> **⚠️ CRITICAL FOR ALL DEVELOPERS: READ THIS FIRST!**
> 
> This document describes the Web+API architecture pattern used by WitchCityRope. WitchCityRope is **migrating from Blazor Server to React** while maintaining the proven API backend architecture. Understanding this is essential to avoid architectural mistakes that could break the entire application.

---

## 📋 Table of Contents

1. [Architecture Overview](#-architecture-overview)
2. [Service Communication](#-service-communication)  
3. [Port Configuration](#-port-configuration)
4. [Authentication Flow](#-authentication-flow)
5. [Database Access Patterns](#-database-access-patterns)
6. [Key Files and Their Purposes](#-key-files-and-their-purposes)
7. [Development Environment](#-development-environment)
8. [Common Mistakes to Avoid](#-common-mistakes-to-avoid)
9. [Troubleshooting](#-troubleshooting)

---

## 🏛️ Architecture Overview

WitchCityRope uses a **Web+API microservices architecture** with the following services:

```
┌─────────────────┐    HTTP Calls    ┌─────────────────┐
│                 │ ───────────────► │                 │
│ React Frontend  │                  │  API Service    │
│ (Mantine UI)    │ ◄─────────────── │  (Minimal API)  │
│                 │   JSON Responses │                 │
└─────────────────┘                  └─────────────────┘
        │                                      │
        │ No Direct DB Access                  │ Full DB Access
        │ (API-only communication)             │ (All Operations)
        ▼                                      ▼
                ┌─────────────────────────────────────┐
                │        PostgreSQL Database          │
                │     (Single source of truth)       │
                └─────────────────────────────────────┘
```

### Services Explained

1. **React Frontend (apps/web)**
   - **Technology**: React + TypeScript + Vite + Mantine UI Framework
   - **Purpose**: Modern single-page application, responsive user interface
   - **Database Access**: None (calls API for all data)
   - **Port**: 5173 (HTTP) - Development and Docker
   - **UI Framework**: Mantine v7 (ADR-004) - TypeScript-first, WCAG compliant

2. **API Service (apps/api)**
   - **Technology**: ASP.NET Core Minimal API (.NET 9)
   - **Purpose**: Business logic, data operations, authentication endpoints
   - **Database Access**: Full access to all business entities and Identity
   - **Port**: 5655 (HTTP) - Development and Docker

3. **Database Service**
   - **Technology**: PostgreSQL 16
   - **Purpose**: Data persistence
   - **Port**: 5433 (mapped from internal 5432)

---

## 🔄 Service Communication

### Web → API Communication

The Web service communicates with the API service via HTTP calls:

**Key Files:**
- `/src/WitchCityRope.Web/Services/ApiClient.cs` - HTTP client wrapper
- `/src/WitchCityRope.Web/Services/AuthService.cs` - Authentication service proxy

**Communication Pattern:**
```csharp
// Example: Web service calling API
var events = await _apiClient.GetAsync<List<EventDto>>("api/v1/events");
```

**Configuration Location:**
```csharp
// In Program.cs (Web Service)
builder.Services.AddHttpClient<ApiClient>(client =>
{
    var apiUrl = builder.Configuration["ApiUrl"] ?? "https://localhost:8181";
    client.BaseAddress = new Uri(apiUrl);
});
```

### API Endpoints

The API service exposes RESTful endpoints under `/api/v1/`:

**Key Endpoints:**
- `GET /api/v1/events` - List events
- `POST /api/v1/auth/login` - User authentication  
- `GET /api/v1/users/profile` - User profile
- `POST /api/v1/events/{id}/register` - Event registration

**Implementation Location:**
- `/src/WitchCityRope.Api/Program.cs` (lines 183-628)

---

## 🔌 Port Configuration

### Development Ports

| Service | Development | Docker External | Docker Internal | URL |
|---------|-------------|-----------------|-----------------|-----|
| React | 5173 | 5173 | 5173 | http://localhost:5173 |
| API | 5655 | 5655 | 8080 | http://localhost:5655 |
| Database | - | 5433 | 5432 | localhost:5433 |
| pgAdmin | - | 5050 | 80 | http://localhost:5050 |

### Configuration Files

**Docker Compose:**
```yaml
# docker-compose.dev.yml - Web service
web:
  ports:
    - "5173:5173"  # Web service (host:container)

# docker-compose.dev.yml - API service
api:
  ports:
    - "5655:8080"  # API service (host:container)
```

**Environment Variables:**
```yaml
# docker-compose.dev.yml - API communication
- ApiBaseUrl=http://api:8080/              # Internal container communication
- ApiBaseUrlExternal=http://localhost:5655  # External access from host
```

---

## 🔐 Authentication Flow

### Architecture Pattern: BFF (Backend-for-Frontend) with httpOnly Cookies

The application uses a **secure BFF authentication pattern** with httpOnly cookie management:

1. **React Frontend Authentication**
   - Uses httpOnly cookies exclusively (no localStorage token storage)
   - HttpOnly cookies prevent XSS attacks and provide automatic CSRF protection
   - React handles login/logout UI interactions with seamless cookie management
   - **Frontend**: React + TypeScript + Mantine UI Framework

2. **BFF Authentication Pattern**
   - React app calls authentication endpoints that set httpOnly cookies
   - All React→API calls use cookie-based authentication automatically
   - Silent token refresh prevents authentication timeouts
   - **📖 See detailed implementation**: `/session-work/2025-09-12/bff-authentication-implementation-summary.md`

3. **API Service Authentication**  
   - Validates JWT tokens from httpOnly cookies for all protected endpoints
   - Automatic token refresh mechanism prevents user interruption
   - Dual authentication support (Bearer tokens + cookies) for backwards compatibility

### Authentication Components

**React Frontend (BFF authentication):**
```typescript
// BFF pattern - no token handling in JavaScript
export const authService = {
  async login(email: string, password: string) {
    const response = await fetch('/api/auth/login', {
      method: 'POST',
      credentials: 'include', // Essential for httpOnly cookies
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });
    // Cookie set automatically, no token in response body
    return response.ok;
  },
  
  async getCurrentUser() {
    const response = await fetch('/api/auth/user', {
      credentials: 'include'
    });
    return response.ok ? response.json() : null;
  },
  
  async refresh() {
    const response = await fetch('/api/auth/refresh', {
      method: 'POST',
      credentials: 'include'
    });
    return response.ok; // Silent refresh
  }
};
```

**API Service (JWT-based):**
```csharp
// Program.cs (lines 46-50)
- Jwt__Secret=${JWT_SECRET:-YourSuperSecretKeyForDevelopmentPurposesOnly!123}
- Jwt__Issuer=WitchCityRope
- Jwt__Audience=WitchCityRopeUsers  
- Jwt__ExpiresInMinutes=60
```

### BFF Authentication Flow

```
1. User logs in via React UI → Calls /api/auth/login → API sets httpOnly cookie
2. React automatically sends cookies with all requests → No token management needed
3. API validates JWT from cookie → Returns data to React
4. Token nears expiry → Silent refresh via /api/auth/refresh → New cookie set
5. User logs out → /api/auth/logout → Cookie deleted server-side
```

**Evidence in Code:**
- `/src/WitchCityRope.Web/Services/AuthService.cs` (lines 77-95) - Login flow
- `/src/WitchCityRope.Api/Program.cs` (lines 188-205) - API login endpoint

---

## 🗄️ Database Access Patterns

### Shared Database Architecture

Both services access the **same PostgreSQL database** but with different responsibilities:

**Web Service Database Access:**
- **Scope**: Identity operations only (login, user management)
- **Context**: `WitchCityRopeIdentityDbContext`
- **Tables**: AspNetUsers, AspNetRoles, etc.

**API Service Database Access:**
- **Scope**: All business logic (events, registrations, payments)  
- **Context**: `WitchCityRopeIdentityDbContext` (same context, full access)
- **Tables**: Events, Registrations, Tickets, etc.

### Database Configuration

**Connection String (Shared):**
```yaml
# docker-compose.yml (line 44)
ConnectionStrings__DefaultConnection=Host=${POSTGRES_HOST:-postgres};Port=5432;Database=${POSTGRES_DB:-witchcityrope_db};Username=${POSTGRES_USER:-postgres};Password=${POSTGRES_PASSWORD:-WitchCity2024!}
```

**Context Registration:**
```csharp
// Web Service Program.cs (lines 44-51)
builder.Services.AddDbContext<WitchCityRopeIdentityDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});
```

### Database Auto-Initialization System

**NEW (August 2025)**: WitchCityRope now features a comprehensive database auto-initialization system that eliminates manual setup:

#### Automatic Operations
- **Migrations**: Applied automatically on API startup using BackgroundService pattern
- **Seed Data**: Comprehensive test users and sample events created automatically
- **Performance**: Complete initialization in under 5 minutes (95%+ improvement from manual setup)
- **Environment Safety**: Production environments skip seed data automatically

#### Key Components
- **DatabaseInitializationService**: `/apps/api/Services/DatabaseInitializationService.cs`
- **SeedDataService**: `/apps/api/Services/SeedDataService.cs`
- **Health Check**: `/api/health/database` endpoint for monitoring
- **Test Accounts**: 7 comprehensive test accounts (admin@witchcityrope.com, etc.)
- **Sample Events**: 12 realistic events for development testing

#### Migration Files
- **Location**: `/apps/api/Migrations/`
- **Applied by**: DatabaseInitializationService automatically
- **Retry Logic**: Exponential backoff for Docker container coordination

---

## 📁 Key Files and Their Purposes

### React Frontend Key Files

| File | Purpose | Critical Details |
|------|---------|------------------|
| `/apps/web/src/main.tsx` | React application entry point | React root, Mantine provider setup |
| `/apps/web/src/services/authService.ts` | Authentication service | HTTP calls to API auth endpoints |
| `/apps/web/src/contexts/AuthContext.tsx` | React authentication context | Global auth state management |
| `/apps/web/src/App.tsx` | Root React component | Router setup, layout, Mantine theme |
| `/apps/web/vite.config.ts` | Vite configuration | Dev server, API proxy, build settings |

### API Service Key Files

| File | Purpose | Critical Details |
|------|---------|------------------|
| `/apps/api/Program.cs` | API service configuration | Minimal API setup, CORS, Identity, endpoints |
| `/apps/api/Controllers/` | API controllers | Business logic endpoints |
| `/apps/api/Services/` | Business services | Authentication, business logic services |
| `/apps/api/Models/` | Data models and DTOs | Request/response models for API |

### Shared Infrastructure Files

| File | Purpose | Critical Details |
|------|---------|------------------|
| `/apps/api/Data/ApplicationDbContext.cs` | Database context | EF Core context for all data operations |
| `/apps/api/Migrations/` | Database migrations | EF Core migrations |
| `/apps/api/Models/` | Domain entities | Business objects and DTOs |

### Configuration Files

| File | Purpose | Critical Details |
|------|---------|------------------|
| `docker-compose.yml` | Production service configuration | Service definitions, ports, environment |
| `docker-compose.dev.yml` | Development overrides | Hot reload, volume mounts |
| `dev.sh` | Development helper script | Start/stop services, view logs |

---

## 🛠️ Development Environment

### Starting the Application

**🚀 MILESTONE ACHIEVED (2025-09-14): React App Fully Functional**

The React migration from Blazor is now complete and operational:
- React app loads successfully at http://localhost:5173
- Login functionality working end-to-end
- Events page loading real data from API
- Zero TypeScript compilation errors
- API port standardized on 5655 (webhook requirement)

**🚀 Zero-Configuration Setup**

With the new database auto-initialization system:

```bash
# Single command setup - database initializes automatically
./dev.sh
# Database migrations and seed data populate automatically in under 5 minutes
```

**Local Development**:
```bash
# Terminal 1: Start API (database auto-initializes)
cd apps/api
dotnet run

# Terminal 2: Start React
cd apps/web  
npm run dev
```

**Docker Development**:
```bash
# Use development build for hot reload
./dev.sh
# OR
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

**📊 Test Accounts Available Immediately**:
- admin@witchcityrope.com / Test123!
- teacher@witchcityrope.com / Test123!
- vetted@witchcityrope.com / Test123!
- member@witchcityrope.com / Test123!
- guest@witchcityrope.com / Test123!

### Development Workflow

1. **Start Services**: Local (above) or `./dev.sh` → Option 1
2. **Access Application**: http://localhost:5173 (React - local and Docker)
3. **Access API**: http://localhost:5655 (API - local and Docker)
4. **View Logs**: `./dev.sh` → Options 4-6 (Docker only)
5. **Hot Reload**: Automatic when files change (Vite HMR)

### Development Tools

**Built-in Tools:**
- `./dev.sh` - Development menu system
- `docker-compose logs -f` - View logs
- pgAdmin at http://localhost:5050 - Database management

**Hot Reload Configuration:**
```yaml
# docker-compose.dev.yml (lines 20-21)
- DOTNET_USE_POLLING_FILE_WATCHER=true
- DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER=true
```

---

## ❌ Common Mistakes to Avoid

### 1. Wrong Docker Command
```bash
# ❌ WRONG - Uses production build, will fail
docker-compose up

# ✅ CORRECT - Uses development build with hot reload  
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### 2. Incorrect API Communication
```typescript
// ❌ WRONG - No direct database access from React
// React cannot access database directly

// ✅ CORRECT - Using API service from React
const user = await authService.getCurrentUser();
// OR
const user = await fetch('/api/users/profile').then(r => r.json());
```

### 3. Wrong Authentication Pattern
```typescript
// ❌ WRONG - Storing tokens in localStorage (XSS risk)
localStorage.setItem('token', token);
fetch('/api/data', {
  headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
});

// ✅ CORRECT - BFF pattern with httpOnly cookies
const response = await fetch('/api/auth/login', {
  method: 'POST',
  credentials: 'include', // Essential for httpOnly cookies
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, password })
});

// ✅ CORRECT - All subsequent requests automatically include cookies
fetch('/api/data', {
  credentials: 'include' // No manual token management needed
});
```

### 4. UI Framework Violations
```typescript
// ❌ WRONG - Mixing UI frameworks
import { ChakraProvider } from '@chakra-ui/react';
import { Button } from '@mui/material';

// ✅ CORRECT - Consistent Mantine usage (ADR-004)
import { MantineProvider, Button } from '@mantine/core';
```

### 5. Vite Proxy Configuration Errors
```typescript
// ❌ WRONG - Missing API proxy in vite.config.ts
export default defineConfig({
  plugins: [react()]
});

// ✅ CORRECT - Proper API proxy configuration
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5655', // Local API
        changeOrigin: true
      }
    }
  }
});
```

---

## 🔧 Troubleshooting

### React Hot Reload Not Working

**Symptoms**: Changes not reflected, build errors
**Solution**:
```bash
# Local development
Ctrl+C and restart: npm run dev

# Docker development
./dev.sh → Option 7 (Rebuild and restart)
```

### Service Communication Failures

**Symptoms**: API calls failing, 500 errors
**Check**:
1. Both services are running: `docker ps`
2. API service logs: `./dev.sh` → Option 6
3. Network connectivity between containers

### Database Connection Issues

**Symptoms**: Migration errors, connection timeouts
**Check**:
1. PostgreSQL is running: `docker ps | grep postgres`
2. Database health: `./dev.sh` → Option 10 OR `/api/health/database` endpoint
3. Connection string in both services
4. **NEW**: Check initialization logs for retry attempts and error classification

**🆕 Enhanced Database Troubleshooting**:
- **Health Check**: Visit `/api/health/database` to see initialization status
- **Automatic Retries**: System retries database connections with exponential backoff
- **Detailed Logging**: Check API logs for correlation IDs and error classification
- **Environment Detection**: Verify environment-specific behavior (dev vs production)

### Authentication Problems

**Symptoms**: Login failures, authorization errors
**Check**:
1. Identity tables exist in database
2. JWT configuration matches between services
3. Cookie settings in Web service

### Port Already in Use

**Symptoms**: Container startup failures
**Solution**:
```bash
# Check what's using the port
sudo lsof -i :5173
sudo lsof -i :5655

# Stop conflicting processes or change ports in docker-compose.yml
```

---

## 🔍 Architecture Evidence

### How to Verify the Architecture

**1. Check Service Definitions:**
```bash
# View service configuration
cat docker-compose.yml | grep -A 10 "api:"
cat docker-compose.yml | grep -A 10 "web:"
```

**2. Verify API Communication:**
```bash
# Check Web→API calls in code
grep -r "ApiClient" src/WitchCityRope.Web/Services/
grep -r "HttpClient" src/WitchCityRope.Web/Program.cs
```

**3. Confirm Database Access:**
```bash
# Check database contexts
find . -name "*DbContext.cs"
grep -r "WitchCityRopeIdentityDbContext" src/
```

**4. Validate Port Configuration:**
```bash
# Check running containers
docker ps --format "table {{.Names}}\t{{.Ports}}"
```

---

## 📚 Additional Resources

- **React Migration Plan**: `/docs/architecture/react-migration/migration-plan.md`
- **UI Framework Decision (Mantine)**: `/docs/architecture/decisions/adr-004-ui-framework-mantine.md`
- **Mantine Documentation**: https://mantine.dev/
- **JWT Service-to-Service Authentication**: `/docs/functional-areas/authentication/jwt-service-to-service-auth.md`
- **Docker Development Guide**: `/docs/guides-setup/docker-operations-guide.md`
- **API Documentation**: Auto-generated Swagger at http://localhost:5655/swagger
- **React Testing Guide**: `/tests/README.md`

---

> **🚨 Remember**: This is a React + API architecture with Mantine UI framework. React communicates with API via HTTP calls with httpOnly cookie authentication. Always use Mantine components for UI consistency (ADR-004). When in doubt, check this document first!