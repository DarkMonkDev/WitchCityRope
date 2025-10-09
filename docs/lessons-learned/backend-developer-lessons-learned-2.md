# Backend Developer Lessons Learned - Part 2

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY STARTUP PROCEDURE IS IN PART 1 🚨
**CRITICAL**: Read Part 1 FIRST for ULTRA CRITICAL startup procedure and architecture documents.

## 📚 MULTI-FILE LESSONS LEARNED
**This is Part 2 of 2**
**Read Part 1 first**: backend-developer-lessons-learned.md - **CONTAINS MANDATORY STARTUP PROCEDURE**
**Write to**: Part 2 ONLY
**Max size**: 2,000 lines per file (NOT 2,500)
**IF READ FAILS**: STOP and fix per documentation-standards.md

## ⛔ CRITICAL: HARD BLOCK - DO NOT PROCEED IF FILES UNREADABLE
If you cannot read ANY file:
1. STOP ALL WORK
2. Fix using procedure in documentation-standards.md
3. Set LESSONS_LEARNED_READABLE=false until fixed
4. NO WORK until LESSONS_LEARNED_READABLE=true

---

## Service Initialization Order

**Problem**: Services using DbContext fail with NullReferenceException when created in base class constructor before DbContext initialized.
**Solution**: Defer service creation to InitializeAsync after base.InitializeAsync completes. Create services AFTER dependencies initialized, not in constructors.

## Entity Property Mismatch After Simplification

**Problem**: Service references properties removed during entity simplification causing compilation failures.
**Solution**: Check entity definition FIRST when compilation errors occur. Map to available properties with defaults, verify enum values exist.

## API Path Confusion - Active vs Archived

**Problem**: Backend developers modify archived legacy API at `/src/_archive/WitchCityRope.Api/` instead of active API.
**Solution**: ONLY modify `/apps/api/`. Check file paths - if you see `/src/_archive/` STOP immediately. Legacy API is READ-ONLY reference.

## Frontend-Backend Contract Misalignment

**Problem**: Frontend expects different endpoint paths and data types than backend provides (string vs int enums).
**Solution**: Check frontend API service files for exact endpoint expectations. Use flexible types (object with runtime parsing) for enum fields. Test with curl after implementation.

## Hard-coded Port References

**Problem**: Multiple port configurations scattered across codebase causing deployment failures (5653 vs 5655).
**Solution**: Use centralized config (`/apps/web/src/config/api.ts`). ALL API calls must use apiRequest() or getApiUrl(). Environment variables for ALL port references.

## Missing Role Information in Auth DTOs

**Problem**: Login endpoint returns user without role properties breaking frontend role checks.
**Solution**: Include role/authorization in ALL auth DTOs. Return both single role AND roles array for frontend compatibility. Test role-based features after auth changes.

## Package Version Mismatches (NU1603)

**Problem**: Requested package versions not available causing NuGet warnings (Testcontainers 4.2.2 resolved to 4.3.0).
**Solution**: Update ALL related packages to same LATEST version across projects. Run `dotnet restore --force` after changes. Verify with grep for NU1603 warnings.

## EventSession DateTime Property Changes

**Problem**: Code uses `StartDateTime` but entity has separate `Date` and `StartTime` properties.
**Solution**: Combine using `session.Date.Add(session.StartTime)`. Check entity signatures before implementing service methods. Use IntelliSense to catch property mismatches.

## Missing DTO Fields in API Responses

**Problem**: API returns null for EventType despite database having values - DTO missing field.
**Solution**: Ensure DTO projections include ALL required entity properties. Test API responses with curl after DTO changes. Convert enums to strings with `.ToString()`.

## EF Core Custom Method Translation Errors

**Problem**: LINQ projection calls `e.GetCurrentAttendeeCount()` which EF Core cannot translate to SQL.
**Solution**: Query database FIRST with `.ToListAsync()`, THEN call business logic methods in memory. Separate database queries from business logic method calls.

## Multiple EventDto Classes Out of Sync

**Problem**: Adding fields to one EventDto class but missing other EventDto copies causes null values.
**Solution**: When adding DTO fields, update ALL EventDto classes. Search codebase for duplicate DTOs. Consider consolidating duplicates.

## RSVP/Ticket Double-Counting Logic

**Problem**: `currentAttendees = RSVPs + Tickets` double-counts people who RSVP and buy tickets.
**Solution**: Social events use RSVP count (tickets are donations). Class events use ticket count only. Never add RSVPs + Tickets.

## Multiple Running API Processes

**Problem**: Multiple concurrent dotnet run processes serve stale code on wrong ports.
**Solution**: Kill existing processes before starting API: `pkill -f "dotnet run"`. Check for running processes with `ps aux | grep dotnet`.

## Legacy vs Modern API Feature Gaps

**Problem**: Modern API missing critical business features (Safety, CheckIn, Vetting) that exist in legacy.
**Solution**: Comprehensive feature inventory before declaring migration complete. Extract features to modern API using vertical slice pattern. Prioritize by compliance/business impact.

## Dashboard vs Events Enhancement Extraction

**Problem**: Unclear which legacy features need extraction vs which are already implemented.
**Solution**: Compare legacy and modern implementations. Extract unique business value (Dashboard engagement). Archive duplicates (Events already complete in modern API).

