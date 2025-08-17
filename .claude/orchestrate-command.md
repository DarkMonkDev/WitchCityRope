# Orchestrate Command Documentation

## Purpose
Master workflow orchestration command for WitchCityRope AI development system. This command initiates and manages the complete 5-phase development workflow with quality gates and human review checkpoints.

## Usage
```
/orchestrate [scope description]
```

**Examples**:
- `/orchestrate Create user profile management screen`
- `/orchestrate Fix authentication cookie expiration bug`
- `/orchestrate Add event RSVP notification system`
- `/orchestrate Refactor payment processing module`
- `/orchestrate Docker containerization of authentication system`

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

## Workflow Overview

### 5-Phase Process
1. **Requirements & Planning** (95% quality gate)
2. **Design & Architecture** (90% quality gate) 
3. **Implementation** (85% quality gate)
4. **Testing & Validation** (100% quality gate)
5. **Finalization** (100% quality gate)

### Mandatory Human Review Points
The orchestrator **MUST PAUSE** and wait for explicit approval at:

1. **After Business Requirements** (CRITICAL)
   - BEFORE creating functional specification
   - Requires Product Manager or Business Stakeholder approval

2. **After UI Design** (CRITICAL - NEW REQUIREMENT)
   - BEFORE other design work (database, API, technical)
   - BEFORE functional specification updates (if needed based on UI)
   - Requires UI/UX Stakeholder or Product Manager approval
   - **Rationale**: UI design changes can influence technical requirements

3. **After First Vertical Slice** (CRITICAL)
   - BEFORE full feature implementation rollout
   - Requires Technical Lead or Project Manager approval

4. **Exception - Testing Phase**
   - NO pause required for test delegation to test-executor
   - Automatic coordination of test-fix cycles

## Design Phase Sequencing (CRITICAL UPDATE)

### Phase 2: Design & Architecture - NEW SEQUENCE
**MANDATORY ORDER**:
1. **UI Design FIRST** (ui-designer agent)
   - Create visual design and wireframes
   - **MANDATORY HUMAN REVIEW CHECKPOINT** - MUST PAUSE
2. **Post-UI Approval Only**:
   - Functional Specification updates (if UI changes require them)
   - Database design (database-designer agent)
   - API design (planned)
   - Technical architecture (planned)

**Critical Rule**: UI design happens FIRST because design changes can influence technical requirements and architecture decisions.

## Quality Gate Standards
- **Feature Development**: R:95% → D:90% → I:85% → T:100%
- **Bug Fixes**: R:80% → D:70% → I:75% → T:100%
- **Hotfixes**: R:70% → D:60% → I:70% → T:100%
- **Documentation**: R:85% → D:N/A → I:N/A → T:90%
- **Refactoring**: R:90% → D:85% → I:80% → T:100%

## File Structure Requirements
For EVERY new scope of work, create:
```
/docs/functional-areas/[feature-name]/new-work/[YYYY-MM-DD]-[description]/
├── requirements/        # Business requirements and specs
├── design/             # UI, database, API, technical designs
├── implementation/     # Code implementation notes
├── testing/           # Test plans and results
├── reviews/           # Human review documents and approvals
├── lessons-learned/   # Feature-specific lessons
└── progress.md        # Phase tracking and coordination
```

## Agent Delegation Requirements

### File Path Protocol
1. **ALWAYS** get paths from librarian's master index first
2. **NEVER** let agents search for files themselves
3. **PASS** exact paths in delegation prompts
4. **UPDATE** master index when functional areas change

### Delegation Template
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

### Critical Agent Knowledge
- **Sub-agents don't read CLAUDE.md** - Include essential constraints in delegation prompts
- **Mandatory lessons learned reading** - Each agent must read their specific lessons file
- **Docker operations** - Include Docker guide references for any containerization work

## Success Metrics
Track and report:
- Time per phase
- Quality gate achievement rates
- Number of retries needed
- Human intervention frequency
- Test coverage achieved
- Documentation completeness

## Emergency Protocols
If violations detected:
- **Root directory pollution**: Relocate and alert
- **Quality gate failures**: Human review required
- **Missing human approvals**: STOP workflow immediately
- **Test delegation violations**: Restart with proper delegation
- **File registry gaps**: Add entries and investigate

## References
- **Single Source Process**: [Workflow Orchestration Process](/home/chad/repos/witchcityrope-react/docs/standards-processes/workflow-orchestration-process.md)
- **Orchestrator Lessons**: [Orchestrator Lessons Learned](/home/chad/repos/witchcityrope-react/docs/lessons-learned/orchestrator-lessons-learned.md)
- **Master Index**: [Functional Area Master Index](/home/chad/repos/witchcityrope-react/docs/architecture/functional-area-master-index.md)
- **File Registry**: [File Registry](/home/chad/repos/witchcityrope-react/docs/architecture/file-registry.md)

---

*This command documentation works in conjunction with the workflow orchestration process document as the single source of truth for all procedures. Update both documents when workflow changes occur.*