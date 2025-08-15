# 🏗️ WitchCityRope Architecture Documentation

> **⚠️ CRITICAL FOR ALL DEVELOPERS: READ THIS FIRST!**
> 
> This document describes the Web+API architecture pattern used by WitchCityRope. Understanding this is essential to avoid architectural mistakes that could break the entire application.

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
│  Web Service    │                  │  API Service    │
│  (Blazor Server)│ ◄─────────────── │  (Minimal API)  │
│                 │   JSON Responses │                 │
└─────────────────┘                  └─────────────────┘
        │                                      │
        │ Direct DB Access                     │ Direct DB Access
        │ (Identity & Auth)                    │ (Business Logic)
        ▼                                      ▼
┌─────────────────────────────────────────────────────────┐
│             PostgreSQL Database                         │
│         (Shared between services)                       │
└─────────────────────────────────────────────────────────┘
```

### Services Explained

1. **Web Service (WitchCityRope.Web)**
   - **Technology**: Blazor Server (.NET 9)
   - **Purpose**: User interface, authentication, session management
   - **Database Access**: Direct access for Identity operations only
   - **Port**: 5651 (HTTP)

2. **API Service (WitchCityRope.Api)**  
   - **Technology**: ASP.NET Core Minimal API (.NET 9)
   - **Purpose**: Business logic, data operations, external integrations
   - **Database Access**: Full access to all business entities
   - **Port**: 5653 (HTTP)

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

### Development Ports (Docker)

| Service | Internal Port | External Port | URL |
|---------|---------------|---------------|-----|
| Web | 8080 | 5651 | http://localhost:5651 |
| API | 8080 | 5653 | http://localhost:5653 |
| Database | 5432 | 5433 | localhost:5433 |
| pgAdmin | 80 | 5050 | http://localhost:5050 |

### Configuration Files

**Docker Compose:**
```yaml
# docker-compose.yml (lines 100-102)
web:
  ports:
    - "5651:8080"  # Web service

# docker-compose.yml (lines 34-36)
api:
  ports:
    - "5653:8080"  # API service
```

**Environment Variables:**
```yaml
# docker-compose.yml (lines 111-112)  
- ApiBaseUrl=http://api:8080/          # Internal container communication
- ApiBaseUrlExternal=https://localhost:5654/  # External access
```

---

## 🔐 Authentication Flow

### Architecture Pattern: JWT Service-to-Service Authentication

The application uses a **hybrid authentication approach** with JWT service-to-service communication:

1. **Web Service Authentication**
   - Uses ASP.NET Core Identity with cookies for user sessions
   - Direct database access to Identity tables
   - Handles login/logout UI interactions

2. **Service-to-Service JWT Authentication**
   - Web service obtains JWT tokens from API service via shared secret
   - All Web→API calls use JWT Bearer tokens
   - Enables authenticated API access on behalf of logged-in users
   - **📖 See detailed implementation**: `/docs/functional-areas/authentication/jwt-service-to-service-auth.md`

3. **API Service Authentication**  
   - Validates JWT tokens for all protected endpoints
   - Can also access Identity tables for user validation
   - Provides service token endpoints for internal communication

### Authentication Components

**Web Service (Cookie-based):**
```csharp
// Program.cs (lines 78-119)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/auth/login";
    // ... cookie configuration
});
```

**API Service (JWT-based):**
```csharp
// Program.cs (lines 46-50)
- Jwt__Secret=${JWT_SECRET:-YourSuperSecretKeyForDevelopmentPurposesOnly!123}
- Jwt__Issuer=WitchCityRope
- Jwt__Audience=WitchCityRopeUsers  
- Jwt__ExpiresInMinutes=60
```

### User Information Flow

```
1. User logs in via Web UI → Saves cookie in Web service
2. Web service needs data → Calls API service with user context  
3. API validates request → Returns data to Web service
4. Web service displays data → User sees result
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

### Migration Management

**Important**: Migrations are managed at the Infrastructure level and applied by both services:

- **Migration Files**: `/src/WitchCityRope.Infrastructure/Data/Migrations/`
- **Applied by**: Both Web and API services during startup
- **Initialization**: `/src/WitchCityRope.Infrastructure/Data/DbInitializer.cs`

---

## 📁 Key Files and Their Purposes

### Web Service Key Files

