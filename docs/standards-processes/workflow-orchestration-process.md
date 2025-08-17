# Workflow Orchestration Process - Single Source of Truth

<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: Orchestrator Agent -->
<!-- Status: Active -->

## Overview

This document is THE authoritative source for the WitchCityRope AI workflow orchestration process. All agents must reference this document for workflow procedures, quality gates, and coordination patterns.

## 5-Phase Workflow Definition

### Phase 1: Requirements & Planning
**Quality Gate Target**: 95% completion
**Mandatory Human Review**: After Business Requirements (BEFORE Functional Specification)

#### Phase Sequence
1. **Business Requirements** (business-requirements agent)
   - Platform analysis and user needs assessment
   - **MANDATORY HUMAN REVIEW CHECKPOINT** - MUST PAUSE for approval
2. **Functional Specification** (ONLY after BR approval)
3. **UI Design** (ui-designer agent) - if UI involved
   - **MANDATORY HUMAN REVIEW CHECKPOINT** - MUST PAUSE after UI design

#### Deliverables
- Business requirements document: `/docs/functional-areas/[feature]/requirements/business-requirements.md`
- Functional specification: `/docs/functional-areas/[feature]/requirements/functional-specification.md`
- UI wireframes (if applicable): `/docs/functional-areas/[feature]/design/ui-wireframes.md`
- Phase 1 review document: `/docs/functional-areas/[feature]/reviews/phase1-requirements-review.md`

### Phase 2: Design & Architecture
**Quality Gate Target**: 90% completion
**MANDATORY SEQUENCING**: UI Design FIRST, then other designs

#### Phase Sequence (CRITICAL ORDER)
1. **UI Design** (ui-designer agent) - FIRST PRIORITY
   - Create visual design and wireframes
   - **MANDATORY HUMAN REVIEW CHECKPOINT** - MUST PAUSE after UI design
2. **Post-UI Approval**: Other design work may proceed
   - **Functional Specification updates** (if needed based on UI)
   - **Database design** (database-designer agent)
   - **API design** (api-designer agent) - planned
   - **Technical architecture** (blazor-architect agent) - planned

**Rationale**: UI design changes can influence technical designs, so it should come first.

#### Agent Coordination
- ui-designer: UI/UX specifications (MUST BE COMPLETED FIRST)
- database-designer: Database schema and migrations (after UI approval)
- api-designer: API contract specifications (planned)
- blazor-architect: System architecture design (planned)

#### Deliverables
- UI design specifications: `/docs/functional-areas/[feature]/design/ui-design.md` (FIRST)
- UI design review document: `/docs/functional-areas/[feature]/reviews/ui-design-review.md` (HUMAN REVIEW)
- Database schema: `/docs/functional-areas/[feature]/design/database-design.md`
- Technical architecture: `/docs/functional-areas/[feature]/design/technical-design.md`
- Phase 2 review document: `/docs/functional-areas/[feature]/reviews/phase2-design-review.md`

### Phase 3: Implementation
**Quality Gate Target**: 85% completion
**Mandatory Human Review**: After First Vertical Slice Implementation

#### Agent Coordination
- blazor-developer: UI component implementation
- backend-developer: API services and business logic
- Maximum 2 agents working simultaneously

#### Deliverables
- Working code implementation
- Unit tests for all components
- Component documentation
- Vertical slice demonstration
- Phase 3 review document: `/docs/functional-areas/[feature]/reviews/phase3-implementation-review.md`

### Phase 4: Testing & Validation
**Quality Gate Target**: 100% completion (all tests must pass)
**NO Human Review Required**: Automatic delegation to test-executor

#### Critical Delegation Rules
- **MANDATORY**: All testing delegated to test-executor agent
- **NEVER**: Run tests directly via bash commands
- **TEST-FIX CYCLE**: test-executor → specialized developer → test-executor
- **File Path Validation**: Check test vs source file patterns before delegation

