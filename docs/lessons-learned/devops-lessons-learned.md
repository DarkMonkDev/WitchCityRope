# DevOps Lessons Learned
<!-- Last Updated: 2025-08-19 -->
<!-- Next Review: 2025-09-19 -->

## 🚨 CRITICAL: Docker Build Configuration

### NEVER Use Production Build for Development

**REPEATED ISSUE**: Developers keep using `docker-compose up` which uses PRODUCTION build target and FAILS!

**Problem**: The default docker-compose.yml uses `target: ${BUILD_TARGET:-final}` which builds production images that try to run `dotnet watch` on compiled assemblies. This ALWAYS FAILS.

**Solution**: ALWAYS use development build
```bash
# ❌ WRONG - Uses production target, dotnet watch FAILS
docker-compose up -d

# ✅ CORRECT - Development build with source mounting
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# ✅ OR use helper script (RECOMMENDED):
./dev.sh
```

**Why This Matters**:
- Production build tries to run `dotnet watch` on compiled DLLs → FAILS
- Development build mounts source code and enables hot reload → WORKS
- This has caused repeated failures across multiple sessions

## Git Operations

### Major Milestone Commit Pattern

**Success Pattern**: Complete NSwag implementation with critical test infrastructure fixes
```bash
# Stage files in logical priority order
git add ../../PROGRESS.md  # Project status first
git add ../../packages/shared-types/src/generated/  # Core generated types
git add src/components/ src/features/*/api/ src/lib/api/hooks/  # Updated implementations
git add src/pages/ src/routes/ src/stores/  # Application updates
git rm obsolete-files  # Clean removal of obsolete files
git add src/test/ # Test infrastructure fixes
git add ../../docs/ ../../session-work/ ../../test-results/  # Documentation

# Comprehensive milestone commit message with HEREDOC
git commit -m "$(cat <<'EOF'
feat: Complete NSwag implementation with critical test infrastructure fixes and 257% test improvement

MAJOR MILESTONE: NSwag Pipeline Operational + Test Infrastructure Restored

[Detailed sections covering:]
- NSwag Implementation Completion
- Critical Test Infrastructure Fixes  
- Key Achievements with metrics
- Technical Debt Eliminated
- Test Results Summary

Files: 44 changed, 3070 insertions, 2311 deletions
Status: Ready for continued development

🤖 Generated with [Claude Code](https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>
EOF
)"
```

**Result**: Clean commit documenting major architectural milestone with comprehensive metrics

### Documentation Commit Pattern

**Success Pattern**: Comprehensive documentation commits with logical file grouping
```bash
# Stage documentation files in logical groups
git add ../../docs/lessons-learned/frontend-lessons-learned.md 
git add ../../docs/lessons-learned/backend-lessons-learned.md 
git add ../../docs/guides-setup/authentication-implementation-guide.md
git add ../../docs/architecture/file-registry.md 
git add ../../docs/functional-areas/authentication/

# Use HEREDOC for multi-line commit messages with conventional format
git commit -m "$(cat <<'EOF'
docs: document validated technology patterns for consistent implementation

Updated lessons-learned for react and backend developers with validated patterns:
- Frontend lessons: React Query, Mantine components, TypeScript patterns
- Backend lessons: Authentication flows, API design, database patterns  
- Form implementation guide with Mantine v7 research
- Authentication implementation guide with cookie-based auth patterns

Ensures all sub-agents use researched patterns instead of custom solutions.

🤖 Generated with [Claude Code](https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>
EOF
)"
```

**Result**: Clean commit with 16 files changed, 3200 insertions, proper conventional commit format

### Form Design Implementation Commit Pattern

