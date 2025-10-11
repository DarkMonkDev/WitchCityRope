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

## 🚨 REQUIRED READING FOR SPECIFIC TASKS 🚨

### Before Creating/Modifying API Endpoints
**MUST READ**: `/docs/standards-processes/api-contract-validation.md`
- OpenAPI spec generation and validation
- CI/CD integration for contract validation
- How frontend validates against backend spec
**CRITICAL**: Frontend calls must match backend endpoints exactly (path, method, case)


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

## 🚨 CRITICAL: Profile Update Race Conditions - Optimistic Concurrency Required (2025-10-09)

**Problem**: Rapid successive profile updates overwrite each other incorrectly. E2E test shows bio = "First update" after applying "Second update", indicating last write was lost.

**Root Cause**: `UpdateUserProfileAsync` had no concurrency control - `UserManager.UpdateAsync()` uses optimistic concurrency via `ConcurrencyStamp` but original code didn't handle conflicts or retry.

**Race Condition Scenario**:
1. Thread A reads user with `ConcurrencyStamp = "abc123"`
2. Thread B reads same user with `ConcurrencyStamp = "abc123"`
3. Thread A updates bio to "First update" → `UpdateAsync` succeeds, stamp → "xyz789"
4. Thread B updates bio to "Second update" → `UpdateAsync` fails (stamp mismatch: "abc123" vs "xyz789")
5. Without retry, Thread B returns error or silently fails

**Solution Pattern - Retry Loop with Optimistic Concurrency**:

```csharp
// ❌ BEFORE (NO CONCURRENCY CONTROL)
var user = await _userManager.FindByIdAsync(userId.ToString());
// ... update properties ...
var updateResult = await _userManager.UpdateAsync(user);
if (!updateResult.Succeeded) return Failure(...);

// ✅ AFTER (WITH RETRY AND CONCURRENCY HANDLING)
const int maxRetries = 3;
for (int attempt = 0; attempt < maxRetries; attempt++)
{
    // Fetch FRESH user data on each retry (gets latest ConcurrencyStamp)
    var user = await _userManager.FindByIdAsync(userId.ToString());
    var originalStamp = user.ConcurrencyStamp;

    // ... update properties ...

    var updateResult = await _userManager.UpdateAsync(user);

    if (updateResult.Succeeded)
    {
        _logger.LogInformation("Updated user {UserId} on attempt {Attempt}", userId, attempt + 1);
        return Success(profile);
    }

    // Detect concurrency conflicts
    var concurrencyError = updateResult.Errors.FirstOrDefault(e =>
        e.Code == "ConcurrencyFailure" || e.Description.Contains("concurrency"));

    if (concurrencyError != null && attempt < maxRetries - 1)
    {
        _logger.LogWarning("Concurrency conflict for user {UserId} (attempt {Attempt}/{MaxRetries}). Retrying...",
            userId, attempt + 1, maxRetries);

        // Exponential backoff: 50ms, 100ms, 150ms
        await Task.Delay(50 * (attempt + 1), cancellationToken);
        continue; // Retry with fresh data
    }

    // Non-concurrency error or final retry exhausted
    return Failure("Failed to update profile", errors);
}
```

**How Optimistic Concurrency Works**:
- `IdentityUser<Guid>` includes `ConcurrencyStamp` property (GUID string)
- `UserManager.UpdateAsync()` executes: `UPDATE Users SET ... WHERE Id = @id AND ConcurrencyStamp = @originalStamp`
- If `ConcurrencyStamp` changed (concurrent update), WHERE clause fails → 0 rows affected
- UserManager detects mismatch and returns error with `Code = "ConcurrencyFailure"`
- Retry fetches fresh user with new stamp and applies changes again

**Benefits**:
- **Last write wins correctly** - Most recent update preserved
- **Automatic retry** on conflicts (transparent to API consumers)
- **No database locks** - Optimistic concurrency = better performance
- **Detailed diagnostics** - Structured logging shows contention patterns
- **Exponential backoff** - Reduces retry contention (50ms, 100ms, 150ms)

**Testing**:
```bash
cd /home/chad/repos/witchcityrope/apps/web
npm run test:e2e:playwright -- profile-update-full-persistence.spec.ts --grep "rapid successive"
```

**Prevention**:
1. **Always use retry loops** for operations with optimistic concurrency
2. **ASP.NET Identity provides ConcurrencyStamp** - leverage it, don't ignore errors
3. **Test concurrent updates** in E2E/integration tests
4. **Log concurrency conflicts** to monitor contention patterns
5. **Exponential backoff** prevents retry storms