## API Unit Tests After Architecture Migration

**Problem**: Tests reference deleted services (IEventService), obsolete namespaces (Microsoft.AspNetCore.Testing), renamed entities (User → ApplicationUser).
**Solution**: Run `dotnet build` on test projects after migrations. Remove tests for deleted services. Update entity references with IDE refactoring. Check package references.

## DTO Type Changes Breaking Tests

**Problem**: DTOs changed from string to Guid/DateTime breaking test initialization.
**Solution**: Update test DTO initialization to match current types. Use Guid.NewGuid() and DateTime.Parse(). Search for all test files using old DTO patterns.

## Service Constructor Parameter Changes

**Problem**: Service adds RoleManager parameter but tests use old constructor signature.
**Solution**: Track constructor signature changes during refactoring. Update test service creation immediately. Mock ALL constructor parameters properly.

## NSubstitute vs Moq Syntax Confusion

**Problem**: Using Moq `.Throws()` syntax with NSubstitute library causing compilation errors.
**Solution**: NSubstitute uses `.Returns<T>(x => throw new Exception())`. Don't mix Moq and NSubstitute syntax. Check which mocking library is referenced.

## Missing API Endpoints for E2E Tests

**Problem**: E2E tests fail with 404 because expected endpoints not implemented (GET /api/vetting/status, GET /api/user/profile).
**Solution**: Verify E2E test endpoints exist before running tests. Implement singular AND plural endpoint forms if tests expect both. Check test files for endpoint expectations.

## Admin Notes Privacy in User Responses

**Problem**: API returns admin notes to regular users violating privacy.
**Solution**: Filter admin-specific data from user-facing responses. Create separate DTOs for admin vs user views. Document privacy rules for data exposure.

## Docker Network Name Inconsistency

**Problem**: docker-compose.override.yml references `witchcity-network` but base defines `witchcity-net`.
**Solution**: Verify network names match between compose files. Use `docker-compose config --quiet` to validate. Check base network definition first.

## Vetting Audit Log Missing Workflow Progression

**Problem**: Status changes don't create audit logs showing workflow progression.
**Solution**: Create audit log entries for ALL state transitions with OldValue/NewValue. Include user attribution and timestamps. Show complete workflow: Draft → UnderReview → Approved.

## Vetting Notes Stored but Not Returned

**Problem**: Notes saved to AdminNotes text field but API returns empty arrays.
**Solution**: Parse stored text fields into DTO arrays for API responses. Create structured Note DTOs from delimited text. Consider migrating to proper Note entity table.

## Draft Events Not Visible to Admins

**Problem**: Admin dashboard shows no draft events because API filters to IsPublished=true only.
**Solution**: Add `includeUnpublished` query parameter with admin authentication. Public endpoints filter to published, admin endpoints show all. Verify role before returning unpublished data.

## Missing PUT /api/events/{id} Endpoint

**Problem**: Frontend sends event updates but API only has GET endpoints returning 405 Method Not Allowed.
**Solution**: Implement complete CRUD (not just GET). Add UpdateEventAsync to service layer. Create PUT endpoint with RequireAuthorization(). Test all HTTP methods.

## Event Update Business Rules Missing

**Problem**: API allows reducing capacity below attendance or updating past events.
**Solution**: Validate cannot update past events. Validate capacity >= current attendance. Validate StartDate < EndDate. Return 400 Bad Request with clear error messages.

## Partial Update Request Models

**Problem**: Full replacement required for event updates when user only changes title.
**Solution**: Use nullable optional fields in UpdateRequest DTOs. Only update non-null fields. Maintain UpdatedAt timestamp automatically.

## JWT Authentication Not Tested

**Problem**: Protected endpoints deployed without verifying JWT requirement.
**Solution**: Test with curl - verify 401 without token, 200 with valid token. Use `.RequireAuthorization()` on ALL protected minimal API endpoints.

## HTTP Status Code Mapping Inconsistent

**Problem**: All errors return 500 instead of proper codes (400 validation, 404 not found).
**Solution**: Map error messages to status codes with switch expression. Document status codes in endpoint XML comments. Use Results.Json() with statusCode parameter.

## Database Initialization Timing

**Problem**: Application startup fails because migrations run before database ready.
**Solution**: BackgroundService with retry logic and delays. Check database connectivity before migrations. Log initialization steps for debugging.

## Multiple DTO Classes for Same Entity

**Problem**: Project has duplicate EventDto in Features/Events/Models/ and Models/ causing sync issues.
**Solution**: Consolidate to single DTO class. If duplicates necessary, document why and keep synchronized. Create shared DTO project for cross-feature models.

## Fallback Data Inconsistency

**Problem**: List endpoint has fallback events but single endpoint returns null when database empty.
**Solution**: Both list AND single endpoints need same fallback logic. Consistent behavior across related endpoints. Use shared fallback data method.

## Enum String Conversion in DTOs

**Problem**: EventType enum not converted to string in DTO projection causing serialization issues.
**Solution**: Use `.ToString()` when mapping enums to DTOs. Test JSON serialization of enum fields. Consider using [JsonConverter] for enum handling.

## Capacity State Distribution for Testing

**Problem**: All seed events have same capacity percentage making frontend testing impossible.
**Solution**: Use deterministic seed based on ID hash. Provide varied states: 20% sold out, 30% nearly full, 30% moderate, 20% low. Test frontend with all capacity scenarios.

