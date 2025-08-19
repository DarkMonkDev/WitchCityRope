# Backend Lessons Learned

## 🚨 MANDATORY STARTUP PROCEDURE - READ FIRST 🚨

### Critical Architecture Documents (MUST READ BEFORE ANY WORK):
1. **Migration Architecture**: `/docs/architecture/react-migration/domain-layer-architecture.md`
2. **DTO Strategy**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
3. **Architecture Discovery Process**: `/docs/standards-processes/architecture-discovery-process.md`
4. **Migration Plan**: `/docs/architecture/react-migration/migration-plan.md`

### Validation Gates (MUST COMPLETE):
- [ ] Read all architecture documents above
- [ ] Check if solution already exists
- [ ] Reference existing patterns in your work
- [ ] NEVER create manual DTO interfaces (use NSwag)

### Backend Developer Specific Rules:
- **DTOs auto-generate via NSwag - NEVER create manual TypeScript interfaces**
- **Run `npm run generate:types` when API changes**
- **Import from @witchcityrope/shared-types only**
- **Add comprehensive OpenAPI annotations - these generate frontend types**

---

## 🚨 CRITICAL: DTO Alignment Strategy (READ FIRST) 🚨
**Date**: 2025-08-19
**Category**: Architecture
**Severity**: Critical

### Context
DTO alignment strategy is MANDATORY for React migration project success. API DTOs are the source of truth.

### What We Learned
- **API DTOs are SOURCE OF TRUTH**: Frontend must adapt to backend DTOs, never reverse
- **NSwag Auto-Generation is THE SOLUTION**: OpenAPI annotations automatically generate TypeScript types
- **NEVER Manual TypeScript Interfaces**: Frontend gets ALL types from packages/shared-types/src/generated/
- **Breaking Changes Require 30-Day Notice**: Any DTO property changes need frontend coordination
- **OpenAPI Annotations Generate Frontend Types**: Well-documented DTOs = perfect TypeScript interfaces
- **Change Control Process**: All DTO modifications go through architecture review board

### Action Items
- [ ] READ: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md` before ANY DTO work
- [ ] READ: `/docs/architecture/react-migration/domain-layer-architecture.md` for NSwag implementation
- [ ] ADD comprehensive OpenAPI annotations to ALL DTOs - these generate frontend types
- [ ] COORDINATE with frontend team before any DTO changes
- [ ] COMMIT both API changes and generated TypeScript updates together
- [ ] FOLLOW 30-day notice period for breaking changes
- [ ] NEVER create manual TypeScript interfaces - ensure NSwag pipeline works

### Tags
#critical #dto-alignment #api-contracts #typescript-integration #migration

---

### Docker Environment Database Connection Resolution - 2025-08-19

**Date**: 2025-08-19
**Category**: DevOps
**Severity**: Critical

**Context**: Fixed database connection issues for full end-to-end testing by resolving Docker container build targets and package dependencies.

**What We Learned**:
- Package.json with non-existent dependencies breaks container builds and prevents proper testing
- Docker multi-stage builds require explicit target specification in development
- Connection string mismatches between appsettings.json and docker-compose.yml cause authentication failures
- EF Core tools must be installed and PATH configured for container-based migrations

**Action Items**: 
- [ ] ALWAYS verify package.json dependencies exist before Docker builds
- [ ] ALWAYS use development target for API containers: `docker build --target development`
- [ ] ALWAYS ensure connection string consistency between appsettings and docker-compose
- [ ] ALWAYS install dotnet-ef tools in development containers for migrations

**Impact**: Achieved full end-to-end testing capability with working database connections, API authentication, and React integration.

**Code Examples**:
```bash
# Correct development environment startup
docker build --target development -t witchcityrope-react_api:latest ./apps/api
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Migration commands in containers
docker exec witchcity-api bash -c "export PATH=\"\$PATH:/root/.dotnet/tools\" && dotnet ef database update"

