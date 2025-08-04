# Claude Code Project Configuration

> 🏗️ **CRITICAL: READ ARCHITECTURE DOCUMENTATION FIRST!** 🏗️
> 
> **BEFORE MAKING ANY CHANGES, READ:** [ARCHITECTURE.md](./ARCHITECTURE.md)
> 
> **This project uses a Web+API microservices architecture:**
> - 🌐 **Web Service** (Blazor Server) - UI and authentication at http://localhost:5651
> - 🔌 **API Service** (Minimal API) - Business logic and data at http://localhost:5653  
> - 🗄️ **Database** (PostgreSQL) - Shared data store at localhost:5433
> 
> **⚠️ COMMON MISTAKES THAT BREAK THE APPLICATION:**
> - ❌ Web service bypassing API for business data
> - ❌ Wrong port configurations or service communication
> - ❌ Violating service boundaries (Web accessing business entities directly)
> - ❌ Using production Docker build instead of development build
> 
> **✅ BEFORE YOU START:**
> 1. Read the full architecture guide: [ARCHITECTURE.md](./ARCHITECTURE.md)
> 2. Understand the Web→API communication pattern
> 3. Use `./dev.sh` for all development operations
> 4. Respect service boundaries (Web handles UI/Auth, API handles business logic)

> 🚨 **CRITICAL: PURE BLAZOR SERVER APPLICATION** 🚨
> 
> **THIS IS NOW A PURE BLAZOR SERVER APPLICATION - NO RAZOR PAGES!**
> 
> **NEVER CREATE RAZOR PAGES (.cshtml) AGAIN:**
> - ❌ NO `_Host.cshtml`, `_Layout.cshtml`, or any Razor Pages
> - ❌ NO `AddRazorPages()` or `MapRazorPages()`
> - ❌ NO hybrid Razor Pages + Blazor architecture
> - ✅ USE `App.razor` as the root HTML document
> - ✅ ALL pages must be Blazor components (.razor files)
> - ✅ USE `@rendermode="InteractiveServer"` for interactive pages
> - ✅ USE pure Blazor Server with `MapRazorComponents<App>()`
> 
> **Why this matters:**
> - Hybrid architecture caused render mode conflicts → ELIMINATED
> - Razor Pages caused layout/CSS issues → FIXED
> - Interactive modes now work properly → WORKING
> - Authentication and antiforgery tokens integrated → SECURED

> 🚨 **CRITICAL: BLAZOR SERVER AUTHENTICATION PATTERN** 🚨
> 
> **THIS PROJECT USES ASP.NET CORE IDENTITY WITH A SPECIFIC PATTERN FOR BLAZOR SERVER**
> 
> **The Authorization Migration Discovery (January 22, 2025):**
> - ❌ **NEVER** use SignInManager directly in Blazor components - causes "Headers are read-only" errors
> - ❌ **NEVER** try to set authentication cookies from within interactive components
> - ✅ **ALWAYS** use API endpoints for authentication operations (login, logout, register)
> - ✅ **PATTERN**: Blazor components → API endpoints → SignInManager → Cookies
> 
> **What We Discovered:**
> - Previous developer tried to use SignInManager directly in Blazor components → FAILED
> - Microsoft's official guidance: SignInManager operations MUST happen outside Blazor's rendering context
> - Solution: Created `/auth/login`, `/auth/logout`, `/auth/register` API endpoints
> - These endpoints handle SignInManager operations and cookie setting properly
> 
> **Implementation Details:**
> - `AuthEndpoints.cs` - Minimal API endpoints for authentication
> - `IdentityAuthService.cs` - Calls API endpoints via HttpClient
> - HttpClient configured to use internal container port (8080)
> - Login form submits to API endpoint, not directly to SignInManager
> 
> **Why This Matters:**
> - Hours were spent trying different approaches that all failed
> - This is the ONLY Microsoft-approved pattern for pure Blazor Server
> - Without this pattern, authentication will NEVER work properly
> - Previous attempts to "fix" this broke Docker containers and caused days of debugging

> 🚨 **CRITICAL: PLAYWRIGHT E2E TESTING ONLY** 🚨
> 
> **ALL E2E TESTS HAVE BEEN MIGRATED TO PLAYWRIGHT - NO PUPPETEER ALLOWED!**
> 
> **NEVER CREATE PUPPETEER TESTS AGAIN:**
> - ❌ NO `const puppeteer = require('puppeteer')` anywhere
> - ❌ NO new Puppeteer test files in any directory
> - ❌ DO NOT debug or modify existing Puppeteer tests in `/tests/e2e/` or `/ToBeDeleted/`
> - ✅ ALL E2E tests are in `/tests/playwright/` directory
> - ✅ USE Playwright TypeScript tests only: `import { test, expect } from '@playwright/test'`
> - ✅ USE existing Page Object Models in `/tests/playwright/pages/`
> - ✅ RUN tests with: `npm run test:e2e:playwright`
> 
> **Why this matters:**
> - 180 Puppeteer tests were successfully migrated to Playwright in January 2025
> - Playwright tests are 40% faster and 86% less flaky
> - All documentation and patterns exist for Playwright
> - Puppeteer tests are deprecated and will be deleted
> 
> **📂 E2E Test Locations:**
> - ✅ **ACTIVE TESTS**: `/tests/playwright/` (20 test files, 8 Page Objects, 6 helpers)
> - ❌ **DEPRECATED**: `/tests/e2e/` (old Puppeteer tests - DO NOT USE)
> - ❌ **DEPRECATED**: `/ToBeDeleted/` (old Puppeteer tests - DO NOT USE)

> 🚨 **CRITICAL: ALWAYS USE DEVELOPMENT DOCKER BUILD** 🚨
> 
> **NEVER run `docker-compose up` directly!** This uses production build and WILL FAIL.
> 
> **ALWAYS use development build:**
> ```bash
> # CORRECT: Development build with hot reload
> docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
> 
> # OR use the helper script:
> ./dev.sh
> ```
> 
> **Why this matters:**
> - Production build (`docker-compose up`) tries to run `dotnet watch` on compiled assemblies → FAILS
> - Development build mounts source code and enables hot reload → WORKS
> - This has caused repeated failures - ALWAYS use dev build!

> ⚠️ **IMPORTANT FOR ALL CLAUDE AI SESSIONS**: This project uses Docker for development. Hot reload has known issues. 
> **MUST READ**: [DOCKER_DEV_GUIDE.md](./DOCKER_DEV_GUIDE.md) for development environment setup and troubleshooting.
> Use `./dev.sh` for all Docker operations and `./restart-web.sh` when hot reload fails.

> 📚 **CRITICAL: NEW DOCUMENTATION STRUCTURE (August 2025)** 📚
> 
> **BEFORE STARTING ANY WORK, YOU MUST:**
> 1. **Read Documentation Navigation**: [/docs/00-START-HERE.md](./docs/00-START-HERE.md) - Complete guide to finding documentation
> 2. **Review Current Project Status**: [PROGRESS.md](./PROGRESS.md) - Authoritative source for project state
> 3. **Check Functional Areas**: [/docs/functional-areas/](./docs/functional-areas/) - Feature-specific documentation
> 4. **Follow Standards**: [/docs/standards-processes/](./docs/standards-processes/) - Development and documentation standards
> 
> **Documentation Organization:**
> - **Single Source of Truth**: Each piece of information exists in exactly ONE place
> - **No Duplication**: This file contains ONLY Claude-specific configuration
> - **Authoritative Sources**:
>   - Project Status → PROGRESS.md
>   - Test Information → /docs/standards-processes/testing/TEST_CATALOG.md
>   - Architecture → ARCHITECTURE.md and /docs/architecture/
>   - Features → /docs/functional-areas/[feature-name]/
>   - Lessons Learned → /docs/lessons-learned/[role].md

## Project Overview
WitchCityRope is a Blazor Server application for a rope bondage community in Salem, offering workshops, performances, and social events.

## 🚀 Quick Start Guide for Claude Code Sessions

### Essential First Steps
1. **📚 REVIEW DOCUMENTATION**: Start with [/docs/00-START-HERE.md](./docs/00-START-HERE.md) for navigation
2. **📊 CHECK PROJECT STATUS**: Read [PROGRESS.md](./PROGRESS.md) for current state and test results
3. **🏗️ UNDERSTAND ARCHITECTURE**: Study [ARCHITECTURE.md](./ARCHITECTURE.md) for Web+API pattern
4. **🚨 USE DEVELOPMENT DOCKER**: Run `./dev.sh` or `docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d`
5. **🔧 DOCKER TROUBLESHOOTING**: See [DOCKER_DEV_GUIDE.md](./DOCKER_DEV_GUIDE.md) for hot reload issues
6. **✅ FOLLOW STANDARDS**: Check [/docs/standards-processes/](./docs/standards-processes/) for guidelines
7. **📝 USE TODOWRITE**: Track multi-step tasks with TodoWrite tool
8. **🔍 USE CONTEXT7**: Add "use context7" for current library documentation

### Architecture Quick Reference
- **Web Service**: Blazor Server UI at `http://localhost:5651` (handles authentication & UI)
- **API Service**: Minimal API at `http://localhost:5653` (handles business logic & data)
- **Database**: PostgreSQL at `localhost:5433` (shared by both services)
- **Key Pattern**: Web → HTTP calls → API → Database (NEVER Web → Database for business data)
- **Start Command**: `./dev.sh` (option 1) or `docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d`

### Current Project State
For detailed project status, test results, and feature implementation progress, see [PROGRESS.md](./PROGRESS.md)
- ✅ **E2E Test Migration**: Successfully migrated all 180 Puppeteer tests to Playwright
- ⚠️ **Admin Role Authorization**: Known issue with role assignment - workaround available
- 🔄 **Test Suite Status**: Unit tests passing, integration tests mostly passing, E2E tests working
- ✅ **E2E Tests**: Updated and functional with pure Blazor Server architecture
- ✅ **Authentication State Management**: Fixed inconsistencies, removed duplicate logic, standardized on AuthorizeView
- ⚠️ **Infrastructure/API Tests**: Have compilation issues due to architectural changes (separate from Core domain)
- 🔄 **Active Development**: Pure Blazor Server with interactive components

### 🚨 MUST READ: Integration Test Changes (Updated: July 18, 2025)

**CRITICAL**: Integration tests now use real PostgreSQL with comprehensive health checks. The in-memory database was hiding bugs. Always check this section before running or writing integration tests.

#### Key Changes:
1. **Real PostgreSQL via Testcontainers** - No more in-memory database
2. **All DateTime must be UTC** - PostgreSQL enforces this strictly
3. **Test data must be unique** - Use GUIDs for all names/emails
4. **Health Check System** - Validates database readiness before tests (NEW)
5. **New test pattern required** - See examples below

#### 🔥 **NEW: Health Check System (July 18, 2025)**

**ALWAYS run health checks before attempting integration tests:**

```bash
# 1. FIRST: Run health checks to verify containers are ready
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# 2. ONLY IF health checks pass: Run integration tests
dotnet test tests/WitchCityRope.IntegrationTests/
```

**Health Check System validates:**
- ✅ Database connection (PostgreSQL container is running)
- ✅ Database schema (all required tables and migrations applied)
- ✅ Seed data (test users and roles exist)

**If health checks fail:**
- Check Docker containers: `docker ps`
- Check database connection: `docker exec witchcity-postgres psql -U postgres -d witchcityrope_db -c "SELECT 1;"`
- Check applied migrations: `docker exec witchcity-postgres psql -U postgres -d witchcityrope_db -c "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' OR table_schema = 'auth';"`

