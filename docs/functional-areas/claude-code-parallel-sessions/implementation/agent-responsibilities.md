# Agent Responsibility Matrix for Git Worktrees
<!-- Last Updated: 2025-08-22 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## 🔴 CRITICAL UNDERSTANDING: Worktrees ARE Branches 🔴

**EVERY AGENT MUST UNDERSTAND:**
- A worktree IS the branch - not a copy of a branch
- When git-manager creates a worktree, it creates THE branch in its own directory
- The branch exists ONLY in the worktree directory
- The main repo NEVER switches branches - it stays on development/master
- Removing the worktree removes the branch entirely

### ❌ WRONG Mental Model:
"Create a branch, then create a worktree from it" - NO! This is redundant!

### ✅ CORRECT Mental Model:
"Create a worktree AS the branch" - The worktree IS where the branch lives!

## Executive Summary

This document defines the responsibility matrix for git worktree management across all AI agents in the WitchCityRope development workflow. It establishes clear ownership boundaries, prevents conflicts, and ensures coordinated worktree lifecycle management. All agents must understand that worktrees ARE branches, not copies.

## Primary Responsibility Matrix

### Core Worktree Operations

| Operation | Primary Agent | Secondary Agent | Backup Agent | Forbidden Agents |
|-----------|---------------|-----------------|--------------|------------------|
| **Create Worktrees (AS branches)** | git-manager | - | orchestrator | ALL others |
| **Manage Worktrees (THE branches)** | git-manager | librarian (docs) | orchestrator | ALL development agents |
| **Clean Up Worktrees (removes branches)** | git-manager | - | orchestrator | ALL others |
| **Monitor Worktrees** | git-manager | librarian (registry) | orchestrator | ALL others |

### Supporting Operations

| Operation | Primary Agent | Secondary Agent | Coordination Required |
|-----------|---------------|-----------------|----------------------|
| **Environment Setup** | git-manager | - | With orchestrator |
| **Documentation Tracking** | librarian | git-manager | File registry updates |
| **Status Reporting** | git-manager | orchestrator | Progress coordination |
| **Error Recovery** | git-manager | orchestrator | User escalation |

## Detailed Agent Responsibilities

### Git Manager Agent (PRIMARY OWNER)

#### Core Responsibilities
1. **Worktree Creation (Creates THE Branch)**
   - Create THE branch AS a worktree (not from a branch, AS the branch!)
   - Use command: `git worktree add ../path -b branch-name` (creates branch IN worktree)
   - Set up environment files (.env.local, .env)
   - Install Node.js dependencies (npm install)
   - Verify environment functionality
   - Report creation success to orchestrator

2. **Worktree Management**
   - Monitor all active worktrees
   - Track branch-to-worktree relationships
   - Manage worktree status and health
   - Coordinate with librarian for documentation

3. **MANDATORY Cleanup Operations** (Phase 5 Finalization)
   - Detect merged branches requiring IMMEDIATE cleanup
   - Remove worktrees as part of workflow completion
   - Clean up associated directories and files
   - Update git repository state
   - Report cleanup completion BEFORE workflow ends

4. **Environment Synchronization**
   - Ensure environment files are current across worktrees
   - Update worktrees when main environment changes
   - Validate environment consistency

#### Specific Functions

##### Worktree Creation Function
```bash
# Function: create_ai_worktree
# Purpose: Creates THE branch AS a worktree (branch exists ONLY in worktree)
# Command: git worktree add ../path -b branch-name (ONE command creates both!)
# Input: branch-name, work-type (feature|bugfix|hotfix)
# Output: worktree path (where THE branch lives), environment status, ready signal
# Dependencies: main repo access (stays on development), environment files
```

##### Worktree Status Function
```bash
# Function: list_worktree_status
# Output: active worktrees, associated branches, disk usage, cleanup candidates
# Dependencies: git worktree list, branch analysis
```

##### MANDATORY Cleanup Function (Phase 5)
```bash
# Function: cleanup_merged_worktrees_finalization
# Purpose: Remove worktree (which removes THE branch - it only existed there!)
# Command: git worktree remove ../path (removes BOTH worktree AND branch)
# Trigger: Part of Phase 5 workflow completion
# Input: branch name, merge confirmation
# Output: cleanup success/failure, branch gone, directory removed
# Dependencies: merge detection, safety verification, workflow completion
```

#### Coordination Requirements
- **With Orchestrator**: Receive worktree creation requests, report status
- **With Librarian**: Provide worktree information for file registry
- **With Development Agents**: Verify working directories, provide environment status

### Orchestrator (COORDINATION OWNER)

#### Core Responsibilities
1. **Worktree Workflow Integration**
   - Request worktree creation from git-manager (creates THE branch AS worktree)
   - Understand: the branch lives ONLY in that worktree directory
   - Coordinate agent working directories (where branches live)
   - Manage worktree context in delegations
   - Trigger cleanup after successful merges (removes branch AND worktree)