# Comprehensive health check
curl -f http://localhost:5655/health && curl -f http://localhost:5173 && echo "✅ Full stack operational"
```

**References**: Docker Operations Guide, Entity Framework Patterns

**Tags**: #docker #database #end-to-end-testing #migration #package-management

---

### Authentication Endpoint Pattern Implementation - 2025-08-19

**Date**: 2025-08-19
**Category**: Authentication
**Severity**: Medium

**Context**: Frontend React app expected `/api/auth/user` endpoint (without ID) but API only had `/api/auth/user/{id}` which required ID parameter. Frontend authentication flow failed with 404 errors.

**What We Learned**:
- Authentication endpoints must match frontend expectations exactly
- JWT token extraction pattern works consistently across controllers
- `[Authorize]` attribute with JWT Bearer authentication handles token validation automatically
- User claims extraction using "sub" claim or ClaimTypes.NameIdentifier provides backward compatibility
- Error handling should distinguish between invalid tokens (401) and missing users (404)

**Action Items**: 
- [ ] ALWAYS implement `/api/auth/user` endpoint (no ID) for JWT-authenticated current user retrieval
- [ ] ALWAYS use consistent JWT claim extraction pattern: `User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value`
- [ ] ALWAYS add proper logging for authentication debugging
- [ ] ALWAYS return 401 for authentication issues, 404 for missing users

**Impact**: Fixed React frontend authentication by providing the expected endpoint pattern, enabling complete authentication flow.

**Code Example**:
```csharp
[HttpGet("user")]
[Authorize] // JWT Bearer token required
public async Task<IActionResult> GetCurrentUser()
{
    var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Unauthorized(new ApiResponse<object> { Success = false, Error = "Invalid token" });
    
    var user = await _authService.GetUserByIdAsync(userId);
    return user != null ? Ok(new ApiResponse<UserDto> { Success = true, Data = user }) 
                       : NotFound(new ApiResponse<object> { Success = false, Error = "User not found" });
}
```

**References**: AuthController.cs, ProtectedController.cs JWT patterns

**Tags**: #authentication #jwt #endpoints #frontend-integration #react

---

# Backend Lessons Learned

This file captures key learnings for backend development in the WitchCityRope project. Focus on actionable insights for C# .NET API development, database integration, and authentication patterns.

## Lessons Learned

### Service-to-Service Authentication Scaling Patterns - 2025-08-16

**Date**: 2025-08-16
**Category**: Security
**Severity**: Medium

**Context**: Researching authentication patterns for scaling beyond current Web+API architecture to support multiple services and mobile apps.

**What We Learned**: 
- OAuth 2.0 Client Credentials Flow is industry standard for service-to-service authentication
- JWT with proper secret management works well for small-medium scale (10-100 users)
- Service mesh (Istio/Linkerd) provides enterprise-grade security but significant complexity overhead
- Short-lived tokens (15-30 minutes) with refresh provides optimal security/usability balance

**Action Items**: 
- [ ] Enhance current JWT with Docker network isolation
- [ ] Implement 15-minute token expiration with refresh
- [ ] Add OAuth 2.0 Client Credentials for future scaling
- [ ] Document service authentication patterns for team

**Impact**: Clear scaling path from current architecture to enterprise-grade authentication without over-engineering.

**Tags**: #authentication #microservices #oauth2 #jwt #security

---

### Hybrid JWT + HttpOnly Cookie Authentication Pattern - 2025-08-16

**Date**: 2025-08-16
**Category**: Authentication
**Severity**: High

**Context**: Designed authentication API for React migration using JWT for service-to-service communication and HttpOnly cookies for web client security.

**What We Learned**: 
- Service-to-service auth requires JWT bridge between cookie-authenticated web and JWT-authenticated API
- HttpOnly cookies with SameSite=Strict provide optimal XSS/CSRF protection
- ASP.NET Core Identity integrates well with PostgreSQL while preserving custom fields
- CORS must allow credentials for cookie-based auth with React dev server

**Action Items**: 
- [ ] Implement JWT claims with custom SceneName field
- [ ] Configure HttpOnly cookies with proper security flags
- [ ] Add rate limiting on auth endpoints (10 req/min)
- [ ] Create service-to-service authentication endpoint with shared secret

**Impact**: Provides secure authentication pattern that balances web security (cookies) with API flexibility (JWT).

**Tags**: #authentication #jwt #cookies #security #cors

---

### PostgreSQL Integration with EF Core - 2025-08-16

**Date**: 2025-08-16
**Category**: Database
**Severity**: Medium

**Context**: Integrating PostgreSQL with EF Core for React API backend, adapting entity models to existing database schema.

**What We Learned**: 
- Entity models must match PostgreSQL column types exactly (timestamptz, text, etc.)
- AsNoTracking() with projections significantly optimizes read-only queries
- Service layer with dependency injection provides clean controller separation
- Health checks essential for database connectivity monitoring
- Fallback patterns ensure API reliability during database issues

**Action Items**: 
- [ ] Always use AsNoTracking() for read-only queries
- [ ] Add health checks for all database connections
- [ ] Implement fallback patterns for critical endpoints
- [ ] Match entity properties to existing PostgreSQL schema

**Impact**: Successfully established React ↔ API ↔ PostgreSQL data flow with performance optimization and reliability patterns.

**Tags**: #postgresql #entity-framework #performance #health-checks

---

### Service Layer Architecture Pattern - 2025-08-12

**Date**: 2025-08-12
**Category**: Architecture
**Severity**: High

**Context**: Moved from direct database access to proper service layer separation for better testability and maintainability.

**What We Learned**: 
- Direct database access from controllers creates tight coupling and testing difficulties
- Service layer with dependency injection enables clean separation of concerns
- Thin controllers improve maintainability and enable easier business logic changes
- API-first design forces better architectural boundaries

**Action Items**: 
- [ ] Always implement service layer between controllers and data access
- [ ] Keep controllers limited to request/response handling only
- [ ] Use dependency injection for service registration
- [ ] Implement consistent error handling patterns

**Impact**: Improved testability, better separation of concerns, easier refactoring, and foundation for future mobile app development.

**Tags**: #architecture #service-layer #separation-of-concerns #dependency-injection

---

### Database Connection Pool Management - 2025-08-12

**Date**: 2025-08-12
**Category**: Performance
**Severity**: Critical

**Context**: Production "too many connections" errors due to improper connection management causing pool exhaustion.

**What We Learned**: 
- Connection pooling configuration is critical for production stability
- Database context must use scoped lifetime to match request scope
- Async/await patterns prevent connection blocking
- Connection string parameters directly impact performance and stability

**Action Items**: 
- [ ] Configure connection pooling with appropriate limits (max 100 for PostgreSQL)
- [ ] Use scoped lifetime for all database contexts
- [ ] Implement async/await consistently in data layer
- [ ] Add connection pool monitoring to production

**Impact**: Eliminated connection exhaustion, improved stability under load, better resource utilization.

**Tags**: #database #performance #connection-pooling #production

---

### Authentication Service Pattern - 2025-08-12

**Context**: JWT token creation failing for existing authenticated users. Authentication events only triggered during login process, not for users with existing sessions needing API access.

**What We Learned**: 
- Authentication middleware must handle both new logins and existing sessions
- Token generation should be on-demand, not event-driven only
- Service-to-service authentication requires different patterns than user authentication
- API endpoint paths must be consistent with routing configuration

**Action Items**: 
- [ ] Implement on-demand token generation for authenticated users
- [ ] Use authentication middleware for API request handling
- [ ] Separate user authentication from service authentication concerns
- [ ] Verify API route prefixes match client expectations
- [ ] Create authentication testing scenarios for both login and existing sessions

**Impact**: 
- Fixed authentication bridge between web and API layers
- Improved user experience for authenticated users
- Better service-to-service communication patterns

**References**:
- JWT authentication patterns
- Service authentication documentation
- API routing configuration

**Tags**: #authentication #jwt #api-integration #middleware

---

### Entity Framework Migration Patterns - 2025-08-12

**Context**: Database migrations failing in production due to unsafe schema changes. Entity discovery through navigation properties causing unexpected migration dependencies.

**What We Learned**: 
- Navigation properties to ignored entities cause migration failures
- Entity ID initialization prevents duplicate key violations during seeding
- Migration reversibility is essential for production safety
- Schema changes must be performed in safe, incremental steps

**Action Items**: 
- [ ] Remove navigation properties to ignored entities completely
- [ ] Initialize entity IDs properly in seed data
- [ ] Make all migrations reversible with proper Down() methods
- [ ] Use nullable columns first, then convert to non-nullable after data population
- [ ] Test migrations against production-like data volumes

**Impact**: 
- Reduced migration failures in production
- Safer database schema evolution
- Better data consistency during deployments

**References**:
- Entity Framework migration best practices
- Safe database deployment patterns

**Tags**: #database #migrations #entity-framework #production-safety

---

### Service Layer Error Handling - 2025-08-12

**Context**: Controllers contained mixed business logic and error handling, making it difficult to test and maintain consistent API responses.

**What We Learned**: 
- Controllers should only handle HTTP concerns (request/response mapping)
- Business logic errors should be separated from HTTP errors
- Consistent status code patterns improve API usability
- Error responses should provide actionable information

**Action Items**: 
- [ ] Move all business logic to service layer
- [ ] Implement consistent error result patterns
- [ ] Use appropriate HTTP status codes for different error types
- [ ] Create standard error response formats
- [ ] Add comprehensive error logging at service boundaries

**Impact**: 
- More maintainable controller code
- Consistent API error responses
- Better error tracking and debugging
- Improved API client experience

**References**:
- HTTP status code standards
- REST API error handling patterns

**Tags**: #error-handling #controllers #service-layer #api-design

---

### Docker Development Configuration - 2025-08-12

**Context**: Services unable to communicate in Docker environment due to hardcoded localhost connections. Service discovery failing between containers.

**What We Learned**: 
- Container networking requires service names instead of localhost
- Environment-specific configuration is essential for Docker deployments
- Service discovery patterns differ between development and production
- Port configuration must be consistent across container definitions

**Action Items**: 
- [ ] Use container service names for internal communication
- [ ] Create environment-specific configuration files
- [ ] Implement service discovery patterns for container environments
- [ ] Configure health checks for service availability
- [ ] Document port mapping for all services

**Impact**: 
- Reliable service communication in containerized environments
- Easier development environment setup
- Better production deployment patterns

**References**:
- Docker networking documentation
- Container service discovery patterns

**Tags**: #docker #networking #service-discovery #deployment

---

### Data Access Performance Optimization - 2025-08-12

**Context**: Slow API responses due to inefficient database queries. N+1 query problems and loading unnecessary data affecting performance.

**What We Learned**: 
- Projection queries (Select) are more efficient than Include for specific fields
- Pagination is essential for large datasets
- Query analysis tools help identify performance bottlenecks
- Strategic indexing significantly improves query performance

**Action Items**: 
- [ ] Use Select projections instead of Include when possible
- [ ] Implement pagination for all list endpoints
- [ ] Add database query profiling to development workflow
- [ ] Create indexes for common query patterns
- [ ] Monitor query performance in production

**Impact**: 
- Improved API response times
- Better resource utilization
- Reduced database load

**References**:
- Database query optimization guides
- Pagination best practices

**Tags**: #performance #database #optimization #queries

---

### Integration Testing Environment Setup - 2025-08-12

**Context**: Integration tests failing with file system permission errors in containerized environments. Data protection configuration incompatible with test containers.

**What We Learned**: 
- Test environments need different configuration than production
- File system dependencies break in container testing
- Ephemeral data protection suitable for testing scenarios
- Environment detection enables configuration flexibility

**Action Items**: 
- [ ] Use ephemeral data protection for test environments
- [ ] Create environment-specific service configurations
- [ ] Avoid file system dependencies in tests
- [ ] Implement proper test isolation patterns
- [ ] Add environment detection for configuration branching

**Impact**: 
- Reliable integration test execution
- Better CI/CD pipeline stability
- Improved development workflow

**References**:
- Integration testing patterns
- Container testing best practices

**Tags**: #testing #integration #containers #configuration

---

### Async/Await Consistency - 2025-08-12

**Context**: Deadlock issues and performance problems caused by mixing synchronous and asynchronous code patterns throughout the application.

**What We Learned**: 
- Mixing sync/async patterns causes deadlocks
- .Result and .Wait() should be avoided in async contexts
- Async must be implemented consistently throughout the call chain
- Performance benefits only realized with complete async implementation

**Action Items**: 
- [ ] Use async/await consistently throughout the application
- [ ] Avoid .Result and .Wait() calls
- [ ] Configure async contexts properly
- [ ] Review all data access methods for async patterns
- [ ] Add async best practices to code review checklist

**Impact**: 
- Eliminated deadlock issues
- Improved application responsiveness
- Better resource utilization

**References**:
- Async/await best practices
- Deadlock prevention patterns

**Tags**: #async #performance #patterns #best-practices

---

### Service Layer Architecture and API Patterns - 2025-08-12

**Context**: Originally implemented with web application directly accessing database, leading to tight coupling and difficult testing. Needed clear separation between presentation and business logic layers.

**What We Learned**: 
- Web + API separation is essential for maintainable architecture
- Controllers should be thin - business logic belongs in services
- Service layer pattern enables better testability and isolation
- Proper HTTP status codes improve API usability
- Dependency injection configuration affects application stability

**Action Items**: 
- [ ] Keep controllers limited to request/response handling only
- [ ] Move all business logic to service layer
- [ ] Use appropriate HTTP status codes for different scenarios
- [ ] Configure dependency injection with proper lifetimes (Scoped for DbContext)
- [ ] Implement interface segregation for focused service contracts

**Impact**: 
- Improved separation of concerns and testability
- Better error handling and API consistency
- Easier maintenance and future mobile app development

**Code Examples**:
```csharp
// ❌ WRONG - Logic in controller
[HttpPost]
public async Task<IActionResult> CreateEvent(EventDto model)
{
    if (model.StartTime < DateTime.UtcNow)
        return BadRequest();
    // More logic...
}

