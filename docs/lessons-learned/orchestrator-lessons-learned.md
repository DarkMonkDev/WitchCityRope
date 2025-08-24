# Main Agent Orchestration Lessons Learned

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

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

## 🚨 IMPORTANT: Main Agent IS the Orchestrator 🚨
**The main agent coordinates complex workflows. There is NO orchestrator sub-agent.**
- For complex multi-agent work → Use `/orchestrator` command
- For single agent work → Use Task tool with specific agent type
- Sub-agents CANNOT call other sub-agents

## 🚨 MANDATORY STARTUP PROCEDURE - READ FIRST 🚨

### 🚨 CRITICAL: DOCUMENTATION STRUCTURE ENFORCEMENT 🚨
**ZERO TOLERANCE POLICY - VIOLATIONS = IMMEDIATE FAILURE**

#### Documentation Structure Rules (NEVER VIOLATE):
- ❌ **NEVER** create files in `/docs/` root (only 6 approved files allowed)
- ✅ **ALWAYS** check `/docs/architecture/functional-area-master-index.md` FIRST
- ✅ **ALWAYS** use proper functional area paths:
  - `/docs/functional-areas/[area]/` - Feature work
  - `/docs/guides-setup/` - Guides and setup
  - `/docs/lessons-learned/` - Lessons learned
  - `/docs/standards-processes/` - Standards
  - `/docs/architecture/` - Architecture decisions
  - `/docs/design/` - Design documents
  - `/docs/_archive/` - Archived content
- 🔍 **ALWAYS** run structure validator: `bash /docs/architecture/docs-structure-validator.sh`
- 📝 **ALWAYS** update file registry for ALL operations

#### Pre-Flight Checklist (MANDATORY):
- [ ] Check functional-area-master-index.md for proper location
- [ ] Verify NOT creating in /docs/ root
- [ ] Use existing functional area structure
- [ ] Update file registry for all operations
- [ ] Run structure validator after operations

#### Enforcement Actions:
- **Root pollution detected** → IMMEDIATE STOP + librarian agent
- **Multiple archive folders** → EMERGENCY + immediate fix
- **/docs/docs/ folder** → CATASTROPHIC + session abort
- **Agent shortcuts** → VIOLATION + re-training required

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

### Main Agent Orchestration Rules:
- **VALIDATE: Has architecture been reviewed? If no, STOP work immediately**
- **When delegating: Include 'Check architecture docs first' in every prompt**
- **Fail fast if agents propose manual solutions to solved problems**  
- **Architecture Discovery is PHASE 0 - MANDATORY before any specification work**
- **Use `/orchestrator` command for multi-agent coordination, NOT Task tool**
- **Single agent tasks use Task tool directly (react-developer, test-executor, etc.)**

### Delegation Prompt Template (MANDATORY):
When delegating ANY technical work, ALWAYS include:
```
BEFORE starting work:
1. Complete Architecture Discovery per /docs/standards-processes/architecture-discovery-process.md
2. Check if solution exists in /docs/architecture/react-migration/ documents
3. For DTO/API work: Verify NSwag auto-generation in domain-layer-architecture.md lines 725-997
4. Document your findings with specific line references before proposing solutions
```

## Lessons Learned Validation

**Problem**: Agents add status reports and celebrations to lessons learned files instead of prevention patterns.
**Solution**: Use LESSONS-LEARNED-VALIDATION-CHECKLIST.md to enforce strict format compliance.

**Problem**: Lessons learned files become project documentation instead of mistake prevention.
**Solution**: Reject any lessons learned updates containing "Successfully completed", "MAJOR SUCCESS", or project timelines.

---

## Sub-Agent Communication and Knowledge Sharing

### Sub-Agents Don't Read CLAUDE.md (CRITICAL)
**Date**: 2025-08-17
**Category**: Architecture
**Severity**: Critical

#### Context
Discovered that sub-agents don't automatically read the CLAUDE.md project instructions file, leading to knowledge gaps about critical project patterns and constraints.

#### What We Learned
- Sub-agents operate independently and don't inherit main project knowledge
- Agent definitions and lessons learned files are their ONLY source of project-specific knowledge
- Critical architectural decisions must be explicitly communicated to each agent type