## Session/Ticket Type Eager Loading

**Problem**: Event details missing sessions and ticket types despite database having data.
**Solution**: Use `.Include()` for navigation properties in EF Core queries. Load ALL related data needed by frontend. Test with populated database not just empty/fallback.

## API Response Structure Consistency

**Problem**: Some endpoints return raw data, others use ApiResponse wrapper causing frontend confusion.
**Solution**: Use consistent ApiResponse<T> wrapper for ALL endpoints. Include Success, Data, Error, Message, Timestamp fields. Document response structure in API docs.

## Business Logic in Controllers vs Services

**Problem**: Complex validation logic in minimal API endpoints making them hard to test.
**Solution**: Move business logic to service layer. Endpoints only handle HTTP concerns (auth, routing, status codes). Service methods return (success, data, error) tuples.

## UTC DateTime Handling

**Problem**: DateTime values stored/returned in local time causing PostgreSQL timezone issues.
**Solution**: Use DateTime.UtcNow for ALL timestamps. Convert to UTC before database operations. Document timezone handling in DTOs.

## Event Registration vs RSVP Confusion

**Problem**: Frontend shows wrong counts because backend doesn't distinguish registration types.
**Solution**: Separate CurrentRSVPs (free Social events) from CurrentTickets (paid). Document business rules: Social allows both, Class only tickets.

## Minimal API Endpoint Documentation

**Problem**: Swagger missing endpoint descriptions and parameter documentation.
**Solution**: Use `.WithName()`, `.WithSummary()`, `.WithDescription()`, `.WithTags()`, `.Produces<T>()` on all endpoints. Document all status codes returned.

## Authentication Claim Extraction Pattern

**Problem**: Inconsistent user ID extraction from JWT tokens across endpoints.
**Solution**: Standard pattern: `user.FindFirst("sub") ?? user.FindFirst(ClaimTypes.NameIdentifier)`. Verify Guid.TryParse succeeds. Return 401 if claim missing.

## Database Transaction Management

**Problem**: Service methods don't use transactions causing partial updates on errors.
**Solution**: Wrap multi-step operations in `using var transaction = await _context.Database.BeginTransactionAsync()`. Commit on success, rollback on error.

## Seed Data Idempotency

**Problem**: Running seed multiple times creates duplicate data.
**Solution**: Check for existing data before seeding. Use `if (!await _context.Events.AnyAsync())`. Clear tables with TRUNCATE CASCADE for fresh seeds.

## Mock vs Real Database in Tests

**Problem**: Tests pass with mocked repository but fail with real database due to EF Core behavior differences.
**Solution**: Use TestContainers for integration tests with real PostgreSQL. Mock only external dependencies (APIs, file system). Test database constraints and triggers.

## Navigation Property Circular References

**Problem**: JSON serialization fails with infinite loop when User has VettingApplications and VettingApplication has User.
**Solution**: Add [JsonIgnore] to navigation properties. Use DTOs instead of returning entities directly. Document which side of relationship to ignore.

## Legacy Code Reference vs Modification

**Problem**: Accidentally modifying archived legacy code when extracting features.
**Solution**: READ legacy code for business logic understanding only. WRITE to `/apps/api/` using modern patterns. Never modify `/src/_archive/`.

## Feature Extraction Without Tests

**Problem**: Extracting legacy features without tests to verify correctness.
**Solution**: Write tests FIRST for extracted features. Compare behavior with legacy implementation. Test edge cases and business rules.

## Vertical Slice Pattern Implementation

**Problem**: Creating separate projects for each layer (Core, Infrastructure, API) instead of feature slices.
**Solution**: Organize by feature: `/Features/Vetting/` contains Endpoints, Services, Models, Entities. Each feature is self-contained.

## Database Constraint Migration

**Problem**: Adding check constraints in code but not generating EF Core migrations.
**Solution**: Use `.ToTable(t => t.HasCheckConstraint())` in entity configuration. Generate migration with `dotnet ef migrations add`. Test constraints with invalid data.

## Obsolete API Pattern Usage

**Problem**: Using deprecated EF Core methods after version upgrades (old HasCheckConstraint syntax).
**Solution**: Check migration guide when upgrading major versions. Fix compiler warnings immediately. Test builds after package updates.

## Test Data Cleanup

**Problem**: Tests fail when run multiple times due to data from previous runs.
**Solution**: Use `[TestInitialize]` to reset database state. Truncate tables or use transactions that rollback. Ensure tests are repeatable.

## Async Method Naming

**Problem**: Service methods lack Async suffix causing confusion about execution model.
**Solution**: ALL async methods end with Async suffix. Document whether method is CPU-bound or I/O-bound. Use Task<T> return types consistently.

## CancellationToken Not Passed

**Problem**: Long-running operations ignore cancellation tokens preventing graceful shutdown.
**Solution**: Accept CancellationToken on ALL async methods. Pass to EF Core methods (.ToListAsync(ct)). Test cancellation behavior.

## Logging Levels Misused

**Problem**: Using LogInformation for errors or LogError for normal operations.
**Solution**: LogError for exceptions/failures. LogWarning for recoverable issues. LogInformation for significant events. LogDebug for development.

## Connection String Security

