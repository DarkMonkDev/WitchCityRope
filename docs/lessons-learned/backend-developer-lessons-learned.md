# Backend Developer Lessons Learned

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY STARTUP PROCEDURE - MUST READ 🚨

### 🚨 ULTRA CRITICAL ARCHITECTURE DOCUMENTS (MUST READ): 🚨
1. **🛑 DTO ALIGNMENT STRATEGY** - **PREVENTS 393 TYPESCRIPT ERRORS**
`/home/chad/repos/witchcityrope/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`

2. **API Architecture Overview** - **CORE BACKEND PATTERNS**
`/home/chad/repos/witchcityrope/docs/architecture/API-ARCHITECTURE-OVERVIEW.md`

3. **Vertical Slice Quick Start** - **FEATURE-BASED ARCHITECTURE**
`/home/chad/repos/witchcityrope/docs/guides-setup/VERTICAL-SLICE-QUICK-START.md`

4. **Entity Framework Patterns** - **DATABASE PATTERNS**
`/home/chad/repos/witchcityrope/docs/standards-processes/development-standards/entity-framework-patterns.md`

5. **Project Architecture** - **TECH STACK AND STANDARDS**
`/ARCHITECTURE.md`

### 📚 DOCUMENT DISCOVERY RESOURCES:
- **File Registry** - `/home/chad/repos/witchcityrope/docs/architecture/file-registry.md` - Find any document
- **Functional Areas Index** - `/home/chad/repos/witchcityrope/docs/architecture/functional-area-master-index.md` - Navigate features
- **Key Documents List** - `/home/chad/repos/witchcityrope/docs/standards-processes/KEY-PROJECT-DOCUMENTS.md` - Critical docs
- **Standards Index** - `/home/chad/repos/witchcityrope/docs/standards-processes/STANDARDS-INDEX.md` - Task-based standards discovery (NEW)

### 📖 ADDITIONAL IMPORTANT DOCUMENTS:
- **Skills Usage Guide** - `/.claude/skills/HOW-TO-USE-SKILLS.md` - Complete guide on when/how to use skills
- **Workflow Process** - `/home/chad/repos/witchcityrope/docs/standards-processes/workflow-orchestration-process.md` - Handoff procedures
- **Agent Boundaries** - `/home/chad/repos/witchcityrope/docs/standards-processes/agent-boundaries.md` - What each agent does
- **Coding Standards** - `/home/chad/repos/witchcityrope/docs/standards-processes/CODING_STANDARDS.md` - General standards
- **Lessons Learned Standards** - Use lessons-learned-validator skill ONLY to validate/fix lessons learned updates - NOT for routine reading

### 🎯 BACKEND-SPECIFIC STANDARDS (Just-In-Time Loading):
**Reference**: `/home/chad/repos/witchcityrope/docs/standards-processes/STANDARDS-INDEX.md` for complete backend standards list

**Quick Backend Standards** (read when needed):
- **API Design Patterns** - `/home/chad/repos/witchcityrope/docs/standards-processes/backend/api-design-patterns.md` - Endpoint conventions, HTTP methods
- **Database Patterns** - `/home/chad/repos/witchcityrope/docs/standards-processes/backend/database-patterns.md` - EF Core quick reference
- **Vertical Slice Architecture** - `/home/chad/repos/witchcityrope/docs/standards-processes/backend/vertical-slice-architecture.md` - Feature-based organization
- **Error Handling** - `/home/chad/repos/witchcityrope/docs/standards-processes/backend/error-handling-patterns.md` - Result pattern, logging
- **Service Layer Patterns** - `/home/chad/repos/witchcityrope/docs/standards-processes/backend/service-layer-patterns.md` - Service implementation

**Cross-cutting Standards**:
- **Microservices Patterns** - `/home/chad/repos/witchcityrope/docs/standards-processes/architecture/microservices-patterns.md` - Web + API architecture
- **Docker Patterns** - `/home/chad/repos/witchcityrope/docs/standards-processes/architecture/docker-patterns.md` - Container development