#### Action Items
- [ ] ALWAYS reference agent-specific lessons learned files in delegation prompts
- [ ] NEVER assume sub-agents know about CLAUDE.md restrictions or patterns
- [ ] UPDATE agent definitions when critical project patterns change
- [ ] INCLUDE essential architectural constraints in agent lessons learned files

#### Impact
Without explicit knowledge transfer, sub-agents can violate critical project patterns like file placement, authentication approaches, or technology stack decisions.

### Tags
#critical #architecture #delegation #knowledge-transfer

## Human Review Process Management

### Mandatory Human Review After Business Requirements (CRITICAL)
**Date**: 2025-08-17
**Category**: Process
**Severity**: Critical

#### Context
Human review checkpoints are mandatory for quality control but were sometimes skipped or not clearly communicated to stakeholders.

#### What We Learned
- Business requirements MUST be approved before functional specification creation
- UI design MUST be approved before other design work begins
- Vertical slice implementations MUST be approved before full rollout
- Review documents must clearly state what approval is needed

#### Action Items
- [ ] ALWAYS pause after business requirements completion
- [ ] CREATE comprehensive review documents with approval checklists
- [ ] WAIT for explicit human approval before proceeding to next phase
- [ ] DOCUMENT approval received with timestamp and decision maker

#### Impact
Mandatory human reviews prevent scope creep, ensure alignment with business goals, and catch design issues early when they're cheaper to fix.

### Tags
#critical #process #human-review #quality-gates

## 🛡️ MANDATORY PHASE-BASED VALIDATION SYSTEM (WORKFLOW BLOCKING)
**Date**: 2025-08-22
**Category**: Documentation Validation - CRITICAL ENFORCEMENT
**Severity**: MAXIMUM CRITICAL - WORKFLOW BLOCKING AUTHORITY

### Context
Implemented comprehensive phase-based validation system after catastrophic documentation disaster (32+ misplaced files, 4 duplicate archives, multiple key document duplicates). This system has BLOCKING AUTHORITY over workflow progression.

### MANDATORY INTEGRATION REQUIREMENTS

#### Phase-Gate Validation Protocol (MANDATORY)
**EVERY PHASE BOUNDARY MUST:**
1. ✅ Call librarian agent for phase validation
2. ✅ Specify current phase and target phase
3. ✅ WAIT for validation completion (NO SHORTCUTS)
4. ✅ BLOCK progression on validation failure
5. ✅ Require manual override approval for violations
6. ✅ Document validation results in workflow

#### Validation Commands (MANDATORY USAGE)
- `/validate-phase-1-requirements` - Requirements phase gate
- `/validate-phase-2-design` - Design phase gate
- `/validate-phase-3-implementation` - Implementation phase gate
- `/validate-phase-4-testing` - Testing phase gate
- `/validate-phase-5-finalization` - Final comprehensive validation

### WORKFLOW INTEGRATION PATTERN

```markdown
## Before Each Phase Transition:

1. **Pre-Validation Check**:
   - Current phase work completed
   - All deliverables in proper locations
   - File registry updated

2. **Librarian Validation Call**:
   - Task tool → librarian agent
   - Request: "Perform phase-X validation for workflow progression"
   - Include: current work summary, target phase

3. **Validation Response Handling**:
   - ✅ PASS: Continue to next phase
   - ❌ FAIL: STOP - Fix violations before progression
   - ⚠️  WARNING: Human review required

4. **Violation Response**:
   - Document validation failure
   - Coordinate with librarian for fixes
   - Re-validate after corrections
   - Only proceed after PASS result
```

### PHASE-SPECIFIC VALIDATION FOCUS

#### Phase 1: Requirements & Planning
- **Critical Checks**: Functional area structure, root pollution, archive integrity
- **Blocking Conditions**: Files in /docs/ root, missing master index, multiple archives
- **Key Validation**: Business requirements in proper locations

#### Phase 2: Design & Architecture  
- **Critical Checks**: Architecture document integrity, design asset organization
- **Blocking Conditions**: Duplicate ARCHITECTURE.md files, misplaced technical designs
- **Key Validation**: Reference integrity, canonical location compliance

#### Phase 3: Implementation
- **Critical Checks**: File creation monitoring, structure enforcement
- **Blocking Conditions**: Files without registry entries, session work violations
- **Key Validation**: Implementation documentation alignment