**Problem**: Database passwords in appsettings.json committed to source control.
**Solution**: Use User Secrets for development. Environment variables for production. Never commit credentials. Document setup in README.

## Service Dependency Injection Lifetime

**Problem**: Registering DbContext as singleton causing threading issues.
**Solution**: DbContext must be scoped. Services using DbContext must be scoped. Singletons can only depend on singletons.

## API Versioning Missing

**Problem**: Breaking changes deployed without version strategy.
**Solution**: Use URL versioning (/api/v1/events). Maintain old versions during migration period. Document deprecation timeline.

## Error Messages Expose Implementation

**Problem**: Stack traces and SQL queries returned to frontend exposing security information.
**Solution**: Generic error messages for production. Detailed errors only in development. Log full details server-side.

## Rate Limiting Not Implemented

**Problem**: Public endpoints vulnerable to abuse without rate limiting.
**Solution**: Use AspNetCore.RateLimiting middleware. Configure per-endpoint limits. Return 429 Too Many Requests with Retry-After header.

## CORS Configuration Too Permissive

**Problem**: AllowAnyOrigin in production allowing XSS attacks.
**Solution**: Explicit origin whitelist. Use WithOrigins() for production. Only AllowAnyOrigin in development.

## Health Check Depth

**Problem**: Health endpoint returns healthy when database is down.
**Solution**: Include database connectivity check. Check critical dependencies. Return detailed status for monitoring.

## Swagger in Production

**Problem**: API documentation exposed in production revealing internal structure.
**Solution**: Enable Swagger only in development environment. Use conditional app.UseSwagger() based on env.IsDevelopment().

## Password Hashing Weakness

**Problem**: Using weak hashing algorithm for passwords (MD5, SHA1).
**Solution**: Use ASP.NET Core Identity password hasher. Configure strong PBKDF2 or Argon2. Never implement custom crypto.

## JWT Secret in Code

**Problem**: JWT signing key hardcoded in appsettings.json.
**Solution**: Use User Secrets for development. Azure Key Vault for production. Generate strong random keys (256-bit minimum).

## File Upload Validation Missing

**Problem**: Accepting any file type/size for uploads enabling DoS attacks.
**Solution**: Whitelist allowed extensions. Limit file size. Validate content type. Scan for malware if possible.

## SQL Injection via Raw SQL

**Problem**: Using string concatenation in FromSqlRaw enabling SQL injection.
**Solution**: Use parameterized queries. Use LINQ when possible. Validate/sanitize inputs before raw SQL.

## Mass Assignment Vulnerability

**Problem**: Binding request directly to entity allowing unauthorized field updates.
**Solution**: Use separate DTOs for requests. Explicit property mapping. Never bind requests to entities directly.

## Sensitive Data in Logs

**Problem**: Logging user passwords, tokens, or PII in application logs.
**Solution**: Sanitize logs to remove sensitive fields. Use structured logging with field filtering. Review logs for data exposure.

## Exception Handling Anti-patterns

**Problem**: Empty catch blocks or catching Exception without logging.
**Solution**: Catch specific exceptions. Log before re-throwing. Use exception filters for cross-cutting concerns.

## Background Service Error Handling

**Problem**: Background service crashes on exception stopping all processing.
**Solution**: Try-catch in ExecuteAsync main loop. Log errors and continue. Implement circuit breaker for repeated failures.

## Email Template Injection

**Problem**: User input directly inserted into email templates enabling phishing.
**Solution**: HTML encode all user content in emails. Use allowlist for HTML if needed. Validate email addresses.

## API Key Rotation Missing

**Problem**: API keys never rotated after compromise or employee departure.
**Solution**: Regular key rotation schedule. Support multiple active keys during rotation. Revocation process documented.

## Dependency Vulnerabilities

**Problem**: Using outdated packages with known security vulnerabilities.
**Solution**: Run `dotnet list package --vulnerable` regularly. Update packages promptly. Use Dependabot or similar.

## Insufficient Authorization Checks

**Problem**: Checking authentication but not authorization allowing horizontal privilege escalation.
**Solution**: Verify user can access specific resource. Check ownership/role for EVERY request. Don't trust client-provided IDs.

## Predictable IDs

**Problem**: Sequential integer IDs enabling enumeration attacks.
**Solution**: Use GUIDs for public-facing IDs. Random IDs for sensitive resources. Implement authorization checks regardless.

## Timing Attack in Comparisons

**Problem**: Using == for token/password comparison enabling timing attacks.
**Solution**: Use CryptographicEquals for security-sensitive comparisons. Constant-time string comparison.

## Missing Input Validation

**Problem**: Trusting client input without validation causing data corruption.
**Solution**: Validate ALL inputs server-side. Use Data Annotations. Implement business rule validation in services.

## Unbounded Query Results

**Problem**: Returning unlimited records enabling DoS via large result sets.
**Solution**: Implement pagination for ALL list endpoints. Max page size limits. Use cursor-based pagination for large datasets.

## Cache Poisoning

**Problem**: Caching user-specific data with shared cache key.
**Solution**: Include user ID in cache keys. Use distributed cache for multi-instance. Set appropriate cache expiration.

## Redirect Validation Missing

**Problem**: Open redirect vulnerability in return URLs after authentication.
**Solution**: Validate redirect URLs against allowlist. Reject absolute URLs. Use relative paths only.