#### Quick Start for Integration Tests:
```bash
# Ensure Docker is running (REQUIRED)
sudo systemctl start docker

# FIRST: Run health checks
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# ONLY IF health checks pass: Run integration tests
dotnet test tests/WitchCityRope.IntegrationTests/

# If tests fail with container issues
docker ps -a | grep testcontainers | awk '{print $1}' | xargs -r docker rm -f
```

### 🔥 **CRITICAL ARCHITECTURAL CHANGE (July 16, 2025) - PURE BLAZOR SERVER**

#### **CONVERTED FROM HYBRID TO PURE BLAZOR SERVER**
**This application is now a PURE BLAZOR SERVER application. NO RAZOR PAGES ANYWHERE.**

**❌ NEVER CREATE THESE AGAIN:**
- Razor Pages (`.cshtml` files in `/Pages/` folder)
- `_Host.cshtml`, `_Layout.cshtml`, or any Razor Pages
- `AddRazorPages()` service registration
- `MapRazorPages()` endpoint mapping
- Hybrid Razor Pages + Blazor architecture

**✅ ALWAYS USE THESE INSTEAD:**
- Blazor components (`.razor` files in `/Components/Pages/` or `/Features/`)
- `App.razor` as the root HTML document (contains DOCTYPE, head, body)
- `@rendermode="InteractiveServer"` for interactive pages requiring user input
- Pure Blazor Server with `MapRazorComponents<App>().AddInteractiveServerRenderMode()`
- `.NET 9 Blazor Server patterns` with proper antiforgery token integration

**🚨 RENDER MODE REQUIREMENTS:**
- **Interactive pages** (forms, buttons, user input): MUST use `@rendermode="InteractiveServer"`
- **Static pages** (content display only): Can use default server-side rendering
- **Authentication pages**: Already configured with proper interactive modes

**✅ CURRENT ARCHITECTURE:**
```csharp
// Program.cs - CORRECT Pure Blazor Server Configuration
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

app.MapRazorComponents<WitchCityRope.Web.App>()
    .AddInteractiveServerRenderMode();
```

**✅ WORKING VERIFICATION:**
- HTML Structure: ✅ Complete with head, title, CSS, scripts (20 head children)
- Layout System: ✅ Working properly (no more hybrid issues)
- Blazor Server: ✅ Fully functional with interactive components
- Authentication: ✅ Login working with 302 redirect (fixed antiforgery)
- Antiforgery Tokens: ✅ Automatic handling - DO NOT manually add `<AntiforgeryToken />`
- Admin Dashboard: ✅ Full access to events, users, reports
- CSS Loading: ✅ 11 stylesheets including Syncfusion, WCR theme
- JavaScript: ✅ Blazor Server and Syncfusion scripts loaded

**🚨 ANTIFORGERY TOKEN RULE:**
- ❌ NEVER add `<AntiforgeryToken />` to EditForm components
- ✅ Framework handles automatically with FormName + SupplyParameterFromForm
- ✅ Use proper middleware order: UseAuthentication → UseAuthorization → UseAntiforgery

---

### 🔮 **FUTURE AUTHENTICATION ENHANCEMENTS (July 16, 2025)**

**Authentication Migration Analysis Completed**: After comprehensive analysis, migration to .NET 9 Blazor interactive authentication is **NOT RECOMMENDED** for this pure Blazor Server application. See `/docs/enhancements/authentication-enhancements/README.md` for detailed analysis and high-value, low-effort enhancement opportunities including:

- **Two-Factor Authentication (2FA)**: Infrastructure exists but disabled (2-3 days to enable)
- **Custom Claims System**: Infrastructure exists but disabled (1-2 days to enable)  
- **Social Login Enhancements**: Build on existing Google OAuth foundation
- **Advanced Security Features**: Audit logging, password breach detection, session management

**Key Finding**: Current ASP.NET Core Identity implementation is production-ready and well-architected for Blazor Server. Focus on feature enhancements rather than architectural migration.

---

### 🚨 **CRITICAL LESSONS LEARNED: Authentication & Container Issues (July 16, 2025)**

**This section documents critical discoveries from authentication modernization that future developers MUST know to avoid repeating the same mistakes.**

#### **1. DOCKER HOT RELOAD IS UNRELIABLE - ALWAYS RESTART CONTAINERS**
**PROBLEM**: Made authentication fixes but user reported "nothing has changed" - changes weren't being picked up by Docker hot reload.
**SOLUTION**: `docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart web`
**LESSON**: When user reports changes aren't working, FIRST restart containers before assuming code issues.
**WHY**: .NET 9 Blazor Server hot reload in Docker containers is notoriously unreliable, especially for authentication/layout changes.

#### **2. .NET 9 RENDER MODE SYNTAX CHANGE**
**PROBLEM**: Used old `@rendermode InteractiveServer` syntax causing compilation errors.
**CORRECT SYNTAX**: `@rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())`
**CRITICAL**: Layout components (MainLayout) CANNOT have interactive render modes - they receive RenderFragment parameters that cannot be serialized.
**SOLUTION**: Move interactive behavior to separate components, embed in layout.

#### **3. AUTHENTICATION STATE INCONSISTENCY PATTERNS**
**PROBLEM**: "My Dashboard" showing for unauthenticated users while login button also showing.
**ROOT CAUSE**: Duplicate authentication logic in MainLayout AND UserMenuComponent competing with each other.
**SYMPTOMS**:
- Different UI elements showing conflicting authentication states
- Race conditions between custom auth service and built-in AuthorizeView
- Stale authentication state after login/logout

**SOLUTION PATTERN**:
```csharp
// ❌ BAD: Multiple auth state sources
private UserDto? _currentUser; // in MainLayout
@if (_currentUser != null) { ... }
// AND separate UserMenuComponent with its own auth logic

// ✅ GOOD: Single source of truth
<AuthorizeView>
  <Authorized>My Dashboard</Authorized>
</AuthorizeView>
// Use AuthorizeView consistently everywhere
```

#### **4. AUTHENTICATION SYSTEM IDENTIFICATION**
**DISCOVERY**: This project uses **ASP.NET Core Identity** (traditional), NOT the latest Blazor Server interactive authentication.
**EVIDENCE**:
- `AddIdentityCore<WitchCityRopeUser>()` + `AddIdentityCookies()`
- Cookie-based authentication with `IdentityConstants.ApplicationScheme`
- Custom `IAuthService` and `SimplifiedIdentityAuthService`
- Manual state management vs. built-in Blazor auth components

**IMPLICATION**: Don't try to modernize to Blazor Server interactive auth patterns - this project correctly uses ASP.NET Core Identity.

#### **5. COMPILATION ERROR DEBUGGING PATTERN**
**PROBLEM**: Application compiled with warnings but failed at runtime with serialization errors.
**DEBUGGING STEPS**:
1. Check container logs for "Waiting for a file to change" (means compilation succeeded)
2. Test with `curl -s -w "%{http_code}" http://localhost:5651 -o /dev/null`
3. If 500 error, check actual error: `curl -s http://localhost:5651 2>&1 | head -10`
4. If container restart needed: `docker-compose restart web`
5. If still fails: `docker exec witchcity-web dotnet build --verbosity minimal`

#### **6. AUTHENTICATION MODERNIZATION CHECKLIST**
When fixing auth state inconsistencies:
- [ ] Remove duplicate auth logic from layouts
- [ ] Standardize on `AuthorizeView` components
- [ ] Fix .NET 9 render mode syntax
- [ ] Move interactive behavior out of layout components
- [ ] Restart containers to ensure changes are picked up
- [ ] Test authentication state consistency across all UI elements
- [ ] Verify mobile menu authentication state matches desktop

#### **7. CONTAINER RESTART TRIGGERS**
Always restart containers after:
- Authentication/authorization changes
- Layout component modifications  
- Render mode changes
- Dependency injection modifications
- Route configuration changes

**COMMAND**: `docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart web`

---

### 🎯 LATEST DEVELOPMENT SESSION (July 18, 2025) - MAJOR MILESTONES COMPLETED

**ALL PRIORITY TASKS COMPLETED ✅**

**Session Achievement**: Successfully completed all three major priority tasks that were blocking development:

1. **✅ PRIORITY 1 - Integration Test Infrastructure (FIXED)**
   - **Problem**: 139 integration tests failing due to container setup issues
   - **Solution**: Created comprehensive health check system and fixed database initialization
   - **Result**: 115+ tests now passing, infrastructure completely stable
   - **Key Fix**: Fixed Event entity date validation for test scenarios with `Event.CreateForTesting()`

2. **✅ PRIORITY 3 - Event Management Validation (COMPLETE)**
   - **Problem**: Inconsistent validation between API and UI layers causing compilation errors
   - **Solution**: Created centralized validation system with shared constants and custom attributes
   - **Result**: Complete validation consistency, 0 compilation errors, enhanced user experience
   - **Key Components**: EventValidationConstants, EventValidationAttributes, EventValidationService

3. **✅ Navigation Route Issues (ALL FIXED)**
   - **Problem**: 22 integration tests failing due to missing navigation routes
   - **Solution**: Created all missing pages following proper Blazor Server architecture
   - **Result**: All navigation working, 0 routing errors, comprehensive page coverage

4. **✅ E2E Test Verification (VALIDATED)**
   - **Achievement**: Successfully ran and verified 27 well-documented E2E tests
   - **Result**: 10/12 validation tests passing, application functionality confirmed
   - **Status**: Infrastructure and core features verified in real browser environment

**📊 Current Application Health**: Production-ready with excellent foundations
- Build: ✅ 0 errors | Core Tests: ✅ 99.5% | Integration: ✅ 115+ passing | E2E: ✅ 83% passing

**📋 Remaining Items**: Minor polish for optimal development experience (see CURRENT_ISSUES_AND_SOLUTIONS.md)

### 🔥 Recent Fixes (January 22, 2025) - Session 2

#### Authentication State Propagation Fixed
- **Issue**: After implementing Razor Pages for authentication, Blazor components weren't updating with auth state
- **Solution**: Properly registered `AuthenticationStateProvider` in Program.cs
- **Key Changes**:
  - MainLayout now uses `AuthorizeView` components instead of custom auth service
  - Removed duplicate authentication state management
  - Authentication state now properly flows from Razor Pages login to Blazor components

#### Event Seeding Fixed
- **Issue**: Events page showed "No events found" despite seeding code existing
- **Root Cause**: Event seeding was commented out in DbInitializer
- **Solution**:
  - Updated Event constructor calls to match current signature
  - Fixed Currency.USD → "USD" string format
  - Fixed user email references (teacher@ → organizer@)
  - Events now properly display on the events page

#### User Dropdown Menu Fixed
- **Issue**: User menu dropdown wouldn't open when clicked
- **Root Cause**: Blazor event handlers weren't binding due to AuthorizeView re-rendering
- **Solution**: Replaced complex JavaScript/Blazor event handling with native HTML `<details>/<summary>` elements
- **Benefits**:
  - No dependency on JavaScript initialization
  - Works reliably with pure HTML/CSS
  - Properly shows user info and menu options
  - Click-outside behavior to close dropdown

### 🔥 Recent Fixes (January 22, 2025) - Session 1

#### Authorization Migration Fix - Blazor Server Authentication Pattern (January 22, 2025)
- **Critical Issue**: "Headers are read-only, response has already started" error when using SignInManager in Blazor components
- **Root Cause**: SignInManager cannot be used directly in Blazor Server interactive components
- **Microsoft's Solution**: Use API endpoints for all authentication operations
- **What Was Done**:
  - Created `AuthEndpoints.cs` with `/auth/login`, `/auth/logout`, `/auth/register` endpoints
  - Modified `IdentityAuthService.cs` to call API endpoints instead of SignInManager
  - Fixed HttpClient to use internal container port (8080) instead of external (5651)
  - Authentication now follows pattern: Blazor → API → SignInManager → Cookies
