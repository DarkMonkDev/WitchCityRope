# Critical Learnings for All Developers

## Overview

This document contains the most critical architectural issues, debugging victories, and patterns that ALL developers working on WitchCityRope must know. These learnings prevent repeating solved problems and architectural mistakes.

## 🚨 Architecture-Breaking Issues

### 1. Blazor Server Authentication
**CRITICAL**: SignInManager cannot be used directly in Blazor Server components
- **Reference**: [Authentication Patterns](/docs/standards-processes/development-standards/authentication-patterns.md)
- **Key Issue**: "Headers are read-only" errors
- **Solution**: API endpoints pattern only

### 2. Entity Framework Core Entity Discovery
**CRITICAL**: EF Core discovers entities through navigation properties even when ignored
- **Reference**: [Entity Framework Patterns](/docs/standards-processes/development-standards/entity-framework-patterns.md#navigation-property-management)
- **Key Issue**: Migration generation fails
- **Solution**: Remove ALL navigation properties to ignored entities

### 3. Docker Development Build Failures
**CRITICAL**: Default docker-compose uses production build and fails
- **Reference**: [Docker Development](/docs/standards-processes/development-standards/docker-development.md)
- **Key Issue**: `dotnet watch` runs on compiled assemblies
- **Solution**: Always use `./dev.sh` or include docker-compose.dev.yml

### 4. DateTime UTC Requirements
**CRITICAL**: PostgreSQL requires UTC DateTimes
- **Reference**: [Entity Framework Patterns](/docs/standards-processes/development-standards/entity-framework-patterns.md#datetime-handling-with-postgresql)
- **Key Issue**: "Cannot write DateTime with Kind=Unspecified" errors
- **Solution**: Always use DateTime.UtcNow or specify DateTimeKind.Utc

### 5. Entity ID Initialization
**CRITICAL**: Default Guid.Empty causes duplicate key violations
- **Reference**: [Backend Developers Lessons](/docs/lessons-learned/backend-developers.md#entity-id-initialization)
- **Key Issue**: PostgreSQL duplicate key violations
- **Solution**: Initialize Id = Guid.NewGuid() in entity constructors

## 🔧 Development Environment Issues

### Web + API Separation
**Architecture**: Web project must call API for all business operations
- **Reference**: [Backend Developers Lessons](/docs/lessons-learned/backend-developers.md#web--api-separation)
- **Never**: Direct database access from Web project
- **Always**: HTTP calls to API endpoints

### Hot Reload Failures
**Issue**: .NET 9 Blazor Server hot reload unreliable in Docker
- **Reference**: [Docker Development](/docs/standards-processes/development-standards/docker-development.md#hot-reload-issues-and-solutions)
- **Solution**: Use `./restart-web.sh` when changes don't appear

### Container Communication
**Issue**: Services can't find each other or wrong ports
- **Internal**: Use container names (api:8080, postgres:5432)
- **External**: Use localhost with mapped ports (5651, 5653, 5433)

## 📚 Development Standards References

### Core Development Standards
- [Coding Standards](/docs/standards-processes/CODING_STANDARDS.md) - Service patterns, documentation, testing
- [Authentication Patterns](/docs/standards-processes/development-standards/authentication-patterns.md) - Blazor auth architecture
- [Entity Framework Patterns](/docs/standards-processes/development-standards/entity-framework-patterns.md) - EF Core best practices
- [Docker Development](/docs/standards-processes/development-standards/docker-development.md) - Container development
- [Blazor Server Patterns](/docs/standards-processes/development-standards/blazor-server-patterns.md) - UI patterns

### Role-Specific Lessons
- [Backend Developers](/docs/lessons-learned/backend-developers.md) - C#, API, EF Core patterns
- [UI Developers](/docs/lessons-learned/ui-developers.md) - Blazor, Syncfusion, responsive design
- [Test Writers](/docs/lessons-learned/test-writers.md) - Playwright, integration testing
- [Database Developers](/docs/lessons-learned/database-developers.md) - PostgreSQL, migrations, performance
- [DevOps Engineers](/docs/lessons-learned/devops-engineers.md) - Docker, deployment, monitoring

## 🚀 Quick Recovery Commands

### When Docker Fails
```bash
# Use development build
./dev.sh

# Or manual
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### When Hot Reload Fails
```bash
# Quick restart
./restart-web.sh

# Or full rebuild
./dev.sh
# Select option 7: Rebuild and restart
```

### When Tests Fail
```bash
# Run health checks first
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# Then run actual tests
dotnet test tests/WitchCityRope.IntegrationTests/
```

### When Authentication Breaks
1. Check if using SignInManager directly (forbidden)
2. Verify API endpoints are working
3. Check HttpClient container URLs (api:8080 not localhost:5653)
4. Review authentication service implementation

### When EF Migrations Fail
1. Check for navigation properties to ignored entities
2. Ensure all DateTime values are UTC
3. Verify entity Id initialization in constructors
4. Run health checks before generating migrations

## 🎯 Development Priorities

### Always Do First
1. **Read this document** and relevant standards
2. **Use correct Docker commands** (`./dev.sh` not `docker-compose up`)
3. **Follow authentication patterns** (API endpoints only)
4. **Initialize entity IDs** in constructors
5. **Use UTC DateTimes** always

### Never Do
1. **Direct SignInManager** in Blazor components
2. **Navigation properties** to ignored entities  
3. **Direct database access** from Web project
4. **Non-UTC DateTimes** with PostgreSQL
5. **Default docker-compose** commands

## 📝 Contributing to Critical Learnings

When you solve a critical issue:
1. **Add to role-specific lessons** first
2. **Reference from here** if architecture-breaking
3. **Include quick recovery commands**
4. **Link to detailed standards**

Remember: These are learnings that prevent architectural failures and hours of debugging. Treat them as mandatory reading.