#### Testing Workflow
```
1. Delegate to test-executor: Run all test suites
2. Receive results: Pass/fail status with detailed failure info
3. Delegate fixes based on file patterns:
   - Source files (/src/) → backend-developer or blazor-developer
   - Test files (test patterns) → test-developer
   - Mixed errors → Split delegations
4. Return to step 1 until all tests pass
```

#### Deliverables
- All tests passing (verified by test-executor)
- Performance validation
- Code review completion
- Phase 4 review document: `/docs/functional-areas/[feature]/reviews/phase4-testing-review.md`

### Phase 5: Finalization
**Quality Gate Target**: 100% completion
**Focus**: Documentation and knowledge capture

#### Deliverables
- Updated main PROGRESS.md
- Feature documentation complete
- Lessons learned captured
- Improvement suggestions documented
- Final workflow summary: `/docs/functional-areas/[feature]/reviews/workflow-completion-summary.md`

## Mandatory Human Review Points

### 1. After Business Requirements (CRITICAL)
- **MUST PAUSE**: Wait for explicit human approval
- **BEFORE**: Creating functional specification
- **Review Document**: Business requirements review with approval checklist
- **Approval Required**: Product Manager or Business Stakeholder

### 2. After UI Design (CRITICAL)
- **MUST PAUSE**: Wait for explicit human approval  
- **BEFORE**: Other design work (database, API, technical)
- **BEFORE**: Functional specification updates (may be needed based on UI)
- **Review Document**: UI design review with mockup approval
- **Approval Required**: UI/UX Stakeholder or Product Manager
- **Impact**: UI changes can influence technical requirements and designs

### 3. After Vertical Slice (CRITICAL)
- **MUST PAUSE**: Wait for explicit human approval
- **BEFORE**: Full feature implementation rollout
- **Review Document**: Vertical slice implementation review
- **Approval Required**: Technical Lead or Project Manager

### 4. Exception: Testing Phase
- **NO PAUSE REQUIRED**: Direct delegation to test-executor
- **AUTOMATIC**: Test-fix cycle coordination
- **HUMAN INVOLVEMENT**: Only if max iterations reached

## Quality Gate Standards

### Calculation Method
```
Quality Score = (Completed Criteria / Total Criteria) × 100
```

### Quality Gate Enforcement
- **PASS**: Score meets or exceeds target percentage
- **FAIL**: Score below target, human intervention required
- **OVERRIDE**: Human can approve with documented reason

### Default Quality Targets by Work Type
- **Feature Development**: R:95% → D:90% → I:85% → T:100%
- **Bug Fixes**: R:80% → D:70% → I:75% → T:100%
- **Hotfixes**: R:70% → D:60% → I:70% → T:100%
- **Documentation**: R:85% → D:N/A → I:N/A → T:90%
- **Refactoring**: R:90% → D:85% → I:80% → T:100%

## File Creation and Naming Standards

### Root Directory Rules (CRITICAL)
- **ONLY ALLOWED**: README.md, PROGRESS.md, ARCHITECTURE.md, CLAUDE.md
- **NEVER CREATE**: Any other files in project root
- **IMMEDIATE ACTION**: Relocate violations and update file registry

### Functional Area Structure
```
/docs/functional-areas/[feature-name]/
├── requirements/        # Business requirements and specs
├── design/             # UI, database, API, technical designs
├── implementation/     # Code implementation notes
├── testing/           # Test plans and results
├── reviews/           # Human review documents and approvals
├── lessons-learned/   # Feature-specific lessons
└── progress.md        # Phase tracking and coordination
```

### File Naming Conventions
- **Documents**: `feature-name-aspect.md`
- **Dates**: `YYYY-MM-DD-description.md`
- **Reviews**: `phase[X]-[phase-name]-review.md`
- **Special**: `README.md`, `TODO.md` (uppercase for special files)