## XML External Entity (XXE) Attack

**Problem**: Parsing XML input without disabling external entities.
**Solution**: Disable DTD processing. Use safe XML parsers. Validate and sanitize XML inputs.

## Insecure Deserialization

**Problem**: Deserializing untrusted data enabling remote code execution.
**Solution**: Validate data before deserializing. Use safe serializers. Implement type allowlists.

## Missing Security Headers

**Problem**: Responses missing security headers enabling attacks.
**Solution**: Add Content-Security-Policy, X-Frame-Options, X-Content-Type-Options headers. Use HSTS for HTTPS enforcement.

## Database Connection Leaks

**Problem**: Not disposing DbContext causing connection pool exhaustion.
**Solution**: Use `using` statements or dependency injection. Monitor connection pool metrics. Set reasonable pool size limits.

## Inefficient Queries

**Problem**: N+1 query problem or missing indexes causing slow responses.
**Solution**: Use `.Include()` for related data. Add database indexes. Use `.AsNoTracking()` for read-only queries.

## Missing Telemetry

**Problem**: No metrics for error rates, response times, or usage patterns.
**Solution**: Implement structured logging. Add performance counters. Use Application Insights or similar.

## Configuration Sprawl

**Problem**: Settings scattered across multiple files and environments.
**Solution**: Centralized configuration management. Environment-specific overrides. Document ALL settings with examples.

## API Documentation Drift

**Problem**: Swagger docs don't match actual endpoint behavior.
**Solution**: Generate docs from code. Add XML comments. Test examples in documentation.

## Missing Retry Logic

**Problem**: Transient failures cause operation failures without retry.
**Solution**: Implement retry with exponential backoff. Use Polly library. Distinguish transient from permanent errors.

## Synchronous I/O in Async Methods

**Problem**: Calling .Result or .Wait() in async methods causing deadlocks.
**Solution**: Use await throughout async call chain. ConfigureAwait(false) for library code. Never mix sync and async.

## Poor Error Context

**Problem**: Errors logged without context making debugging impossible.
**Solution**: Include request ID, user ID, operation context in logs. Use structured logging with properties.

## Missing API Client Generation

**Problem**: Frontend manually maintains API types that drift from backend.
**Solution**: Use NSwag or similar to generate TypeScript types from C# DTOs. Automate in build pipeline.

## Test Coverage Gaps

**Problem**: High line coverage but critical paths untested.
**Solution**: Test business rules and edge cases. Use mutation testing. Review coverage reports for meaningful gaps.

## Hardcoded Configuration

**Problem**: URLs, timeouts, or limits hardcoded instead of configurable.
**Solution**: Move ALL magic numbers to configuration. Use strongly-typed Options pattern. Validate configuration on startup.

## Missing Correlation IDs

**Problem**: Can't trace requests across services in logs.
**Solution**: Add correlation ID to all log entries. Propagate through call chain. Include in responses for debugging.

## Inadequate Load Testing

**Problem**: Performance issues discovered in production under load.
**Solution**: Load test realistic scenarios before deployment. Test database under concurrent load. Monitor resource usage.

## Feature Flags Not Used

**Problem**: All-or-nothing deployments requiring rollback on issues.
**Solution**: Implement feature flags for new features. Gradual rollout capability. Quick disable without deployment.

## API Deprecation Strategy Missing

**Problem**: No way to sunset old endpoints causing maintenance burden.
**Solution**: Document deprecation timeline. Return deprecation headers. Provide migration guide. Monitor usage of old endpoints.

## Email Notifications Within Database Transactions

**Problem**: Email service calls block database transactions or cause rollbacks when emails fail.
**Solution**: Send emails AFTER SaveChangesAsync but BEFORE transaction commit. Wrap email calls in try-catch to prevent failures from blocking business logic. Log email errors but continue with status change. Email audit trail in VettingEmailLog provides tracking even when emails fail.

**Pattern**:
```csharp
await _context.SaveChangesAsync(cancellationToken);

// Send email after DB save, before commit (non-blocking)
try
{
    var emailResult = await _emailService.SendStatusUpdateAsync(...);
    if (!emailResult.IsSuccess)
    {
        _logger.LogWarning("Email failed: {Error}", emailResult.Error);
    }
}
catch (Exception ex)
{
    _logger.LogError(ex, "Email exception - continuing");
}

await transaction.CommitAsync(cancellationToken);
```

## 🚨 CRITICAL: Early Return Validation Blocking Business Logic Execution

**Problem**: Integration tests show 400 Bad Request when approving vetting applications, but backend "fixes" for `user.IsVetted = true` and audit log action name have no effect. Test pass rate stuck at 67.7% despite multiple iterations of backend changes.

**Root Cause**: Status validation in `ApproveApplicationAsync` method returns early with failure **BEFORE** any business logic executes:
```csharp
// ❌ TOO RESTRICTIVE - Blocks approvals that haven't completed full interview workflow
if (application.Status < VettingStatus.InterviewScheduled)
{
    return Result.Failure("Invalid status for approval", "...");
}
```

This validation prevented approval for applications in `Submitted` or `UnderReview` status, causing:
- 400 Bad Request responses from API
- Business logic (setting `IsVetted`, creating audit logs) never executing
- Backend fixes applied to unreachable code paths
- False assumption that database persistence or validation was the issue

