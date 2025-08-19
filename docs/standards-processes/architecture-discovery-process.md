# Architecture Discovery Process

<!-- Last Updated: 2025-08-19 -->
<!-- Version: 1.0 -->
<!-- Owner: Architecture Team -->
<!-- Status: ACTIVE - CRITICAL PROCESS -->

## 🚨 MANDATORY PHASE 0 FOR ALL TECHNICAL WORK 🚨

**This process is REQUIRED before any technical requirements, specifications, or development work begins.**

## Purpose

This document establishes the mandatory Architecture Discovery Process to prevent agents from proposing solutions that already exist in our architecture documents. This process was created after we spent hours creating manual DTO interfaces when NSwag auto-generation was already specified in our migration architecture.

## When This Process is Required

Architecture Discovery is MANDATORY for ALL technical work involving:
- ✅ API integration or data transfer
- ✅ Type definitions or interfaces  
- ✅ Authentication patterns
- ✅ Build processes or tooling
- ✅ Database schema or entity changes
- ✅ Frontend/backend communication
- ✅ New technology adoption
- ✅ Infrastructure or deployment changes

**NO EXCEPTIONS** - If your work has technical components, complete Architecture Discovery first.

## Phase 0: Architecture Discovery Checklist

### Step 1: Core Architecture Document Review (MANDATORY)

**MUST READ in this order:**

#### 1.1 Migration Architecture
- **Document**: `/docs/architecture/react-migration/domain-layer-architecture.md`
- **Focus Areas**: NSwag implementation, type generation pipeline, package structure
- **Key Sections**: Lines 725-997 (NSwag configuration and generation strategy)
- **Red Flags**: Any work involving DTO/type generation without referencing NSwag

#### 1.2 DTO Strategy  
- **Document**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
- **Focus Areas**: API as source of truth, auto-generation requirements
- **Key Sections**: Lines 85-213 (NSwag auto-generation implementation)
- **Red Flags**: Manual interface creation, assumptions about API structure

#### 1.3 Migration Plan
- **Document**: `/docs/architecture/react-migration/migration-plan.md`
- **Focus Areas**: Build process, dependency flow, migration phases
- **Key Sections**: Build and dependency information
- **Red Flags**: Proposing build changes without checking existing process

### Step 2: Functional Area Research

#### 2.1 Check Master Index
- **Document**: `/docs/architecture/functional-area-master-index.md`
- **Purpose**: Identify if your work area already has documentation
- **Action**: Search for related functional areas and existing work

#### 2.2 Search Existing Functional Areas
- **Location**: `/docs/functional-areas/*/`
- **Method**: Use keyword search for related concepts
- **Document**: List all related areas found

#### 2.3 Check Standards and Processes
- **Location**: `/docs/standards-processes/`
- **Focus**: Look for established patterns in your work area
- **Document**: Reference any applicable standards

### Step 3: Solution Verification

#### 3.1 Document Your Findings
For EACH architecture document reviewed, document:
```markdown
## Architecture Discovery Results

### Documents Reviewed:
- **domain-layer-architecture.md**: Lines [X-Y] - [solution found/not found]
- **DTO-ALIGNMENT-STRATEGY.md**: Lines [X-Y] - [solution found/not found]  
- **migration-plan.md**: Lines [X-Y] - [solution found/not found]
- **functional-area-master-index.md**: [areas searched]
- **[other docs]**: [findings]

### Existing Solutions Found:
- [List any existing solutions with specific line references]
- [Include why they do/don't meet requirements]

### Verification Statement:
"[Confirmed existing solution meets requirements / Confirmed no existing solution covers this requirement]"
```

#### 3.2 Reference Architecture in Your Work
- **Requirements**: Must reference specific lines from architecture docs
- **Example**: "Per domain-layer-architecture.md lines 725-750, NSwag auto-generates types"
- **Integration**: Show how your work builds on existing architecture

## Common Architecture Discovery Failures

### ❌ CRITICAL FAILURE: The NSwag Miss
**What Happened**: Agent created manual DTO interfaces without checking architecture docs
**Why It Failed**: Domain-layer-architecture.md lines 725-997 specify complete NSwag implementation
**Cost**: Hours of rework, architectural violations, type mismatches
**Prevention**: ALWAYS read architecture docs before proposing solutions

### ❌ Skipping Architecture Review
**Example**: "I'll create interfaces for the API responses"
**Problem**: Assumes manual work when automation exists
**Fix**: Check domain-layer-architecture.md first - NSwag generates all interfaces

### ❌ Incomplete Document Review
**Example**: Only reading titles or summaries
**Problem**: Missing critical implementation details
**Fix**: Read specific sections related to your work area

### ❌ Not Documenting Research
**Example**: "I checked the docs" without specifics
**Problem**: No verification that correct sections were reviewed
**Fix**: Document specific lines/sections reviewed

