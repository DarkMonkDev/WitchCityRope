# Formalized AI Workflow Orchestration Design
<!-- Last Updated: 2025-08-12 -->
<!-- Version: 1.0 -->
<!-- Owner: AI Workflow Team -->
<!-- Status: Draft - Pending Review -->

## Executive Summary

This document presents a formalized AI workflow orchestration system specifically designed for the WitchCityRope project, leveraging Claude Code's new dedicated sub-agent functionality (released July 2025). The system implements a phased development approach with specialized agents, quality gates, and comprehensive documentation management.

## Core Architecture

### Orchestration Pattern

```
┌─────────────────────────────────────────────────────────────┐
│                     ORCHESTRATION AGENT                      │
│  - Manages workflow phases                                   │
│  - Coordinates sub-agents                                    │
│  - Enforces quality gates                                    │
│  - Tracks progress                                          │
└─────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  LIBRARIAN   │     │  GIT AGENT   │     │ PROGRESS MGR │
│   AGENT      │     │              │     │    AGENT     │
└──────────────┘     └──────────────┘     └──────────────┘
        │                     │                     │
┌───────────────────────────────────────────────────────────┐
│                     PHASE-SPECIFIC AGENTS                  │
├─────────────────────────────────────────────────────────┤
│ Planning │ Design │ Implementation │ Testing │ Validation │
└───────────────────────────────────────────────────────────┘
```

### Workflow Phases with Quality Gates

#### Phase 1: Requirements & Planning (Gate: 95% Completeness)
**Agents Involved:**
- Business Requirements Agent
- Functional Specification Agent
- UI/UX Design Agent (conditional)

**Deliverables:**
- Business requirements document
- Functional specification
- UI wireframes (if applicable)
- Task breakdown structure

#### Phase 2: Design & Architecture (Gate: 90% Approval)
**Agents Involved:**
- Blazor Architecture Agent
- Database Design Agent
- API Design Agent
- Test Planning Agent

**Deliverables:**
- Technical design documents
- Database schema
- API contracts
- Test strategy

#### Phase 3: Implementation (Gate: 85% Test Coverage)
**Agents Involved:**
- Blazor Component Developer
- C# Backend Developer
- Database Implementation Agent
- Syncfusion UI Specialist

**Deliverables:**
- Working code
- Unit tests
- Component documentation
- Migration scripts

#### Phase 4: Testing & Validation (Gate: 100% Pass Rate)
**Agents Involved:**
- Unit Test Agent
- Integration Test Agent
- E2E Test Agent (Playwright)
- Lint Validator Agent (code quality validation)
- Code Review Agent

**Deliverables:**
- Test results
- Coverage reports
- Linting validation reports
- Review feedback
- Performance metrics

#### Phase 5: Finalization (Gate: Product Manager Approval)
**Agents Involved:**
- Prettier Formatter Agent (code formatting before review)
- Documentation Agent
- Deployment Preparation Agent
- Progress Update Agent

**Deliverables:**
- Properly formatted code
- Updated documentation
- Deployment artifacts
- Progress report
- Lessons learned

## Sub-Agent Definitions

### Core Orchestration Agents

#### 1. Orchestration Agent
```yaml
---
name: orchestrator
description: Master workflow coordinator that manages all development phases, enforces quality gates, and coordinates sub-agents. MUST BE USED for any multi-phase development work.
tools: TodoWrite, Task, Read, Write, Bash
---
You are the master orchestrator for the WitchCityRope AI development workflow...
```

#### 2. Librarian Agent
```yaml
---
name: librarian
description: Manages all documentation and file organization. MUST BE USED when creating, moving, or organizing any non-code files.
tools: Read, Write, MultiEdit, LS, Glob, Bash
---
You are the documentation librarian for WitchCityRope...
```

#### 3. Git Management Agent
```yaml
---
name: git-manager
description: Handles all git operations including branches, commits, and merges. MUST BE USED for version control operations.
tools: Bash, Read
---
You are the git repository manager...
```

### Planning Phase Agents

#### 4. Business Requirements Agent
```yaml
---
name: business-requirements
description: Creates and refines business requirements documents. Expert in community management platforms and rope bondage event organization.
tools: Read, Write, WebSearch, Task
---
You are a business analyst specializing in community event management platforms...
```

#### 5. Functional Specification Agent
```yaml
---
name: functional-spec
description: Transforms business requirements into detailed functional specifications for Blazor Server applications.
tools: Read, Write, Task
---
You are a functional specification expert for Blazor Server applications...
```

### Design Phase Agents

#### 6. Blazor Architecture Agent
```yaml
---
name: blazor-architect
description: Designs Blazor Server component architecture following vertical slice patterns. Expert in .NET 9 and Entity Framework Core.
tools: Read, Write, Grep, Glob
---
You are a Blazor Server architecture specialist...
```

