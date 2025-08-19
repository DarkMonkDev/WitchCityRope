# DTO Alignment Strategy for React Migration
<!-- Last Updated: 2025-08-19 -->
<!-- Version: 1.0 -->
<!-- Owner: Architecture Team -->
<!-- Status: ACTIVE - CRITICAL FOR ALL MIGRATION WORK -->

## 🚨 CRITICAL: API DTOs ARE SOURCE OF TRUTH 🚨

**ALL DEVELOPERS MUST READ THIS BEFORE ANY MIGRATION WORK**

This document establishes the fundamental data alignment strategy for the React migration project. **VIOLATION OF THESE PRINCIPLES WILL CAUSE PROJECT DELAYS AND INTEGRATION FAILURES.**

## Core Principles (NON-NEGOTIABLE)

### 1. API DTOs as Source of Truth
- ✅ **CORRECT**: Frontend adapts to backend DTO structure
- ❌ **WRONG**: Backend changes DTOs to match frontend expectations
- **Rationale**: Existing .NET API DTOs represent tested, stable data contracts

### 2. Frontend Adaptation Required
- ✅ **CORRECT**: TypeScript interfaces match C# DTOs exactly
- ❌ **WRONG**: Assume property names or create "ideal" interfaces
- **Implementation**: Always reference actual API responses, never assumptions

### 3. Breaking Changes Governance
- ✅ **CORRECT**: 30-day notice for any breaking DTO changes
- ❌ **WRONG**: Immediate backend changes without frontend coordination
- **Process**: Change control board approval required

### 4. Type Safety Enforcement
- ✅ **CORRECT**: Strict TypeScript mode with exact property matching
- ❌ **WRONG**: Type assertions or `any` types to bypass mismatches
- **Validation**: Automated tests verify DTO/interface alignment

## Quick Reference Examples

### User Authentication DTO
```csharp
// C# DTO (SOURCE OF TRUTH)
public class UserDto
{
    public string SceneName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> Roles { get; set; }
}
```

```typescript
// TypeScript Interface (MUST MATCH EXACTLY)
interface User {
  sceneName: string;
  createdAt: string; // ISO 8601 format
  lastLoginAt: string | null;
  roles: string[];
}
```

### Event Registration DTO
```csharp
// C# DTO (SOURCE OF TRUTH)
public class EventRegistrationDto
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; }
    public DateTime EventDate { get; set; }
    public string AttendeeSceneName { get; set; }
    public RegistrationStatus Status { get; set; }
    public decimal? PaymentAmount { get; set; }
}
```

```typescript
// TypeScript Interface (MUST MATCH EXACTLY)
interface EventRegistration {
  eventId: string; // Guid serializes to string
  eventTitle: string;
  eventDate: string; // ISO 8601 format
  attendeeSceneName: string;
  status: 'pending' | 'confirmed' | 'cancelled';
  paymentAmount: number | null;
}
```

## 🚨 CRITICAL: NSwag Auto-Generation is THE SOLUTION 🚨

**NEVER MANUALLY CREATE DTO INTERFACES**

The original migration architecture specifies NSwag for auto-generating TypeScript types from C# DTOs. This is THE solution that prevents the exact problems we just spent hours fixing (manual User interface not matching API).

### Implementation via NSwag Auto-Generation

**See**: `/docs/architecture/react-migration/domain-layer-architecture.md` for complete NSwag implementation details.