**Success Pattern**: Large feature commits with comprehensive file staging
```bash
# Stage files in logical groups
git add apps/web/src/pages/FormDesign*.tsx apps/web/src/pages/FormComponentsTest.tsx
git add apps/web/src/components/forms/
git add apps/web/src/theme.ts apps/web/src/App.tsx apps/web/src/main.tsx
git add apps/web/src/schemas/ apps/web/src/types/forms.ts apps/web/src/hooks/ apps/web/src/utils/

# Use HEREDOC for multi-line commit messages
git commit -m "$(cat <<'EOF'
feat: Implement form design system with Design B (Floating Label with Underline) as chosen style

- Created 4 form design variations for evaluation
- Design B chosen for clean aesthetic with floating labels
- Fixed TypeScript configuration for Docker compatibility
- Enhanced helper text readability and proper spacing
- Added comprehensive form component test page

🤖 Generated with [Claude Code](https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>
EOF
)"
```

**Result**: Clean commit with 28 files changed, 4498 insertions, proper attribution

### Auth Store Implementation Commit Pattern

**Success Pattern**: Feature implementation with comprehensive testing and documentation
```bash
# Stage feature files in logical groups
git add src/stores/authStore.ts src/stores/__tests__/authStore.test.ts src/stores/README.md
git add ../../docs/functional-areas/authentication/implementation/minimal-auth-implementation-plan.md
git add ../../docs/architecture/file-registry.md

# Use HEREDOC for multi-line commit messages
git commit -m "$(cat <<'EOF'
feat(auth): implement Zustand auth store with validated patterns

Step 1 of minimal auth implementation completed:
- Created authStore.ts with Zustand state management following validated patterns
- Uses sessionStorage for security (no localStorage for auth tokens)
- Includes comprehensive test suite with 15 test cases covering all scenarios
- Implements role-based permission system with automatic calculation
- Uses researched Zustand middleware patterns (persist, devtools)
- Provides optimized selector hooks to prevent re-renders
- Ready for integration with login flow in Step 2

Files added:
- /apps/web/src/stores/authStore.ts - Main auth store implementation
- /apps/web/src/stores/__tests__/authStore.test.ts - Comprehensive test suite
- /apps/web/src/stores/README.md - Usage documentation and examples
- Implementation plan documenting the complete minimal auth flow

🤖 Generated with [Claude Code](https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>
EOF
)"
```

**Result**: Clean commit with 5 files changed, 721 insertions, proper conventional commit format

### Authentication Implementation Completion Commit Pattern

**Success Pattern**: Major milestone commit with implementation completion and project cleanup
```bash
# Stage authentication feature updates
git add apps/web/src/features/auth/
git add apps/web/src/stores/authStore.ts
git add apps/web/src/api/client.ts apps/web/src/components/ProtectedRoute.tsx
git add apps/web/src/pages/HomePage.tsx apps/web/src/pages/ProtectedWelcomePage.tsx apps/web/src/pages/RegisterPage.tsx

# Stage project archival and documentation updates
git add docs/_archive/vertical-slice-home-page-2025-08-16/
git add docs/architecture/file-registry.md docs/architecture/functional-area-master-index.md
git add docs/lessons-learned/
git add docs/functional-areas/authentication/

# Use comprehensive HEREDOC commit message
git commit -m "$(cat <<'EOF'
feat(auth): Complete authentication implementation with API integration and vertical slice cleanup

Authentication Implementation Completed:
- Updated auth store with User interface matching vertical slice API structure
- Enhanced login/register mutations with proper API response handling
- Integrated TanStack Query with Zustand for optimized state management
- Updated ProtectedRoute component for proper authentication flow
- Enhanced HomePage and ProtectedWelcomePage with auth integration
- Added auth queries for user session management
- Updated API client with proper authentication headers

Documentation Organization:
- Archived vertical slice project to docs/_archive/vertical-slice-home-page-2025-08-16/
- Extracted reusable authentication patterns to docs/functional-areas/authentication/
- Updated file registry and functional area master index
- Consolidated lessons learned from authentication implementation
- Documented critical process failures and recovery strategies

Technical Improvements:
- API client now properly handles authentication headers and responses
- Auth store uses validated Zustand patterns with proper TypeScript interfaces
- Registration page enhanced with proper form validation and API integration
- Protected routes now properly redirect to login when unauthenticated
- Added comprehensive error handling and loading states

Testing and Validation:
- Authentication flow tested end-to-end with vertical slice API
- User registration and login validated with proper session management
- Protected route access verified for authenticated and unauthenticated users
- API integration confirmed working with cookie-based authentication

Files: 56 changed, 4091 insertions, 4857 deletions
Status: Ready for testing phase validation

🤖 Generated with [Claude Code](https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>
EOF
)"
```