| File | Purpose | Critical Details |
|------|---------|------------------|
| `/src/WitchCityRope.Web/Program.cs` | Web service configuration | Blazor Server setup, Identity, HTTP clients |
| `/src/WitchCityRope.Web/Services/ApiClient.cs` | HTTP communication with API | Web→API communication wrapper |
| `/src/WitchCityRope.Web/Services/AuthService.cs` | Authentication proxy | Bridges Web auth with API calls |
| `/src/WitchCityRope.Web/App.razor` | Root Blazor component | Entry point for all Blazor UI |

### API Service Key Files

| File | Purpose | Critical Details |
|------|---------|------------------|
| `/src/WitchCityRope.Api/Program.cs` | API service configuration | Minimal API setup, endpoints, JWT |
| `/src/WitchCityRope.Api/Controllers/` | API controllers | Business logic endpoints |
| `/src/WitchCityRope.Api/Services/` | Business services | Core application services |
| `/src/WitchCityRope.Api/Features/` | Feature modules | Organized by domain (Auth, Events, etc.) |

### Shared Infrastructure Files

| File | Purpose | Critical Details |
|------|---------|------------------|
| `/src/WitchCityRope.Infrastructure/Data/WitchCityRopeIdentityDbContext.cs` | Database context | Shared by both services |
| `/src/WitchCityRope.Infrastructure/Data/Migrations/` | Database migrations | Applied by both services |
| `/src/WitchCityRope.Core/` | Domain entities | Shared business objects |

### Configuration Files

| File | Purpose | Critical Details |
|------|---------|------------------|
| `docker-compose.yml` | Production service configuration | Service definitions, ports, environment |
| `docker-compose.dev.yml` | Development overrides | Hot reload, volume mounts |
| `dev.sh` | Development helper script | Start/stop services, view logs |

---

## 🛠️ Development Environment

### Starting the Application

**⚠️ CRITICAL**: Always use development builds for hot reload:

```bash
# CORRECT: Use development build
./dev.sh
# OR
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# WRONG: This will fail with hot reload issues
docker-compose up
```

### Development Workflow

1. **Start Services**: `./dev.sh` → Option 1
2. **Access Application**: http://localhost:5651 (Web)
3. **Access API**: http://localhost:5653 (API) 
4. **View Logs**: `./dev.sh` → Options 4-6
5. **Hot Reload**: Automatic when files change

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
```csharp
// ❌ WRONG - Bypassing the API service
var user = await _dbContext.Users.FindAsync(userId);

// ✅ CORRECT - Using API service  
var user = await _apiClient.GetAsync<UserDto>($"api/v1/users/{userId}");
```

### 3. Wrong Database Context Usage
```csharp
// ❌ WRONG - Web service accessing business entities directly
var events = await _dbContext.Events.ToListAsync();

// ✅ CORRECT - Web service calling API for business data
var events = await _apiClient.GetAsync<List<EventDto>>("api/v1/events");
```

### 4. Architecture Pattern Violations
```csharp
// ❌ WRONG - Adding Razor Pages to pure Blazor Server
builder.Services.AddRazorPages(); // Don't do this!

// ✅ CORRECT - Pure Blazor Server pattern
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
```

### 5. Port Configuration Errors
```yaml
# ❌ WRONG - Port conflicts or incorrect mapping
api:
  ports:
    - "5651:8080"  # This conflicts with web service!

# ✅ CORRECT - Unique external ports
api:
  ports:
    - "5653:8080"  # API gets its own port
```

---

## 🔧 Troubleshooting

### Hot Reload Not Working

**Symptoms**: Changes not reflected, build errors
**Solution**:
```bash
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
2. Database health: `./dev.sh` → Option 10
3. Connection string in both services

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
sudo lsof -i :5651
sudo lsof -i :5653

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

- **Clean Architecture Guide**: `/docs/architecture/CURRENT-ARCHITECTURE-SUMMARY.md`
- **Docker Development Guide**: `/DOCKER_DEV_GUIDE.md`
- **JWT Service-to-Service Authentication**: `/docs/functional-areas/authentication/jwt-service-to-service-auth.md`
- **Legacy Authentication Documentation**: `/docs/architecture/AUTHENTICATION-ARCHITECTURE.md`
- **API Documentation**: Auto-generated Swagger at http://localhost:5653/swagger
- **Testing Guide**: `/TESTING_GUIDE.md`

---

> **🚨 Remember**: This is a microservices architecture. Always respect service boundaries and use proper communication patterns. When in doubt, check this document first!