## Red Flag Keywords - STOP and Check Architecture

When you see these words in requirements or discussions:
- **"alignment"** → Check DTO-ALIGNMENT-STRATEGY.md  
- **"DTO"** → Check domain-layer-architecture.md NSwag sections
- **"type generation"** → Check NSwag implementation details
- **"interfaces"** → Verify auto-generation vs manual creation
- **"API contracts"** → Check existing DTO strategy
- **"build process"** → Check migration-plan.md
- **"authentication"** → Check established auth patterns

## Architecture Discovery Templates

### Template 1: API/DTO Related Work
```markdown
## Architecture Discovery - API/DTO Work

### Core Documents Reviewed:
- [ ] domain-layer-architecture.md - NSwag sections (lines 725-997)
- [ ] DTO-ALIGNMENT-STRATEGY.md - Auto-generation requirements  
- [ ] migration-plan.md - Build process impact

### Findings:
- **NSwag Implementation**: [Found/Not Found] - Lines [X-Y]
- **Type Generation**: [Automated/Manual Required] - Lines [X-Y]
- **Build Integration**: [Exists/Needs Creation] - Lines [X-Y]

### Solution Approach:
[Reference existing architecture or justify new approach]
```

### Template 2: General Technical Work
```markdown
## Architecture Discovery - [Work Type]

### Documents Reviewed:
- [ ] [Primary architecture doc] - Lines [X-Y]
- [ ] [Secondary docs] - Sections [A-B]
- [ ] Functional areas searched: [list]
- [ ] Standards checked: [list]

### Existing Solutions:
- [List with line references]
- [Applicability assessment]

### New Work Justified:
[Why existing solutions don't meet requirements]
```

## Quality Gates

### Before Technical Requirements Creation
- [ ] Architecture Discovery completed
- [ ] Findings documented with line references  
- [ ] Existing solutions evaluated
- [ ] Integration approach defined

### Before Specification Creation
- [ ] Architecture patterns referenced
- [ ] Line numbers cited from architecture docs
- [ ] Build process impact assessed
- [ ] Standards compliance verified

### Before Development Work
- [ ] Implementation approach aligns with architecture
- [ ] Generated code patterns used where specified
- [ ] Manual work minimized per architecture guidance
- [ ] Dependencies follow established patterns

## Enforcement

### Agent Responsibility
Every agent MUST complete Architecture Discovery before technical work. No exceptions.

### Review Process
All technical deliverables must include Architecture Discovery section showing:
1. Documents reviewed (with line numbers)
2. Existing solutions found/evaluated
3. Integration with established patterns

### Escalation
If Architecture Discovery reveals missing solutions or conflicts:
1. Flag to Architecture Review Board
2. Pause work until resolution
3. Update architecture docs if needed

## Success Metrics

- **Zero Manual Solutions** for problems already solved in architecture
- **100% Reference Rate** - All technical work references architecture docs
- **50% Faster Development** - Less rework due to architecture alignment
- **Zero Architecture Violations** - All work follows established patterns

## Examples of Successful Architecture Discovery

### Example 1: Type Generation Work
```markdown
## Architecture Discovery - User Profile Types

### Documents Reviewed:
- domain-layer-architecture.md lines 725-997: Found complete NSwag implementation
- DTO-ALIGNMENT-STRATEGY.md lines 85-213: Confirmed auto-generation requirement

### Existing Solutions:
- NSwag pipeline generates all TypeScript interfaces from C# DTOs
- Build process includes type generation via npm run generate:types
- Package structure already defined in packages/shared-types/

### Approach:
Following existing NSwag implementation rather than manual interface creation.
Types will be auto-generated per domain-layer-architecture.md lines 725-750.
```

### Example 2: New Functional Area
```markdown
## Architecture Discovery - Payment Processing

### Documents Reviewed:
- functional-area-master-index.md: No existing payment processing area
- domain-layer-architecture.md: Payment domain models specified lines 64-65
- migration-plan.md: Payment service mentioned in build dependencies

### Existing Solutions:
- Domain models exist but no functional area documentation
- Architecture provides foundation but needs functional specification

### Approach:
Create new functional area following template structure, building on existing domain models.
```

## Related Documentation

- **Migration Architecture**: `/docs/architecture/react-migration/domain-layer-architecture.md`
- **DTO Strategy**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`  
- **Master Index**: `/docs/architecture/functional-area-master-index.md`
- **Migration Plan**: `/docs/architecture/react-migration/migration-plan.md`

---

**Remember**: Architecture Discovery prevents costly rework and ensures your work builds on established patterns. When in doubt, read the architecture docs first.

*Created: 2025-08-19 - Response to NSwag solution miss*
*Last Updated: 2025-08-19*