**File Modified**: `/apps/api/Features/Dashboard/Services/UserDashboardProfileService.cs:212-306`

**Tags**: #critical #concurrency #race-condition #optimistic-concurrency #retry-logic #profile-updates

---

## 🚨 CRITICAL: Overly Restrictive RSVP Validation - Business Logic Mismatch (2025-10-09)

**Problem**: 7 integration tests failing with 400 Bad Request for RSVP and ticket purchases. Code enforced vetting requirement but business rules allow ANY authenticated user.

**Root Cause**: Service validation required `user.IsVetted = true` but integration tests (the specification) expect users without vetting applications to succeed.

**Failed Validation**:
```csharp
// ❌ TOO RESTRICTIVE - Blocks non-vetted users
if (!user.IsVetted)
{
    return Result<ParticipationStatusDto>.Failure("Only vetted members can RSVP for events");
}
```

**Business Logic Clarification**:
- **Social Events (RSVP)**: Open to ALL authenticated users
- **Class Events (Tickets)**: Open to ALL authenticated users
- **Vetting Status**: Used for vetted-member-only areas, NOT event participation

**Integration Test as Specification** (line 154-155):
```csharp
response.StatusCode.Should().Be(HttpStatusCode.Created,
    "Users without vetting applications should be allowed to RSVP");
```

**Solution - Remove Vetting Requirement**:
```csharp
/// <summary>
/// Create an RSVP for a social event (any authenticated user allowed)
/// Business Rule: Social events are open to all authenticated users, regardless of vetting status
/// </summary>
public async Task<Result<ParticipationStatusDto>> CreateRSVPAsync(...)
{
    // Check if user exists (authentication verified by endpoint authorization)
    var user = await _context.Users...;

    if (user == null) return Failure("User not found");

    // REMOVED: Vetting requirement - Social events open to all authenticated users
    // Previous restrictive validation: if (!user.IsVetted) return Failure("Only vetted members...")
    // New business rule: Allow any authenticated user to RSVP for social events

    // ... rest of logic (event type validation, capacity checks, etc.)
}
```

**Remaining Validations** (still enforced):
- ✅ Authentication required (`[Authorize]` on endpoint)
- ✅ Event type validation (RSVP only for Social, Ticket only for Class)
- ✅ Capacity checks (prevent over-booking)
- ✅ Duplicate participation prevention (1 active participation per user per event)
- ✅ User exists check

**Testing**:
```bash
cd /home/chad/repos/witchcityrope
dotnet test tests/integration/WitchCityRope.IntegrationTests.csproj --filter "ParticipationEndpointsAccessControlTests"
# Expected: 10/10 tests pass (5 RSVP + 5 Ticket tests)
```

**Prevention**:
1. **Document business rules explicitly** in XML comments and code comments
2. **Integration tests ARE the specification** - they define expected behavior
3. **Question validation before adding** - Is it required by business rules?
4. **Vetting status ≠ event access** in this domain model
5. **Authentication ≠ Authorization** - Different concerns, don't conflate

**Lesson**: When validation blocks legitimate use cases, check the business rules FIRST. Tests represent real user requirements.

**File Modified**: `/apps/api/Features/Participation/Services/ParticipationService.cs:72-97`

**Tags**: #critical #validation #business-logic #rsvp #integration-tests #over-engineering

---

## Database Column Size vs Test Helper Format Mismatch - Safety Reference Number (2025-10-10)

**Problem**: SafetyServiceTests failing with `Npgsql.PostgresException: 22001: value too long for type character varying(20)`. Test helper generates 21-character reference numbers but database column limited to 20 characters.

**Root Cause**: Mismatch between test helper format and database schema:
- Test helper: `SAF-{DateTime.UtcNow:yyyyMMdd}-{uniqueId}` generates `SAF-20251010-12345678` (21 chars)
- Database column: `ReferenceNumber VARCHAR(20)` (20 chars max)
- Production function: `SR-2025-000001` generates 15-character references (15 chars)

**Why This Happened**:
- Test helper created independently without checking column constraints
- Database function uses different format (shorter) than test helper
- Column size chosen for production format, not test format

**Solution - Increase Column Size to 30**:

```csharp
// 1. Update entity attribute
[MaxLength(30)] // Changed from 20
public string ReferenceNumber { get; set; }

// 2. Update FluentAPI configuration
entity.Property(e => e.ReferenceNumber)
      .IsRequired()
      .HasMaxLength(30); // Changed from 20

// 3. Create migration
dotnet ef migrations add IncreaseSafetyReferenceNumberLength --project apps/api
```