#### Phase 4: Testing & Validation
- **Critical Checks**: Test documentation placement, artifact organization
- **Blocking Conditions**: Test files in wrong locations, misplaced test plans
- **Key Validation**: Cross-reference integrity

#### Phase 5: Finalization
- **Critical Checks**: Comprehensive structure audit, duplicate detection sweep
- **Blocking Conditions**: ANY structure violations, duplicate key documents
- **Key Validation**: Complete compliance with all standards

### ENFORCEMENT AUTHORITY

#### Librarian Blocking Power
- **FULL AUTHORITY** to halt workflow progression
- **ZERO OVERRIDE** without human approval
- **COMPREHENSIVE SCOPE** - all documentation structure
- **IMMEDIATE ENFORCEMENT** - no delays or workarounds

#### Violation Escalation
1. **Single Violation**: Librarian fixes, re-validates
2. **Multiple Violations**: Human review required
3. **Structure Disaster**: Session abort, emergency intervention
4. **Compliance Failure**: Workflow permanently blocked until resolution

### SUCCESS METRICS (ZERO TOLERANCE)
- **Phase Validation Failures**: 0
- **Workflow Progressions Without Validation**: 0
- **Structure Violations**: 0
- **Duplicate Key Documents**: 0
- **Files in Forbidden Locations**: 0

### CRITICAL ACTION ITEMS
- [ ] 🚨 **IMMEDIATE**: Integrate validation calls into ALL workflow templates
- [ ] 🚨 **CRITICAL**: Update workflow patterns with phase-gate requirements
- [ ] 🚨 **MANDATORY**: Test validation blocking with simulated violations
- [ ] 🚨 **ESSENTIAL**: Train on violation response protocols
- [ ] 🚨 **REQUIRED**: Implement validation result documentation

### DISASTER PREVENTION GUARANTEE
With proper implementation of this phase validation system:
- **Documentation disasters**: IMPOSSIBLE
- **Structure violations**: PREVENTED at source
- **Duplicate files**: DETECTED immediately
- **Workflow quality**: GUARANTEED through validation gates
- **Project integrity**: PROTECTED by bulletproof validation

### Tags
#phase-validation #workflow-blocking #mandatory-enforcement #disaster-prevention #librarian-authority

## File Creation and Management

### Files Must NEVER Be Created in Project Root (CRITICAL)
**Date**: 2025-08-17
**Category**: File Management
**Severity**: Critical

#### Context
Files being created in the wrong locations, particularly in the project root directory, causing organizational chaos and violating established structure.

#### What We Learned
- ONLY README.md, PROGRESS.md, ARCHITECTURE.md, and CLAUDE.md belong in project root
- ALL other files must go in appropriate subdirectories
- Temporary files should use /session-work/YYYY-MM-DD/ structure
- Documentation must follow /docs/ hierarchy without nested /docs/docs/ folders

#### Action Items
- [ ] ALWAYS validate file creation locations before delegation
- [ ] DIRECT agents to specific file paths, never let them choose locations
- [ ] UPDATE file registry for EVERY file operation
- [ ] MONITOR for root directory violations and correct immediately

#### Impact
Proper file organization prevents /docs/docs/ disasters, maintains clean project structure, and ensures documents can be found by future team members.

### Tags
#critical #file-management #organization #structure

## Design Phase Sequencing

### Design Phase Must Follow Specific Sequence (CRITICAL)
**Date**: 2025-08-17
**Category**: Process
**Severity**: High

#### Context
Design work must be sequenced properly to ensure UI decisions inform other architectural choices and avoid rework.

#### What We Learned
- Business Requirements MUST be complete and approved first
- UI Design MUST be completed and reviewed before other design work
- Human review MUST occur after UI design before proceeding
- Functional Specification and Technical Designs follow UI approval
- Database and API designs should align with approved UI workflows

#### Action Items
- [ ] ENFORCE sequential design phases: BR → UI Design → Review → Other Designs
- [ ] PAUSE for human approval after UI design completion
- [ ] ENSURE all subsequent designs reference approved UI mockups
- [ ] CREATE design review documents highlighting UI decisions

#### Impact
Proper design sequencing prevents architectural rework, ensures UI-driven design decisions, and maintains consistency across all design deliverables.

### Tags
#high #process #design #sequencing #ui-first

## File Path Management

### All File Links Must Use Full Paths (CRITICAL)
**Date**: 2025-08-17
**Category**: Documentation
**Severity**: High