### Validation Gates (MUST COMPLETE):
- [ ] **Read DTO Alignment Strategy FIRST** - Prevents TypeScript error floods
- [ ] Review API Architecture Overview for core backend patterns
- [ ] Check Vertical Slice Quick Start for feature-based implementation
- [ ] Review Entity Framework patterns for database standards
- [ ] Check Project Architecture for current tech stack
- [ ] Review File Registry if you need to find any document

### Backend Developer Specific Rules:
- **DTO Alignment Strategy PREVENTS 393 TypeScript errors** - read before ANY DTO work
- **Modern API is ONLY development target** - `/apps/api/` not archived `/src/` projects
- **Docker-only testing environment** - NO local dev servers allowed
- **Entity Framework ID generation** - NEVER initialize IDs in model properties
- **API Response pattern (Pattern B - OFFICIAL STANDARD)** - Direct `Results.Ok(dto)` or `Results.Problem()` (RFC 9457 Problem Details) - **ApiResponse<T> wrapper is DEPRECATED**
- **Service Layer pattern** - Use `Result<T>` or tuple pattern `(bool Success, T? Data, string Error)`
- **Path format** - ALWAYS use repo-relative paths like `/home/chad/repos/witchcityrope/docs/...` NOT full system paths

## 🛠️ AVAILABLE DEVELOPMENT TOOLS

### Chrome DevTools MCP (NEW - 2025-10-03)
**Purpose**: Debug web pages, inspect API responses, and validate frontend-backend integration

**Key Capabilities for Backend Developers**:
- **Network Inspection**: Monitor API calls, inspect request/response headers, validate HTTP status codes
- **Console Monitoring**: View frontend JavaScript errors that may indicate API issues
- **Performance Analysis**: Measure API response times and identify performance bottlenecks
- **Runtime Debugging**: Test API endpoints through browser-based testing

**Use Cases for Backend Development**:
- API integration validation - Confirm API responses match frontend expectations
- CORS debugging - Inspect headers and identify cross-origin issues
- Authentication flow testing - Validate cookie setting and token handling
- Error response validation - Ensure error responses are properly formatted and handled

**Configuration**: Automatically available via MCP - see `/home/chad/repos/witchcityrope/docs/standards-processes/MCP/MCP_SERVERS.md`

**Best Practices**:
- Use to validate API endpoint responses during development
- Monitor network traffic when debugging integration issues
- Inspect console errors to identify frontend-backend communication problems
- Test authentication flows end-to-end with visual confirmation

---

## 🚨 IF THIS FILE EXCEEDS 1700 LINES, add new lessons learned to PART 2! BOTH FILES CAN BE UP TO 1700 LINES EACH 🚨

## 📚 MULTI-FILE LESSONS LEARNED
**Files**: 4 total
**Part 1**: `/home/chad/repos/witchcityrope/docs/lessons-learned/backend-developer-lessons-learned.md` (THIS FILE - STARTUP ONLY)
**Part 2**: `/home/chad/repos/witchcityrope/docs/lessons-learned/backend-developer-lessons-learned-2.md` (MAIN LESSONS FILE)
**Part 3**: `/home/chad/repos/witchcityrope/docs/lessons-learned/backend-developer-lessons-learned-3.md` (OVERFLOW LESSONS FILE)
**Part 4**: `/home/chad/repos/witchcityrope/docs/lessons-learned/backend-developer-lessons-learned-4.md` (CURRENT LESSONS FILE)
**Read ALL**: Part 1, Part 2, Part 3, AND Part 4 are MANDATORY
**Write to**: Part 4 ONLY - **NEVER ADD NEW LESSONS TO PART 1, 2, OR 3**
**Maximum file size**: 2,000 lines (to stay under token limits). All parts can be up to 2,000 lines each
**IF READ FAILS**: STOP and use lessons-learned-validator skill to fix immediately

## 🚨 ULTRA CRITICAL: NEW LESSONS GO TO PART 4, NOT HERE! 🚨
**PART 1 PURPOSE**: Startup procedures and critical navigation ONLY
**ALL NEW LESSONS**: Must go to Part 4 - `/home/chad/repos/witchcityrope/docs/lessons-learned/backend-developer-lessons-learned-4.md`
**IF YOU ADD LESSONS HERE**: You are violating the split pattern!

