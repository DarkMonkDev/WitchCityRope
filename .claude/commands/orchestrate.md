# Orchestrate Command
<!-- Last Updated: 2025-08-15 -->
<!-- Status: Active -->
<!-- Purpose: AI workflow orchestration as a Claude Code command -->

## Command Overview

The `/orchestrate` command implements the complete AI workflow orchestration system for WitchCityRope development. This command manages the entire development lifecycle through phases with quality gates, specialized sub-agent coordination, and comprehensive documentation.

## Usage

```
/orchestrate [scope-description]
```

Examples:
- `/orchestrate Implement user management admin screen`
- `/orchestrate Fix login authentication bug`
- `/orchestrate Add event scheduling feature`

## Key Workflow Phases

Based on the formalized workflow design, the orchestrator manages these phases:

### Phase 1: Requirements & Planning (Quality Gate: 95%)
**Mandatory Human Review Points:**
- **AFTER Business Requirements Document**: STOP and wait for explicit approval BEFORE creating functional spec
- **AFTER Requirements Phase Complete**: STOP and wait for explicit approval before Phase 2

**Sub-Agents Used:**
- `librarian`: Get exact file paths from master index
- `business-requirements`: Create business requirements document
- `functional-spec`: Create functional specifications (only after BR approval)
- `ui-designer`: Create UI wireframes if applicable

**Deliverables:**
- Business requirements document
- Business requirements review document
- Functional specification (after approval)
- UI wireframes (if applicable)
- Phase 1 review document

### Phase 2: Design & Architecture (Quality Gate: 90%)
**Sub-Agents Used:**
- `database-designer`: Database schema design
- `ui-designer`: UI/UX design specifications
- Future agents: `react-architect`, `api-designer`, `test-planner`

**Deliverables:**
- Database design documents
- UI/UX design specifications
- Technical design approach

### Phase 3: Implementation (Quality Gate: 85%)
**Mandatory Human Review:**
- **AFTER First Vertical Slice**: STOP and wait for explicit approval

**Sub-Agents Used:**
- `react-developer`: React components and UI
- `backend-developer`: Node.js/Express API services and business logic
- Parallel execution: Up to 2 agents simultaneously

**Deliverables:**
- Working code
- Unit tests
- Component documentation

### Phase 4: Testing & Validation (Quality Gate: 100%)
**Critical File Path Validation Required:**
Before delegating to backend-developer, must validate file paths:
- Source files only (`/src/`) → `backend-developer`
- Test files detected → `test-developer`
- Mixed changes → Split into separate delegations

**Sub-Agents Used:**
- `test-executor`: **MANDATORY** for ALL test execution - reports results back
- `test-developer`: **EXCLUSIVE OWNERSHIP** of ALL test files
- `lint-validator`: **MANDATORY** code quality validation after implementation
- `code-reviewer`: Code quality review after tests and linting pass
- `backend-developer`: Fix API/service issues ONLY (forbidden from test files)
- `react-developer`: Fix UI/component issues ONLY (forbidden from test files)

**Testing Workflow:**
1. Delegate to test-executor
2. Receive results from test-executor
3. Delegate to lint-validator for code quality validation
4. Delegate fixes based on results (with path validation)
5. After fixes complete, return to step 1
6. Continue until all tests pass and linting validates

**Deliverables:**
- All tests passing (verified by test-executor)
- Code quality validated (verified by lint-validator)
- Code review complete
- Performance validated

### Phase 5: Finalization
**Sub-Agents Used:**
- `prettier-formatter`: **MANDATORY** code formatting before final review
- `librarian`: Documentation updates and cleanup

**Finalization Workflow:**
1. Delegate to prettier-formatter for code formatting
2. Delegate to librarian for documentation updates
3. Final progress update and lessons learned capture

**Deliverables:**
- Code properly formatted (verified by prettier-formatter)
- Updated PROGRESS.md
- Feature documentation complete
- Lessons learned captured
- Improvement suggestions consolidated

## Command Implementation Details

### Initialization Protocol
When `/orchestrate` is invoked:

1. **Determine work type** (feature/bug/hotfix/docs/refactor)
2. **Invoke librarian** to check master index and get exact paths
3. **Invoke git-manager** to create feature branch: `feature/[YYYY-MM-DD]-[description]`
4. **Invoke librarian** to create scope folder structure:
   ```
   /docs/functional-areas/[feature-name]/new-work/[YYYY-MM-DD]-[description]/
   ├── requirements/
   ├── design/
   ├── implementation/
   ├── testing/
   ├── reviews/
   └── progress.md
   ```
5. **Initialize progress tracking** in both detailed and summary locations
6. **Document scope type and size**

### Critical Delegation Rules

**ABSOLUTE PROHIBITION - VIOLATIONS CAUSE ORCHESTRATION FAILURE:**
- Writing ANY code (not even a single line)
- Creating ANY documentation (delegate to librarian)
- Running ANY tests directly (delegate to test-executor)
- Fixing ANY bugs yourself (delegate to specialized developers)
- Executing ANY implementation commands
- Making ANY file edits

**MUST DELEGATE ALL WORK via Task tool:**
```python
Task(
    subagent_type="[agent-name]",
    description="[specific task]",
    prompt="[detailed instructions including exact file paths]"
)
```

### Quality Gate Enforcement

