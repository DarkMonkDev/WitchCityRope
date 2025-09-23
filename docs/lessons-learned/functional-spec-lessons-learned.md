# Functional Specification Lessons Learned

## 🚨 MANDATORY STARTUP PROCEDURE 🚨

### 🚨 ULTRA CRITICAL ARCHITECTURE DOCUMENTS (MUST READ): 🚨
1. **🛑 DTO ALIGNMENT STRATEGY** - **PREVENTS 393 TYPESCRIPT ERRORS**
`/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`

2. **Migration Architecture** - **CORE TYPE GENERATION PATTERNS**
`/docs/architecture/react-migration/domain-layer-architecture.md`

3. **API Architecture Overview** - **BACKEND PATTERNS**
`/docs/architecture/API-ARCHITECTURE-OVERVIEW.md`

4. **Migration Plan** - **IMPLEMENTATION ROADMAP**
`/docs/architecture/react-migration/migration-plan.md`

### 📚 DOCUMENT DISCOVERY RESOURCES:
- **File Registry** - `/docs/architecture/file-registry.md` - Find any document
- **Functional Areas Index** - `/docs/architecture/functional-area-master-index.md` - Navigate features
- **Key Documents List** - `/docs/standards-processes/KEY-PROJECT-DOCUMENTS.md` - Critical docs

### 📖 ADDITIONAL IMPORTANT DOCUMENTS:
- **Workflow Process** - `/docs/standards-processes/workflow-orchestration-process.md` - 5-phase workflow
- **Phase Validation** - `/docs/standards-processes/PHASE-BASED-VALIDATION-SYSTEM.md` - Quality gates
- **Agent Handoff Template** - `/docs/standards-processes/agent-handoff-template.md` - Documentation format
- **Agent Boundaries** - `/docs/standards-processes/agent-boundaries.md` - What each agent does

### Validation Gates (MUST COMPLETE):
- [ ] Read all architecture documents above
- [ ] Complete Architecture Discovery Phase FIRST
- [ ] Check if solution already exists in architecture
- [ ] Reference existing patterns in your work
- [ ] NEVER create manual DTO interfaces (use NSwag)
- [ ] Create functional spec handoff document when complete

### Functional Specification Agent Specific Rules:
- **PHASE 0 is now MANDATORY: Architecture Discovery before any specification**
- **RED FLAG words: 'alignment', 'DTO', 'type generation' → STOP and check NSwag docs**
- **Reference line numbers from architecture docs in specifications**
- **Document: 'Verified existing solutions in: [architecture docs checked]'**

## 🚨 MANDATORY: Agent Handoff Documentation Process 🚨

**CRITICAL**: This is NOT optional - handoff documentation is REQUIRED for workflow continuity.

### 📋 WHEN TO CREATE HANDOFF DOCUMENTS
- **END of functional spec phase** - BEFORE implementation begins
- **COMPLETION of technical specifications** - Document all decisions
- **DISCOVERY of technical constraints** - Share immediately
- **API CONTRACT FINALIZATION** - Document all endpoints

### 📁 WHERE TO SAVE HANDOFFS
**Location**: `/docs/functional-areas/[feature]/handoffs/`
**Naming**: `functional-spec-YYYY-MM-DD-handoff.md`
**Template**: `/docs/standards-processes/agent-handoff-template.md`

### 📝 WHAT TO INCLUDE (TOP 5 CRITICAL)
1. **Technical Decisions**: Architecture choices and patterns
2. **API Contracts**: Complete endpoint specifications
3. **Data Models**: DTOs and domain models
4. **Integration Points**: External services and dependencies
5. **Performance Requirements**: SLAs and constraints

### 🤝 WHO NEEDS YOUR HANDOFFS
- **Backend Developers**: Technical implementation specs
- **React Developers**: API contracts and data models
- **Test Developers**: Technical test requirements
- **Database Designers**: Data model requirements

### ⚠️ MANDATORY READING BEFORE STARTING
**ALWAYS READ EXISTING HANDOFFS FIRST**:
1. Check `/docs/functional-areas/[feature]/handoffs/` for previous work
2. Read business requirements handoff FIRST
3. Understand technical decisions already made
4. Build on existing specs - don't contradict decisions

### 🚨 FAILURE TO CREATE HANDOFFS = IMPLEMENTATION FAILURES
**Why this matters**:
- Implementation will miss technical requirements
- API contracts will be wrong
- Performance requirements get ignored
- Integration points fail

**NO EXCEPTIONS**: Create handoff documents or workflow WILL fail.

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