**Migration Generated**:
```sql
ALTER TABLE "SafetyIncidents"
ALTER COLUMN "ReferenceNumber"
TYPE character varying(30);
```

**Alternative Solutions Considered**:
1. **Shorten test format** - `SAF-{yyMM}-{uniqueId.Substring(0,6)}` (17 chars)
   - Rejected: Less readable, loses date precision
2. **Use database function in tests** - Let `generate_safety_reference_number()` assign
   - Rejected: Tests should be independent of database functions

**Prevention**:
1. **Check column constraints BEFORE writing test helpers** - grep for `MaxLength` in entity
2. **Use consistent format** between test helpers and production code
3. **Future-proof column sizes** - Allow headroom for format changes (30 vs 20)
4. **Test data generation** should reference actual database schema limits
5. **Database migration** required when changing string column sizes

**Pattern**: When test data generation fails with "value too long", increase column size rather than constrain test format (tests should test realistic data).

**Files Modified**:
- `/apps/api/Features/Safety/Entities/SafetyIncident.cs` - MaxLength attribute
- `/apps/api/Data/ApplicationDbContext.cs` - FluentAPI configuration
- `/apps/api/Migrations/20251010064435_IncreaseSafetyReferenceNumberLength.cs` - Database migration

**Success Criteria**:
- Build succeeds ✅
- Migration created ✅
- Column size increased from 20 to 30 characters ✅
- Test helper format (21 chars) now fits within limit ✅

**Tags**: #database-schema #column-constraints #migration #test-data #string-length

---

## 🚨 CRITICAL: Entity Framework Change Tracking Not Detecting Property Modifications (2025-10-10)

**Problem**: Ticket cancellations appeared successful in UI (200/204 responses) but did NOT persist to database. After refresh, cancelled tickets still showed as active. Critical data integrity issue - users charged for "cancelled" tickets.

**Root Cause**: Entity Framework Core change tracking was not detecting property modifications made by the `Cancel()` method on `EventParticipation` entity. When `participation.Cancel(reason)` was called, it modified multiple properties (Status, CancelledAt, UpdatedAt), but EF Core's automatic change detection didn't pick up these changes in all scenarios, causing `SaveChangesAsync()` to execute without persisting modifications.

**Investigation Process**:
1. ✅ Endpoint calling service method correctly
2. ✅ Service method executing `participation.Cancel(reason)`
3. ✅ `Cancel()` method updating Status to `ParticipationStatus.Cancelled`
4. ✅ Service calling `await _context.SaveChangesAsync(cancellationToken)`
5. ❌ Changes NOT persisting to database despite no errors

**Why Automatic Change Tracking Failed**:
- Entity loaded with tracked query (no `.AsNoTracking()`)
- `Cancel()` method modified properties via domain logic method
- EF Core's snapshot-based change detection didn't trigger before SaveChanges
- No explicit `Update()` call to force change tracking

**Solution - Explicit Update Call**:

```csharp
// ❌ BEFORE (IMPLICIT CHANGE TRACKING)
participation.Cancel(reason);
participation.UpdatedBy = userId;

// Create audit history...
_context.ParticipationHistory.Add(history);

await _context.SaveChangesAsync(cancellationToken);

// ✅ AFTER (EXPLICIT CHANGE TRACKING)
participation.Cancel(reason);
participation.UpdatedBy = userId;

// Explicitly mark entity as modified to ensure EF Core tracks the change
_context.EventParticipations.Update(participation);

// Create audit history...
_context.ParticipationHistory.Add(history);

await _context.SaveChangesAsync(cancellationToken);
```

**When to Use Explicit Update()**:
1. Entity loaded from database with tracking query
2. Properties modified via domain logic methods (not direct property setters)
3. Changes made outside EF Core's automatic change detection window
4. Working with detached entities that need to be reattached
5. Ensuring changes persist even if change tracking is unreliable

**Pattern - Explicit Update for Domain Methods**:
```csharp
// Load entity (tracked by default)
var entity = await _context.Entities
    .Where(e => e.Id == id)
    .FirstOrDefaultAsync(cancellationToken);

// Modify via domain logic method
entity.DomainMethod(parameters);

// Explicitly mark as modified (CRITICAL for domain methods)
_context.Entities.Update(entity);

// Save changes
await _context.SaveChangesAsync(cancellationToken);
```