- **Key Learning**: This is the ONLY way authentication works in pure Blazor Server applications
- **Documentation**: See `/docs/AUTHORIZATION_MIGRATION.md` for complete details

### 🔥 Previous Fixes (January 15, 2025)

#### Integration Test Infrastructure Migration to PostgreSQL (January 15, 2025) - COMPLETED
- **Task**: Fix database provider conflicts and migrate to real PostgreSQL for integration testing
- **Initial Problem**: "Services for database providers 'Npgsql.EntityFrameworkCore.PostgreSQL', 'Microsoft.EntityFrameworkCore.InMemory' have been registered"
- **What Was Done**:
  - Migrated from in-memory database to PostgreSQL using Testcontainers
  - Fixed all database-related bugs that were hidden by in-memory provider
  - Updated all test classes to use shared PostgreSQL fixture pattern
  - Fixed all DateTime UTC issues for PostgreSQL compatibility
  - Fixed all duplicate test data issues (SceneName, Email)
- **Critical Bugs Discovered and Fixed**:
  1. **RefundAmount nullable owned entity bug** - EF Core can't configure nullable owned entities
     - Solution: Restructured to use separate nullable properties (RefundAmountValue, RefundCurrency)
  2. **RSVP duplicate key violations** - Missing Id initialization in constructor
     - Solution: Added `Id = Guid.NewGuid()` to RSVP constructor
  3. **DateTime UTC requirements** - PostgreSQL timestamp with time zone only accepts UTC
     - Solution: Added automatic UTC conversion in DbContext.UpdateAuditFields()
  4. **Duplicate test data** - Tests failing due to unique constraint violations
     - Solution: All test data now uses GUIDs for uniqueness
- **Test Results**:
  - Before: 93 tests couldn't run due to architectural issues
  - After migration: 37 failures (database issues exposed)
  - Final: 115 passing, 18 failing (UI/navigation tests only)
- **Key Files Updated**:
  - `/tests/WitchCityRope.IntegrationTests/TestWebApplicationFactory.cs` - PostgreSQL container setup
  - `/tests/WitchCityRope.IntegrationTests/PostgreSqlFixture.cs` - Shared container fixture
  - `/src/WitchCityRope.Infrastructure/Data/WitchCityRopeIdentityDbContext.cs` - UTC conversion
  - `/src/WitchCityRope.Core/Entities/Payment.cs` - RefundAmount restructuring
  - `/src/WitchCityRope.Core/Entities/Rsvp.cs` - Id initialization fix

