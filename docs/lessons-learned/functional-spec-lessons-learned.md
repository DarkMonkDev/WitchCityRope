# Functional Specification Lessons Learned

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

### Functional Specification Agent Specific Rules:
- **PHASE 0 is now MANDATORY: Architecture Discovery before any specification**
- **RED FLAG words: 'alignment', 'DTO', 'type generation' → STOP and check NSwag docs**
- **Reference line numbers from architecture docs in specifications**
- **Document: 'Verified existing solutions in: [architecture docs checked]'**

---

## CRITICAL: Architecture Discovery Phase is Phase 0

**Date**: 2025-08-19
**Category**: Process
**Severity**: Critical

### Context
We missed the NSwag solution because agents didn't check existing architecture documents before creating specifications. Architecture Discovery is now MANDATORY Phase 0.

### What We Learned
- Architecture Discovery must happen BEFORE functional specification creation
- Existing solutions already documented in architecture docs prevent reinventing solutions
- NSwag auto-generation was already specified in domain-layer-architecture.md
- Manual DTO interface creation violates established architecture patterns
- Agent assumptions without architecture review cause massive rework

### Action Items
- [ ] ALWAYS complete Architecture Discovery Phase before any specification work
- [ ] READ domain-layer-architecture.md for ALL API/DTO related work
- [ ] REFERENCE specific line numbers from architecture docs in specifications
- [ ] DOCUMENT which architecture documents were reviewed and what solutions exist
- [ ] FAIL FAST if proposing solutions that already exist in architecture

### Implementation Rules
```markdown
## Architecture Discovery Phase (MANDATORY PHASE 0)

### Documents Reviewed:
- [ ] `/docs/architecture/react-migration/domain-layer-architecture.md` - Lines reviewed: [specific lines]
- [ ] `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md` - Lines reviewed: [specific lines]
- [ ] `/docs/architecture/react-migration/migration-plan.md` - Lines reviewed: [specific lines]

### Existing Solutions Found:
- NSwag auto-generation: [reference specific lines from domain-layer-architecture.md]
- DTO patterns: [reference specific lines from DTO-ALIGNMENT-STRATEGY.md]
- Build process: [reference specific lines from migration-plan.md]

### Verification Statement:
"Confirmed no existing solution covers this requirement in reviewed architecture documents."
```

### Tags
#critical #architecture-discovery #process #nswag-solution #phase-0

---

## Never Specify Manual DTO Interface Creation

**Date**: 2025-08-19
**Category**: Architecture
**Severity**: Critical

### Context
Functional specifications must never specify manual DTO interface creation. All TypeScript types are auto-generated via NSwag pipeline.

### What We Learned
- NSwag auto-generation is THE solution specified in original architecture
- Manual interface creation violates migration architecture principles
- Generated types ensure perfect backend/frontend alignment automatically
- Specifications should reference NSwag patterns, not manual creation

### Action Items
- [ ] NEVER specify manual TypeScript interface creation for DTOs
- [ ] ALWAYS reference NSwag generation in specifications: "Types will be auto-generated via NSwag"
- [ ] SPECIFY OpenAPI annotation requirements for backend
- [ ] REFERENCE packages/shared-types structure from domain-layer-architecture.md

### Tags
#critical #nswag #dto-generation #specification-standards

---

*This file captures lessons learned for functional specification creation. Add new lessons immediately when discovered, remove outdated entries as needed.*
*Last updated: 2025-08-19 - Initial creation with critical architecture discovery lessons*