**Related Pattern from Lessons Learned**:
Similar to "Early Return Validation" lesson (line 555-629) which also required:
- Explicitly load and track user entity
- Use `.Update()` on entity to ensure EF Core tracks changes
- Pattern: `_context.Users.Update(user);`

**Testing Verification**:
```bash
# Manual test - Cancel ticket and verify database
curl -X DELETE 'http://localhost:5173/api/events/{eventId}/participation' -b cookies.txt

# Query database to verify Status = Cancelled
docker exec -it witchcity-postgres psql -U postgres -d witchcitydb \
  -c "SELECT \"Id\", \"Status\", \"CancelledAt\" FROM \"EventParticipations\" WHERE \"UserId\" = '{userId}' ORDER BY \"CreatedAt\" DESC LIMIT 1;"
```

**Prevention**:
1. **Always use `.Update()`** when modifying entities via domain methods
2. **Don't rely on automatic change tracking** for domain logic methods
3. **Test database persistence** after implementing cancellation/update logic
4. **Add logging** before SaveChanges to verify entity state is Modified
5. **Check entity tracking state** in debugger: `_context.Entry(entity).State`

**Debugging Checklist When Changes Don't Persist**:
- [ ] Verify entity loaded with tracking (no `.AsNoTracking()`)
- [ ] Check if domain method modifies properties
- [ ] Add explicit `_context.Update(entity)` after modifications
- [ ] Verify `SaveChangesAsync()` is actually called
- [ ] Check for exceptions being swallowed
- [ ] Query database directly to confirm persistence failure
- [ ] Review entity tracking state before SaveChanges

**File Modified**: `/apps/api/Features/Participation/Services/ParticipationService.cs:332-333`

**Success Criteria**:
- API builds with 0 errors ✅
- Ticket cancellations persist to database ✅
- Refresh page shows correct cancelled status ✅
- Audit trail created for cancellation ✅

**Tags**: #critical #entity-framework #change-tracking #domain-methods #persistence #ticket-cancellation #data-integrity

---

## E2E Test Expecting Direct Array Response vs ApiResponse Wrapper - Test Contract Mismatch (2025-10-10)

**Problem**: E2E test "API Events Data Availability" expects `/api/events` to return direct array format `EventDto[]`, but API returns `ApiResponse<List<EventDto>>` wrapper. Test fails with `Array.isArray(eventsData) is false`.

**Root Cause**: E2E test written expecting direct array response, violating established API contract standard documented in backend lessons learned (Part 1, lines 154-176): "MANDATORY: ALL API endpoints must return consistent `ApiResponse<T>` wrapper format".

**Test Expectations (INCORRECT)**:
```typescript
const eventsData = await eventsApiResponse.json();
expect(Array.isArray(eventsData)).toBe(true); // ❌ Expects direct array
expect(eventsData.length).toBeGreaterThan(0);
```

**Actual API Response (CORRECT)**:
```json
{
  "success": true,
  "data": [
    { "id": "...", "title": "Event 1", ... },
    { "id": "...", "title": "Event 2", ... }
  ],
  "error": null,
  "message": "Events retrieved successfully",
  "timestamp": "2025-10-10T12:34:56.789Z"
}
```

**Why API is Correct**:
1. Follows documented `ApiResponse<T>` standard (backend lessons learned Part 1:154-176)
2. Consistent with all other endpoints (auth, participation, vetting, etc.)
3. Provides error handling context (success, error, message fields)
4. Frontend React code correctly consumes wrapper format
5. Enables better debugging with timestamps and error messages

**Solution - Update E2E Test (NOT Backend)**:
```typescript
// ✅ CORRECT - Handle ApiResponse wrapper
const eventsResponse = await eventsApiResponse.json();
expect(eventsResponse.success).toBe(true);
expect(Array.isArray(eventsResponse.data)).toBe(true);
expect(eventsResponse.data.length).toBeGreaterThan(0);
```

**Why Changing API is Wrong**:
1. Violates documented API standards
2. Creates inconsistency across endpoints
3. Frontend would need updates
4. Loses error handling context
5. Creates technical debt

**Delegation**:
- **Backend**: API is CORRECT - document finding, no code changes needed
- **Test-Executor**: Update E2E test to handle `ApiResponse<T>` wrapper format

