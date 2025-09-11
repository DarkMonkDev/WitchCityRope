# Functional Specification Lessons Learned

## 🚨 CRITICAL: WORKTREE COMPLIANCE - MANDATORY 🚨

### ALL WORK MUST BE IN THE SPECIFIED WORKTREE DIRECTORY

**VIOLATION = CATASTROPHIC FAILURE**

When given a Working Directory like:
`/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management`

**YOU MUST:**
- Write ALL files to paths within the worktree directory
- NEVER write to `/home/chad/repos/witchcityrope-react/` main repository
- ALWAYS use the full worktree path in file operations
- VERIFY you're in the correct directory before ANY file operation

**Example:**
- ✅ CORRECT: `/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/docs/...`
- ❌ WRONG: `/home/chad/repos/witchcityrope-react/docs/...`

**Why This Matters:**
- Worktrees isolate feature branches
- Writing to main repo pollutes other branches
- Can cause merge conflicts and lost work
- BREAKS the entire development workflow

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

## Approved Research Websites for .NET and C# Technologies

**Date**: 2025-08-22
**Category**: Research Sources
**Severity**: Critical

### Context
When creating functional specifications that involve .NET 9 technologies, use these curated, authoritative sources for research and validation.

### Approved Sources
1. **Milan Jovanović's Blog** (https://www.milanjovanovic.tech/)
   - Clean architecture, domain-driven design
   - .NET performance optimization
   - Minimal APIs and vertical slice architecture

2. **CodeOpinion** (https://codeopinion.com/)
   - Event-driven architecture patterns
   - Microservices and distributed systems
   - CQRS and Event Sourcing

3. **Scott Brady's Articles** (https://www.scottbrady.io/articles)
   - Security and authentication best practices
   - OAuth 2.0 and OpenID Connect
   - ASP.NET Core security patterns

4. **Microsoft .NET Blog** (https://devblogs.microsoft.com/dotnet/)
   - Official .NET announcements and releases
   - Performance improvements and benchmarks
   - New feature documentation

### Research Guidelines
- **ALWAYS verify article date** - Must be published within last 6 months for .NET 9
- **CHECK .NET version compatibility** - Ensure content applies to .NET 9
- **CROSS-REFERENCE sources** - Validate recommendations across multiple authorities

### Tags
#critical #research-sources #dotnet9 #approved-websites

---

*This file captures lessons learned for functional specification creation. Add new lessons immediately when discovered, remove outdated entries as needed.*
*Last updated: 2025-08-22 - Added approved research websites for .NET technologies*