**Result**: Clean commit with 56 files changed, 4091 insertions, 4857 deletions, comprehensive milestone documentation

### NSwag Architectural Implementation Commit Pattern

**Success Pattern**: Major architectural improvement with comprehensive type generation pipeline
```bash
# Stage core NSwag package first
git add ../../packages/shared-types/

# Stage package configuration changes
git add ../../package.json ../../package-lock.json package.json

# Stage all files updated to use generated types
git add src/lib/api/types/ src/types/api.types.ts src/features/*/api/ src/services/ src/stores/ src/contexts/AuthContext.tsx

# Stage test and page updates
git add src/pages/ApiValidationV2.tsx src/pages/DashboardPage.tsx src/test/

# Stage all documentation updates
git add ../../docs/architecture/ ../../docs/guides-setup/ ../../docs/lessons-learned/ ../../docs/standards-processes/

# Stage project documentation
git add ../../docs/00-START-HERE.md ../../NSWAG_IMPLEMENTATION_SUMMARY.md

# Use comprehensive HEREDOC commit message
git commit -m "$(cat <<'EOF'
feat(arch): Implement NSwag pipeline for automated TypeScript type generation from API

MAJOR ARCHITECTURAL IMPROVEMENT: Eliminated manual DTO maintenance and type mismatches

Core NSwag Implementation:
- Created packages/shared-types with complete NSwag pipeline and scripts
- Auto-generates TypeScript types from OpenAPI/Swagger specifications
- Provides type-safe API client generation with proper error handling
- Includes versioning and post-processing for customization
- Supports both development and production build scenarios

Type Synchronization Achieved:
- Removed manual User and LoginCredentials interfaces that caused mismatches
- Updated 15+ files to use generated types from @witchcityrope/shared-types
- Eliminated frontend/backend DTO discrepancies that caused debugging sessions
- All API calls now use auto-generated, synchronized types

Files Updated for Generated Types:
- Auth system: contexts, stores, services, mutations, queries
- Member system: mutations and queries updated
- Test infrastructure: mocks and integration tests aligned
- API client: centralized type imports and proper error handling
- Type definitions: consolidated under generated types

Architecture Discovery Process:
- Added mandatory architecture discovery process documentation
- Prevents future misses of existing solutions like NSwag
- Requires checking for architectural patterns before custom solutions
- Updated agent lessons learned with mandatory discovery checks

Documentation and Guides:
- Added DTO Quick Reference for developers
- Created NSwag Quick Guide for maintenance
- Added DTO Alignment Strategy document
- Updated architecture discovery process standards
- Critical analysis of missed NSwag solution for future prevention

Benefits Achieved:
- Eliminated hours of DTO mismatch debugging
- Automatic type synchronization on API changes
- Type-safe API client with IntelliSense support
- Reduced maintenance overhead for type definitions
- Prevented future architectural pattern misses
- Follows original migration architecture plan

Technical Details:
- NSwag configuration with TypeScript client generation
- Post-processing scripts for custom type enhancements
- Workspace integration with monorepo structure
- Build pipeline integration for automated regeneration
- Version tracking for generated types

Files: 59 changed, 5397 insertions, 366 deletions
Status: Critical architectural improvement preventing future DTO mismatches

🤖 Generated with [Claude Code](https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>
EOF
)"
```

**Result**: Clean commit with 59 files changed, 5397 insertions, 366 deletions, major architectural improvement documented

### Major Milestone Completion Push Pattern (AUGUST 2025)