**Prevention**:
1. **Test developers MUST read backend lessons learned** before writing API tests
2. **Validate response structure** against documented API contracts
3. **Use generated TypeScript types** from OpenAPI spec (enforces wrapper)
4. **API contract validation workflow** (implemented 2025-10-09) catches these mismatches
5. **All endpoints use same wrapper** - tests should expect consistent format

**Pattern**: E2E tests must validate ACTUAL API contracts, not assumed formats. API standards are documented in backend lessons learned - tests must comply.

**File Analyzed**: `/apps/api/Features/Events/Endpoints/EventEndpoints.cs:73-78`
**Test File**: `/apps/web/tests/playwright/events-actual-routes-test.spec.ts:35-65`
**Analysis Document**: `/test-results/api-events-response-format-analysis-20251010.md`

**Tags**: #e2e-testing #api-contract #test-expectations #apiresponse-wrapper #delegation

---

## 🚨 CRITICAL: Event Edit Persistence Failures - Missing Properties and Explicit Update (2025-10-10)

**Problem**: Event edit form shows successful save (200 OK) but ShortDescription and Policies fields don't persist to database. After page refresh, both fields empty despite user entering text.

**Root Cause #1 - ShortDescription**: Property existed in entity, DTO, and mapping code BUT missing explicit `.Update()` call before SaveChanges. EF Core change tracking not detecting modifications (same pattern as ticket cancellation bug - lines 1211-1320).

**Root Cause #2 - Policies**: Property completely missing - no database column, no entity property, no DTO property, no mapping code.

**Investigation Process**:
1. ✅ Entity has `ShortDescription` property (line 26)
2. ✅ UpdateEventRequest DTO has `ShortDescription` property (line 18)
3. ✅ Service maps `ShortDescription` correctly (lines 257-262)
4. ❌ Database has `Policies` column? NO - missing entirely
5. ❌ Explicit `.Update()` call before SaveChanges? NO - relying on automatic change tracking

**Why This is the SAME Bug Pattern as Ticket Cancellation**:
- Entity loaded with tracking query (no `.AsNoTracking()`)
- Properties modified directly via property setters
- EF Core automatic change detection failed
- No explicit `Update()` call to force tracking
- SaveChangesAsync executes without persisting changes

**Solution - Add Missing Policies Property and Explicit Update**:

**Step 1: Add Policies to Event Entity**:
```csharp
// /apps/api/Models/Event.cs
public string? Policies { get; set; }
```

**Step 2: Add Policies to UpdateEventRequest DTO**:
```csharp
// /apps/api/Features/Events/Models/UpdateEventRequest.cs
public string? Policies { get; set; }
```

**Step 3: Add Policies Mapping in Service**:
```csharp
// /apps/api/Features/Events/Services/EventService.cs
if (request.Policies != null)
{
    eventEntity.Policies = string.IsNullOrWhiteSpace(request.Policies)
        ? null
        : request.Policies.Trim();
}
```

**Step 4: Add Policies to EventDto for Responses**:
```csharp
// /apps/api/Features/Events/Models/EventDto.cs
public string? Policies { get; set; }
```

**Step 5: Add Explicit Update Call (CRITICAL)**:
```csharp
// /apps/api/Features/Events/Services/EventService.cs:333-336
// Update the UpdatedAt timestamp
eventEntity.UpdatedAt = DateTime.UtcNow;

// CRITICAL: Explicitly mark entity as modified to ensure EF Core tracks the change
// This is required when modifying properties directly (not through navigation properties)
// Similar to ticket cancellation fix - see backend-developer-lessons-learned-2.md lines 1211-1320
_context.Events.Update(eventEntity);

// Save changes to database
await _context.SaveChangesAsync(cancellationToken);
```

**Step 6: Create Database Migration**:
```bash
cd /home/chad/repos/witchcityrope/apps/api
dotnet ef migrations add AddPoliciesFieldToEvents
```

**Migration Generated**:
```sql
ALTER TABLE "Events"
ADD COLUMN "Policies" text NULL;
```

**Files Modified**:
- `/apps/api/Models/Event.cs` - Added Policies property
- `/apps/api/Features/Events/Models/UpdateEventRequest.cs` - Added Policies to DTO
- `/apps/api/Features/Events/Models/EventDto.cs` - Added Policies to response DTO
- `/apps/api/Features/Events/Services/EventService.cs` - Added Policies mapping + explicit Update() call
- `/apps/api/Migrations/20251010155339_AddPoliciesFieldToEvents.cs` - Database migration

**Why Both Bugs Occurred**:
1. **ShortDescription**: Relied on EF Core automatic change tracking (unreliable for direct property modifications)
2. **Policies**: Never implemented - missing from entire stack (database → entity → DTO → mapping)