**Solution Pattern**:
1. **Relax workflow validation** to allow approval from `UnderReview` status or later (not just `InterviewScheduled`)
2. **Add terminal state check** to prevent modification of already-approved/denied applications
3. **Explicitly load and track user entity** instead of relying on navigation properties
4. **Use `.Update()` on entity** to ensure EF Core tracks changes to user properties

```csharp
// ✅ CORRECT - Allows approval from UnderReview or later, prevents terminal state modification
if (application.Status < VettingStatus.UnderReview)
{
    return Result.Failure("Invalid status",
        $"Application must be in UnderReview or later. Current: {application.Status}");
}

if (application.Status == VettingStatus.Approved ||
    application.Status == VettingStatus.Denied ||
    application.Status == VettingStatus.Withdrawn)
{
    return Result.Failure("Cannot modify terminal state",
        $"Application is already {application.Status}");
}

// Load user explicitly (don't rely on navigation property)
var user = await _context.Users
    .FirstOrDefaultAsync(u => u.Id == application.UserId.Value, cancellationToken);

if (user != null)
{
    user.Role = "VettedMember";
    user.IsVetted = true;

    // Explicitly mark as modified to ensure EF tracks the change
    _context.Users.Update(user);

    _logger.LogInformation("Set IsVetted=true for user {UserId}", user.Id);
}

await _context.SaveChangesAsync(cancellationToken);
```

**Debugging Methodology**:
1. **Check HTTP status codes FIRST** - 400 = validation failure, 500 = execution error
2. **Trace execution path** - Early returns prevent downstream code from executing
3. **Verify business logic is reachable** - Add logging BEFORE validation checks
4. **Test validation boundaries** - Ensure validation rules match test scenarios
5. **Don't assume database issues** when validation is rejecting requests

**Prevention**:
- Read endpoint code FIRST to understand validation rules
- Check test setup to verify entities are in expected states
- Use logging to confirm code paths are executing
- Test validation boundaries explicitly (edge cases between statuses)
- When fixes don't work, question whether the code is even executing

**Tags**: #critical #validation #debugging #early-return #unreachable-code

---

## Dashboard VettingStatus Logic Error - Enum Comparison Excludes Valid State

**Problem**: Users with `VettingStatus = 0 (UnderReview)` incorrectly shown as NOT having a vetting application, causing dashboard to display "Submit Application" button instead of "Under Review" status badge.

**Root Cause**: Flawed enum comparison logic in UserDashboardService line 55:
```csharp
// ❌ WRONG - Excludes VettingStatus = 0 (UnderReview)
HasVettingApplication = user.VettingStatus > 0
```

This logic assumes `VettingStatus > 0` means "has application", but the `VettingStatus` enum starts at 0:
- 0 = UnderReview (VALID state with application)
- 1 = InterviewScheduled
- 2 = Approved
- 3 = Denied

**Why It's Wrong**:
- Relies on enum value instead of actual database data
- Breaks when enum values change or start at 0
- Violates single source of truth principle (database has the answer)

**Solution**: Query the actual VettingApplications table:
```csharp
// ✅ CORRECT - Queries actual database table
var hasVettingApplication = await _context.VettingApplications
    .AnyAsync(va => va.UserId == userId, cancellationToken);
```

**Prevention**:
1. **Never assume enum value ranges** - Enums can start at any value
2. **Query source of truth** - Check database tables, not derived fields
3. **Test edge cases** - Verify enum boundary values (0, first value, etc.)
4. **Use meaningful checks** - `.AnyAsync()` clearly expresses "does record exist"

**Pattern**: When checking "does user have X?", query the X table - don't infer from status fields.

**Tags**: #critical #enum #database-query #dashboard #vetting


---

## Vetting Same-State Updates - REVERTED: Strict Status-Change-Only Enforcement

**Initial Implementation**: Allowed same-state updates (e.g., UnderReview → UnderReview) to enable adding notes.

**Stakeholder Decision (REVERTED)**: Same-state updates should NOT be allowed. Status update endpoint is for ACTUAL status transitions only.

**Current Implementation**: Reject same-state updates with clear error message:

```csharp
// ✅ CORRECT - Enforce strict status changes
if (oldStatus == newStatus)
{
    _logger.LogWarning(
        "Attempted same-state update for application {ApplicationId}: {Status}. " +
        "Use AddSimpleApplicationNote endpoint for adding notes without status change.",
        application.Id,
        oldStatus);

    return Result<ApplicationDetailResponse>.Failure(
        "Invalid status update",
        "Status is already set to the requested value. Use the AddSimpleApplicationNote endpoint to add notes without changing status.");
}
```

**Business Rationale**:
- Status update endpoint is for status CHANGES only (name reflects purpose)
- Separate endpoint exists for adding notes: `AddSimpleApplicationNote`
- Same-state "updates" don't make business sense
- Logging helps detect bugs or API misuse

**Prevention**:
- Status endpoints should enforce actual state transitions
- Provide separate endpoints for non-state-changing operations (notes, metadata)
- Document API contract clearly in method comments
- Use logging to detect incorrect API usage patterns

**Tags**: #vetting #workflow #api-design #business-logic #reverted

---

