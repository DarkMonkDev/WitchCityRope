# Business Requirements Lessons Learned


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

### Business Requirements Agent Specific Rules:
- **BEFORE creating ANY technical requirements, complete Architecture Discovery Phase**
- **If work involves DTOs, APIs, or types → Check domain-layer-architecture.md FIRST**

---

## 🚨 MANDATORY: Agent Handoff Documentation Process 🚨

**CRITICAL**: This is NOT optional - handoff documentation is REQUIRED for workflow continuity.

### 📋 WHEN TO CREATE HANDOFF DOCUMENTS
- **END of requirements phase** - BEFORE design begins
- **COMPLETION of user stories** - Document critical business rules
- **DISCOVERY of constraints** - Share immediately
- **STAKEHOLDER DECISIONS** - Document all approvals and changes

### 📁 WHERE TO SAVE HANDOFFS
**Location**: `/docs/functional-areas/[feature]/handoffs/`
**Naming**: `business-requirements-YYYY-MM-DD-handoff.md`
**Template**: `/docs/standards-processes/agent-handoff-template.md`

### 📝 WHAT TO INCLUDE (TOP 5 CRITICAL)
1. **Business Rules**: Critical rules that MUST be implemented
2. **User Stories**: Complete stories with acceptance criteria
3. **Stakeholder Decisions**: Approved requirements and changes
4. **Constraints**: Technical and business limitations discovered
5. **Success Criteria**: How to validate implementation

### 🤝 WHO NEEDS YOUR HANDOFFS
- **UI Designers**: User flow and interaction requirements
- **Functional Spec Agents**: Business rules for technical specs
- **Backend Developers**: Domain logic and validation rules
- **Test Developers**: Acceptance criteria for test cases

### ⚠️ MANDATORY READING BEFORE STARTING
**ALWAYS READ EXISTING HANDOFFS FIRST**:
1. Check `/docs/functional-areas/[feature]/handoffs/` for previous work
2. Read ALL handoff documents in the functional area
3. Understand business context already established
4. Build on existing requirements - don't contradict decisions

### 🚨 FAILURE TO CREATE HANDOFFS = IMPLEMENTATION FAILURES
**Why this matters**:
- Design will miss critical business rules
- Implementation will violate requirements
- Test cases won't cover acceptance criteria
- Features will need complete rework

**NO EXCEPTIONS**: Create handoff documents or workflow WILL fail.
- **Document: 'Verified no existing solution in: [docs checked]'**
- **NEVER specify manual DTO creation - all types come from NSwag auto-generation**

---

## 🚨 CRITICAL: DTO Specification Requirements (READ FIRST) 🚨
**Date**: 2025-08-19
**Category**: Requirements Documentation
**Severity**: Critical

### Context
Business requirements must specify exact data structure requirements to prevent DTO/interface mismatches during implementation.

### What We Learned
- **NSwag Auto-Generation Workflow**: Requirements drive C# DTOs which auto-generate TypeScript types
- **OpenAPI Documentation Critical**: Well-documented DTOs in requirements ensure perfect generated types
- **No Frontend Type Specifications**: Requirements specify business needs - NSwag handles TypeScript
- **DTO Changes Auto-Propagate**: C# DTO modifications automatically update frontend via generation
- **Business Logic vs Data Transfer**: Requirements must separate business rules from API data contracts
- **Change Management**: Breaking DTO changes require 30-day notice for frontend coordination

### Action Items
- [ ] READ: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md` (updated with NSwag emphasis)
- [ ] READ: `/docs/architecture/react-migration/domain-layer-architecture.md` for NSwag workflow
- [ ] SPECIFY: Business data needs - let NSwag handle TypeScript generation
- [ ] DOCUMENT: OpenAPI annotation requirements for comprehensive type generation
- [ ] REFERENCE: Existing C# DTOs in packages/contracts/ before new specifications
- [ ] COORDINATE: DTO changes with 30-day frontend notice requirement

### Requirements Documentation Standards
```markdown
## Data Structure Requirements

### User Profile Data
- sceneName: string (required, 3-50 characters)
- createdAt: DateTime (required, ISO 8601 format)
- lastLoginAt: DateTime? (optional, can be null)
- roles: List<string> (required, from predefined role enum)
- isVetted: boolean (required, default: false)
- membershipLevel: string (required, from membership enum)

