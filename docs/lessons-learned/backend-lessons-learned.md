# Backend Lessons Learned

## Overview
This document captures backend development lessons learned across service architecture, API design, database patterns, and deployment considerations. These lessons apply to any backend technology stack but include specific examples for Node.js/Express and REST API implementations.

## Lessons Learned

### API Layer Architecture - 2025-08-12

**Context**: Originally implemented with web application directly accessing database, leading to tight coupling and difficult testing. Needed to separate concerns between presentation layer and business logic.

**What We Learned**: 
- Direct database access from web layers creates maintenance nightmares
- Service layer separation is essential for testability and scalability
- API-first design forces better architectural boundaries
- Controllers should be thin - business logic belongs in services

**Action Items**: 
- [ ] Always implement service layer between controllers and data access
- [ ] Use dependency injection for service registration
- [ ] Keep controllers limited to request/response handling
- [ ] Implement proper error handling and status codes

**Impact**: 
- Improved testability through service layer isolation
- Better separation of concerns
- Easier to refactor business logic without affecting presentation
- Enables future mobile app development

**References**:
- REST API design patterns
- Service layer pattern documentation
- Dependency injection best practices

**Tags**: #architecture #api-design #service-layer #separation-of-concerns

---

### Database Connection Management - 2025-08-12

**Context**: Experiencing "too many connections" errors in production environment. Database connections not being properly managed, leading to connection pool exhaustion.

**What We Learned**: 
- Connection pooling is critical for production stability
- Database context lifetime must match request scope
- Async patterns prevent connection blocking
- Connection string parameters significantly impact performance

**Action Items**: 
- [ ] Configure connection pooling with appropriate limits
- [ ] Use scoped lifetime for database contexts
- [ ] Implement async/await consistently throughout data layer
- [ ] Monitor connection pool metrics in production
- [ ] Set up connection timeout and retry policies

**Impact**: 
- Eliminated connection pool exhaustion issues
- Improved application stability under load
- Better resource utilization

**References**:
- Connection pooling documentation
- Database performance monitoring

**Tags**: #database #performance #connection-management #production

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

---

## Template Usage Instructions

### When to Use This Template
- Copy this template when creating a new domain-specific lessons learned file
- Replace `[Domain]` with the specific domain (e.g., "Backend", "Frontend", "Testing")
- Delete these instructions after copying

### Domain Examples
- Backend Lessons Learned
- Frontend Lessons Learned
- Testing Lessons Learned
- Database Lessons Learned
- DevOps Lessons Learned
- Security Lessons Learned
- Performance Lessons Learned
- Integration Lessons Learned
- UI/UX Lessons Learned
- Architecture Lessons Learned

### Writing Guidelines
1. **Be Specific**: Avoid vague statements; provide concrete details
2. **Be Actionable**: Every lesson should result in specific action items
3. **Provide Context**: Future readers should understand the situation without additional research
4. **Use Consistent Dating**: Always use YYYY-MM-DD format
5. **Tag Appropriately**: Use standardized tags for discoverability
6. **Link Extensively**: Connect to related documentation and resources

### Common Tags to Use
- `#critical` - Critical issues that caused significant problems
- `#process` - Process improvements and workflow changes
- `#tooling` - Tool selection and configuration lessons
- `#debugging` - Debugging techniques and troubleshooting
- `#performance` - Performance-related insights
- `#security` - Security considerations and best practices
- `#integration` - Third-party service integration lessons
- `#testing` - Testing strategy and implementation insights
- `#deployment` - Deployment and infrastructure lessons
- `#communication` - Team communication and coordination

### Maintenance Notes
- Review entries monthly for completeness and actionability
- Archive obsolete lessons to `docs/archive/obsolete-lessons/`
- Update cross-references when documentation structure changes
- Ensure action items are tracked and completed