## Test Failures From Obsolete Enum Values - Workflow Migration Issues

**Problem**: Integration tests fail with errors like "'Submitted' is not a valid vetting status" even though backend code is correct.

**Root Cause**: Tests written for old workflow where VettingStatus enum included "Submitted" status. Current workflow starts applications directly in "UnderReview" status, but tests still reference obsolete values.

**Symptoms**:
- Test creates application with status UnderReview
- Test expects OldValue = "Submitted" in audit logs
- Enum parsing fails for "Submitted" before business logic executes

**Detection**:
1. Check `.bak` files for evidence of old enum values
2. Compare test expectations to current enum definition
3. Verify all submission endpoints - what initial status do they set?

**Solution**:
- Backend code is correct - do NOT add obsolete enum values
- Tests must be updated to match current workflow
- Delegate test fixes to test-executor agent

**Boundary Enforcement**:
- Backend developer: Write code matching current domain model
- Test developer: Update tests to match current workflow
- Do NOT modify domain model to fix broken tests

**Prevention**:
- When enum values are removed, search entire codebase for string references
- Update tests immediately when workflow changes
- Document enum migrations in ADRs

**Tags**: #critical #testing #enum-migration #workflow #delegation

---

## 🚨 CRITICAL: Cross-Origin Cookie Persistence Bug - BFF Pattern Broken (2025-10-08)

**Problem**: E2E tests show 401 Unauthorized on `/api/auth/user` after successful login. Login succeeds (200), user redirects to dashboard, dashboard tries to fetch user info, gets 401. **LAUNCH BLOCKER**.

**Root Cause**: Frontend configuration bypassed Vite proxy, making direct cross-origin requests to API server.

**Technical Details**:
```typescript
// BEFORE (BROKEN) - /apps/web/src/config/api.ts
export const getApiBaseUrl = (): string => {
  return import.meta.env.VITE_API_BASE_URL || 'http://localhost:5655'
}
```

This caused:
1. Frontend at `localhost:5173` makes POST to `http://localhost:5655/api/auth/login` (cross-origin)
2. API sets cookie: `Set-Cookie: auth-token=...; samesite=lax; httponly`
3. Browser associates cookie with domain `localhost:5655` (API server)
4. Dashboard at `localhost:5173` makes GET to `http://localhost:5655/api/auth/user` (cross-origin fetch)
5. With `SameSite=Lax`, cookies NOT sent cross-origin for fetch/XHR → 401 Unauthorized

**Why SameSite=Lax Failed**:
- `SameSite=Lax` sends cookies for:
  - Same-site requests (same domain + same port)
  - Cross-site top-level navigation (clicking links)
- `SameSite=Lax` does NOT send cookies for:
  - Cross-origin fetch/XHR requests
  - Different ports count as different origins (5173 ≠ 5655)

**Solution**:
```typescript
// AFTER (FIXED) - /apps/web/src/config/api.ts
export const getApiBaseUrl = (): string => {
  // In development, use empty string to get relative URLs (go through Vite proxy)
  // This ensures cookies are set for localhost:5173 (web server) not localhost:5655 (API)
  if (import.meta.env.DEV) {
    return ''
  }

  // In production/staging, use absolute URL from environment
  return import.meta.env.VITE_API_BASE_URL || ''
}
```

**How This Fixes It**:
1. Frontend at `localhost:5173` makes POST to `/api/auth/login` (relative URL)
2. Vite proxy forwards to `http://localhost:5655/api/auth/login`
3. API sets cookie in response
4. Proxy passes response back to browser
5. Browser thinks response came from `localhost:5173` → cookie set for `localhost:5173` ✅
6. Dashboard at `localhost:5173` makes GET to `/api/auth/user` (relative URL)
7. Vite proxy forwards with cookie (same-origin) → 200 OK ✅

**Key Insight - BFF Pattern Requirements**:
- BFF (Backend-for-Frontend) pattern REQUIRES same-origin requests
- Frontend must NEVER talk directly to API server in development
- All API calls MUST go through proxy on same origin
- This is NOT optional - cross-origin cookies don't work with SameSite=Lax

**Vite Proxy Configuration** (already correct in vite.config.ts):
```typescript
proxy: {
  '/api': {
    target: 'http://localhost:5655',
    changeOrigin: true,
    ...
  }
}
```

**Why We Didn't Change Cookie Settings**:
- Alternative: `SameSite=None; Secure` would work cross-origin
- Rejected: Requires HTTPS in development, more complex
- Proxy solution is cleaner, more secure, matches production pattern

**Manual Testing Verification**:
```bash
# Test 1: Login through proxy
curl -X POST 'http://localhost:5173/api/auth/login' -d '{"email":"...","password":"..."}' -c cookies.txt
# Result: ✅ 200 OK, cookie set for localhost

# Test 2: Authenticated request through proxy
curl 'http://localhost:5173/api/auth/user' -b cookies.txt
# Result: ✅ 200 OK, user data returned
```

**Impact**:
- Fixes 6 out of 10 failing E2E tests (launch blocker resolved)
- All authentication flows work correctly
- BFF pattern properly implemented

**Prevention**:
1. **Check for absolute URLs**: Grep for `http://localhost:` in API client code
2. **Verify proxy usage**: Ensure `getApiUrl()` returns relative paths in dev
3. **Test cookie behavior**: Manual browser testing before E2E runs
4. **Document BFF requirements**: Update architecture docs