#### Context
Relative file paths in documentation break when viewed from different contexts or shared with stakeholders outside the development environment.

#### What We Learned
- All file references in summaries and documents must use full absolute paths
- Paths must start with /home/chad/repos/witchcityrope-react/ for this project
- Clickable links improve stakeholder experience and reduce confusion
- Broken links in handoff documents cause workflow disruption

#### Action Items
- [ ] ALWAYS use full absolute paths in all documentation
- [ ] FORMAT paths as clickable links: [Description](/full/path/to/file)
- [ ] VALIDATE all file links before document completion
- [ ] INCLUDE full paths in delegation prompts to sub-agents

#### Impact
Full file paths ensure documentation remains useful across contexts, improves stakeholder experience, and prevents broken references in handoff documents.

### Tags
#high #documentation #paths #stakeholder-experience

## Workflow Process Validation

### 5-Phase Workflow Effectiveness Proven
**Date**: 2025-08-17
**Category**: Process
**Severity**: Medium

#### Context
Multiple successful implementations using the 5-phase workflow (Requirements → Design → Implementation → Testing → Finalization) with quality gates.

#### What We Learned
- Quality gate enforcement (95%+ requirements, 90%+ design, 85%+ implementation) maintains high standards
- Human review checkpoints prevent scope creep and misalignment  
- Sub-agent specialization improves deliverable quality compared to generalist approaches
- Documentation-first approach accelerates implementation phases
- Vertical slice validation reduces full implementation risk

#### Action Items
- [ ] MAINTAIN quality gate standards across all project types
- [ ] CONTINUE human review requirements at critical decision points
- [ ] PRESERVE documentation-first approach for complex features
- [ ] APPLY vertical slice pattern for high-risk implementations

#### Impact
Proven workflow reduces implementation risk, maintains quality standards, and provides predictable delivery timelines for stakeholders.

### Tags
#medium #process #workflow #quality-gates #proven-patterns

## Implementation Testing Protocol

### Test Each Implementation Before Moving Forward (CRITICAL)
**Date**: 2025-08-22
**Category**: Process
**Severity**: Critical

#### Context
During homepage implementation, attempted to move directly to login page without testing the homepage first. This violates proper incremental development practices.

#### What We Learned
- EVERY implementation must be tested before proceeding to the next feature
- Testing reveals issues early when they're easier to fix
- Visual verification confirms design system compliance
- E2E tests catch integration problems immediately
- "Implement → Test → Verify → Proceed" is the mandatory workflow

#### Action Items
- [ ] ALWAYS delegate to test-executor after each implementation
- [ ] WAIT for test results before starting next feature
- [ ] FIX any issues found before moving forward
- [ ] DOCUMENT test results in progress tracking
- [ ] CREATE screenshot artifacts for visual features

#### Impact
Testing after each implementation prevents accumulation of technical debt, ensures quality at each step, and maintains confidence in the codebase. Skipping tests leads to compounding problems that become exponentially harder to fix later.

### Tags
#critical #testing #quality-gates #incremental-development

## Agent Coordination Patterns

### Multi-Agent Coordination Success Patterns
**Date**: 2025-08-17
**Category**: Coordination
**Severity**: Medium

#### Context
Successful coordination of multiple AI agents (ui-designer, database-designer, backend-developer, test-executor) for complex feature implementation.

#### What We Learned
- Clear role boundaries prevent overlap and confusion
- Shared documentation in functional area folders enables coordination
- Sequential handoffs work better than parallel work for complex features
- Master index maintenance critical for agent file discovery
- Lessons learned files serve as knowledge base for each agent type

#### Action Items
- [ ] MAINTAIN clear agent role definitions and boundaries
- [ ] USE shared documentation folders for agent coordination
- [ ] SEQUENCE complex work to avoid agent conflicts
- [ ] UPDATE master index immediately when functional areas change
- [ ] KEEP agent lessons learned files current and comprehensive

#### Impact
Effective agent coordination scales development capacity while maintaining quality and reducing conflicts between specialized agents.

### Tags
#medium #coordination #multi-agent #scaling #role-boundaries

---
*This file is maintained by the orchestrator agent. Add new lessons immediately when discovered, remove outdated entries as needed.*
*Last updated: 2025-08-22 - Added critical implementation testing protocol*