# Orchestrator Lessons Learned

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

### Orchestrator Agent Specific Rules:
- **VALIDATE: Has architecture been reviewed? If no, STOP work immediately**
- **When delegating: Include 'Check architecture docs first' in every prompt**
- **Fail fast if agents propose manual solutions to solved problems**  
- **Architecture Discovery is PHASE 0 - MANDATORY before any specification work**

### Delegation Prompt Template (MANDATORY):
When delegating ANY technical work, ALWAYS include:
```
BEFORE starting work:
1. Complete Architecture Discovery per /docs/standards-processes/architecture-discovery-process.md
2. Check if solution exists in /docs/architecture/react-migration/ documents
3. For DTO/API work: Verify NSwag auto-generation in domain-layer-architecture.md lines 725-997
4. Document your findings with specific line references before proposing solutions
```

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
*Last updated: 2025-08-17 - Initial creation with critical lessons from workflow implementation experience*