### Business Rules
- sceneName must be unique across platform
- lastLoginAt updates on each successful authentication
- roles array cannot be empty for authenticated users
```

### Tags
#critical #requirements #dto-specification #data-structures #api-contracts

---

# Business Requirements Lessons Learned

## Always Check File Existence Before Reading

**Date**: 2025-08-17
**Category**: File Management
**Severity**: Critical

### Context
Attempting to read files that may not exist causes errors and breaks the workflow. Business requirements analysis often involves reading various documentation files that may or may not exist.

### What We Learned
- Always check if files exist before attempting to read them
- Use file discovery patterns to verify file presence
- Don't assume files exist based on naming conventions
- Handle file-not-found gracefully rather than failing

### Action Items
- [ ] Use LS or Glob to check file existence before Read operations
- [ ] Provide graceful fallbacks when expected files don't exist
- [ ] Document when files are expected vs optional in requirements analysis
- [ ] Create files only when explicitly required, never speculatively

### Impact
Prevents workflow failures and provides more robust requirements analysis process.

### Tags
#critical #file-management #error-handling

## Check Master Index Before Starting Requirements Work

**Date**: 2025-08-17
**Category**: Process
**Severity**: Critical

### Context
Must check the functional area master index before starting new requirements work to avoid duplication and understand existing work.

### What We Learned
- Master index contains authoritative list of all functional areas
- Existing requirements may already cover requested functionality
- Current work status prevents conflicts with ongoing development
- Historical work provides essential context

### Action Items
- [ ] Always check `/docs/architecture/functional-area-master-index.md` first
- [ ] Identify existing functional areas with relevant requirements
- [ ] Review current work status to avoid conflicts
- [ ] Reference existing requirements when building on previous work

### Impact
Prevents duplicate work, ensures consistency, and builds on established patterns.

### Tags
#critical #process #master-index #duplication-prevention

## Apply Domain-Specific Business Knowledge

**Date**: 2025-08-17
**Category**: Analysis
**Severity**: High

### Context
WitchCityRope serves a specialized community with unique needs that must be understood for effective requirements analysis.

### What We Learned
- Rope bondage community has specific consent, safety, and privacy needs
- Event management requires specialized pricing models (sliding scale)
- Membership tiers significantly affect access patterns and requirements
- Community vetting processes are critical for trust and safety

### Action Items
- [ ] Consider safety and consent implications in all requirements
- [ ] Apply community-specific patterns for membership and access
- [ ] Reference existing successful platform patterns
- [ ] Leverage domain expertise rather than generic business analysis

### Impact
Ensures requirements align with community needs and leverage domain understanding for better solutions.

### Tags
#high #domain-expertise #community-analysis #business-patterns

## Analyze All User Roles in Requirements

**Date**: 2025-08-17
**Category**: Quality
**Severity**: High

### Context
Platform has distinct user roles (Admin, Teacher, Vetted Member, General Member, Guest) with different capabilities and needs.

### What We Learned
- Each user role has different access capabilities and restrictions
- Safety and privacy requirements differ significantly by role
- Business rules must account for role-based permissions
- Access patterns vary between role types

### Action Items
- [ ] Analyze impact on all user roles for every requirement
- [ ] Document role-specific business rules and restrictions
- [ ] Consider privacy implications for each role interaction
- [ ] Validate requirements against existing role permission patterns

### Impact
Ensures comprehensive requirements that work for all users and maintain appropriate access controls.

### Tags
#high #quality #user-roles #access-control

## Align Requirements with React Architecture

**Date**: 2025-08-17
**Category**: Architecture
**Severity**: Medium

### Context
All requirements must be feasible within React + TypeScript frontend and .NET API backend architecture.

### What We Learned
- React component patterns influence UI requirements design
- API-driven architecture affects data flow requirements
- Authentication uses hybrid JWT + HttpOnly Cookies pattern
- Real-time features may require SignalR implementation

### Action Items
- [ ] Ensure UI requirements align with React component patterns
- [ ] Consider API design implications in data requirements
- [ ] Reference existing authentication patterns for security requirements
- [ ] Validate technical feasibility with development team when uncertain

### Impact
Ensures requirements can be implemented efficiently within existing technology stack.

### Tags
#medium #architecture #technology-alignment #feasibility

## Use Structured Requirements Documentation

**Date**: 2025-08-19
**Category**: Documentation
**Severity**: Medium

### Context
Business requirements need consistent structure for effective communication with development teams and stakeholders.

### What We Learned
- Standardized format improves clarity and completeness
- User stories with acceptance criteria provide clear implementation guidance
- Business rules must be explicitly documented
- Success metrics are essential for validating implementation

### Action Items
- [ ] Use standardized business requirements template
- [ ] Include user stories with clear acceptance criteria
- [ ] Document business rules explicitly
- [ ] Define measurable success metrics
- [ ] Provide examples and scenarios for complex requirements

### Impact
Improves communication clarity and reduces implementation ambiguity.

### Tags
#medium #documentation #structure #communication