#### Complete Form Migration to WCR Components (January 15, 2025)
- **Task**: Achieve 100% form migration coverage using standardized WCR validation components
- **What Was Done**:
  - Migrated ALL remaining forms from various input types to WCR components
  - Made WcrInputNumber and WcrInputDate generic to handle type mismatches
  - Created EmailSenderAdapter to implement IEmailSender for Identity
  - Removed entire Areas/Identity folder and migrated to Blazor components
  - Updated all routes from /Identity/Account/* to clean URLs (/login, /register, etc.)
  - Created missing pages (ConfirmEmail, Logout)
  - REMOVED ALL MUDBLAZOR references per user requirement
  - Fixed Integration and E2E test compilation errors
- **Key Technical Changes**:
  - EventCallback type fixes in validation components using @typeparam
  - WitchCityRopeUser constructor changed from protected to public
  - Registration entity refactored to Ticket throughout tests
  - Money.Create parameter order fixed (amount first, then currency)
  - Updated async assertion syntax in E2E tests
- **Final Status**:
  - Web Application: Builds with 0 errors
  - Core Tests: 202 passed, 0 failed, 1 skipped
  - 100% form migration coverage achieved
  - Complete consistency across entire application
- **Key Files**:
  - `/src/WitchCityRope.Web/Components/Validation/WcrInput*.razor` - All validation components
  - `/src/WitchCityRope.Infrastructure/Services/EmailSenderAdapter.cs` - New adapter
  - `/docs/FINAL_MIGRATION_REPORT.md` - Complete migration documentation
- **User Feedback**: "I want a consistent way of doing things everywhere" - ACHIEVED

### 🔥 Previous Fixes (January 11, 2025)

#### Member RSVP Flow Testing (January 11, 2025 - Session 4)
- **Task**: Create and test complete member RSVP flow functionality
- **What Was Done**:
  - Created comprehensive E2E test suite for RSVP functionality
  - Fixed date formatting in tests (manual formatting instead of toISOString)
  - Fixed member routes (/events → /member/events)
  - Created diagnostic tools for debugging form issues
  - Updated TEST_REGISTRY.md with all 12 E2E tests
- **Issues Found**:
  - Event creation form not submitting (stays on /admin/events/new)
  - No visible error messages when form fails
  - JavaScript errors preventing proper form submission
  - Events not appearing in lists after creation
- **Key Files**:
  - `/tests/e2e/test-member-rsvp-flow.js` - Main RSVP test
  - `/tests/e2e/test-member-rsvp-api.js` - API-based test
  - `/tests/e2e/diagnose-event-form.js` - Form diagnostic tool
- **Key Learnings**:
  - Blazor forms need proper error display for debugging
  - Silent failures make testing difficult
  - API-based tests can bypass UI issues for validation

#### Test Compilation Fixes (January 11, 2025 - Session 3)
- **Task**: Fix remaining test compilation errors after Registration → Ticket refactoring
- **What Was Done**:
  - Renamed RegistrationTests.cs to TicketTests.cs and updated all references
  - Fixed Ticket constructor calls to include emergency contact parameters
  - Updated test expectations (eventToAttend → eventToRegister)
  - Fixed EventTests.cs and UserTests.cs navigation property references
  - Updated E2E test selectors and Puppeteer compatibility issues
- **Key Files**:
  - `/tests/WitchCityRope.Core.Tests/Entities/TicketTests.cs` - Complete refactoring
  - `/tests/e2e/test-rsvp-functionality.js` - Selector and compatibility fixes
- **Result**: All Core tests passing (202/203), E2E tests updated
- **Key Learnings**:
  - Always check parameter names in constructor error messages
  - Puppeteer doesn't support :has-text() pseudo-selector
  - waitForTimeout was deprecated, use setTimeout instead
  - Keyboard shortcuts need proper key sequences (down/press/up)

#### Registration to Ticket Refactoring Fixes (January 11, 2025 - Session 2)
- **Task**: Fix all compilation errors after Registration → Ticket + RSVP refactoring
- **What Was Done**:
  - Fixed duplicate DTO definitions (EventTicketRequest, RsvpDto already in CommonDtos.cs)
  - Created missing RsvpStatus enum (Confirmed, Waitlisted, CheckedIn, Cancelled)
  - Updated all RegistrationStatus references to TicketStatus throughout codebase
  - Fixed property name changes (RegisteredAt → PurchasedAt)
  - Updated DbContext, AutoMapper profiles, and services to use Tickets
  - Resolved navigation property issues (WitchCityRopeUser has no Tickets collection)
  - Created comprehensive E2E Puppeteer tests for RSVP and Ticket functionality
- **Key Files**:
  - `/src/WitchCityRope.Core/Enums/RsvpStatus.cs` - Created new enum
  - `/src/WitchCityRope.Infrastructure/Data/Configurations/TicketConfiguration.cs` - Renamed from RegistrationConfiguration
  - `/src/WitchCityRope.Infrastructure/Mapping/*.cs` - Updated all AutoMapper profiles
  - `/tests/e2e/test-rsvp-functionality.js` - RSVP flow testing
  - `/tests/e2e/test-ticket-functionality.js` - Ticket flow testing
- **Key Learnings**: 
  - Always search for ALL references when refactoring entities (navigation properties, DbSets, mappers, services)
  - Duplicate DTO definitions cause confusing compilation errors
  - EF Core requires navigation properties to exist on entities for LINQ queries

#### Admin Events Management Full Implementation (January 11, 2025 - Session 1)
- **Task**: Complete implementation of Admin Events Management with all 4 tabs connected to backend
- **What Was Done**:
  - Fixed API compilation errors (VolunteerController, EventEmailController, EventService)
  - Connected EventEdit component to real APIs (LoadEvent, SaveEvent, LoadAvailableUsers)
  - Implemented volunteer task CRUD operations with API integration
  - Connected email template operations (load, save, send test, send custom)
  - Added real orders/attendees data loading
  - Created comprehensive Puppeteer test suite
- **Key Files**:
  - `/src/WitchCityRope.Web/Features/Admin/Pages/EventEdit.razor` - Full API integration
  - `/src/WitchCityRope.Web/Services/ApiClient.cs` - Added GetUsersAsync, CreateEventAsync, UpdateEventAsync, GetEventAttendeesAsync
  - `/src/WitchCityRope.Core/DTOs/CommonDtos.cs` - Added CreateEventRequest DTO
  - `/tests/e2e/admin-events-management.test.js` - Comprehensive test suite
  - `/tests/e2e/run-admin-events-tests.js` - Simplified test runner
- **Result**: All 4 tabs (Basic Info, Tickets/Orders, Emails, Volunteers/Staff) now fully functional with real data

#### EmailAddress Entity Discovery Issue Fixed (January 10, 2025)
- **Issue**: EF Core migration generation failed with "The entity type 'EmailAddress' requires a primary key to be defined"
- **Root Cause**: The recent migration from custom authentication to ASP.NET Core Identity left navigation properties to the old Core.User entity in VolunteerAssignment, causing EF Core to discover the ignored User entity and its EmailAddress value object
- **Fixes**:
  - Removed User navigation property from VolunteerAssignment entity
  - Updated VolunteerAssignmentConfiguration to remove User relationship mapping
  - Fixed VolunteerService to remove references to User navigation property in Include statements
  - Added explicit exclusion of Core.User entity in WitchCityRopeIdentityDbContext
- **Files**: 
  - `/src/WitchCityRope.Core/Entities/VolunteerAssignment.cs`
  - `/src/WitchCityRope.Infrastructure/Data/Configurations/VolunteerAssignmentConfiguration.cs`
  - `/src/WitchCityRope.Infrastructure/Services/VolunteerService.cs`
  - `/src/WitchCityRope.Infrastructure/Data/WitchCityRopeIdentityDbContext.cs`
- **Key Learning**: When migrating authentication systems, ensure all navigation properties to the old user entity are removed to prevent EF Core entity discovery issues

#### Authentication Fixed (January 7, 2025)
- **Issue**: Login failed with "Invalid login attempt"
- **Fix**: Updated Login.cshtml.cs to use `PasswordSignInByEmailOrSceneNameAsync` instead of standard `PasswordSignInAsync`
- **File**: `/src/WitchCityRope.Web/Areas/Identity/Pages/Account/Login.cshtml.cs`

#### Event Edit Fixed (January 7, 2025)
- **Issue**: Compilation errors and form not working
- **Fixes**: 
  - Route parameter: `{EventId:int}` → `{EventId:guid}`
  - Property mappings: Title→Name, StartDate→StartDateTime, etc.
  - Form selector: `input[name="Title"]` → `input[name="model.Title"]`
- **Files**: `/src/WitchCityRope.Web/Features/Admin/Pages/EventEdit.razor`, `/src/WitchCityRope.Web/Services/ApiClient.cs`

#### Docker Hot Reload Fixed (January 7, 2025)
- **Issue**: Changes not reflected without full container restart
- **Fix**: Created dev tools: `./dev.sh`, `./restart-web.sh`, `docker-compose.dev.yml`
- **Usage**: Use `./restart-web.sh` for quick restart, `./dev.sh` option 7 for rebuild

### 🛠️ Common Development Commands

#### Testing Commands
```bash
# Run Core domain tests (fully working)
dotnet test tests/WitchCityRope.Core.Tests/WitchCityRope.Core.Tests.csproj

# Run Integration tests (uses PostgreSQL Testcontainers)
dotnet test tests/WitchCityRope.IntegrationTests/WitchCityRope.IntegrationTests.csproj

# Run specific integration test class
dotnet test tests/WitchCityRope.IntegrationTests/WitchCityRope.IntegrationTests.csproj --filter "FullyQualifiedName~AuthenticationTests"

# Check compilation status
dotnet build tests/WitchCityRope.Core.Tests/WitchCityRope.Core.Tests.csproj --no-restore
dotnet build tests/WitchCityRope.IntegrationTests/WitchCityRope.IntegrationTests.csproj --no-restore

# Run all tests
dotnet test --verbosity normal

# Run Playwright E2E tests (requires app running)
npm run test:e2e:playwright             # All Playwright tests
npx playwright test --grep "auth"       # Authentication tests only
npx playwright test --grep "admin"      # Admin functionality tests
npx playwright test --grep "events"     # Event management tests
npx playwright test --ui                # Interactive UI mode

# Clean up test containers if needed
docker ps -a | grep testcontainers | awk '{print $1}' | xargs -r docker rm -f
```

#### Integration Testing with PostgreSQL Containers
- **Framework**: Testcontainers for .NET
- **Database**: PostgreSQL 16-alpine (same as production)
- **Benefits**: 
  - Real database behavior testing
  - Migration validation
  - Seed data testing
  - No false positives from in-memory database
- **Requirements**: Docker must be running
- **Test Collection**: Tests use `[Collection("PostgreSQL Integration Tests")]` to share container

#### Development Workflow

> 🚨 **CRITICAL WARNING: DOCKER BUILD CONFIGURATION** 🚨
> 
> **REPEATED ISSUE:** Developers keep using `docker-compose up` which uses PRODUCTION build target and FAILS!
> 
> **ALWAYS use development build commands:**

```bash
# 🚨 WRONG - WILL FAIL: 
# docker-compose up                      # ❌ Uses production target, dotnet watch fails

# ✅ CORRECT - Development build with source mounting:
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# ✅ OR use the helper script (RECOMMENDED):
./dev.sh                                 # Interactive menu with all options

# ✅ Quick restart when hot reload fails:
./restart-web.sh                         

# IMPORTANT: Use Docker for development with hot reload support
# We have custom scripts to handle hot reload issues!

# ❌ LEGACY COMMANDS - DO NOT USE:
# docker-compose up -d                   # ❌ WRONG: Uses production build, WILL FAIL
# docker-compose logs -f web             # ❌ WRONG: Only use with dev build
# docker-compose down                    # ✅ OK: This still works for cleanup

# ✅ CORRECT COMMANDS:
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f web    # View web app logs  
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down           # Stop all services

# Alternative: Direct .NET development (NOT RECOMMENDED)
cd /home/chad/repos/witchcityrope/WitchCityRope
dotnet run --project src/WitchCityRope.Web
# Access at: http://localhost:5000 or https://localhost:5001
```

#### 🔥 Docker Hot Reload Issues - MUST READ
Hot reload in Docker doesn't always work perfectly. We've created tools to handle this:

1. **First Time Setup**: Use `./dev.sh` and select option 1
2. **When Changes Don't Apply**: Use `./restart-web.sh` for quick restart
3. **For Major Changes**: Use `./dev.sh` option 7 (rebuild and restart)
4. **Full Guide**: See [DOCKER_DEV_GUIDE.md](./DOCKER_DEV_GUIDE.md) for complete troubleshooting

#### EF Core Migration Best Practices
To avoid migration generation issues like the EmailAddress entity discovery problem:

1. **Generate migrations using the provided script**:
   ```bash
   ./scripts/generate-migration.sh MigrationName
   ```

2. **Common issues and solutions**:
   - **"Entity requires a primary key"**: Check for unwanted entity discovery through navigation properties
   - **Value objects being treated as entities**: Ensure they're configured with `OwnsOne` or explicitly ignored
   - **Compilation errors**: Always fix all compilation errors before generating migrations

3. **Before generating migrations**:
   - Ensure all code compiles: `dotnet build`
   - Check for unused navigation properties to old entities
   - Review entity configurations in DbContext

4. **After architecture changes** (like authentication migration):
   - Remove all navigation properties to replaced entities
   - Update all Include() statements in services
   - Add explicit Ignore() statements for removed entities

#### 🚨 CRITICAL PostgreSQL Integration Test Learnings

**WARNING**: These issues will cause test failures if not handled properly. Read this section before working on integration tests!

1. **PostgreSQL vs In-Memory Database Differences**
   - **Problem**: In-memory database hides real database constraints and behaviors
   - **Example**: RefundAmount nullable configuration worked in-memory but failed in PostgreSQL
   - **Solution**: ALWAYS use real PostgreSQL for integration tests via Testcontainers
   - **Benefit**: Caught 3 critical bugs that were hidden for months

2. **DateTime Must Be UTC for PostgreSQL**
   - **Problem**: "Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone'"
   - **Wrong**: `new DateTime(1990, 1, 1)` - Kind is Unspecified
   - **Right**: `new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)`
   - **Best Practice**: DbContext now auto-converts all DateTime to UTC in UpdateAuditFields()

3. **Test Data Must Be Unique**
   - **Problem**: Duplicate key violations when tests run in parallel or database isn't cleaned
   - **Wrong**: `SceneName.Create("TestUser")` - Same name used by multiple tests
   - **Right**: `SceneName.Create($"TestUser_{Guid.NewGuid():N}")`
   - **Apply To**: SceneNames, Emails, any unique fields

4. **Nullable Owned Entities Don't Work**
   - **Problem**: EF Core can't configure nullable owned entities with required properties
   - **Wrong**: `public Money? RefundAmount { get; set; }` with `OwnsOne` configuration
   - **Right**: Separate nullable properties that compose into computed property:
   ```csharp
   public decimal? RefundAmountValue { get; private set; }
   public string? RefundCurrency { get; private set; }
   public Money? RefundAmount => RefundAmountValue.HasValue && !string.IsNullOrEmpty(RefundCurrency)
       ? Money.Create(RefundAmountValue.Value, RefundCurrency) : null;
   ```

5. **Entity Constructors Must Initialize All Required Fields**
   - **Problem**: Default Guid.Empty causes duplicate key violations
   - **Wrong**: Not setting Id in constructor
   - **Right**: Always initialize in constructor:
   ```csharp
   public Rsvp(Guid userId, Event @event)
   {
       Id = Guid.NewGuid(); // CRITICAL: Must set this!
       CreatedAt = DateTime.UtcNow;
       UpdatedAt = DateTime.UtcNow;
       // ... other initialization
   }
   ```

#### 🚨 CRITICAL EF Core Entity Discovery Learnings

**WARNING**: These issues caused major migration failures and took hours to debug. Read carefully!

1. **Navigation Properties Can Break Migrations**
   - **Problem**: Even if you explicitly ignore an entity in DbContext, EF Core will still discover it through navigation properties
   - **Example**: We had `modelBuilder.Ignore<Core.User>()` but VolunteerAssignment had a `User` navigation property
   - **Result**: EF Core discovered User entity → found EmailAddress value object → tried to create it as entity → "requires primary key" error
   - **Solution**: Remove ALL navigation properties to ignored entities, use only foreign key IDs

2. **Value Objects vs Entities**
   - **Problem**: EF Core doesn't automatically know what's a value object vs entity
   - **Wrong**: Having a property like `public EmailAddress Email { get; set; }` without configuration
   - **Right**: Configure as owned type: `entity.OwnsOne(e => e.Email)` OR store as primitive: `public string Email { get; set; }`
   - **Our Solution**: Store email as string in database, use value object in domain model

3. **Include() Statements After Removing Navigation Properties**
   - **Problem**: Services using `.Include(a => a.User)` fail after removing User navigation property
   - **Solution**: Remove the Include() and fetch user data separately when needed:
   ```csharp
   // OLD - fails after removing navigation
   var assignment = await _context.VolunteerAssignments
       .Include(a => a.User)
       .FirstOrDefaultAsync(a => a.Id == id);
   
   // NEW - works without navigation
   var assignment = await _context.VolunteerAssignments
       .FirstOrDefaultAsync(a => a.Id == id);
   var user = await _userManager.FindByIdAsync(assignment.UserId.ToString());
   ```

4. **Constructor Parameter Names Matter**
   - **Problem**: EF Core constructor binding is case-sensitive for parameter names
   - **Example**: `new VolunteerTask(..., grantsEventAccess: true)` failed because parameter didn't exist
   - **Solution**: Always check entity constructors match your seed data parameter names

5. **DateTime vs TimeOnly Properties**
   - **Problem**: Using DateTime where entity expects TimeOnly causes runtime errors
   - **Example**: VolunteerTask expects `TimeOnly` for start/end times, not `DateTime`
   - **Solution**: Convert properly: `TimeOnly.FromDateTime(dateTime)`

#### Current Architecture Status
- **Core Domain**: Clean, testable, no dependencies on infrastructure
- **Authentication**: ASP.NET Core Identity with JWT hybrid approach
- **Database**: WitchCityRopeIdentityDbContext (not WitchCityRopeDbContext)
- **Test Strategy**: Domain-first testing with integration tests for full flows
- **Registration Model**: REFACTORED to Ticket (paid events) + RSVP (free social events)
  - Ticket entity: For workshops/classes requiring payment
  - RSVP entity: For social events like meetups and play parties
  - TicketStatus enum: Pending, Confirmed, Cancelled, CheckedIn, Waitlisted, RequiresVetting
  - RsvpStatus enum: Confirmed, Waitlisted, CheckedIn, Cancelled

## 🚨 CRITICAL: E2E TESTING USES PLAYWRIGHT ONLY

**UPDATED: January 21, 2025** - All 180 Puppeteer tests migrated to Playwright

**DO NOT CREATE NEW PUPPETEER TESTS - USE EXISTING PLAYWRIGHT TESTS**

All login testing patterns are available in the Playwright Page Object Models:

```typescript
// Use existing Playwright Login Page Object Model
import { LoginPage } from '/tests/playwright/pages/login.page';
import { test, expect } from '@playwright/test';

test('login as admin', async ({ page }) => {
  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.login('admin@witchcityrope.com', 'Test123!');
  await expect(page).toHaveURL(/dashboard/);
});
```

### ✅ Playwright Login Patterns Available:
- **Login Page Object**: `/tests/playwright/pages/login.page.ts`
- **Register Page Object**: `/tests/playwright/pages/register.page.ts`
- **Admin Dashboard Page Object**: `/tests/playwright/pages/admin-dashboard.page.ts`
- **Event Page Object**: `/tests/playwright/pages/event.page.ts`
- **Auth Helpers**: `/tests/playwright/helpers/auth.helpers.ts`

### Test Accounts Available:
- **Admin**: admin@witchcityrope.com / Test123!
- **Teacher**: teacher@witchcityrope.com / Test123!
- **Vetted Member**: vetted@witchcityrope.com / Test123!
- **General Member**: member@witchcityrope.com / Test123!
- **Attendee**: guest@witchcityrope.com / Test123!

### Authentication System Status:
- **Framework**: ASP.NET Core Identity (fully migrated)
- **Authentication Types**: Hybrid (Cookie + JWT)
- **Login URL**: `/login` (primary) or `/Account/Login` (alternate)
- **Logout URL**: `/Identity/Account/Logout`

### ✅ How to Run Existing E2E Tests:
```bash
# Run all authentication tests
npx playwright test --grep "auth"

# Run specific login tests
npx playwright test auth/login-basic.spec.ts

# Run admin tests
npx playwright test --grep "admin"

# Run in UI mode for debugging
npx playwright test --ui
```

### 📁 Playwright Test Reference Files:
- **Auth Tests**: `/tests/playwright/specs/auth/` (4 test files)
- **Event Tests**: `/tests/playwright/specs/events/` (3 test files)
- **Admin Tests**: `/tests/playwright/specs/admin/` (5 test files)
- **RSVP Tests**: `/tests/playwright/specs/rsvp/` (4 test files)
- **Full Documentation**: `/docs/enhancements/playwright-migration/`

## Environment
- **Operating System**: Ubuntu 24.04 (Native Linux - NOT WSL)
- **Development Path**: `/home/chad/repos/witchcityrope/WitchCityRope`
- **MCP Servers Path**: `/home/chad/mcp-servers/`

## GitHub Repository
- **Repository**: https://github.com/DarkMonkDev/WitchCityRope.git
- **Authentication**: Personal Access Token (PAT) is configured in ~/.git-credentials
- **Git Config**: Credential helper is set to 'store' for persistent authentication

## Database Configuration

### PostgreSQL Authentication
- **Default Password**: WitchCity2024!
- **Username**: postgres
- **Database Name**: witchcityrope_db

### Docker Compose PostgreSQL Configuration
When using Docker Compose:
- **Container Name**: `witchcityrope-db`
- **Port**: 5433
- **Volume**: `witchcityrope_postgres_dev_data`
- **Password**: WitchCity2024!
- **User**: postgres
- **Database**: witchcityrope_db

### Connection String Configuration
- **Docker Compose (Internal)**: `Host=postgres;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!`
- **Docker Compose (External)**: `Host=localhost;Port=5433;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!`
- **Local PostgreSQL**: `Host=localhost;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!`

## IMPORTANT: Docker-Only Development

> 🚨 **CRITICAL REPEATED ERROR PREVENTION** 🚨
> 
> **Developers repeatedly use `docker-compose up` which FAILS!**
> 
> The default docker-compose.yml uses `target: ${BUILD_TARGET:-final}` which builds production images that try to run `dotnet watch` on compiled assemblies. This ALWAYS FAILS.

**CRITICAL**: This project MUST be run using Docker containers only. Do not run the application directly on the host machine. All database connections, API services, and the web application should run through Docker Compose.

### Why Docker Only?
1. **Consistent Environment**: Ensures all developers have the same environment
2. **Database Isolation**: PostgreSQL runs in its own container with proper networking
3. **Service Communication**: Services communicate through Docker networks
4. **No Port Conflicts**: Avoids conflicts with local services
5. **Easy Cleanup**: Simple to stop and remove all services

## Development Guidelines

### Docker Development Commands (REQUIRED)

> **🚨 CRITICAL: ALWAYS USE DEVELOPMENT BUILD** 🚨
> 
> **WRONG:** `docker-compose up -d` ❌ Uses production target, dotnet watch FAILS
> 
> **CORRECT:** Use development compose files that mount source code ✅

```bash
# ✅ CORRECT: Start all services with development build
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# ✅ OR use helper script:
./dev.sh

# ✅ View logs for all services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f

# ✅ View logs for specific service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f web
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f api
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f postgres

# Stop all services
docker-compose down

# Rebuild and restart services
docker-compose down && docker-compose up -d --build

# Access the application
# Web: http://localhost:5651
# API: http://localhost:5653
# Database: localhost:5433 (PostgreSQL)

# Run database migrations
docker-compose exec web dotnet ef database update

# Execute commands in containers
docker-compose exec web bash
docker-compose exec api bash
docker-compose exec postgres psql -U postgres -d witchcityrope_db
```

### Current Docker Configuration

**Services Running:**
- **PostgreSQL Database**: Port 5433 (internal: 5432)
- **API Service**: Port 5653 (HTTP only)
- **Web Application**: Port 5651 (HTTP only)

**Note**: HTTPS is disabled in Docker development. Use HTTP URLs:
- Web: http://localhost:5651
- API: http://localhost:5653

### Important: Clean Up After Testing

**CRITICAL**: Any processes started during MCP testing MUST be properly shut down when you're done! Failure to do this will cause port conflicts when trying to run the application from Visual Studio.

#### Port Configuration

The application uses the following ports:

**Direct Launch (Visual Studio/dotnet run)**:
- API: 8180 (HTTP), 8181 (HTTPS) 
- Web: 8280 (HTTP), 8281 (HTTPS)

**Docker Compose**:
- Web: 5000 (HTTP), 5001 (HTTPS) - mapped from container's 8080/8443
- Database: 5433 - mapped from container's 5432

#### Common Processes to Clean Up

1. **dotnet run processes** (ports 5000, 5001, 7125, etc.):
   ```bash
   # Check for running dotnet processes
   ps aux | grep dotnet
   
   # Kill specific dotnet process by PID
   kill -9 <PID>
   
   # Kill all dotnet processes (use with caution)
   pkill -f dotnet
   ```

2. **Chrome DevTools** (port 9222):
   ```bash
   # From Windows PowerShell/CMD (NOT WSL)
   netstat -ano | findstr :9222
   taskkill /PID <PID> /F
   
   # Or close ALL Chrome instances
   taskkill /IM chrome.exe /F
   ```

3. **Docker containers**:
   ```bash
   # Stop all project containers
   docker-compose down
   
   # Check for any remaining containers
   docker ps
   
   # Force stop specific container
   docker stop <container_name>
   ```

4. **PostgreSQL** (port 5432):
   ```bash
   # Check if PostgreSQL is using the port
   netstat -an | grep 5432
   
   # Stop PostgreSQL service (if running locally)
   sudo service postgresql stop
   ```

#### Port Conflict Resolution

If Visual Studio reports "port already in use" errors:

1. **Find what's using the port**:
   ```bash
   # From WSL
   sudo lsof -i :5000    # Replace 5000 with the conflicted port
   
   # From Windows PowerShell
   netstat -ano | findstr :5000
   ```

2. **Kill the process**:
   ```bash
   # From WSL (using PID from lsof)
   kill -9 <PID>
   
   # From Windows (using PID from netstat)
   taskkill /PID <PID> /F
   ```

3. **Common ports used by the application**:
   - `5000` - HTTP development server
   - `5001` - HTTPS development server  
   - `7125` - Alternative HTTPS port
   - `9222` - Chrome DevTools debugging
   - `5432` - PostgreSQL database

#### Best Practices

1. **Always clean up after MCP testing sessions**
2. **Use `docker-compose down` instead of just `stop`** to fully clean up
3. **Close Chrome DevTools sessions** when done with browser testing
4. **Check for orphaned processes** before starting Visual Studio
5. **Document any persistent services** you're running for the project

### Project Structure
- `/src/WitchCityRope.Core` - Core domain logic
- `/src/WitchCityRope.Infrastructure` - Data access and external services
- `/src/WitchCityRope.Api` - API endpoints
- `/src/WitchCityRope.Web` - Blazor Server UI
- `/tests` - Unit and integration tests

### Key Technologies
- ASP.NET Core 9.0
- Blazor Server
- Syncfusion Blazor Components (SUBSCRIPTION - DO NOT ADD OTHER UI FRAMEWORKS)
- Entity Framework Core
- SQL Server (migrating to PostgreSQL)
- Docker & Docker Compose
- ASP.NET Core Identity (authentication/authorization)

## Docker Deployment Architecture

### Overview
WitchCityRope is containerized using Docker for both development and production environments. The application uses a multi-container architecture with separate containers for the web application and database, orchestrated using Docker Compose.

### Container Architecture

#### 1. Application Container (`witchcityrope-web`)
- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:9.0` (runtime)
- **Build Image**: `mcr.microsoft.com/dotnet/sdk:9.0` (for multi-stage builds)
- **Exposed Ports**: 
  - `8080` (HTTP) - Internal container port
  - `8443` (HTTPS) - Internal container port for SSL
- **Health Check**: HTTP endpoint at `/health`
- **Working Directory**: `/app`

#### 2. Database Container (`witchcityrope-db`)
- **Image**: `postgres:16-alpine` (lightweight PostgreSQL)
- **Exposed Ports**: 
  - `5432` - PostgreSQL default port
- **Volumes**: 
  - `postgres_data:/var/lib/postgresql/data` - Data persistence
  - `./db/init:/docker-entrypoint-initdb.d` - Initialization scripts
- **Health Check**: `pg_isready` command

### Container Networking

#### Development Environment
- **Network**: `witchcityrope-network` (bridge network)
- **Service Discovery**: Containers communicate using service names
- **Connection String**: `Host=witchcityrope-db;Port=5432;Database=witchcityrope;Username=postgres;Password=${POSTGRES_PASSWORD}`

#### Production Environment
- **Network Isolation**: Each service in separate network segments
- **Reverse Proxy**: Nginx or Traefik for SSL termination and load balancing
- **External Access**: Only through reverse proxy on ports 80/443

### Volume Management

#### Persistent Volumes
1. **Database Data**: 
   - Volume: `postgres_data`
   - Mount: `/var/lib/postgresql/data`
   - Purpose: Persist database between container restarts

2. **Application Logs**:
   - Volume: `app_logs`
   - Mount: `/app/logs`
   - Purpose: Persist application logs for debugging

3. **Upload Storage**:
   - Volume: `uploads`
   - Mount: `/app/wwwroot/uploads`
   - Purpose: Persist user-uploaded files

#### Bind Mounts (Development Only)
- `./src:/src:cached` - Source code for hot reload
- `./appsettings.Development.json:/app/appsettings.Development.json` - Dev config

### Environment Configuration

#### Environment Variables
```yaml
# Application Container
- ASPNETCORE_ENVIRONMENT=Development|Staging|Production
- ASPNETCORE_URLS=http://+:8080;https://+:8443
- ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}
- Syncfusion__LicenseKey=${SYNCFUSION_LICENSE}
- Email__SmtpServer=${SMTP_SERVER}
- Email__SmtpPort=${SMTP_PORT}
- Email__Username=${SMTP_USERNAME}
- Email__Password=${SMTP_PASSWORD}

# Database Container
- POSTGRES_USER=postgres
- POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
- POSTGRES_DB=witchcityrope
- PGDATA=/var/lib/postgresql/data/pgdata
```

### Docker Compose Configuration

#### Development (`docker-compose.yml`)
```yaml
version: '3.8'

services:
  web:
    build:
      context: .
      dockerfile: Dockerfile
      target: development
    container_name: witchcityrope-web
    ports:
      - "5000:8080"
      - "5001:8443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=witchcityrope;Username=postgres;Password=${POSTGRES_PASSWORD}
    depends_on:
      db:
        condition: service_healthy
    volumes:
      - ./src:/src:cached
      - app_logs:/app/logs
    networks:
      - witchcityrope-network

  db:
    image: postgres:16-alpine
    container_name: witchcityrope-db
    ports:
      - "5432:5432"
    environment:
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
      - POSTGRES_DB=witchcityrope
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./db/init:/docker-entrypoint-initdb.d
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - witchcityrope-network

volumes:
  postgres_data:
  app_logs:

networks:
  witchcityrope-network:
    driver: bridge
```

#### Production (`docker-compose.prod.yml`)
```yaml
version: '3.8'

services:
  web:
    image: witchcityrope:latest
    container_name: witchcityrope-web
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}
    depends_on:
      - db
    volumes:
      - app_logs:/app/logs
      - uploads:/app/wwwroot/uploads
    networks:
      - backend
      - frontend

  db:
    image: postgres:16-alpine
    container_name: witchcityrope-db
    restart: unless-stopped
    environment:
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
      - POSTGRES_DB=witchcityrope
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - backend

  nginx:
    image: nginx:alpine
    container_name: witchcityrope-proxy
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf
      - ./nginx/ssl:/etc/nginx/ssl
    depends_on:
      - web
    networks:
      - frontend

volumes:
  postgres_data:
  app_logs:
  uploads:

networks:
  backend:
    internal: true
  frontend:
```

### Dockerfile (Multi-stage Build)
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/WitchCityRope.Web/WitchCityRope.Web.csproj", "WitchCityRope.Web/"]
COPY ["src/WitchCityRope.Core/WitchCityRope.Core.csproj", "WitchCityRope.Core/"]
COPY ["src/WitchCityRope.Infrastructure/WitchCityRope.Infrastructure.csproj", "WitchCityRope.Infrastructure/"]
RUN dotnet restore "WitchCityRope.Web/WitchCityRope.Web.csproj"
COPY src/ .
WORKDIR "/src/WitchCityRope.Web"
RUN dotnet build "WitchCityRope.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "WitchCityRope.Web.csproj" -c Release -o /app/publish

# Development stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
WORKDIR /app
EXPOSE 8080 8443
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENTRYPOINT ["dotnet", "watch", "run", "--project", "/src/WitchCityRope.Web/WitchCityRope.Web.csproj"]

# Final production stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080 8443
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "WitchCityRope.Web.dll"]
```

### Development Workflow

#### Getting Started
```bash
# Clone the repository
git clone https://github.com/DarkMonkDev/WitchCityRope.git
cd WitchCityRope

# Create .env file for environment variables
cp .env.example .env

# Build and start containers
docker-compose up -d

# View logs
docker-compose logs -f

# Run database migrations
docker-compose exec web dotnet ef database update

# Stop containers
docker-compose down
```

#### Hot Reload Development
- Source code is mounted as a volume in development
- Changes to .cs files trigger automatic rebuild
- Changes to .razor files reflect immediately
- Database changes persist across container restarts

### Production Deployment

#### Build and Deploy
```bash
# Build production image
docker build -t witchcityrope:latest -f Dockerfile --target final .

# Tag for registry
docker tag witchcityrope:latest registry.example.com/witchcityrope:latest

# Push to registry
docker push registry.example.com/witchcityrope:latest

# Deploy using production compose
docker-compose -f docker-compose.prod.yml up -d
```

#### Database Migrations
```bash
# Run migrations in production
docker-compose -f docker-compose.prod.yml exec web dotnet ef database update

# Backup database
docker-compose -f docker-compose.prod.yml exec db pg_dump -U postgres witchcityrope > backup.sql
```

### Container Health Monitoring

#### Health Checks
- **Web App**: HTTP endpoint at `/health` returns 200 OK
- **Database**: PostgreSQL `pg_isready` command
- **Container Restart Policy**: `unless-stopped` in production

#### Logging
- Application logs persisted to volume
- Structured logging with Serilog
- Log aggregation with ELK stack (optional)

### Security Considerations

1. **Secrets Management**:
   - Never commit `.env` files
   - Use Docker secrets in Swarm mode
   - Consider HashiCorp Vault for production

2. **Network Security**:
   - Database not exposed externally in production
   - Internal networks for service communication
   - SSL termination at reverse proxy

3. **Image Security**:
   - Regular base image updates
   - Vulnerability scanning with Trivy
   - Non-root user in containers

## 🔧 Troubleshooting & Test Information

### Test Suite Status & Commands
For comprehensive test information, see [/docs/standards-processes/testing/TEST_CATALOG.md](./docs/standards-processes/testing/TEST_CATALOG.md)

### Recent Work & Session History
For detailed session history and recent fixes, see [PROGRESS.md](./PROGRESS.md#recent-session-history)

### Common Compilation Issues & Solutions

#### 1. DbContext Reference Errors
**Error**: `WitchCityRopeDbContext could not be found`
**Solution**: Update to `WitchCityRopeIdentityDbContext`
```csharp
// OLD:
var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeDbContext>();

// NEW:
var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
```

#### 2. User Property Reference Errors
**Error**: `'User' does not contain a definition for 'Email'`
**Solution**: Use `EmailAddress` property
```csharp
// OLD:
user.Email.Should().Be(newEmail);

// NEW:
user.EmailAddress.Should().Be(newEmail);
```

#### 3. FluentAssertions API Changes
**Error**: `'BeGreaterOrEqualTo' is not available`
**Solution**: Use correct method name
```csharp
// OLD:
value.Should().BeGreaterOrEqualTo(0);

// NEW:
value.Should().BeGreaterThanOrEqualTo(0);
```

#### 4. Method Parameter Type Mismatches
**Error**: `Cannot convert from 'User' to 'Guid'`
**Solution**: Pass ID property instead of entire object
```csharp
// OLD:
@event.RemoveOrganizer(user);

// NEW:
@event.RemoveOrganizer(user.Id);
```

### Important Notes
1. Always run `dotnet build` after making changes to check for compilation errors
2. The project uses nullable reference types - be mindful of null checks
3. Syncfusion components require proper namespace imports - DO NOT add MudBlazor or other UI frameworks
4. CSS escape sequences in Razor files need @@ (e.g., @@keyframes, @@media)
5. **Follow coding standards**: Apply SOLID principles where they add value and document all code comprehensively - see [/docs/standards-processes/](./docs/standards-processes/)
6. **CRITICAL**: This project uses Syncfusion Blazor Components ONLY. Do not add MudBlazor, Radzen, or any other UI component libraries
7. **Service Types**: ApiClient is a concrete class, not an interface. Check actual implementations before assuming patterns
8. **Use Context7 MCP**: When implementing new features or using unfamiliar APIs, add "use context7" to get current documentation
9. **MCP Servers Available**: Context7 (docs), FileSystem, Docker, GitHub, Memory, Puppeteer/Stagehand (browser testing)

### Performance Optimizations Implemented
- Response compression (Brotli + Gzip)
- Static file caching with 1-year cache headers
- CSS/JS minification
- Google Fonts optimization
- SignalR circuit optimization

### Admin Features
- Dashboard with metrics and charts
- User management interface
- Financial reports with export functionality
- Incident management system
- Event management
- Vetting queue for member approvals

## 🚨 CRITICAL: Read This First

**⚠️ MANDATORY FOR ALL DEVELOPERS**: Before working on this project, you MUST read:
- **[CRITICAL_LEARNINGS_FOR_DEVELOPERS.md](./docs/CRITICAL_LEARNINGS_FOR_DEVELOPERS.md)** - Contains critical lessons learned from major issues that caused hours of debugging. Reading this 10-minute document can save you days of troubleshooting!

**Key Warnings Preview**:
- Value objects not initialized from database → NullReferenceException in authentication
- Wrong Docker build target → Complete application failure  
- HTTPS redirect issues → Web+API communication breakdown
- Navigation properties to ignored entities → Migration failures

## 📁 Project Structure & Key Files

### Clean Architecture Layers
```
/src/
├── WitchCityRope.Core/              # Domain layer (business logic)
│   ├── Entities/                    # Domain entities (User, Event, Registration)
│   ├── ValueObjects/                # Value objects (EmailAddress, SceneName, Money)
│   ├── Enums/                       # Domain enums
│   └── Interfaces/                  # Domain service interfaces
├── WitchCityRope.Infrastructure/    # Infrastructure layer (data access)
│   ├── Data/                        # DbContext, repositories, migrations
│   └── Identity/                    # ASP.NET Core Identity implementation
├── WitchCityRope.Api/              # API layer (controllers, DTOs)
├── WitchCityRope.Web/              # Presentation layer (Blazor Server)
    ├── Features/                    # Feature-based organization
    │   ├── Admin/                   # Admin portal
    │   ├── Members/                 # Member features
    │   └── Public/                  # Public pages
    └── Shared/                      # Shared components and layouts
```

### Key Testing Projects
```
/tests/
├── WitchCityRope.Core.Tests/        # ✅ Domain unit tests (working)
├── WitchCityRope.IntegrationTests/  # ✅ End-to-end tests (working)
├── WitchCityRope.Infrastructure.Tests/ # ⚠️ Data layer tests (needs DbContext fix)
├── WitchCityRope.Api.Tests/         # ⚠️ API tests (needs architecture updates)
└── WitchCityRope.Tests.Common/      # Test utilities and builders
```

### Critical Configuration Files
- **appsettings.json** - Application settings, connection strings, Syncfusion license
- **Program.cs** - Service registration, middleware pipeline, Identity configuration  
- **docker-compose.yml** - Development environment setup
- **/src/WitchCityRope.Infrastructure/Data/WitchCityRopeIdentityDbContext.cs** - Main DbContext
- **/src/WitchCityRope.Infrastructure/Data/DbInitializer.cs** - Seed data and test accounts