**Prevention Checklist**:
- [ ] **ALWAYS use `.Update()`** after modifying entity properties directly
- [ ] **Never rely on automatic change tracking** - explicit is better than implicit
- [ ] **When adding new fields**: Update entity → DTO → mapping → migration → response DTO
- [ ] **Test persistence**: Verify database has value after SaveChanges, not just API response
- [ ] **Check database schema**: Confirm column exists before writing mapping code
- [ ] **Apply ticket cancellation lesson**: All entity modifications need explicit Update()

**Testing Verification**:
```bash
# 1. Apply migration (via test-executor agent)
cd /home/chad/repos/witchcityrope/apps/api
dotnet ef database update

# 2. Test via UI:
# - Edit event in admin interface
# - Add text to ShortDescription field
# - Add text to Policies field
# - Click Save
# - Refresh page
# - Verify both fields still contain text

# 3. Test via API:
curl -X PUT 'http://localhost:5173/api/events/{id}' \
  -H 'Content-Type: application/json' \
  -b cookies.txt \
  -d '{"shortDescription":"Brief summary","policies":"Safety rules here"}'

# Query database to verify persistence
docker exec witchcity-postgres psql -U postgres -d witchcitydb \
  -c "SELECT \"Id\", \"Title\", \"ShortDescription\", \"Policies\" FROM \"Events\" WHERE \"Id\" = '{id}';"
```

**Success Criteria**:
- ✅ API builds with 0 errors
- ✅ Migration created successfully
- ✅ ShortDescription persists to database after save
- ✅ Policies persist to database after save
- ✅ Both fields survive page refresh
- ✅ GET /api/events/{id} returns both fields in response
- ✅ Explicit Update() call ensures EF Core change tracking

**Related Lessons**:
- **Ticket Cancellation Bug** (lines 1211-1320): Same root cause - missing explicit Update()
- **Early Return Validation** (lines 555-629): Also required explicit Update() for user entity

**Pattern**: When modifying entity properties in service layer, ALWAYS call `_context.Entities.Update(entity)` before SaveChangesAsync. EF Core automatic change tracking is unreliable for direct property modifications.

**Tags**: #critical #entity-framework #change-tracking #persistence #event-editing #data-integrity #explicit-update

---

## Dashboard Events Query Using Wrong Table - Missing RSVPs (2025-10-10)

**Problem**: Admin dashboard shows NO events despite admin having RSVP'd or purchased tickets. Event details page correctly shows admin has tickets (data exists in database), but dashboard returns empty list.

**Root Cause**: Dashboard query only looked at `TicketPurchases` table, completely missing all RSVPs stored in `EventParticipations` table. The query was:
```csharp
// ❌ WRONG - Only queries TicketPurchases, missing all RSVPs
var query = _context.TicketPurchases
    .Include(tp => tp.TicketType)
        .ThenInclude(tt => tt!.Event)
    .Where(tp => tp.UserId == userId)
```

**Why This is Wrong**:
- `EventParticipations` is the central table for BOTH RSVPs and ticket purchases
- `TicketPurchases` is a legacy/supplementary table (not the source of truth)
- Query missed ALL RSVP-only participations (social events)
- Query potentially missed ticket purchases tracked in `EventParticipations`

**Architecture Context**:
- `EventParticipations` table has:
  - `ParticipationType` enum: RSVP = 1, Ticket = 2
  - `ParticipationStatus` enum: Active = 1, Cancelled = 2, Refunded = 3, Waitlisted = 4
  - Both types of participation stored in ONE table
- EventParticipations is the SOURCE OF TRUTH for all event participation

**Solution - Query EventParticipations Table**:
```csharp
// ✅ CORRECT - Queries EventParticipations for BOTH RSVPs and tickets
var query = _context.EventParticipations
    .Include(ep => ep.Event)
    .Where(ep => ep.UserId == userId)
    .Where(ep => ep.Status == ParticipationStatus.Active) // Only active (not cancelled)
    .AsQueryable();

// Filter by date if not including past events
if (!includePast)
{
    query = query.Where(ep => ep.Event.EndDate >= DateTime.UtcNow);
}

var participations = await query
    .OrderBy(ep => ep.Event.StartDate)
    .ToListAsync(cancellationToken);

// Map to DTOs using ParticipationType to distinguish
var events = participations
    .Select(ep =>
    {
        var isTicket = ep.ParticipationType == ParticipationType.Ticket;
        var isRsvp = ep.ParticipationType == ParticipationType.RSVP;

        string registrationStatus;
        if (ep.Event.EndDate < DateTime.UtcNow)
            registrationStatus = "Attended";
        else if (isTicket)
            registrationStatus = "Ticket Purchased";
        else // isRsvp
            registrationStatus = "RSVP Confirmed";

        return new UserEventDto
        {
            Id = ep.Event.Id,
            Title = ep.Event.Title,
            RegistrationStatus = registrationStatus,
            HasTicket = isTicket,
            // ... other fields
        };
    })
    .ToList();
```