2. **Agent Coordination**
   - Pass worktree context to all delegated agents
   - Verify agents are working in correct worktrees
   - Coordinate parallel development in separate worktrees
   - Manage workflow state across worktrees

3. **Quality Gate Enforcement**
   - Verify environment setup before proceeding
   - Ensure proper isolation between work streams
   - Validate cleanup completion
   - Monitor worktree health during workflow

#### Delegation Patterns

##### Standard Delegation with Worktree Context
```markdown
Task: [agent-name]
Prompt: [specific instructions]

Worktree Context:
- Working Directory: /home/chad/repos/witchcityrope-worktrees/[worktree-name]
- Branch: [branch-name] (exists ONLY in this worktree directory!)
- Main Repo: Stays on development/master (NEVER switches branches)
- Environment: [environment-status]

Required Verification:
- Confirm working directory before starting
- Verify environment files present
- Check dependencies installed
```

##### Environment Verification Delegation
```markdown
Task: git-manager
Prompt: Verify worktree environment for [worktree-name]

Required Checks:
- Environment files (.env.local, .env)
- Node.js dependencies (package.json, node_modules)
- Database connection configuration
- Docker configuration if applicable
```

### Librarian Agent (DOCUMENTATION OWNER)

#### Core Responsibilities
1. **Worktree Documentation Management**
   - Track worktree creation in file registry
   - Document worktree lifecycle events
   - Maintain worktree-to-documentation relationships
   - Update functional area documentation

2. **File Registry Maintenance**
   - Log all worktree operations in file registry
   - Track files created/modified in each worktree
   - Document cleanup activities
   - Maintain worktree history

3. **Cross-Worktree Documentation**
   - Ensure documentation consistency across worktrees
   - Manage shared documentation updates
   - Coordinate documentation merges
   - Archive worktree-specific documentation

#### File Registry Updates Required

##### Worktree Creation Entry
```markdown
| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| YYYY-MM-DD | /worktrees/[name]/ | WORKTREE_CREATED | [Feature description] | [Task] | ACTIVE | [Merge date] |
```

##### Worktree Cleanup Entry
```markdown
| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| YYYY-MM-DD | /worktrees/[name]/ | WORKTREE_REMOVED | Merged to main | Cleanup | ARCHIVED | YYYY-MM-DD |
```

### Development Agents (CONTEXT-AWARE WORKERS)

#### Shared Responsibilities
1. **Working Directory Awareness**
   - Always verify current working directory at startup
   - Confirm working in assigned worktree
   - Report working directory in all status updates
   - Escalate if working directory incorrect

2. **Environment Verification**
   - Check for required environment files
   - Verify dependencies installed
   - Validate configuration settings
   - Report environment issues to orchestrator

3. **Isolation Maintenance**
   - Work only within assigned worktree
   - Avoid cross-worktree dependencies
   - Report any isolation issues
   - Coordinate shared resource usage

#### React Developer Agent Specific
- **Working Directory**: `/home/chad/repos/witchcityrope-worktrees/[worktree-name]`
- **Environment Requirements**: .env.local for React dev server
- **Dependencies**: Node.js packages via npm
- **Ports**: Must coordinate React dev server ports to avoid conflicts

#### Backend Developer Agent Specific
- **Working Directory**: `/home/chad/repos/witchcityrope-worktrees/[worktree-name]`
- **Environment Requirements**: Database connection strings, API configuration
- **Dependencies**: .NET packages, TestContainers
- **Ports**: Must coordinate API ports to avoid conflicts

#### Test Developer Agent Specific
- **Working Directory**: `/home/chad/repos/witchcityrope-worktrees/[worktree-name]`
- **Environment Requirements**: Test database configuration, TestContainers
- **Dependencies**: Test frameworks, test data
- **Isolation**: Prevent test interference between worktrees

### Test Executor Agent (EXECUTION OWNER)

#### Core Responsibilities
1. **Worktree-Aware Test Execution**
   - Execute tests in correct worktree context
   - Verify test environment isolation
   - Report results with worktree context
   - Coordinate parallel test execution

2. **Test Environment Management**
   - Verify test databases isolated per worktree
   - Manage TestContainers in worktree context
   - Coordinate test resource usage
   - Report test environment status

## Agent Awareness Matrix

### Who Needs to Know About Worktrees

| Agent | Awareness Level | Specific Knowledge Required |
|-------|----------------|---------------------------|
| **git-manager** | EXPERT | Worktrees ARE branches concept, full lifecycle, git internals, cleanup |
| **orchestrator** | ADVANCED | Workflow integration, delegation context, coordination |
| **librarian** | INTERMEDIATE | Documentation tracking, file registry, lifecycle events |
| **react-developer** | BASIC | Working directory verification, environment setup |
| **backend-developer** | BASIC | Working directory verification, database configuration |
| **test-developer** | INTERMEDIATE | Test isolation, environment separation, resource management |
| **test-executor** | INTERMEDIATE | Execution context, environment verification, result reporting |
| **ui-designer** | MINIMAL | Working directory awareness only |
| **business-requirements** | MINIMAL | Working directory awareness only |