**Key Points:**
- **packages/shared-types/**: Contains ALL generated TypeScript interfaces
- **NSwag Pipeline**: Automatically generates types from OpenAPI specification
- **CI/CD Integration**: Build fails if types are out of sync
- **Zero Manual Work**: No TypeScript interfaces should be manually created for DTOs

#### NSwag Generation Process
```bash
# 1. API generates OpenAPI specification
# 2. NSwag reads specification and generates TypeScript
npm run generate:types

# 3. All components import from generated types
import { User, Event, Registration } from '@witchcityrope/shared-types';
```

#### Directory Structure (Per Original Architecture)
```
packages/
├── shared-types/                   # Generated TypeScript types
│   ├── src/
│   │   ├── models/                # Generated interfaces from DTOs
│   │   ├── enums/                 # Generated enums from C#
│   │   ├── api/                   # Generated API client
│   │   └── generated/             # Raw NSwag output
│   ├── scripts/
│   │   ├── generate-types.js      # Generation script
│   │   └── nswag.json            # NSwag configuration
│   └── package.json
```

## Implementation Requirements

### For React Developers
1. **NEVER manually create DTO interfaces** - Import from @witchcityrope/shared-types
2. **ALWAYS use generated types** from packages/shared-types/src/generated/
3. **Run generation script** when API changes: `npm run generate:types`
4. **Test with generated types** - No manual interface creation ever

### For Backend Developers
1. **Add OpenAPI annotations** to all DTOs - This generates frontend types automatically
2. **Version breaking changes** using API versioning
3. **Communicate changes** minimum 30 days before implementation
4. **Commit both API and generated type changes** together in same PR

### For QA Engineers
1. **Test actual API responses** match documented DTOs
2. **Validate interface alignment** in integration tests
3. **Verify null handling** across all scenarios
4. **Cross-browser validation** of data handling

## Change Management Process

### Non-Breaking Changes (Allowed)
- Adding new optional properties
- Adding new enum values (with backward compatibility)
- Documentation updates
- Internal implementation changes not affecting response structure

### Breaking Changes (Requires Approval)
- Removing properties
- Changing property names
- Changing property types
- Making optional properties required
- Changing enum values

### Emergency Override
Only for critical security issues or data corruption fixes. Requires:
1. Executive approval
2. Immediate frontend team notification
3. Emergency deployment procedures
4. Post-incident review

## NSwag Auto-Generation Pipeline

### How to Update Types When API Changes

**Step 1**: Backend developer adds/modifies C# DTO
```csharp
// Add OpenAPI annotations
[ApiController]
public class UsersController : ControllerBase
{
    /// <summary>
    /// Gets user profile information
    /// </summary>
    /// <returns>User profile data</returns>
    [HttpGet("{id}")]
    [ProducesResponseType<UserDto>(200)]
    public async Task<UserDto> GetUser(Guid id) { ... }
}
```

**Step 2**: Frontend developer regenerates types
```bash
# Ensure API is running
docker-compose up api

# Generate fresh TypeScript types
npm run generate:types

# Types are automatically updated in packages/shared-types/
```

**Step 3**: Use generated types in components
```typescript
// CORRECT - Use generated types
import { User } from '@witchcityrope/shared-types';

interface UserProfileProps {
  user: User; // Generated from C# UserDto
}

// WRONG - Never create manual interfaces
interface User {
  // This violates the architecture!
}
```

### Automated Validation
```bash
# CI/CD Pipeline Checks
npm run generate:types         # Auto-generate from API
npm run test:integration       # Test actual API responses
npm run type-check             # Strict TypeScript validation
```

### Manual Validation
1. **Code Reviews**: All interface changes reviewed by both frontend and backend teams
2. **API Testing**: Real API responses tested against TypeScript interfaces
3. **Documentation Updates**: All DTO changes documented immediately

## Common Violations to Avoid

### ❌ CRITICAL VIOLATION: Manual Interface Creation
```typescript
// WRONG - Creating manual DTO interfaces
interface User {
  sceneName: string;
  // This should NEVER be manually created!
  // Use generated types from @witchcityrope/shared-types
}

// CORRECT - Import generated types
import { User } from '@witchcityrope/shared-types';
```

### ❌ Frontend Creates "Ideal" Interfaces
```typescript
// WRONG - Assuming ideal property names
interface User {
  firstName: string;  // API doesn't return this
  lastName: string;   // API doesn't return this
  email: string;      // API doesn't return this
}
```

### ❌ Backend Changes DTOs Without Notice
```csharp
// WRONG - Breaking change without coordination
public class UserDto
{
    // Removed SceneName property without notice
    public string FullName { get; set; } // New property
}
```

### ❌ Using Type Assertions to Bypass Mismatches
```typescript
// WRONG - Hiding type mismatches
const user = response.data as User; // Bypasses type checking
```

### ❌ Inconsistent Null Handling
```typescript
// WRONG - Not handling null/undefined properly
interface User {
  lastLoginAt: string; // Should be string | null
}
```

## Success Metrics

- **Zero Manual DTO Interfaces**: All types generated via NSwag pipeline
- **Zero Type Mismatch Errors**: Generated types automatically match backend DTOs
- **90% Reduction in API Integration Issues**: No more manual alignment debugging
- **100% Test Suite Success**: Generated types ensure API/frontend consistency
- **50% Faster Feature Development**: No manual interface creation or alignment work

## Current State: Temporary Manual Implementation

**IMPORTANT**: The manual User interface we recently fixed was created before NSwag pipeline implementation. This is exactly the problem NSwag prevents!

**Action Required**: Replace all manual DTO interfaces with generated types once NSwag pipeline is established.

## Related Documentation

- **🚨 CRITICAL**: `/docs/architecture/react-migration/domain-layer-architecture.md` - NSwag implementation details
- **Migration Plan**: `/docs/architecture/react-migration/migration-plan.md`
- **Quick Reference**: `/docs/guides-setup/dto-quick-reference.md`
- **Coding Standards**: `/docs/standards-processes/CODING_STANDARDS.md`
- **Forms Validation**: `/docs/standards-processes/forms-validation-requirements.md`

## Questions or Violations?

**Contact immediately**:
- **Frontend Issues**: React Developer Team Lead
- **Backend Issues**: .NET API Team Lead  
- **Process Questions**: Project Manager
- **Emergency**: Architecture Review Board

---

**Remember**: This strategy prevents costly debugging cycles, reduces integration failures, and ensures smooth migration execution. When in doubt, **API DTOs are always the source of truth**.

*Last updated: 2025-08-19 - Created for React migration project*