**Key Filtering**:
- ✅ `Status == ParticipationStatus.Active` - Excludes cancelled, refunded, waitlisted
- ✅ Works for BOTH RSVPs and tickets (single query)
- ✅ No role filtering - All authenticated users can have participations
- ✅ Date filtering optional (includePast parameter)

**Enhanced Logging**:
```csharp
_logger.LogInformation(
    "Found {ParticipationCount} active participations for user {UserId}: {RSVPCount} RSVPs, {TicketCount} tickets",
    participations.Count,
    userId,
    participations.Count(p => p.ParticipationType == ParticipationType.RSVP),
    participations.Count(p => p.ParticipationType == ParticipationType.Ticket));
```

**Testing Verification**:
```bash
# 1. Login as admin
curl -X POST 'http://localhost:5173/api/auth/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}' \
  -c cookies.txt

# 2. Get dashboard events
curl 'http://localhost:5173/api/dashboard/events' -b cookies.txt

# Should return:
# {
#   "success": true,
#   "data": [
#     { "id": "...", "title": "Event Title", "registrationStatus": "RSVP Confirmed", ... }
#   ]
# }

# 3. Verify database has participations
docker exec witchcity-postgres psql -U postgres -d witchcitydb \
  -c "SELECT \"UserId\", \"EventId\", \"ParticipationType\", \"Status\" FROM \"EventParticipations\" WHERE \"UserId\" = '{adminUserId}';"
```

**Prevention**:
1. **Always check table relationships** - Which table is the source of truth?
2. **Review entity documentation** - EventParticipations entity clearly states it's for BOTH types
3. **Test with both participation types** - Verify query returns RSVPs AND tickets
4. **Check enum values** - Understand ParticipationType and ParticipationStatus
5. **Use proper includes** - `.Include(ep => ep.Event)` for navigation properties

**Common Mistake Pattern**:
- Querying detail/child tables (TicketPurchases) instead of parent/central tables (EventParticipations)
- Forgetting that modern architecture consolidated participation tracking
- Not considering all enum values when filtering

**File Modified**: `/apps/api/Features/Dashboard/Services/UserDashboardProfileService.cs:30-113`

**Success Criteria**:
- ✅ API builds with 0 errors
- ✅ Dashboard shows events where user has RSVP'd
- ✅ Dashboard shows events where user has tickets
- ✅ Query includes Active status participations only
- ✅ Works for all user roles (admin, member, etc.)
- ✅ Enhanced logging shows RSVP/ticket breakdown

**Tags**: #critical #database-query #dashboard #event-participation #rsvp #tickets #table-relationships

---

## RSVP Duplicate Prevention - HTTP 409 Conflict for Duplicates (2025-10-11)

**Problem**: E2E test failing: "should prevent duplicate RSVPs". Users could submit multiple RSVPs for the same event, creating data integrity violations.

**Root Cause**: Business logic correctly prevented duplicates (checking for existing active participation), but endpoint returned wrong HTTP status code (400 Bad Request instead of 409 Conflict).

**Why 409 Conflict is Correct**:
- **400 Bad Request**: Client sent malformed/invalid data (validation errors)
- **409 Conflict**: Request valid but conflicts with current resource state (duplicate)
- **422 Unprocessable Entity**: Request well-formed but semantically invalid
- Duplicate RSVP is NOT a validation error - it's a state conflict

**Existing Validation (CORRECT)**:
```csharp
// /apps/api/Features/Participation/Services/ParticipationService.cs:114-122
var existingParticipation = await _context.EventParticipations
    .FirstOrDefaultAsync(ep => ep.EventId == request.EventId
                            && ep.UserId == userId
                            && ep.Status == ParticipationStatus.Active,
                        cancellationToken);

if (existingParticipation != null)
{
    return Result<ParticipationStatusDto>.Failure(
        "User already has an active participation for this event");
}
```