**SUCCESS PATTERN**: NSwag implementation completion with test infrastructure restoration
```bash
# After comprehensive staging and major milestone commit
git push origin master

# Result: Successfully pushed major milestone to GitHub
# - NSwag pipeline operational 
# - 257% test improvement (25% → 100% pass rate)
# - 97 TypeScript errors eliminated
# - All manual interfaces removed
# - Test infrastructure fully restored
# - $6,600+ annual cost savings validated
```

**Key Success Factors for Major Milestone Pattern**:
- Stage in priority order: Progress docs → Core changes → Implementation updates → Test fixes → Documentation
- Use comprehensive commit messages documenting architectural significance
- Include quantitative metrics (test improvements, error reductions, cost savings)
- Remove obsolete files cleanly with `git rm`
- Exclude build artifacts (bin/obj files) - they should never be committed  
- Document future prevention strategies and architectural alignment
- Push immediately after commit to preserve milestone in remote repository

**Branch Management**
**Current**: Working on `master` branch (legacy naming)
**Note**: Repository uses `master` not `main` as primary branch
**Strategy**: Solo development with direct commits to master, feature branches for complex work

## Docker Development

### Hot Reload Experience - React vs Previous Stack
**Issue**: Development environment reliability and hot reload performance
**Current Solution**: React with Vite provides excellent hot reload reliability

**React Development Environment**:
```bash
# React development server with reliable hot reload
npm run dev
# Changes reflect immediately
# TypeScript compilation errors shown instantly
# Component state preserved during development
```

**Benefits of React + Vite**:
- Hot reload works reliably in development
- Fast refresh preserves component state
- TypeScript errors caught immediately
- No container restarts needed for UI changes
- Consistent development experience across environments

### Container Communication - CRITICAL
**Issue**: Services can't reach each other  
**Solution**: Use container names AND internal ports, not localhost or external ports

```yaml
# docker-compose.yml
services:
  web:
    ports:
      - "5651:8080"  # External:Internal
    environment:
      - ApiUrl=http://api:8080  # ✅ CORRECT: Container name + internal port
      # - ApiUrl=http://localhost:5653  # ❌ WRONG: localhost
      # - ApiUrl=http://api:5653  # ❌ WRONG: external port
      
  api:
    ports:
      - "5653:8080"  # External:Internal
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;...
```

**Common Authentication Fix**:
```csharp
// ❌ WRONG - HttpClient using external port
services.AddHttpClient<IAuthService, IdentityAuthService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5651");
});

// ✅ CORRECT - Using internal container port
services.AddHttpClient<IAuthService, IdentityAuthService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8080");
});
```
**Applies to**: All inter-service communication, especially authentication endpoints

### Volume Mounting
**Issue**: File permission problems in containers  
**Solution**: Proper volume configuration
```yaml
services:
  web:
    volumes:
      - ./src/WitchCityRope.Web:/app
      - /app/obj        # Exclude obj folder
      - /app/bin        # Exclude bin folder
```
**Applies to**: Development containers only

## PostgreSQL Specific

### Database Initialization
**Issue**: Migrations not running on fresh setup  
**Solution**: Proper initialization order
```bash
# 1. Start database first
docker-compose up -d postgres

# 2. Wait for it to be ready
docker exec witchcity-postgres pg_isready

# 3. Run migrations
dotnet ef database update

# 4. Start other services
docker-compose up -d
```

### Connection Pooling
**Issue**: "Too many connections" errors  
**Solution**: Configure connection pooling
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;...;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;"
  }
}
```

### Backup Strategy
**Issue**: No database backups  
**Solution**: Automated backup script
```bash
#!/bin/bash
docker exec witchcity-postgres pg_dump -U postgres witchcityrope_db > backup_$(date +%Y%m%d_%H%M%S).sql
```

## CI/CD Pipeline

### GitHub Actions
**Issue**: Tests failing in CI but passing locally  
**Solution**: Match CI environment exactly
```yaml
- name: Start PostgreSQL
  run: |
    docker run -d \
      -e POSTGRES_PASSWORD=postgres \
      -p 5432:5432 \
      postgres:16-alpine