## ⛔ CRITICAL: HARD BLOCK - DO NOT PROCEED IF FILES UNREADABLE
If you cannot read ANY file:
1. STOP ALL WORK
2. Fix using lessons-learned-validator skill
3. Set LESSONS_LEARNED_READABLE=false until fixed
4. NO WORK until LESSONS_LEARNED_READABLE=true

## 🚨 CRITICAL: Entity Framework ID Generation Pattern - NEVER Initialize IDs in Models 🚨

**CRITICAL ROOT CAUSE DISCOVERED**: Entity models having `public Guid Id { get; set; } = Guid.NewGuid();` initializers causes Entity Framework to think new entities are existing ones, leading to UPDATE attempts instead of INSERTs, resulting in `DbUpdateConcurrencyException`.

**NEVER DO THIS**:
```csharp
// ❌ CATASTROPHIC ERROR - Causes UPDATE instead of INSERT
public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();  // THIS BREAKS EVERYTHING!
}
```

**ALWAYS DO THIS**:
```csharp
// ✅ CORRECT - Let Entity Framework handle ID generation
public class Event
{
    public Guid Id { get; set; }  // Simple property, no initializer
}
```

**Symptoms**: "Database operation expected to affect 1 row(s) but actually affected 0 row(s)"
**Prevention**: Remove ALL ID initializers from entity model properties

## 🚨 CRITICAL: JWT Token Missing Role Claims - Role Authorization Failure 🚨

**Problem**: JWT tokens missing role claims, causing ALL role-based authorization to fail with 403 Forbidden
**Root Cause**: JWT token generation missing the role claim

**BEFORE (BROKEN)**:
```csharp
// ❌ MISSING ROLE CLAIM - Authorization will always fail
var claims = new[]
{
    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
    new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
};
```

**AFTER (FIXED)**:
```csharp
// ✅ INCLUDES ROLE CLAIM - Authorization works correctly
var claims = new[]
{
    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
    new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
    new Claim(ClaimTypes.Role, user.Role ?? "Member") // CRITICAL: Role claim
};
```

**CRITICAL**: Role names in `[Authorize(Roles = "")]` MUST match database role values exactly

## 🚨 CRITICAL: API Response Pattern B - THE OFFICIAL STANDARD 🚨

**DECISION MADE (2025-11-13)**: Pattern B is now the **OFFICIAL STANDARD** for WitchCityRope API responses.

**Pattern B Definition (THE STANDARD)**:
- **Success responses**: Direct `Results.Ok(dto)` - NO wrapper
- **Error responses**: `Results.Problem()` with RFC 9457 Problem Details
- **Service layer**: Tuple pattern `(bool Success, T? Data, string Error)` OR Result<T> pattern
- **ApiResponse<T> wrapper**: **DEPRECATED - DO NOT USE**

**ENDPOINT LAYER** (Minimal API - Pattern B):
```csharp
// ✅ CORRECT - Direct Results pattern (Pattern B - OFFICIAL STANDARD)
app.MapGet("/api/events/{id}", async (int id, EventService service) =>
{
    var result = await service.GetEventAsync(id);
    return result.IsSuccess
        ? Results.Ok(result.Value)      // Direct DTO - NO wrapper
        : Results.Problem(result.Error); // RFC 9457 Problem Details
});
```

**SERVICE LAYER** (Business Logic):
```csharp
// ✅ CORRECT - Result<T> pattern OR tuple pattern
public async Task<Result<EventDto>> GetEventAsync(int id)
{
    // Internal tuple: (bool Success, EventDto? Data, string Error)
    var eventEntity = await _db.Events.FindAsync(id);
    if (eventEntity == null)
        return Result<EventDto>.Failure("Event not found");

    return Result<EventDto>.Success(new EventDto(eventEntity));
}
```

**DEPRECATED PATTERN** (ApiResponse<T> wrapper - NO LONGER USED):
```csharp
// ❌ DEPRECATED - ApiResponse<T> wrapper is NO LONGER THE STANDARD
return Results.Ok(new ApiResponse<EventDto>
{
    Success = true,
    Data = dto,
    Timestamp = DateTime.UtcNow
});
```