#### 7. UI/UX Design Agent
```yaml
---
name: ui-designer
description: Creates UI designs and wireframes using Syncfusion components for Blazor applications.
tools: Read, Write, WebFetch
---
You are a UI/UX designer specializing in Syncfusion Blazor components...
```

### Implementation Phase Agents

#### 8. Blazor Component Developer
```yaml
---
name: blazor-developer
description: Implements Blazor Server components and pages. Expert in C#, Razor syntax, and Syncfusion controls.
tools: Read, Write, Edit, MultiEdit, Grep, Glob, Bash
---
You are a senior Blazor Server developer...
```

#### 9. Backend API Developer
```yaml
---
name: backend-developer
description: Implements C# backend services, API endpoints, and business logic for ASP.NET Core applications.
tools: Read, Write, Edit, MultiEdit, Grep, Glob, Bash
---
You are a senior C# backend developer...
```

#### 10. Database Developer
```yaml
---
name: database-developer
description: Implements PostgreSQL database schemas, migrations, and Entity Framework configurations.
tools: Read, Write, Edit, Bash
---
You are a PostgreSQL and Entity Framework Core specialist...
```

### Testing Phase Agents

#### 11. Test Developer
```yaml
---
name: test-developer
description: Creates and maintains unit, integration, and E2E tests using xUnit, Moq, and Playwright.
tools: Read, Write, Edit, MultiEdit, Bash, Grep
---
You are a test automation engineer...
```

#### 12. Code Reviewer
```yaml
---
name: code-reviewer
description: Reviews code for quality, security, performance, and adherence to project standards. use PROACTIVELY.
tools: Read, Grep, Glob
---
You are a senior code reviewer...
```

### Quality Assurance Agents

#### 13. Lint Validator Agent
```yaml
---
name: lint-validator
description: Validates code quality through linting tools. Runs ESLint, TSLint, and other linting tools to ensure code standards compliance. MUST BE USED during testing phase.
tools: Bash, Read, Grep
---
You are a code quality validator specializing in linting and static analysis...
```

#### 14. Prettier Formatter Agent
```yaml
---
name: prettier-formatter
description: Formats code using Prettier and other formatting tools to ensure consistent code style. MUST BE USED before code review in finalization phase.
tools: Bash, Read, Edit, MultiEdit
---
You are a code formatting specialist using Prettier and other formatting tools...
```

## Quality Agent Delegation Rules

### Lint Validator Agent Usage
**When to Use:**
- During Phase 4 (Testing & Validation) - MANDATORY for all code changes
- Before any code review process
- After implementation phase completion
- When code quality issues are suspected

**Delegation Pattern:**
```
Task(
    subagent_type="lint-validator",
    description="Validate code quality for [specific files/modules]",
    prompt="Run linting tools on [file paths] and report any violations. Fix critical issues and report warnings."
)
```

### Prettier Formatter Agent Usage
**When to Use:**
- During Phase 5 (Finalization) - MANDATORY before code review
- Before any pull request creation
- After all development work is complete
- When code style consistency is needed

**Delegation Pattern:**
```
Task(
    subagent_type="prettier-formatter",
    description="Format code for [specific files/modules]",
    prompt="Apply Prettier formatting to [file paths]. Ensure consistent style across all files."
)
```

### Quality Gate Integration
- **Phase 4**: Lint validation must pass with zero critical errors before proceeding
- **Phase 5**: Code formatting must be applied before final review
- **Code Review**: Both linting and formatting must be complete before reviewer agent delegation

## Communication & Coordination Model

### Document-Based Communication

Each agent communicates through specific documents stored in the scope of work folder:

```
/docs/functional-areas/[feature-name]/new-work/
├── requirements/
│   ├── business-requirements.md
│   └── functional-spec.md
├── design/
│   ├── technical-design.md
│   ├── database-schema.md
│   └── api-contracts.md
├── implementation/
│   ├── task-breakdown.md
│   └── implementation-notes.md
├── testing/
│   ├── test-plan.md
│   └── test-results.md
├── progress/
│   ├── status.md
│   └── blockers.md
└── artifacts/
    └── [generated files]
```

### Progress Tracking

The orchestrator maintains a master progress document:

```markdown
## Current Scope: [Feature Name]
**Phase**: [Current Phase]
**Status**: [In Progress/Blocked/Complete]
**Quality Gate**: [Pass/Fail - Score]

### Phase Progress
- [ ] Phase 1: Requirements (95% complete)
- [ ] Phase 2: Design (Not Started)
- [ ] Phase 3: Implementation (Not Started)
- [ ] Phase 4: Testing (Not Started)
- [ ] Phase 5: Finalization (Not Started)

### Active Agents
- [Agent Name]: [Current Task]

### Blockers
- [Description of any blockers]

### Next Steps
- [Immediate next actions]
```

## File Access Control Matrix