### Full Path Requirements
- **ALL file links**: Must use full absolute paths
- **Format**: `/home/chad/repos/witchcityrope-react/docs/...`
- **Clickable**: Format as `[Description](/full/path/to/file)`
- **Validation**: Verify all links work before document completion

## Agent Delegation Patterns

### File Path Discovery Protocol
1. **ALWAYS**: Get paths from librarian's master index first
2. **NEVER**: Let agents search for files themselves
3. **PASS**: Exact paths in delegation prompts
4. **UPDATE**: Master index when functional areas change

### Delegation Prompt Template
```
Task: [agent-name]
Prompt: [specific instructions]

Required Reading:
- Agent lessons learned: /docs/lessons-learned/[agent]-lessons-learned.md
- [Relevant process documents]

File Paths:
- [Exact path 1]: Purpose
- [Exact path 2]: Purpose

Context: [Current project phase and feature description]
```

### Agent Startup Requirements
- **MANDATORY**: Read agent-specific lessons learned file first
- **KNOWLEDGE**: Sub-agents don't read CLAUDE.md automatically
- **COMMUNICATION**: Include essential constraints in delegation prompts

## Docker Operations Integration

### Mandatory Docker Guide Reading
**EVERY Docker-related delegation MUST include:**
```
MANDATORY: Read /docs/guides-setup/docker-operations-guide.md before ANY Docker operations.
Follow the guide exactly for all container procedures.
Update the guide if you discover new procedures.
```

### Docker Documentation References
- **Central Architecture**: `/docs/architecture/docker-architecture.md`
- **Operations Guide**: `/docs/guides-setup/docker-operations-guide.md`

## Progress Tracking Standards

### Update Frequency
- At each phase boundary
- After human reviews
- When blockers encountered
- Phase completion

### Main PROGRESS.md Format
```markdown
## [Date] - [Feature Name] - ORCHESTRATED
**Type**: [Feature/Bug/Hotfix/Docs/Refactor]
**Branch**: feature/[date]-[description]  
**Status**: Phase [X] - [Status]
**Quality Gates**: R:[X%] D:[X%] I:[X%] T:[X%]
**Next Review**: [After requirements/After vertical slice/None]
```

### Detailed Progress in Functional Area
- Track ALL decisions and rationale
- Document blockers and resolutions
- Record agent outputs and reviews
- Capture human feedback and approval

## Success Metrics and Monitoring

### Key Performance Indicators
- Time per phase
- Quality gate achievement rates
- Number of retries needed
- Human intervention frequency
- Test coverage achieved
- Documentation completeness

### Continuous Improvement
- Collect agent improvement suggestions
- Store in `/.claude/workflow-data/improvements/`
- Consolidate and review monthly
- Update process based on learnings

## Critical Violation Responses

### Immediate Action Required
1. **Root directory pollution**: Relocate and alert
2. **Quality gate failures**: Human review required
3. **Missing human approvals**: STOP workflow immediately
4. **Test delegation violations**: Restart with proper delegation
5. **File registry gaps**: Add entries and investigate

### Escalation Procedures
- **Technical issues**: Escalate to appropriate developer agent
- **Process violations**: Document and update prevention measures
- **Quality concerns**: Require human review and approval
- **Workflow blockage**: Human intervention and decision required

## Workflow Data Management

### Storage Locations
- **Active work**: `/.claude/workflow-data/active/`
- **Completed work**: `/.claude/workflow-data/completed/`
- **Templates**: `/.claude/workflow-data/templates/`
- **Improvements**: `/.claude/workflow-data/improvements/`

### Data Retention
- Active work: Until phase completion
- Completed work: Permanent archive
- Improvements: Review quarterly
- Templates: Update as processes evolve

---

**REMEMBER**: This document is the SINGLE SOURCE OF TRUTH for workflow orchestration. All agents must reference this document. Update this document when processes change, never create conflicting documentation elsewhere.

*Maintained by: Orchestrator Agent*
*Review Schedule: Monthly or when major process changes occur*