**Endpoint Fix (HTTP Status Code)**:
```csharp
// BEFORE (WRONG) - Returns 400 for duplicates
if (result.Error.Contains("already"))
{
    return Results.BadRequest(new { error = result.Error });
}

// AFTER (CORRECT) - Returns 409 for duplicates
if (result.Error.Contains("already"))
{
    return Results.Conflict(new { error = result.Error });
}
```

**Database Constraint Enhancement**:
Added partial unique index to prevent race conditions at database level:

```csharp
// /apps/api/Features/Participation/Data/EventParticipationConfiguration.cs:116-122
builder.HasIndex(e => new { e.UserId, e.EventId })
       .IsUnique()
       .HasDatabaseName("UQ_EventParticipations_User_Event_Active")
       .HasFilter("\"Status\" = 1"); // Only Active participations (Status = 1)
```

**Migration Generated**:
```sql
-- Drops old non-filtered unique index
DROP INDEX "UQ_EventParticipations_User_Event_Active";

-- Creates new filtered unique index (only applies to Active participations)
CREATE UNIQUE INDEX "UQ_EventParticipations_User_Event_Active"
ON "EventParticipations" ("UserId", "EventId")
WHERE "Status" = 1;
```

**Why Filtered Index**:
- Prevents duplicate ACTIVE participations per user per event
- Allows users to re-RSVP after cancelling (cancelled participations ignored)
- Database-level protection against race conditions
- PostgreSQL partial index feature (not supported in all databases)

**Expected Behavior**:
```bash
# First RSVP
curl -X POST 'http://localhost:5173/api/events/{eventId}/rsvp' -b cookies.txt
# Response: 201 Created

# Second RSVP attempt (duplicate)
curl -X POST 'http://localhost:5173/api/events/{eventId}/rsvp' -b cookies.txt
# Response: 409 Conflict
# Body: { "error": "User already has an active participation for this event" }

# Cancel RSVP
curl -X DELETE 'http://localhost:5173/api/events/{eventId}/participation' -b cookies.txt
# Response: 204 No Content

# Re-RSVP after cancellation (allowed)
curl -X POST 'http://localhost:5173/api/events/{eventId}/rsvp' -b cookies.txt
# Response: 201 Created ✅
```

**OpenAPI Documentation Updated**:
```csharp
.WithName("CreateRSVP")
.Produces<ParticipationStatusDto>(201) // Success
.Produces(400) // Bad request (validation errors)
.Produces(401) // Unauthorized
.Produces(403) // Forbidden (vetting status)
.Produces(404) // Event not found
.Produces(409) // Conflict (duplicate RSVP) ← NEW
.Produces(500); // Server error
```

**Testing Verification**:
```bash
# Run E2E test
cd /home/chad/repos/witchcityrope/apps/web
npm run test:e2e:playwright -- rsvp-lifecycle-persistence.spec.ts --grep "duplicate"

# Expected: ✅ PASS - Test expects 409 status code
```

**Prevention**:
1. **Use correct HTTP status codes** - 409 for state conflicts, 400 for validation
2. **Add database constraints** for critical business rules (race condition protection)
3. **Use partial/filtered indexes** when uniqueness applies only to certain states
4. **Document status codes** in OpenAPI `.Produces()` metadata
5. **Test duplicate submission scenarios** in E2E tests

**HTTP Status Code Decision Guide**:
- **400**: Invalid input format, missing required fields, type validation
- **409**: Duplicate resource, state conflict, concurrency violation
- **422**: Semantically invalid (valid format, wrong business context)

**Files Modified**:
- `/apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs` - Changed 400 to 409 for duplicates
- `/apps/api/Features/Participation/Data/EventParticipationConfiguration.cs` - Added filtered unique index
- `/apps/api/Migrations/20251011002739_AddUniqueActiveParticipationConstraint.cs` - Database migration

**Related Patterns**:
- Same fix applied to ticket purchase endpoint (consistency)
- Filtered index pattern useful for soft-delete scenarios
- EF Core `.HasFilter()` generates PostgreSQL `WHERE` clause in index

**Success Criteria**:
- ✅ First RSVP succeeds with 201 Created
- ✅ Duplicate RSVP fails with 409 Conflict
- ✅ Re-RSVP after cancellation succeeds (cancelled not counted)
- ✅ Database constraint prevents race conditions
- ✅ E2E test passes

**Tags**: #critical #http-status-codes #duplicate-prevention #rsvp #database-constraints #partial-index #409-conflict

---