**Default Quality Gates by Work Type:**
- **Feature Development**: Requirements(95%) → Design(90%) → Implementation(85%) → Testing(100%)
- **Bug Fixes**: Requirements(80%) → Design(70%) → Implementation(75%) → Testing(100%)
- **Hotfixes/Emergency**: Requirements(70%) → Design(60%) → Implementation(70%) → Testing(100%)
- **Documentation Updates**: Requirements(85%) → Design(N/A) → Implementation(N/A) → Review(90%)
- **Refactoring**: Requirements(90%) → Design(85%) → Implementation(80%) → Testing(100%)

### Progress Tracking

**Update Points:**
- At each phase boundary
- After human reviews
- When blockers encountered

**Progress Format in PROGRESS.md:**
```markdown
## [Date] - [Feature Name] - ORCHESTRATED
**Type**: [Feature/Bug/Hotfix/Docs/Refactor]
**Branch**: feature/[date]-[description]
**Status**: Phase [X] - [Status]
**Quality Gates**: R:[X%] D:[X%] I:[X%] T:[X%]
**Next Review**: [After requirements/After vertical slice/None]
```

### Critical Test Delegation Rule

**When user mentions "testing", "debugging", or "fixing":**
1. Must immediately delegate to `test-executor`
2. Must NOT run any `npm test` or test execution commands directly
3. Must NOT attempt to fix any code directly
4. **NO HUMAN REVIEW REQUIRED** - delegate directly to test-executor
5. VIOLATION = ORCHESTRATION FAILURE

### File Path Protocol
1. **ALWAYS** get paths from librarian's master index first
2. **NEVER** let agents search for files themselves
3. **PASS** exact paths in the prompt to each agent

### Git Workflow (Via Delegation)
All git operations must be delegated to git-manager:

1. **Branch Creation:**
   ```
   Task: git-manager
   Prompt: Create feature branch from main: feature/[YYYY-MM-DD]-[description]
   ```

2. **Commits:**
   ```
   Task: git-manager
   Prompt: Commit changes with message: "[phase]: [description]"
   ```

3. **Status Checks:**
   ```
   Task: git-manager
   Prompt: Check git status and current branch
   ```

## Lessons Learned Documentation

Each sub-agent must document lessons learned in their respective areas:

- **Requirements agents**: Document requirement gathering improvements
- **Design agents**: Document design pattern discoveries
- **Implementation agents**: Document coding challenges and solutions
- **Testing agents**: Document test strategy improvements
- **All agents**: Document workflow efficiency improvements

The orchestrator consolidates all lessons learned into:
- `/.claude/workflow-data/improvements/[date].md`
- Final phase consolidation and presentation

## Key Changes from Sub-Agent Version

### 1. Command vs Sub-Agent
- **Before**: Orchestrator was a sub-agent invoked via Task tool
- **After**: Orchestrator is a command invoked directly by user
- **Benefit**: Direct access to user context, no delegation overhead

### 2. Tool Access
- **Before**: Limited to TodoWrite and Task tools only
- **After**: Full access to all Claude Code tools for coordination
- **Benefit**: Can verify folder creation, check progress, manage git operations

### 3. Session Management
- **Before**: Sub-agent session isolation
- **After**: Main session orchestration with sub-agent delegation
- **Benefit**: Better context continuity, shared progress tracking

### 4. Human Interaction
- **Before**: All human interaction through main session
- **After**: Direct human interaction for approvals and reviews
- **Benefit**: Clearer communication, immediate feedback loops

### 5. Error Handling
- **Before**: Errors bubbled up through delegation chain
- **After**: Direct error handling and recovery coordination
- **Benefit**: Faster resolution, better debugging

### 6. Progress Visibility
- **Before**: Progress updates through sub-agent reports
- **After**: Real-time progress tracking and reporting
- **Benefit**: Better user visibility, more responsive feedback

## Available Sub-Agents for Delegation

### Planning Phase
- `business-requirements`: Requirements analysis and business documentation
- `functional-spec`: Technical specifications and detailed requirements
- `ui-designer`: UI/UX design and wireframes

### Design Phase
- `database-designer`: Database schema design and migrations
- `ui-designer`: UI/UX design specifications

### Implementation Phase
- `react-developer`: React components and UI implementation
- `backend-developer`: Node.js/Express API services and business logic

### Testing Phase
- `test-executor`: **MANDATORY** for ALL test execution
- `test-developer`: Test creation and test strategy
- `code-reviewer`: Code quality review and validation

### Utility
- `librarian`: Documentation management and file organization
- `git-manager`: Version control and branch operations

### Quality Assurance
- `lint-validator`: **MANDATORY** code linting and validation during Phase 4
- `prettier-formatter`: **MANDATORY** code formatting and style enforcement during Phase 5

## Success Metrics

The orchestrator tracks and reports:
- Time per phase
- Quality gate scores
- Number of retries needed
- Human intervention points
- Test coverage achieved
- Documentation completeness

## Critical Rules Summary

1. **NEVER** skip phases without explicit approval
2. **NEVER** proceed past failed quality gates without override
3. **ALWAYS** wait for human review at designated points
4. **ALWAYS** use TodoWrite to track phase progression
5. **ALWAYS** document decisions and rationale
6. **ALWAYS** coordinate parallel agents through documents
7. **NEVER** create implementation files directly
8. **ALWAYS** delegate work to specialized sub-agents
9. **ALWAYS** validate file paths before delegating
10. **ALWAYS** maintain progress tracking throughout workflow

---

This command ensures high-quality software delivery through systematic orchestration while maintaining development velocity and comprehensive documentation.