### Agent Training Requirements

#### Expert Level (git-manager)
- **CRITICAL**: Understand worktrees ARE branches (not copies)
- Create branches AS worktrees in one command
- Complete git worktree functionality
- Automation script development
- Cleanup removes both worktree AND branch
- Environment synchronization
- Recovery procedures

#### Advanced Level (orchestrator)
- Worktree workflow integration
- Agent coordination patterns
- Context passing requirements
- Quality gate verification
- Cleanup coordination

#### Intermediate Level (librarian, test-developer, test-executor)
- Worktree lifecycle understanding
- Documentation requirements
- Cross-worktree coordination
- Isolation principles

#### Basic Level (development agents)
- Working directory verification
- Environment file awareness
- Basic troubleshooting
- Escalation procedures

#### Minimal Level (design/requirements agents)
- Working directory awareness only
- When to escalate issues

## Coordination Protocols

### Worktree Creation Protocol

1. **Orchestrator** → **Git Manager**: Request worktree creation
2. **Git Manager**: Create worktree, setup environment
3. **Git Manager** → **Orchestrator**: Report creation success
4. **Orchestrator** → **Librarian**: Document worktree creation
5. **Librarian**: Update file registry

### Development Work Protocol

1. **Orchestrator** → **Development Agent**: Delegate work with worktree context
2. **Development Agent**: Verify working directory and environment
3. **Development Agent**: Execute work in assigned worktree
4. **Development Agent** → **Orchestrator**: Report completion
5. **Orchestrator**: Verify work completed in correct context

### MANDATORY Cleanup Protocol (Phase 5 Finalization)

1. **Orchestrator**: Confirm PR merge and begin Phase 5 finalization
2. **Orchestrator** → **Git Manager**: MANDATORY cleanup delegation
3. **Git Manager**: Verify merge and perform IMMEDIATE cleanup
4. **Git Manager**: Remove worktree directory immediately
5. **Git Manager** → **Orchestrator**: Report cleanup SUCCESS (required)
6. **Orchestrator**: CANNOT complete workflow until cleanup confirmed
7. **Orchestrator** → **Librarian**: Document cleanup completion
8. **Librarian**: Update file registry with ARCHIVED status

## Conflict Resolution

### Responsibility Conflicts

#### Scenario: Multiple Agents Want to Create Worktrees
**Resolution**: Only git-manager creates worktrees. All requests go through orchestrator → git-manager.

#### Scenario: Development Agent Needs Environment Changes
**Resolution**: Request through orchestrator → git-manager for environment sync.

#### Scenario: Cleanup Uncertainty
**Resolution**: git-manager has final authority BUT cleanup is MANDATORY in Phase 5. No worktree can remain after workflow completion.

### Communication Protocols

#### Error Escalation
1. **Development Agent** → **Orchestrator**: Report worktree issues
2. **Orchestrator** → **Git Manager**: Request diagnosis/fix
3. **Git Manager**: Investigate and resolve or escalate to user

#### Status Communication
1. **Git Manager**: Maintains authoritative worktree status
2. **Orchestrator**: Requests status for workflow decisions
3. **Librarian**: Requests status for documentation updates

## Success Metrics

### Agent Performance Metrics
- **git-manager**: Worktree creation time <5 minutes, **MANDATORY cleanup success rate 100%**
- **orchestrator**: Successful agent coordination in worktree context >98%, **Phase 5 cleanup delegation 100%**
- **librarian**: Complete documentation of all worktree operations 100%
- **development agents**: Working directory accuracy 100%, environment verification success >95%

### Coordination Metrics
- **Communication Clarity**: Zero agent confusion about responsibilities
- **Conflict Resolution**: All responsibility conflicts resolved within 1 session
- **Workflow Integration**: Seamless worktree integration without workflow delays

### System Health Metrics
- **Worktree Accumulation**: <3 active worktrees target (ZERO accumulation goal)
- **Cleanup Efficiency**: **IMMEDIATE cleanup within workflow completion**
- **Environment Consistency**: >98% environment sync success rate
- **Documentation Accuracy**: 100% worktree operations documented

## Implementation Checklist

### Phase 1: Agent Definition Updates
- [ ] Update git-manager agent definition with worktree responsibilities
- [ ] Update orchestrator command with worktree coordination
- [ ] Update librarian agent with worktree documentation requirements
- [ ] Update all development agents with working directory verification

### Phase 2: Training Materials
- [ ] Create quick reference guides for each agent type
- [ ] Document coordination protocols
- [ ] Create troubleshooting guides
- [ ] Establish escalation procedures

### Phase 3: Testing and Validation
- [ ] Test worktree creation workflow
- [ ] Verify agent coordination patterns
- [ ] Test cleanup procedures
- [ ] Validate documentation tracking

---

**Document Owner**: Librarian Agent  
**Review Required**: Before worktree implementation begins  
**Success Criteria**: Clear responsibility boundaries with zero agent conflicts