### E2E Testing Location

**🚨 CRITICAL: E2E Tests are in `/tests/playwright/` 🚨**

See `E2E_TESTING_GUIDE.md` for complete testing documentation.

Quick reference:
- Run all tests: `npx playwright test`
- Run specific test: `npx playwright test tests/playwright/auth/login-basic.spec.ts`
- Run in UI mode: `npx playwright test --ui`

## Key Documentation References
- **[/docs/00-START-HERE.md](./docs/00-START-HERE.md)** - Documentation navigation guide
- **[PROGRESS.md](./PROGRESS.md)** - Current project status and progress
- **[/docs/functional-areas/](./docs/functional-areas/)** - Feature-specific documentation
- **[/docs/standards-processes/](./docs/standards-processes/)** - Development standards
- **[/docs/lessons-learned/](./docs/lessons-learned/)** - Role-based experience guides
- **[/docs/architecture/site-map.md](./docs/architecture/site-map.md)** - Application navigation structure

### Test Accounts (DbInitializer.cs)
- **Admin**: admin@witchcityrope.com / Test123!
- **Teacher**: teacher@witchcityrope.com / Test123!
- **Vetted Member**: vetted@witchcityrope.com / Test123!
- **General Member**: member@witchcityrope.com / Test123!
- **Attendee**: attendee@witchcityrope.com / Test123!

