# Orchestrate Command
<!-- Last Updated: 2025-08-15 -->
<!-- Status: Active -->
<!-- Purpose: AI workflow orchestration as a Claude Code command -->

## Command Overview

The `/orchestrate` command implements the complete AI workflow orchestration system for WitchCityRope development. This command manages the entire development lifecycle through phases with quality gates, specialized sub-agent coordination, and comprehensive documentation.

## CRITICAL: MANDATORY STARTUP PROCEDURE
**BEFORE orchestrating ANY work, the orchestrator MUST:**

1. **Read Orchestrator Lessons Learned** (MANDATORY)
   - Location: `/home/chad/repos/witchcityrope-react/docs/lessons-learned/orchestrator-lessons-learned.md`
   - Contains critical knowledge specific to orchestrator role
   - Apply these lessons to all workflow coordination

2. **Reference Workflow Process** (SINGLE SOURCE OF TRUTH)
   - Location: `/home/chad/repos/witchcityrope-react/docs/standards-processes/workflow-orchestration-process.md`
   - THE authoritative source for all workflow procedures
   - Contains 5-phase workflow definition, quality gates, human review points

3. **Clickable File Links** (REQUIRED)
   - ALL file links in documentation must use full absolute paths
   - Format: `/home/chad/repos/witchcityrope-react/...`
   - Enables clickable navigation for stakeholders

## Usage

```
/orchestrate [scope-description]
```

Examples:
- `/orchestrate Implement user management admin screen`
- `/orchestrate Fix login authentication bug`
- `/orchestrate Add event scheduling feature`
- `/orchestrate Create user profile management screen`
- `/orchestrate Fix authentication cookie expiration bug`
- `/orchestrate Add event RSVP notification system`
- `/orchestrate Refactor payment processing module`
- `/orchestrate Docker containerization of authentication system`

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
- `technology-researcher`: Technology evaluation and research for architecture decisions

**Deliverables:**
- Business requirements document
- Business requirements review document
- Functional specification (after approval)
- UI wireframes (if applicable)
- Phase 1 review document

### Phase 2: Design & Architecture (Quality Gate: 90%)
**CRITICAL: Design Phase Sequencing (MANDATORY ORDER)**:
1. **UI Design FIRST** (ui-designer agent)
   - Create visual design and wireframes
   - **MANDATORY HUMAN REVIEW CHECKPOINT** - MUST PAUSE
2. **Post-UI Approval Only**:
   - Functional Specification updates (if UI changes require them)
   - Database design (database-designer agent)
   - API design (planned)
   - Technical architecture (planned)

**Critical Rule**: UI design happens FIRST because design changes can influence technical requirements and architecture decisions.

**Sub-Agents Used:**
- `database-designer`: Database schema design
- `ui-designer`: UI/UX design specifications
- `technology-researcher`: Technology evaluation and selection for implementation needs
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
3. **Invoke git-manager** to create branch: `git checkout -b feature/[YYYY-MM-DD]-[description]`
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

**TECHNOLOGY-RESEARCHER DELEGATION RESTRICTIONS:**
- Only orchestrator can invoke technology-researcher (other agents cannot)
- Technology-researcher provides structured output: date, project, topic, requirements, findings, decision matrices, recommendations. This should be written in a mark up file that can be passed to other sub-agents during the current or in future workflows. These files should be created and logged by the librarian is standard locations based on the type of research it is (ex. scope of work specific, entire project architecture, temporary). 
- Use during Planning Phase for technology selection decisions
- Use during Design Phase for implementation technology evaluation

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

## Quality Gate Standards Summary
- **Feature Development**: R:95% → D:90% → I:85% → T:100%
- **Bug Fixes**: R:80% → D:70% → I:75% → T:100%
- **Hotfixes**: R:70% → D:60% → I:70% → T:100%
- **Documentation**: R:85% → D:N/A → I:N/A → T:90%
- **Refactoring**: R:90% → D:85% → I:80% → T:100%

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
3. **PASS** exact paths in delegation prompts
4. **UPDATE** master index when functional areas change

### Agent Delegation Requirements

#### File Path Protocol
1. **ALWAYS** get paths from librarian's master index first
2. **NEVER** let agents search for files themselves
3. **PASS** exact paths in delegation prompts
4. **UPDATE** master index when functional areas change

#### Delegation Template
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

#### Critical Agent Knowledge
- **Sub-agents don't read CLAUDE.md** - Include essential constraints in delegation prompts
- **Mandatory lessons learned reading** - Each agent must read their specific lessons file and should update that file with lessons learned as these lessons are discovered
- **STRICT LESSONS LEARNED FORMAT** - Agents MUST follow /docs/lessons-learned/LESSONS-LEARNED-TEMPLATE.md - NO status reports, NO celebrations, ONLY prevention patterns
- **Docker operations** - Include Docker guide references for any containerization work


### Git Workflow (Via Delegation)
All git operations must be delegated to git-manager:

1. **Branch Creation:**
   ```
   Task: git-manager
   Prompt: Create feature branch: feature/[YYYY-MM-DD]-[description]
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

### STRICT FORMAT ENFORCEMENT
**CRITICAL**: The orchestrator has ABSOLUTE AUTHORITY to reject lessons learned that violate format rules.

#### Validation Process (MANDATORY)
1. **Check format compliance** using `/docs/lessons-learned/LESSONS-LEARNED-VALIDATION-CHECKLIST.md`
2. **REJECT if contains**: Status reports, celebrations, project history, implementation guides
3. **REQUIRE rewrite** if violations found - BLOCK workflow until compliant
4. **Template enforcement**: All updates MUST follow `/docs/lessons-learned/LESSONS-LEARNED-TEMPLATE.md`

#### Allowed Content (ONLY)
- **Prevention patterns**: What went wrong and how to prevent it
- **Mistakes to avoid**: Common errors and their solutions
- **Brief problem/solution pairs**: Maximum 2 sentences each

#### Forbidden Content (INSTANT REJECTION)
- ❌ "Successfully completed" entries
- ❌ "MAJOR SUCCESS" or achievement reports
- ❌ Project timelines or history
- ❌ Implementation tutorials
- ❌ Code examples > 3 lines

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
- `technology-researcher`: Technology evaluation and research for architecture decisions (orchestrator-only access)

### Design Phase
- `database-designer`: Database schema design and migrations
- `ui-designer`: UI/UX design specifications (MUST BE FIRST in Phase 2)
- `technology-researcher`: Technology evaluation and selection for implementation needs (orchestrator-only access)

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
- `technology-researcher`: Technology evaluation and research (orchestrator-only access)

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

## References
- **Single Source Process**: [Workflow Orchestration Process](/home/chad/repos/witchcityrope-react/docs/standards-processes/workflow-orchestration-process.md)
- **Orchestrator Lessons**: [Orchestrator Lessons Learned](/home/chad/repos/witchcityrope-react/docs/lessons-learned/orchestrator-lessons-learned.md)
- **Master Index**: [Functional Area Master Index](/home/chad/repos/witchcityrope-react/docs/architecture/functional-area-master-index.md)
- **File Registry**: [File Registry](/home/chad/repos/witchcityrope-react/docs/architecture/file-registry.md)

---

*This command documentation works in conjunction with the workflow orchestration process document as the single source of truth for all procedures. Update both documents when workflow changes occur.*

This command ensures high-quality software delivery through systematic orchestration while maintaining development velocity and comprehensive documentation.