**WHY PATTERN B IS THE STANDARD**:
1. **Industry Best Practice**: Milan Jovanović (2025), Microsoft .NET 9 guidance, RFC 9457 compliance
2. **Direct Results**: Cleaner, simpler, less ceremony
3. **RFC 9457 Compliance**: Standardized error format (Problem Details)
4. **No Double Wrapping**: Frontend doesn't need to unwrap twice
5. **Better HTTP Semantics**: Status codes match actual success/failure

**MIGRATION NOTE**:
- Legacy `ApiResponse<T>` references in OLD documentation are **historical only**
- Current standard is **Pattern B - Direct Results + RFC 9457 Problem Details**
- **ALL NEW CODE** must use Pattern B
- **DO NOT** add ApiResponse<T> wrappers to new endpoints
- See `/home/chad/repos/witchcityrope/docs/standards-processes/backend/api-design-patterns.md` for full details

## 🚨 CRITICAL: Path Format Standard - NO Full System Paths 🚨

**WRONG**: `/home/chad/repos/[repo-name]/docs/...` (absolute paths are not portable)
**RIGHT**: `/home/chad/repos/witchcityrope/docs/...`

**All documentation references must use repo-relative paths starting from project root**

## 🚨 CRITICAL: Docker-Only Testing Environment 🚨

**NEVER run local dev servers** - Docker containers ONLY for testing

**MANDATORY PRE-TESTING CHECKLIST**:
```bash
# 1. Use container-restart skill to verify and restart Docker containers
# The skill handles container verification and startup automatically

# 2. Verify API health (REQUIRED)
curl -f http://localhost:5655/health && echo "API healthy"

# 3. Kill any rogue local API processes
lsof -i :5655 | grep -v docker || echo "No conflicts"
```

**EMERGENCY PROTOCOL**: If tests fail, verify Docker containers FIRST before anything else

---

## 🚨 MANDATORY: Agent Handoff Documentation Process 🚨

**CRITICAL**: Create handoff documents for ALL backend work

**Location**: `/home/chad/repos/witchcityrope/docs/functional-areas/[feature]/handoffs/`
**Naming**: `backend-developer-YYYY-MM-DD-handoff.md`

**MUST INCLUDE**:
1. **API Endpoints**: New/modified endpoints with contracts
2. **Database Changes**: Schema updates, migrations, constraints
3. **Business Logic**: Validation rules and domain logic
4. **Integration Points**: External services and dependencies
5. **Testing Requirements**: API test needs and data setup

**FAILURE TO CREATE HANDOFFS = IMPLEMENTATION FAILURES**

---

## 🚨 CRITICAL ARCHITECTURE WARNINGS 🚨

### Legacy API vs Modern API
**MANDATORY**: ALL backend development must use the modern API only:
- ✅ **Use**: `/apps/api/` - Modern Vertical Slice Architecture
- ❌ **NEVER use**: `/src/_archive/WitchCityRope.Api/` - ARCHIVED legacy API

### DTO Alignment Strategy
**READ BEFORE ANY DTO CHANGES**: `/home/chad/repos/witchcityrope/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
- API DTOs are source of truth
- Frontend type generation must happen after DTO changes
- 393 TypeScript errors = ignored DTO alignment strategy

### Entity Framework Navigation Properties
**CRITICAL**: Both sides of EF relationships MUST have navigation properties
- Missing navigation properties cause silent persistence failures
- Check for bidirectional relationships before troubleshooting infrastructure

---
## 🚨 CMS Backend Implementation Reference 🚨

**Architecture Reference**: All CMS API endpoints, database schema, and seeding procedures are documented in `/home/chad/repos/witchcityrope/docs/guides-setup/cms-implementation-guide.md`. Consult this guide when:
- Implementing CMS API endpoints
- Modifying CMS database schema
- Adding new CMS pages via seed data
- Understanding CMS entity relationships
- Debugging CMS content retrieval issues

**Key Points**:
- CMS content stored in `CmsPage` and `CmsPageRevision` tables
- New pages added ONLY through `CmsSeedData.cs` (no admin UI for page creation)
- Revision history tracked automatically for all content changes
- API endpoints follow Pattern B standard (Direct Results + RFC 9457 Problem Details)
- Content validation handled server-side before persistence

---