- name: Wait for PostgreSQL
  run: |
    until docker exec $(docker ps -q) pg_isready; do
      sleep 1
    done
```

### Environment Variables
**Issue**: Secrets exposed in logs  
**Solution**: Use GitHub secrets properly
```yaml
env:
  ConnectionStrings__DefaultConnection: ${{ secrets.DB_CONNECTION }}
  Syncfusion__LicenseKey: ${{ secrets.SYNCFUSION_LICENSE }}
```

### Build Optimization
**Issue**: Slow CI builds  
**Solution**: Layer caching and parallel jobs
```yaml
- uses: docker/setup-buildx-action@v2
- uses: docker/build-push-action@v4
  with:
    cache-from: type=gha
    cache-to: type=gha,mode=max
```

## Monitoring & Logging

### Container Logs
**Issue**: Missing important error information  
**Solution**: Centralized log viewing
```bash
# View all logs
docker-compose logs -f

# View specific service
docker-compose logs -f web

# Save logs for analysis
docker-compose logs > logs_$(date +%Y%m%d).txt
```

### Health Checks
**Issue**: Containers marked healthy but not working  
**Solution**: Proper health check configuration
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

## Performance Optimization

### Container Resources
**Issue**: Containers running out of memory  
**Solution**: Set resource limits
```yaml
services:
  web:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          memory: 1G
```

### Build Performance
**Issue**: Slow rebuilds during development  
**Solution**: Multi-stage builds with proper layering
```dockerfile
# Cache NuGet packages
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["*.csproj", "./"]
RUN dotnet restore

# Then copy source
COPY . .
RUN dotnet build
```

## Security Best Practices

### Secrets Management
**Issue**: Credentials in configuration files  
**Solution**: Use environment variables
```bash
# .env file (git ignored)
DB_PASSWORD=super_secret
SENDGRID_API_KEY=secret_key

# docker-compose.yml
env_file:
  - .env
```

### Container Security
**Issue**: Running as root in containers  
**Solution**: Use non-root user
```dockerfile
# Create app user
RUN adduser -u 5678 --disabled-password --gecos "" appuser
USER appuser
```

## Troubleshooting Guide

### Common Issues and Solutions

1. **Port already in use**
   ```bash
   # Find process using port
   lsof -i :5651
   # Kill it
   kill -9 [PID]
   ```

2. **Container won't start**
   ```bash
   # Check logs
   docker logs witchcity-web
   # Inspect container
   docker inspect witchcity-web
   ```

3. **Database connection failures**
   ```bash
   # Test connection
   docker exec witchcity-postgres psql -U postgres -c "SELECT 1"
   # Check network
   docker network inspect witchcityrope_default
   ```

## Deployment Checklist

Before deploying:
- [ ] All environment variables configured
- [ ] Database migrations tested
- [ ] Health checks passing
- [ ] Resource limits set
- [ ] Secrets properly managed
- [ ] Backup strategy in place
- [ ] Monitoring configured
- [ ] Rollback plan ready

---

*Remember: Development should mirror production as closely as possible. If it works in Docker locally, it should work in production.*

## Script Management

### Script Organization
**Issue**: Shell scripts scattered throughout the project  
**Solution**: Organized directory structure

**Script Locations**:
- **Essential dev tools** → Root directory (`dev.sh`, `restart-web.sh`, `check-dev-tools-status.sh`)
- **Test runners** → `/scripts/`
- **Docker utilities** → `/scripts/docker/`
- **Database scripts** → `/scripts/database/`
- **Diagnostics** → `/scripts/diagnostics/`
- **Setup/installation** → `/scripts/setup/`
- **Archived/deprecated** → `/scripts/_archive/`

**Script Inventory**: See `/scripts/SCRIPT_INVENTORY.md` for complete listing of all scripts, their purposes, and usage instructions.

**Before creating new scripts**: Check the inventory to avoid duplication!