// ✅ CORRECT - Logic in service
[HttpPost]
public async Task<IActionResult> CreateEvent(EventDto model)
{
    var result = await _eventService.CreateEventAsync(model);
    return result.Success ? Ok(result) : BadRequest(result);
}

// ✅ CORRECT - Proper status codes
return result switch
{
    not null => Ok(result),           // 200
    null => NotFound(),               // 404
    _ when !ModelState.IsValid => BadRequest(ModelState), // 400
    _ => StatusCode(500)              // 500
};
```

**References**:
- Service layer pattern documentation
- REST API design patterns
- Entity Framework Patterns documentation

**Tags**: #architecture #api-design #service-layer #controllers #separation-of-concerns

---

### PostgreSQL Database Optimization and Management - 2025-08-12

**Context**: Experiencing production database issues including "too many connections" errors, slow queries, and migration failures. Needed to establish proper PostgreSQL patterns and optimization strategies.

**What We Learned**: 
- PostgreSQL requires specific configuration patterns different from SQL Server
- Connection pooling is critical for production stability
- Case sensitivity and JSON support require special handling
- Safe migration patterns are essential for production deployments
- Database constraints beyond EF Core improve data integrity

**Action Items**: 
- [ ] Configure connection pooling with appropriate limits (max 100 connections)
- [ ] Use CITEXT extension or proper collation for case-insensitive searches
- [ ] Implement JSONB columns with GIN indexes for flexible schema needs
- [ ] Always make migrations reversible with proper Down() methods
- [ ] Add database-level constraints for critical business rules
- [ ] Use EXPLAIN ANALYZE for query optimization on slow operations

**Impact**: 
- Eliminated connection pool exhaustion in production
- Improved query performance with proper indexing
- Safer database deployments with reversible migrations
- Better data integrity with database-level constraints

**Code Examples**:
```sql
-- Connection string with pooling
Host=localhost;Database=witchcityrope_db;Username=postgres;Password=xxx;
Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;Connection Lifetime=0;