## 📝 Development History & Session Notes
For detailed development history, recent fixes, and session notes, see [PROGRESS.md - Recent Session History](./PROGRESS.md#recent-session-history)

## MCP Server Usage Patterns

### File Operations
**Use FileSystem MCP for:**
- Reading/writing project files in allowed directories
- Accessing files in: `C:\Users\chad\source\repos`, Documents, Downloads, Desktop
- Example: "Read the TODO.md file" or "Update the configuration in appsettings.json"

**DO NOT use FileSystem MCP for:**
- System files or directories outside allowed paths
- Use regular Read/Write tools for files in the current working directory

### Database Operations
**Use PostgreSQL MCP for:**
- Database queries once migrated from SQLite (currently placeholder)
- Read-only database operations for safety
- Schema inspection and data analysis
- Connection: `postgresql://postgres:your_password_here@localhost:5432/witchcityrope_db`

**Current Status:**
- Still using SQLite - PostgreSQL migration pending
- When ready, update password in connection string

## Browser Testing & Automation

### 🚀 CRITICAL: E2E TESTING WITH PLAYWRIGHT ONLY

**ALL E2E TESTING USES PLAYWRIGHT - PUPPETEER IS DEPRECATED**

For browser automation and UI testing, use these tools:

1. **Playwright Tests (ONLY CHOICE)** - Use existing converted tests in `/tests/playwright/`
2. **Playwright MCP Server** - For quick visual verification during development
3. **Stagehand MCP Server (Optional)** - For natural language automation when needed

### E2E Testing with Playwright (REQUIRED)

**All E2E tests are in `/tests/playwright/` directory**
- ✅ **180 tests fully converted** from Puppeteer to Playwright
- ✅ **TypeScript with Page Object Models** for maintainability
- ✅ **40% faster execution** than Puppeteer
- ✅ **86% less flaky** than Puppeteer
- ✅ **Cross-browser testing** (Chrome, Firefox, Safari)

**How to run Playwright tests:**
```bash
# Run all E2E tests
npm run test:e2e:playwright

# Run specific test category
npx playwright test --grep "authentication"
npx playwright test --grep "admin"
npx playwright test --grep "events"

# Run in UI mode for debugging
npx playwright test --ui

# Generate test reports
npx playwright show-report
```

**Test Structure:**
```
/tests/playwright/
├── specs/                    # Test files (.spec.ts)
│   ├── auth/                # Authentication tests
│   ├── events/              # Event management tests
│   ├── admin/               # Admin functionality tests
│   └── validation/          # Form validation tests
├── pages/                   # Page Object Models
├── helpers/                 # Utility functions
└── playwright.config.ts     # Playwright configuration
```

### Playwright MCP Server (For Quick Testing)

For quick visual verification during development:

**When to use:**
- Quick visual verification during development
- When you need Claude to capture screenshots
- For exploratory testing with Claude's assistance

**Available commands:**
```javascript
// In Claude, use these commands:
mcp_puppeteer.screenshot({
  url: "https://localhost:5652",
  fullPage: true
})

mcp_puppeteer.screenshot({
  url: "https://localhost:5652/events",
  selector: ".event-list"
})
```

**Common Puppeteer Tasks:**
- Navigate to URLs
- Take full page or element screenshots
- Click elements by selector
- Fill form fields
- Select dropdown options
- Execute JavaScript in page context
- Get page HTML content
- Wait for elements to appear

### Stagehand MCP Server (AI-powered) - Optional
**Location**: `/home/chad/mcp-servers/mcp-server-browserbase/stagehand/`

**What it's good for:**
- Natural language browser commands ("Click the login button")
- AI-powered element selection when selectors are complex
- Testing complex user flows with conversational instructions
- Taking screenshots of specific UI elements by description
- Handling dynamic content and AJAX-heavy applications
- Testing when exact selectors are unknown or change frequently

**How to use:**
```bash
# Set OpenAI API key (required):
export OPENAI_API_KEY='your-api-key-here'

# Run quickstart (includes Chrome launch):
/home/chad/mcp-servers/mcp-server-browserbase/stagehand/quickstart.sh

# Or start Chrome debug manually first:
google-chrome --remote-debugging-port=9222 --no-first-run --no-default-browser-check
```

**MCP Tools provided:**
- `stagehand_navigate` - Navigate using natural language
- `stagehand_action` - Perform actions using natural language
- `stagehand_extract` - Extract data using natural language
- `stagehand_screenshot` - Take screenshots by description
- `stagehand_observe` - Describe what's on the page

### When to Use Each Tool

#### Use Puppeteer when:
- You know exact CSS selectors or XPath
- You need performance metrics
- You want to monitor network requests
- You need to test specific viewport sizes
- You're automating repetitive tasks
- You need accessibility compliance testing

#### Use Stagehand MCP when:
- You want to describe actions in plain English
- UI elements are hard to select programmatically
- You're testing complex user journeys
- Selectors change frequently
- You need AI to understand page context
- You're exploring an unfamiliar UI

### Common Browser Testing Tasks

#### 1. Testing Login Flow
```javascript
// With Puppeteer:
const browser = await puppeteer.launch();
const page = await browser.newPage();
await page.goto('http://localhost:5000/login');
await page.type('#username', 'admin@witchcityrope.com');
await page.type('#password', 'Test123!');
await page.click('button[type="submit"]');
await page.waitForNavigation();
// Verify dashboard loads
const dashboardElement = await page.$('.dashboard-content');
expect(dashboardElement).toBeTruthy();
await browser.close();

// With Stagehand:
- "Go to the login page"
- "Enter admin@witchcityrope.com as username"
- "Enter the password Test123!"
- "Click the login button"
- "Verify I'm on the dashboard"
```

#### 2. Taking Screenshots
```javascript
// Full page screenshot with Puppeteer:
const browser = await puppeteer.launch();
const page = await browser.newPage();
await page.goto('http://localhost:5000');
await page.screenshot({ path: 'full-page.png', fullPage: true });
await browser.close();

// Element screenshot with Stagehand:
"Take a screenshot of the navigation menu"
```

#### 3. Form Testing
```javascript
// Puppeteer approach:
const browser = await puppeteer.launch();
const page = await browser.newPage();
await page.goto('http://localhost:5000/register');
// Fill form fields
await page.type('#email', 'test@example.com');
await page.type('#password', 'Test123!');
// Submit and check for errors
await page.click('button[type="submit"]');
const errorMessages = await page.$$('.validation-error');
expect(errorMessages.length).toBe(0);
await browser.close();

// Stagehand approach:
- "Fill out the registration form with test data"
- "Submit the form"
- "Check if there are any validation errors"
```

#### 4. Responsive Design Testing
```javascript
// Puppeteer (precise viewport control):
const browser = await puppeteer.launch();
const page = await browser.newPage();
await page.goto('http://localhost:5000');

// Test different viewports
await page.setViewport({ width: 375, height: 667 });  // iPhone SE
await page.screenshot({ path: 'mobile.png' });

await page.setViewport({ width: 768, height: 1024 }); // iPad
await page.screenshot({ path: 'tablet.png' });

await page.setViewport({ width: 1920, height: 1080 }); // Desktop
await page.screenshot({ path: 'desktop.png' });

await browser.close();

// Stagehand (descriptive):
"Show me how the page looks on mobile"
"Check if the menu is accessible on tablet"
```

### Starting the Test Environment

#### Quick Start Commands
```bash
# For Puppeteer tests:
npm test

# For Stagehand server (with API key):
export OPENAI_API_KEY='your-key-here'
/home/chad/mcp-servers/mcp-server-browserbase/stagehand/quickstart.sh

# Verify Chrome connection (for Stagehand):
curl http://localhost:9222/json/version
```

#### Testing Configuration
- Puppeteer manages its own Chrome/Chromium instance
- Stagehand uses local Chrome installation at `/usr/bin/google-chrome`
- Chrome DevTools port: 9222 (for Stagehand)
- No WSL workarounds needed on Ubuntu native

### Troubleshooting Browser Testing

#### Common Issues and Solutions

1. **"Cannot connect to Chrome DevTools"**
   ```bash
   # Check if Chrome is running:
   ps aux | grep chrome
   
   # Check if port 9222 is in use:
   lsof -i :9222
   
   # Kill existing Chrome processes:
   pkill -f "chrome.*remote-debugging"
   
   # Restart Chrome with debug port:
   google-chrome --remote-debugging-port=9222 --no-first-run
   ```

2. **"Sandbox errors" or "Permission denied"**
   - For Puppeteer, use: `puppeteer.launch({ args: ['--no-sandbox', '--disable-setuid-sandbox'] })`
   - Check available system resources: `free -h`
   - Ensure Chrome is executable: `chmod +x /usr/bin/google-chrome`

3. **"Element not found" errors**
   - Add explicit waits: `await page.waitForSelector('.element')`
   - Use Stagehand for dynamic content
   - Check if element is in iframe
   - Verify selector in browser DevTools first

4. **"Timeout" errors**
   - Increase timeout values in tool calls
   - Check if page is actually loading
   - Monitor network tab for hanging requests
   - Use headed mode to see what's happening

#### Testing the Setup
```bash
# Test Puppeteer directly:
node test-puppeteer.js

# Test Chrome connection (for Stagehand):
curl http://localhost:9222/json/version

# Check Chrome process:
ps aux | grep chrome | grep 9222

# Test with actual WitchCityRope site:
# 1. Start the application
cd /home/chad/repos/witchcityrope/WitchCityRope
~/.dotnet/dotnet run --project src/WitchCityRope.Web

# 2. Run your Puppeteer tests
npm test
```

### Best Practices for Browser Testing

1. **Always use automated testing** - Don't rely on manual testing alone
2. **Start with headed mode** when debugging new tests
3. **Use headless mode** for CI/CD and repeated runs
4. **Take screenshots** at key points for debugging
5. **Clean up resources** - Close browser sessions when done
6. **Use appropriate timeouts** - Don't make tests flaky with short timeouts
7. **Test at different viewports** for responsive design
8. **Capture console logs** to debug JavaScript errors
9. **Use AI (Stagehand) for complex interactions** that are hard to script
10. **Verify before automating** - Manually check the flow works first

### Example Test Scenarios

#### Complete Login Test with Puppeteer:
```javascript
const puppeteer = require('puppeteer');

async function testLogin() {
  const browser = await puppeteer.launch();
  const page = await browser.newPage();
  
  // Navigate to login
  await page.goto('http://localhost:5000/login');
  
  // Fill credentials
  await page.type('#username', 'admin@witchcityrope.com');
  await page.type('#password', 'Test123!');
  
  // Submit form
  await page.click('button[type="submit"]');
  
  // Wait for dashboard
  await page.waitForSelector('.dashboard-content', { timeout: 5000 });
  
  // Take screenshot for verification
  await page.screenshot({ path: 'login-success.png' });
  
  await browser.close();
}

testLogin();
```

#### Natural Language Test with Stagehand:
```
"Go to the WitchCityRope login page"
"Log in as the admin user with email admin@witchcityrope.com"
"Verify that I can see the admin dashboard"
"Take a screenshot of the dashboard"
"Navigate to the user management section"
"Check if I can see the list of users"
```

Remember: Automated browser testing with Puppeteer provides reliable, repeatable testing that manual testing cannot match.

### Version Control
**Use GitHub MCP for:**
- Creating issues and pull requests
- Checking repository status
- Managing releases
- Viewing and creating commits
- Example: "Create an issue for the bug we just found"

**Use regular Git commands for:**
- Local git operations (add, commit, push)
- Branch management
- Merge operations

### Docker Operations
**Use Docker MCP for:**
- Managing containers (start, stop, restart, logs)
- Inspecting container status and health checks
- Executing commands in containers (e.g., database migrations)
- Building and managing Docker images
- Managing Docker networks and volumes
- Requires: Docker Desktop running

**Common Docker MCP Tasks:**
1. **Container Management:**
   - Start/stop the web and database containers
   - View real-time logs: `docker logs -f witchcityrope-web`
   - Check container health: `docker inspect witchcityrope-web --format='{{.State.Health.Status}}'`
   - Execute commands: `docker exec witchcityrope-web dotnet ef database update`

2. **Database Operations:**
   - Access PostgreSQL shell: `docker exec -it witchcityrope-db psql -U postgres`
   - Create database backup: `docker exec witchcityrope-db pg_dump -U postgres witchcityrope > backup.sql`
   - Restore database: `docker exec -i witchcityrope-db psql -U postgres witchcityrope < backup.sql`

3. **Development Workflow:**
   - Build development image: `docker-compose build`
   - Start all services: `docker-compose up -d`
   - View service status: `docker-compose ps`
   - Tail logs: `docker-compose logs -f web`
   - Stop services: `docker-compose down`

4. **Troubleshooting:**
   - Check container resource usage: `docker stats`
   - Inspect network connectivity: `docker network inspect witchcityrope-network`
   - Clean up unused resources: `docker system prune -a`
   - View volume contents: `docker run --rm -v witchcityrope_app_logs:/logs alpine ls -la /logs`

**Docker Compose Commands:**
```bash
# Development environment
docker-compose up -d                    # Start all services
docker-compose logs -f web              # Follow web container logs
docker-compose exec web bash            # Shell into web container
docker-compose down -v                  # Stop and remove volumes

# Production environment
docker-compose -f docker-compose.prod.yml up -d
docker-compose -f docker-compose.prod.yml exec web dotnet ef database update
```

### System Commands
**Use Commands MCP for:**
- PowerShell scripts
- Curl commands for API testing
- System automation tasks
- Limited to: curl, powershell commands only

### Memory & Context
**Use Memory MCP for:**
- Storing project-specific knowledge
- Remembering decisions and patterns
- Building a knowledge graph of the project
- Persistent information across sessions

**Good things to remember:**
- Architecture decisions
- Bug patterns and solutions
- User preferences
- Project-specific workflows

### Context7 - Documentation MCP ⭐ NEW (January 15, 2025)
**Use Context7 MCP for:**
- Getting up-to-date documentation for any library or framework
- Ensuring code examples match the current version of libraries
- Avoiding hallucinated or outdated API references
- Fetching version-specific code examples

**How to use Context7:**
1. Add "use context7" to any prompt where you need current documentation
2. Context7 will automatically fetch the latest docs for detected libraries
3. Examples:
   - "How do I configure ASP.NET Core Identity in .NET 9? use context7"
   - "Show me Blazor Server component lifecycle methods use context7"
   - "Explain Entity Framework Core migrations with PostgreSQL use context7"
   - "How to use Syncfusion DataGrid in Blazor? use context7"

**Benefits for WitchCityRope:**
- Always get .NET 9 specific documentation (not older versions)
- Accurate Syncfusion Blazor component examples
- Current Entity Framework Core patterns
- Up-to-date ASP.NET Core Identity configuration
- PostgreSQL-specific EF Core guidance

**Installation:**
Run `./install-context7.sh` in the project root to set up Context7

## MCP Server Prerequisites

### 1. Browser Testing Setup (Ubuntu Native)

**Puppeteer (Recommended)**:
- Install with: `npm install puppeteer`
- Manages its own Chromium instance
- No additional configuration needed
- Full documentation at: https://pptr.dev/

**Stagehand MCP Server (Optional)**:
- Already installed at: `/home/chad/mcp-servers/mcp-server-browserbase/stagehand/`
- Requires: `export OPENAI_API_KEY='your-key'`
- Uses local Chrome (not Browserbase cloud)
- AI-powered natural language browser control

### 2. Docker Setup
- Docker installed and running
- User added to docker group (requires logout/login to take effect)
- Use `docker` commands (docker-compose is integrated)

### 3. PostgreSQL Setup
- PostgreSQL 16 installed and running
- Service: `sudo systemctl status postgresql`
- Create database: `sudo -u postgres createdb witchcityrope_db`
- Default password from CLAUDE.md: WitchCity2024!

## Common MCP Usage Examples

### Example 1: UI Testing
"Use Stagehand to navigate to the login page and test the login flow with the admin@witchcityrope.com account"

### Example 2: File Analysis
"Use FileSystem MCP to read all .cs files in the Infrastructure folder and analyze the repository pattern"

### Example 3: Documentation Lookup with Context7
"How do I implement a custom validation attribute in ASP.NET Core 9? use context7"
"Show me the correct way to configure Blazor Server SignalR options in .NET 9 use context7"
"What's the proper pattern for using owned entities in EF Core 9 with PostgreSQL? use context7"

### Example 4: Docker Logs
"Use Docker MCP to check the logs of the PostgreSQL container"

### Example 5: GitHub Integration
"Use GitHub MCP to create an issue for implementing lazy loading of Syncfusion components"

### Example 6: Memory Storage
"Use Memory MCP to remember that we decided to migrate from SQLite to PostgreSQL and the current status of that migration"

## Browser Automation Troubleshooting (Ubuntu)

### Common Issues and Solutions

1. **"Cannot connect to Chrome DevTools"**
   - Check if Chrome is running: `ps aux | grep chrome`
   - Ensure port 9222 is free: `lsof -i :9222`
   - Launch Chrome with debug port:
     ```bash
     google-chrome --remote-debugging-port=9222 --no-first-run --no-default-browser-check
     ```
   - Test connection: `curl http://localhost:9222/json/version`

2. **"Permission denied" errors**
   - Try safe mode config first: `mcp-config-safe.json`
   - If needed, use no-sandbox config: `mcp-config.json`
   - Ensure Chrome binary is executable: `ls -la /usr/bin/google-chrome`

3. **"Sandbox errors"**
   - Start with headed+safe mode (option 2 in start-server.sh)
   - Only use no-sandbox if absolutely necessary
   - Check system resources: `free -h` and `df -h`

4. **Testing Browser Automation:**
   ```bash
   # Test browser-tools directly:
   cd /home/chad/mcp-servers/browser-tools-server
   node test-puppeteer-direct.js
   
   # Test Chrome connection:
   curl http://localhost:9222/json/version
   
   # Check Chrome process:
   ps aux | grep chrome | grep 9222
   ```

### Quick Reference Commands

```bash
# Browser-tools MCP Server
cd /home/chad/mcp-servers/browser-tools-server
./start-server.sh  # Interactive menu with 4 modes

# Stagehand MCP Server  
export OPENAI_API_KEY='your-key'
/home/chad/mcp-servers/mcp-server-browserbase/stagehand/quickstart.sh

# Launch Chrome for debugging
google-chrome --remote-debugging-port=9222 --no-first-run --no-default-browser-check

# Test Chrome DevTools connection
curl http://localhost:9222/json/version

# Run browser automation test
cd /home/chad/repos/witchcityrope
node test-browser-automation.js

# Start WitchCityRope application
cd /home/chad/repos/witchcityrope/WitchCityRope
~/.dotnet/dotnet run --project src/WitchCityRope.Web
```

### Browser Testing Best Practices (Ubuntu)

1. **Clean browser state** - Each test gets a fresh browser instance
2. **Choose appropriate mode** - Headed for debugging, headless for CI
3. **Use Puppeteer's built-in features** - Screenshots, PDF generation, performance metrics
4. **Direct connection** - No proxies or tunnels needed on native Linux
5. **Test before automating** - Always verify the application is running first

## 🚨 CRITICAL: Blazor Server Architecture Requirements

**This project uses PURE BLAZOR SERVER architecture. Any new pages MUST follow these patterns:**

### New Page Checklist:
- [ ] Uses `.razor` extension (not `.cshtml`)
- [ ] Has `@page` directive with proper route
- [ ] Uses `@layout MainLayout`
- [ ] Has `@attribute [Authorize]` for protected pages
- [ ] Uses `@rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())` for interactive pages
- [ ] Injects required services with `@inject`
- [ ] Uses `@@media` and `@@keyframes` in CSS (double @)
- [ ] Uses proper string escaping in event handlers
- [ ] Uses WCR validation components in forms
- [ ] Follows feature-based folder structure

### Common CSS Escape Issues:
```css
/* ❌ WRONG - Causes compilation errors */
<style>
    @media (max-width: 768px) { }
    @keyframes fadeIn { }
</style>

/* ✅ CORRECT - Always use double @ */
<style>
    @@media (max-width: 768px) { }
    @@keyframes fadeIn { }
</style>
```

### Architecture Validation:
Run `./scripts/validate-blazor-architecture.sh` to check for common issues.

**See `/docs/BLAZOR_ARCHITECTURE_REQUIREMENTS.md` for complete details.**

## 🚨 UPDATE: Current Test Suite Status (January 23, 2025)

### Test Suite Overview
The project has a comprehensive test suite across multiple layers:
- **Unit Tests**: Core domain logic tests
- **Integration Tests**: API and database integration tests using PostgreSQL Testcontainers
- **E2E Tests**: Playwright-based end-to-end tests (Puppeteer tests deprecated)

### Current Test Results
```bash
# Unit Tests: ✅ PASSING
dotnet test tests/WitchCityRope.Core.Tests
# Result: All tests pass

# Integration Tests: ⚠️ MOSTLY PASSING
dotnet test tests/WitchCityRope.Integration.Tests
# Result: Most tests pass, some timing/concurrency issues remain

# E2E Tests: ✅ PASSING (when run individually)
npm run test:e2e:playwright
# Result: 20 test files migrated from Puppeteer, running successfully
```

### Known Test Issues
1. **Integration Test Concurrency**: Some tests fail when run in parallel due to database state conflicts
2. **Test Isolation**: Need better cleanup between test runs
3. **Timing Issues**: Occasional failures due to async operation timing

## 🔒 Authentication/Authorization Fixes Applied (January 2025)

### Major Authentication Fixes
1. **Blazor Server Authentication Pattern**: 
   - Migrated from direct SignInManager usage to API endpoint pattern
   - Created dedicated auth endpoints: `/auth/login`, `/auth/logout`, `/auth/register`
   - Fixed "Headers are read-only" errors in Blazor components

2. **Cookie Authentication Configuration**:
   - Fixed cookie settings for Docker environment
   - Proper SameSite and Secure cookie configuration
   - Fixed authentication state propagation between services

3. **Authorization Policies**:
   - Implemented role-based authorization policies
   - Fixed authorize attributes on Blazor pages
   - Proper claims propagation from API to Web service

### Implementation Details
- **AuthEndpoints.cs**: Minimal API endpoints handle all authentication operations
- **IdentityAuthService.cs**: Service layer that calls auth endpoints via HttpClient
- **Cookie Middleware**: Properly configured for both development and production

## ⚠️ Admin Role Issue - PENDING

### Current Status
There is an ongoing issue with admin role authorization that needs attention:

1. **Problem**: Admin users may not have proper role assignment after registration
2. **Symptoms**: 
   - Admin pages return 403 Forbidden
   - Role claims not properly attached to user principal
   - Inconsistent role checking between Web and API services

3. **Temporary Workaround**: 
   - Manually assign admin role via database update
   - Use DbInitializer seeded admin accounts

4. **Required Fix**:
   - Implement proper role assignment during user registration
   - Add role management UI for administrators
   - Ensure role claims are properly synchronized between services

## 📋 Testing Approach Updates

### Current Testing Strategy
1. **Docker-First Development**: All development uses Docker containers for consistency
2. **Testcontainers**: Integration tests use PostgreSQL Testcontainers for isolation
3. **Playwright Only**: All E2E tests migrated to Playwright (no Puppeteer)
4. **API Testing**: Direct API testing for business logic validation

### Testing Best Practices
- Always run `./dev.sh up` before running tests
- Use `--no-build` flag for faster test runs when code hasn't changed
- Run integration tests sequentially to avoid concurrency issues
- Use Playwright UI mode for debugging E2E test failures

## 🐛 Recently Fixed Bugs (January 2025)

### Critical Bugs Fixed
1. **Event Date Validation**: Fixed validation issues in Event entity for test scenarios
2. **User Dropdown Menu**: Fixed null reference exceptions in user menu component
3. **Authentication State Loss**: Fixed state propagation between page navigations
4. **Docker Hot Reload**: Fixed file watching issues in containerized development
5. **Form Validation**: Migrated all forms to WCR validation components

### Performance Improvements
- Optimized database queries with proper eager loading
- Fixed N+1 query issues in event listings
- Improved caching for static resources

## 🗄️ Database Seeding and Role Names

### Database Initialization
The `DbInitializer` class handles initial database seeding with:

### Default Roles
```csharp
public static class Roles
{
    public const string Admin = "Admin";
    public const string Member = "Member";
    public const string Guest = "Guest";
}
```

### Seeded Test Accounts
- **Admin**: admin@witchcityrope.com / Test123!
- **Member**: member@witchcityrope.com / Test123!
- **Guest**: guest@witchcityrope.com / Test123!

### Role Assignment Process
1. Roles are created during database initialization
2. Default admin user is assigned Admin role
3. New registrations default to Member role
4. Role upgrades must be done manually (admin UI pending)

### Important Notes on Seeding
- Seeding only runs on empty databases
- Re-seeding requires database reset
- Production seeding uses environment variables for admin credentials
- Development uses hardcoded test credentials