**File Modified**: `/apps/web/src/config/api.ts`
**Fix Document**: `/test-results/authentication-persistence-fix-20251008.md`

**Tags**: #critical #authentication #cookies #bff-pattern #cors #launch-blocker #e2e-tests


---

## 🚨 CRITICAL: API Contract Validation Missing from CI/CD - Ticket Cancellation Bug Pattern (2025-10-09)

**Problem**: Frontend-backend API endpoint mismatches reach production undetected, causing bugs like ticket cancellation (frontend calls `/ticket`, backend has `/participation`) where UI shows success but database unchanged.

**Root Cause**: No automated validation ensuring frontend API calls match backend OpenAPI specification. Manual code reviews miss endpoint path typos and method mismatches.

**Real-World Impact**:
- Ticket cancellation bug: Frontend DELETE `/api/events/{id}/ticket` returns 404, silently handled as success
- Backend only has DELETE `/api/events/{id}/participation` endpoint
- User sees "Ticket cancelled" but database unchanged
- Bug discovered in production during manual testing, not by CI/CD

**Solution Pattern - OpenAPI Contract Validation in CI/CD**:

```yaml
# .github/workflows/api-contract-validation.yml
jobs:
  validate-api-contract:
    steps:
      # 1. Build API and export OpenAPI spec
      - name: Build API
        run: dotnet build apps/api --configuration Release

      - name: Start API and export spec
        run: |
          dotnet run --urls http://localhost:5655 &
          sleep 10
          curl -f http://localhost:5655/swagger/v1/swagger.json -o apps/api/openapi.json

      # 2. Validate frontend API calls match spec
      - name: Install frontend dependencies
        run: cd apps/web && npm ci

      - name: Validate API contract
        run: node scripts/validate-api-contract.js
        # Exits with code 1 if mismatches found, blocking merge
```

**Validation Script Capabilities**:
- Scans frontend code for API calls (axios, fetch, apiRequest patterns)
- Compares endpoint paths and HTTP methods against OpenAPI spec
- Provides fuzzy matching suggestions (85%+ similarity)
- Shows file/line numbers for fix guidance
- Exits with error code to fail CI/CD on violations

**Example Validation Output**:
```
❌ API Contract Mismatches Found:

1. DELETE /api/events/${eventId}/ticket
   File: /apps/web/src/hooks/useParticipation.ts:188
   💡 Did you mean one of these?
      DELETE /api/events/{id}/participation (90% match)
         Cancel user's participation in an event

🔧 How to Fix:
   1. Update frontend to use correct endpoint path
   2. Or implement missing endpoint in backend
   3. Re-export OpenAPI spec after changes
```

**Integration Points**:
1. **CI/CD Pipeline**: Runs on push/PR for API or frontend changes
2. **Type Generation**: Validates TypeScript types match OpenAPI spec
3. **DTO Alignment Strategy**: Enforces NSwag-generated types usage
4. **Artifact Upload**: OpenAPI spec uploaded for debugging (30-day retention)
5. **PR Comments**: Posts validation results with fix guidance

**Prevention Checklist**:
- [ ] OpenAPI spec exported after every backend change
- [ ] Validation runs in CI/CD before merge
- [ ] Frontend uses generated TypeScript types, not manual interfaces
- [ ] Validation failures block deployment
- [ ] Team trained on reading validation output

**Benefits**:
- Catches endpoint mismatches before production (ticket bug prevented)
- Enforces DTO alignment strategy automatically
- Provides clear fix guidance with fuzzy matching
- Blocks PRs with API contract violations
- Reduces manual code review burden

**Files Created**:
- `.github/workflows/api-contract-validation.yml` - CI/CD workflow (370 lines)
- `scripts/validate-api-contract.js` - Validation script (295 lines)
- `apps/api/Scripts/post-build-export-openapi.sh` - Auto-export hook
- `/docs/standards-processes/api-contract-validation.md` - Documentation (449 lines)

**Workflow Triggers**:
- Push to main/develop branches
- Pull requests to main/develop
- Changes to `apps/api/**`, `apps/web/src/**`, or validation scripts
- Manual workflow dispatch

**Failure Modes**:
- **API not starting**: Retry logic with 30-second timeout
- **Spec export fails**: Clear error with troubleshooting steps
- **Mismatches found**: Exit code 1 blocks merge
- **No spec file**: Fails with instructions to run export script

**Related Documentation**:
- **DTO Alignment Strategy**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
- **API Contract Validation Guide**: `/docs/standards-processes/api-contract-validation.md`
- **API Architecture Overview**: `/docs/architecture/API-ARCHITECTURE-OVERVIEW.md`

**Detection of Existing Violations**:
```bash
# Run validation locally
node scripts/validate-api-contract.js

# Found violations in codebase - validation catches these patterns
```

**Debugging Tip**: When frontend shows success but database unchanged, check Network tab FIRST:
1. Look for 404 Not Found responses (wrong endpoint)
2. Verify HTTP method matches (GET vs POST)
3. Check endpoint path spelling/capitalization
4. Run validation script to find correct endpoint

**Tags**: #critical #api-contract #ci-cd #openapi #validation #dto-alignment #ticket-bug-prevention

---
