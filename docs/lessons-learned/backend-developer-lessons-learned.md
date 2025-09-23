# Backend Developer Lessons Learned

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY STARTUP PROCEDURE - MUST READ 🚨

### 🚨 ULTRA CRITICAL ARCHITECTURE DOCUMENTS (MUST READ): 🚨
1. **🛑 DTO ALIGNMENT STRATEGY** - **PREVENTS 393 TYPESCRIPT ERRORS**
`/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`

2. **API Architecture Overview** - **CORE BACKEND PATTERNS**
`/docs/architecture/API-ARCHITECTURE-OVERVIEW.md`

3. **Vertical Slice Quick Start** - **FEATURE-BASED ARCHITECTURE**
`/docs/guides-setup/VERTICAL-SLICE-QUICK-START.md`

4. **Entity Framework Patterns** - **DATABASE PATTERNS**
`/docs/standards-processes/development-standards/entity-framework-patterns.md`

5. **Project Architecture** - **TECH STACK AND STANDARDS**
`/ARCHITECTURE.md`

### 📚 DOCUMENT DISCOVERY RESOURCES:
- **File Registry** - `/docs/architecture/file-registry.md` - Find any document
- **Functional Areas Index** - `/docs/architecture/functional-area-master-index.md` - Navigate features
- **Key Documents List** - `/docs/standards-processes/KEY-PROJECT-DOCUMENTS.md` - Critical docs
- **Standards Index** - `/docs/standards-processes/CANONICAL-DOCUMENT-LOCATIONS.md` - Document locations

### 📖 ADDITIONAL IMPORTANT DOCUMENTS:
- **Workflow Process** - `/docs/standards-processes/workflow-orchestration-process.md` - Handoff procedures
- **Agent Boundaries** - `/docs/standards-processes/agent-boundaries.md` - What each agent does
- **Coding Standards** - `/docs/standards-processes/CODING_STANDARDS.md` - General standards
- **Documentation Standards** - `/docs/standards-processes/documentation-standards.md` - How to document

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
- **API Response wrappers** - ALL endpoints must return `ApiResponse<T>` format
- **Path format** - ALWAYS use repo-relative paths like `/docs/...` NOT full system paths

---

## 🚨 IF THIS FILE EXCEEDS 1700 LINES, add new lessons learned to PART 2! BOTH FILES CAN BE UP TO 1700 LINES EACH 🚨

## 📚 MULTI-FILE LESSONS LEARNED
**Files**: 2 total
**Part 1**: `/docs/lessons-learned/backend-developer-lessons-learned.md` (THIS FILE - STARTUP ONLY)
**Part 2**: `/docs/lessons-learned/backend-developer-lessons-learned-2.md` (MAIN LESSONS FILE)
**Read ALL**: Both Part 1 AND Part 2 are MANDATORY
**Write to**: Part 2 ONLY - **NEVER ADD NEW LESSONS TO THIS FILE (PART 1)**
**Maximum file size**: 1700 lines (to stay under token limits). Both Part 1 and Part 2 files can be up to 1700 lines each
**IF READ FAILS**: STOP and fix per documentation-standards.md

## 🚨 ULTRA CRITICAL: NEW LESSONS GO TO PART 2, NOT HERE! 🚨
**PART 1 PURPOSE**: Startup procedures and critical navigation ONLY
**ALL NEW LESSONS**: Must go to Part 2 - `/docs/lessons-learned/backend-developer-lessons-learned-2.md`
**IF YOU ADD LESSONS HERE**: You are violating the split pattern!

## ⛔ CRITICAL: HARD BLOCK - DO NOT PROCEED IF FILES UNREADABLE
If you cannot read ANY file:
1. STOP ALL WORK
2. Fix using procedure in documentation-standards.md
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

## 🚨 CRITICAL: API Response Format Mismatch - Frontend Shows "No Data" 🚨

**Problem**: Frontend shows "No data" despite API returning valid data
**Root Cause**: API returns `List<T>` directly, but frontend expects `ApiResponse<List<T>>` wrapper

**BEFORE (BROKEN)**:
```csharp
// ❌ RETURNS RAW ARRAY - Frontend gets undefined data
return Results.Ok(result.Value);  // Direct array
```

**AFTER (FIXED)**:
```csharp
// ✅ RETURNS WRAPPED FORMAT - Frontend gets data properly
return Results.Ok(new ApiResponse<List<EventParticipationDto>>
{
    Success = true,
    Data = result.Value,  // Array in 'data' property
    Timestamp = DateTime.UtcNow
});
```

**MANDATORY**: ALL API endpoints must return consistent `ApiResponse<T>` wrapper format

## 🚨 CRITICAL: Path Format Standard - NO Full System Paths 🚨

**WRONG**: `/home/chad/repos/witchcityrope-react/docs/...`
**RIGHT**: `/docs/...`

**All documentation references must use repo-relative paths starting from project root**

## 🚨 CRITICAL: Docker-Only Testing Environment 🚨

**NEVER run local dev servers** - Docker containers ONLY for testing

**MANDATORY PRE-TESTING CHECKLIST**:
```bash
# 1. Verify Docker API container (CRITICAL)
docker ps | grep witchcity-api | grep "5655"

# 2. Verify API health (REQUIRED)
curl -f http://localhost:5655/health && echo "API healthy"

# 3. Kill any rogue local API processes
lsof -i :5655 | grep -v docker || echo "No conflicts"
```

**EMERGENCY PROTOCOL**: If tests fail, verify Docker containers FIRST before anything else

---

## 🚨 MANDATORY: Agent Handoff Documentation Process 🚨

**CRITICAL**: Create handoff documents for ALL backend work

**Location**: `/docs/functional-areas/[feature]/handoffs/`
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
**READ BEFORE ANY DTO CHANGES**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
- API DTOs are source of truth
- Frontend type generation must happen after DTO changes
- 393 TypeScript errors = ignored DTO alignment strategy

### Entity Framework Navigation Properties
**CRITICAL**: Both sides of EF relationships MUST have navigation properties
- Missing navigation properties cause silent persistence failures
- Check for bidirectional relationships before troubleshooting infrastructure

---