-- Case insensitive email lookups
CREATE EXTENSION IF NOT EXISTS citext;
ALTER TABLE "Users" ALTER COLUMN "Email" TYPE citext;

-- JSONB with indexes for performance
metadata JSONB NOT NULL DEFAULT '{}'
CREATE INDEX idx_events_metadata ON "Events" USING GIN (metadata);
SELECT * FROM "Events" WHERE metadata @> '{"type": "workshop"}';

-- Safe migration patterns
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Add nullable column first
    migrationBuilder.AddColumn<string>("NewColumn", "Events", nullable: true);
    // Populate data
    migrationBuilder.Sql("UPDATE \"Events\" SET \"NewColumn\" = 'default'");
    // Then make non-nullable
    migrationBuilder.AlterColumn<string>("NewColumn", "Events", nullable: false);
}

// Business rule constraints
ALTER TABLE "Events" 
ADD CONSTRAINT chk_event_dates 
CHECK ("EndTime" > "StartTime");
```

**References**:
- PostgreSQL performance tuning documentation
- Entity Framework Core PostgreSQL provider guide
- Database indexing strategy patterns

**Tags**: #postgresql #database #performance #migrations #connection-pooling #indexing

### Docker Container Development for .NET APIs - 2025-08-17

**Date**: 2025-08-17
**Category**: DevOps
**Severity**: Medium

**Context**: Containerizing .NET API development with hot reload functionality and proper debugging support.

**What We Learned**:
- Docker hot reload with `dotnet watch` works reliably with proper volume mounting
- Container service names (postgres) replace localhost for internal communication
- CORS configuration needs both localhost and container origins
- Authentication patterns work identically in containers with proper networking

**Action Items**: 
- [ ] Use service names (postgres:5432) for container database connections
- [ ] Configure CORS for both localhost and container origins
- [ ] Set up hot reload with proper volume mounting
- [ ] Document container debugging procedures for team

**Impact**: Enables consistent containerized development while maintaining hot reload and debugging capabilities.

**References**: [Docker Operations Guide](/docs/guides-setup/docker-operations-guide.md)

**Tags**: #docker #containers #dotnet #hot-reload #debugging

---

### Multi-Stage Docker Builds for .NET APIs - 2025-08-17

**Date**: 2025-08-17
**Category**: DevOps
**Severity**: Medium

**Context**: Implementing multi-stage Docker builds for .NET 9 Minimal API with development and production optimization.

**What We Learned**:
- Multi-stage builds reduce production image size from 1.2GB to 200MB (83% reduction)
- Non-root user execution (user 1001) essential for production security
- Layer caching with NuGet packages significantly improves build performance
- File-based secrets better than environment variables for production

**Action Items**: 
- [ ] Use multi-stage builds with development and production targets
- [ ] Configure non-root user execution for production images
- [ ] Implement file-based secret management for production
- [ ] Add health checks for container orchestration

**Impact**: Provides secure, optimized containerization with development efficiency and production hardening.

**Tags**: #docker #multi-stage-builds #security #production

---

### Docker Compose Multi-Environment Strategy - 2025-08-17

**Date**: 2025-08-17
**Category**: DevOps  
**Severity**: Medium

**Context**: Implementing layered Docker Compose configuration strategy for development, test, and production environments.

**What We Learned**:
- Layered configuration (base + overrides) reduces duplication and enables environment-specific optimization
- Environment variables with defaults provide secure fallbacks while enabling customization
- Custom networks with defined subnets enable predictable service communication
- Volume strategy varies by environment (bind mounts for dev, ephemeral for test, named for production)

**Action Items**: 
- [ ] Use base docker-compose.yml with environment-specific overrides
- [ ] Implement environment variables with secure defaults
- [ ] Configure custom networks for service isolation
- [ ] Document environment-specific deployment commands

**Impact**: Provides complete Docker configuration strategy supporting all deployment scenarios with environment-specific optimization.

**Tags**: #docker-compose #multi-environment #networking #volumes

---

### Dockerfile Implementation Best Practices - 2025-08-17

**Date**: 2025-08-17
**Category**: DevOps
**Severity**: Medium

**Context**: Implementing production-ready Dockerfile for .NET 9 Minimal API with development and production stages.

**What We Learned**:
- `DOTNET_USE_POLLING_FILE_WATCHER=true` essential for cross-platform file watching in containers
- Layer caching with separate dependency restore significantly improves build performance
- Alpine-based production images provide minimal attack surface and faster startup
- .dockerignore optimization reduces build context by ~80% and improves security

**Action Items**: 
- [ ] Use multi-stage builds with development and production targets
- [ ] Configure proper file watching environment variables
- [ ] Implement comprehensive .dockerignore for build optimization
- [ ] Add health checks for container orchestration

**Impact**: Enables consistent development with hot reload and secure production deployment with optimized image size.

**Tags**: #docker #dockerfile #dotnet #hot-reload #alpine

---

### Docker Environment Configuration Strategy - 2025-08-17

**Date**: 2025-08-17
**Category**: DevOps
**Severity**: Medium

**Context**: Implementing environment-specific Docker configurations for development, test, and production deployment scenarios.

**What We Learned**:
- Layered configuration (base + overrides) significantly reduces duplication across environments
- Environment variable defaults with overrides provide secure fallbacks and customization
- Service name standardization simplifies container communication and documentation
- File-based secrets better than environment variables for production security

**Action Items**: 
- [ ] Create base docker-compose.yml with shared service definitions
- [ ] Implement environment-specific overrides (dev, test, prod)
- [ ] Use file-based secrets for production credential management
- [ ] Document environment deployment commands for team

**Impact**: Provides complete containerization supporting all deployment scenarios with appropriate security and optimization for each environment.

**Tags**: #docker-compose #multi-environment #secrets-management #production

---

### Docker Helper Scripts for Development Workflow - 2025-08-17

**Date**: 2025-08-17
**Category**: DevOps
**Severity**: Low

**Context**: Created helper scripts to simplify common Docker operations and improve developer experience.

**What We Learned**:
- Command-line scripts with argument parsing significantly improve workflow efficiency
- Pre-flight checks (Docker daemon, port availability) catch issues early
- Interactive modes for destructive operations prevent accidental data loss
- Color-coded output and confirmation prompts improve script reliability

**Action Items**: 
- [ ] Create scripts for common operations (start, stop, logs, health checks)
- [ ] Add pre-flight checks and error handling to all scripts
- [ ] Implement confirmation prompts for destructive operations
- [ ] Document script usage patterns for team adoption

**Impact**: Simplifies Docker operations and reduces complexity for team members regardless of Docker expertise.

**Tags**: #docker #helper-scripts #developer-experience #automation

---

### Frontend Integration API Requirements - 2025-08-19

**Date**: 2025-08-19
**Category**: Architecture
**Severity**: Critical

**Context**: Backend APIs must integrate with validated React technology stack: TanStack Query v5, Zustand state management, and React Router v7.

**What We Learned**:
- APIs must return data in format expected by TanStack Query (PaginatedResponse pattern)
- Authentication MUST use httpOnly cookies (NO JWT tokens in localStorage)
- CORS must allow credentials for cookie-based authentication
- API responses must match frontend TypeScript types exactly
- Optimistic updates require complete entity returns after mutations

**Action Items**: 
- [ ] ALWAYS implement httpOnly cookie authentication (no JWT in response)
- [ ] ALWAYS follow TanStack Query pagination patterns
- [ ] ALWAYS return complete entities after mutations for cache updates  
- [ ] ALWAYS configure CORS with AllowCredentials for cookies
- [ ] NEVER implement custom authentication patterns outside established contracts

**Impact**: Ensures seamless backend integration with validated React technology stack while maintaining security and performance standards.

**References**: 
- API Integration Patterns: `/docs/functional-areas/api-integration-validation/requirements/functional-specification-v2.md`
- API Type Definitions: `/apps/web/src/types/api.types.ts`

**Tags**: #frontend-integration #api-patterns #authentication #httponly-cookies #cors #pagination