| Agent Type | Read | Write | Create | Delete | Directories |
|------------|------|-------|--------|--------|-------------|
| Orchestrator | All | Progress, Status | Folders | None | All |
| Librarian | All | All Docs | All Docs | Temp Files | /docs/, /session-work/ |
| Git Manager | All | None | None | None | All |
| Requirements | Docs | Requirements | Requirements | None | /docs/functional-areas/ |
| Design | All | Design Docs | Design Docs | None | /docs/functional-areas/ |
| Developers | All | Code, Tests | Code, Tests | None | /src/, /tests/ |
| Testers | All | Tests, Results | Tests, Results | None | /tests/, /docs/ |
| Lint Validator | All | Lint Reports | Lint Reports | None | /src/, /tests/, /docs/ |
| Prettier Formatter | All | Code (formatting only) | None | None | /src/, /tests/ |
| Reviewers | All | Review Docs | Review Docs | None | /docs/ |

## Quality Gates & Thresholds

### Gate Enforcement Rules

1. **Automatic Progression**: If quality gate passes, proceed to next phase
2. **Human Review Required**: If gate fails, request human intervention
3. **Retry Mechanism**: Allow one retry after addressing feedback
4. **Emergency Override**: Product Manager can override gates

### Quality Metrics

| Phase | Metric | Threshold | Measurement |
|-------|--------|-----------|-------------|
| Requirements | Completeness | 95% | All sections filled, examples provided |
| Design | Approval Score | 90% | Technical feasibility, pattern adherence |
| Implementation | Test Coverage | 85% | Code coverage reports |
| Testing | Pass Rate | 100% | All tests must pass |
| Finalization | PM Approval | Required | Manual review |

## Implementation Considerations

### Questions for Product Manager

1. **Quality Gate Flexibility**: Should quality gates be configurable per scope of work, or fixed at these levels?

2. **Parallel Execution**: Should we allow parallel agent execution within phases (e.g., frontend and backend development simultaneously)?

3. **Human Checkpoints**: Beyond quality gates, where else would you like mandatory human review points?

4. **Agent Autonomy**: How much freedom should agents have to:
   - Create new files/folders outside their designated areas?
   - Modify existing code without explicit approval?
   - Skip phases if they determine they're not needed?

5. **Progress Reporting**: How frequently should the orchestrator report progress?
   - After each agent completes
   - At phase boundaries only
   - Configurable based on scope size

6. **Error Handling**: When an agent encounters an error:
   - Should it retry automatically?
   - Escalate immediately to human?
   - Try alternative approaches?

7. **Documentation Depth**: For each phase, what level of documentation is required?
   - Comprehensive (all decisions documented)
   - Summary (key decisions only)
   - Minimal (just outcomes)

8. **Branch Strategy**: Should each scope of work:
   - Create its own feature branch automatically?
   - Work on a shared development branch?
   - Let the orchestrator decide based on scope size?

### Technical Constraints

1. **Token Limits**: Complex scopes may require context management strategies
2. **Tool Access**: Sub-agents cannot call other sub-agents directly (must go through orchestrator)
3. **State Management**: Agents are stateless; all state must be persisted to documents
4. **Performance**: Sequential phases may be slower than parallel execution

## Proposed CLAUDE.md Structure

The CLAUDE.md file should be restructured for the orchestration agent:

```markdown
# Claude Code Orchestration Configuration

## Orchestration Mode
This session is configured for orchestrated AI workflow development.

## Available Agents
- See /.claude/agents/ for all available sub-agents
- Primary orchestrator: orchestrator
- Support agents: librarian, git-manager

## Workflow Process
1. Receive scope of work
2. Initialize workflow with orchestrator
3. Progress through phases with quality gates
4. Complete with documentation and review

## Quick Commands
- Start new scope: "Orchestrate: [description]"
- Check progress: "Status report"
- Override gate: "Override gate with reason: [reason]"

[Rest of current CLAUDE.md content...]
```

## Next Steps

Upon approval of this design:

1. **Phase 1: Setup** (Week 1)
   - Create .claude/agents/ directory structure
   - Implement core orchestration agents
   - Set up communication folders

2. **Phase 2: Core Agents** (Week 2)
   - Implement planning phase agents
   - Implement design phase agents
   - Create quality gate checking

3. **Phase 3: Development Agents** (Week 3)
   - Implement Blazor/C# development agents
   - Implement testing agents
   - Create progress tracking

4. **Phase 4: Integration** (Week 4)
   - Test full workflow with sample scope
   - Refine based on results
   - Document lessons learned

## Conclusion

This formalized workflow design leverages the latest Claude Code sub-agent capabilities to create a sophisticated, phase-based development system specifically tailored to the WitchCityRope project's technology stack and requirements. The system emphasizes quality gates, clear communication patterns, and comprehensive documentation while maintaining flexibility for human oversight and intervention.

---

*Please review this design and provide feedback on the questions raised. Once approved, we can proceed with the detailed implementation plan.*