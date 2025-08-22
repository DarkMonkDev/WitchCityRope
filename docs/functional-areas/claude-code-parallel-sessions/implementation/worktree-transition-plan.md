# Git Worktree Transition Implementation Plan
<!-- Last Updated: 2025-08-22 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Executive Summary

This document provides a comprehensive implementation plan for transitioning the AI development workflow from using git branches in the same directory to using git worktrees. This transition is specifically designed for AI agents (not human developers) and addresses the critical concern of proper cleanup of worktree folders after work is merged.

**Key Deliverables:**
- 5-phase implementation approach
- Current branch migration strategy (3-4 active branches)
- Agent responsibility assignments
- Automated cleanup process design
- Complete workflow documentation updates

## Phase 1: Preparation (2-3 days)

### Workflow Process Document Updates Needed

#### 1.1 Orchestrator Command Update
**File**: `/home/chad/repos/witchcityrope-react/.claude/commands/orchestrate.md`

**Required Changes:**
- Update git workflow section to reference worktree creation instead of branch creation
- Add worktree directory structure requirements
- Update agent delegation to include worktree context
- Add cleanup procedures to finalization phase

**New Sections to Add:**
```markdown
### Worktree Workflow Integration
- Worktree creation delegation to git-manager
- Directory structure requirements for isolated development
- Environment setup automation requirements
- Cleanup coordination between phases
```

#### 1.2 Workflow Orchestration Process Update
**File**: `/home/chad/repos/witchcityrope-react/docs/standards-processes/workflow-orchestration-process.md`

**Required Changes:**
- Replace branch-based isolation with worktree-based isolation
- Update initialization procedures to include worktree setup
- Add environment synchronization requirements
- Define worktree naming conventions for AI agents

### Agent Definition Updates Required

#### 1.3 Git Manager Agent Enhancement
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/utility/git-manager.md`

**Major Additions Required:**
1. **Worktree Management Section**
   - Creation procedures with environment setup
   - Status checking across multiple worktrees
   - Cleanup procedures for merged branches
   - Directory structure management

2. **AI Agent Specific Patterns**
   - Automated worktree naming for AI workflows
   - Environment file synchronization
   - Docker port management for parallel development
   - Cleanup triggers and procedures

3. **Recovery Procedures Update**
   - Worktree-specific recovery options
   - Cross-worktree state management
   - Isolation failure handling

#### 1.4 Orchestrator Lessons Learned Update
**File**: `/home/chad/repos/witchcityrope-react/docs/lessons-learned/orchestrator-lessons-learned.md`

**New Lessons to Add:**
- Worktree initialization requirements for each workflow
- Environment synchronization between worktrees
- Cleanup coordination patterns
- Agent delegation with worktree context

### Lessons Learned Updates for Each Agent

#### 1.5 All Development Agents
**Files to Update:**
- `/home/chad/repos/witchcityrope-react/docs/lessons-learned/backend-lessons-learned.md`
- `/home/chad/repos/witchcityrope-react/docs/lessons-learned/react-developer-lessons-learned.md`
- `/home/chad/repos/witchcityrope-react/docs/lessons-learned/test-developer-lessons-learned.md`

**Lessons to Add:**
- Working directory awareness (know which worktree you're in)
- Environment file synchronization checks
- Dependency installation in worktree context
- Database connection string awareness

#### 1.6 Librarian Lessons Update
**File**: `/home/chad/repos/witchcityrope-react/docs/lessons-learned/librarian-lessons-learned.md`

**New Lessons:**
- Worktree directory structure documentation patterns
- Cross-worktree file organization
- Cleanup documentation requirements
- Progress tracking across worktrees

### New Guides/Documentation Needed

#### 1.7 AI Agent Worktree Quick Reference
**New File**: `/home/chad/repos/witchcityrope-react/docs/guides-setup/ai-agent-worktree-guide.md`

**Content:**
- Quick commands for agents
- Environment setup checklist
- Common troubleshooting
- Cleanup procedures

#### 1.8 Worktree Environment Setup Guide
**New File**: `/home/chad/repos/witchcityrope-react/docs/standards-processes/worktree-environment-setup.md`

**Content:**
- Environment file synchronization procedures
- Docker configuration for parallel development
- Node.js dependency management
- Database connection configuration

## Phase 2: Current Branch Migration Strategy (1 day)

### Timing: After Current Development Completes

**CRITICAL**: This migration MUST wait until all current development on existing branches is complete and merged to avoid disruption.

### Pre-Migration Assessment

#### 2.1 Branch Inventory (To be updated at migration time)
**Current Active Branches (Estimated 3-4):**
- `feature/2025-08-22-api-architecture-modernization`
- Additional branches to be identified at migration time

#### 2.2 Migration Decision Matrix
For each existing branch:

| Branch | Status | Action | Rationale |
|--------|--------|--------|-----------|
| Active Development | In progress | Wait, then convert | Minimize disruption |
| Ready for Merge | Complete | Merge first, then archive | Clean slate |
| Experimental | Testing | Archive or convert | Based on value |
| Hotfix | Critical | Special handling | Priority conversion |

### Migration Steps for Each Branch

#### 2.3 Standard Migration Process
For each branch identified:

1. **Assessment Phase**
   ```bash
   # Check branch status
   git status
   git log --oneline -10
   git diff main..branch-name --stat
   ```

2. **Decision Point**
   - If actively developed: Convert to worktree
   - If ready for merge: Merge to main first
   - If experimental: Archive or convert based on value

3. **Conversion Process** (For branches to convert)
   ```bash
   # Create worktree from existing branch
   git worktree add ../witchcityrope-worktrees/[branch-name] [branch-name]
   
   # Setup environment in worktree
   cd ../witchcityrope-worktrees/[branch-name]
   cp ../../witchcityrope-react/.env.local . 2>/dev/null || true
   npm install
   ```

4. **Verification**
   - Verify all files present
   - Verify environment works
   - Test basic functionality

5. **Cleanup Original Branch**
   ```bash
   # Remove original branch after verification
   git branch -D [branch-name]
   ```

### Migration Rollback Plan

#### 2.4 Recovery Procedures
If migration encounters issues:

1. **Branch Recreation**
   ```bash
   # Recreate original branch if needed
   git checkout -b [branch-name] [last-known-good-commit]
   ```

2. **Worktree Removal**
   ```bash
   # Remove problematic worktree
   git worktree remove ../witchcityrope-worktrees/[branch-name]
   ```

3. **Documentation Update**
   - Log migration issues in librarian lessons learned
   - Update migration procedures based on learnings

## Phase 3: Worktree Implementation (3-4 days)

### Step-by-Step Implementation Process

#### 3.1 Directory Structure Creation
**Responsibility**: Git Manager Agent

**Directory Structure:**
```
/home/chad/repos/
├── witchcityrope-react/              # Main repository
└── witchcityrope-worktrees/          # Worktrees directory
    ├── feature-[YYYY-MM-DD]-[name]/  # Feature development
    ├── bugfix-[YYYY-MM-DD]-[name]/   # Bug fixes
    ├── enhancement-[YYYY-MM-DD]-[name]/ # UI improvements
    └── hotfix-[YYYY-MM-DD]-[name]/   # Emergency fixes
```

#### 3.2 Agent Implementation Assignments

##### 3.2.1 Git Manager Agent Implementation
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/utility/git-manager.md`

**New Functions to Implement:**
1. **Worktree Creation Function**
   - Automated directory naming
   - Environment file copying
   - Dependency installation
   - Verification procedures

2. **Worktree Status Function**
   - List all active worktrees
   - Show status of each worktree
   - Identify stale worktrees

3. **Worktree Cleanup Function**
   - Identify merged branches
   - Remove corresponding worktrees
   - Clean up environment files

##### 3.2.2 Orchestrator Integration
**File**: `/home/chad/repos/witchcityrope-react/.claude/commands/orchestrate.md`

**Integration Points:**
1. **Initialization Phase Update**
   - Replace `git checkout -b` with worktree creation delegation
   - Add environment setup verification
   - Update documentation folder creation

2. **Delegation Enhancement**
   - Pass worktree context to all agents
   - Include working directory in all prompts
   - Verify agent working directory awareness

3. **Finalization Phase Update**
   - Add cleanup delegation to git-manager
   - Verify all changes committed
   - Coordinate worktree removal after merge

##### 3.2.3 Development Agent Updates
**Agents**: react-developer, backend-developer, test-developer

**Required Updates:**
1. **Working Directory Awareness**
   - Always verify current working directory
   - Check for correct environment files
   - Validate dependency installation

2. **Environment Verification**
   - Check for proper .env files
   - Verify database connections
   - Validate Docker configurations

### Automation Scripts Needed

#### 3.3 Worktree Management Scripts
**Location**: `/home/chad/repos/witchcityrope-react/scripts/worktree/`

##### 3.3.1 Creation Script
**File**: `create-worktree.sh`
```bash
#!/bin/bash
# Create new worktree with full environment setup
# Usage: ./create-worktree.sh <branch-name>
```

##### 3.3.2 Status Script
**File**: `worktree-status.sh`
```bash
#!/bin/bash
# Show status of all worktrees
# Identify stale or ready-to-cleanup worktrees
```

##### 3.3.3 Cleanup Script
**File**: `cleanup-worktrees.sh`
```bash
#!/bin/bash
# Remove worktrees for merged branches
# Clean up environment files and directories
```

#### 3.4 Agent Integration Scripts
**Location**: `/.claude/scripts/`

##### 3.4.1 Agent Environment Setup
**File**: `setup-agent-environment.sh`
```bash
#!/bin/bash
# Verify agent working directory
# Check environment files
# Validate dependencies
```

## Phase 4: Cleanup Process Design

### Cleanup Responsibility Recommendation: **Git Manager Agent**

#### 4.1 Rationale for Git Manager Ownership

**Advantages:**
1. **Domain Expertise**: Git operations are core git-manager responsibility
2. **Workflow Integration**: Already handles branch lifecycle management
3. **Centralized Control**: Single point of responsibility for git state
4. **Existing Knowledge**: Already understands branch-to-main merge patterns

**vs. Librarian Ownership:**
- **Librarian Focus**: File organization, not git operations
- **Domain Separation**: Git operations should stay with git specialist
- **Complexity**: Would require librarian to learn git worktree internals

#### 4.2 Cleanup Workflow After PR Merge

##### 4.2.1 Trigger Points
**Automatic Cleanup Triggers:**
1. **After Merge to Main**: When orchestrator completes finalization
2. **Manual Trigger**: When git-manager detects merged branches
3. **Scheduled Cleanup**: Daily automated cleanup scan

##### 4.2.2 Cleanup Process Steps
1. **Merge Detection**
   ```bash
   # Identify merged branches
   git branch --merged main | grep -E "(feature|bugfix|hotfix)/"
   ```

2. **Worktree Verification**
   ```bash
   # Verify corresponding worktree exists
   git worktree list | grep [branch-name]
   ```

3. **Safety Checks**
   - Verify branch is actually merged
   - Check for uncommitted changes in worktree
   - Confirm no active processes in worktree

4. **Cleanup Execution**
   ```bash
   # Remove worktree
   git worktree remove ../witchcityrope-worktrees/[worktree-name]
   
   # Remove branch
   git branch -d [branch-name]
   ```

5. **Documentation Update**
   - Log cleanup in file registry
   - Update orchestrator on cleanup completion

### Preventing Folder Accumulation

#### 4.3 Prevention Strategies

##### 4.3.1 Automated Monitoring
**Implementation**: Git Manager Agent daily check
- Monitor worktree directory size
- Count number of active worktrees
- Alert when thresholds exceeded

**Thresholds:**
- **Warning**: >5 active worktrees
- **Critical**: >10 active worktrees
- **Disk Space**: >10GB total worktree space

##### 4.3.2 Proactive Cleanup
**Daily Cleanup Tasks:**
1. **Stale Branch Detection**
   - Identify branches older than 30 days
   - Flag inactive worktrees
   - Suggest cleanup to orchestrator

2. **Orphaned Worktree Detection**
   - Find worktrees without corresponding branches
   - Identify corrupted worktree states
   - Automatic cleanup with safety checks

##### 4.3.3 Emergency Cleanup Procedures
**When Cleanup Fails:**
1. **Manual Intervention Protocol**
   - Document the failure
   - Provide manual cleanup steps
   - Update automation to prevent recurrence

2. **Force Cleanup (Last Resort)**
   ```bash
   # Force remove worktree directory
   rm -rf ../witchcityrope-worktrees/[worktree-name]
   git worktree prune
   ```

## Phase 5: Agent Training (Documentation Updates)

### Complete Agent Update Matrix

#### 5.1 Orchestrator Updates
**File**: `/home/chad/repos/witchcityrope-react/.claude/commands/orchestrate.md`

**Specific Changes:**
1. **Initialization Protocol Section**
   - Replace branch creation with worktree creation
   - Add environment setup delegation
   - Update directory structure creation

2. **Delegation Rules Section**
   - Add worktree context passing requirements
   - Update agent working directory verification
   - Add cleanup coordination rules

3. **Quality Gate Enforcement Section**
   - Add worktree environment verification
   - Update testing procedures for isolated environments
   - Add cleanup verification steps

#### 5.2 Git Manager Agent Complete Overhaul
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/utility/git-manager.md`

**New Sections Required:**
1. **Worktree Management (New)**
   - Creation procedures with full environment setup
   - Status monitoring and reporting
   - Cleanup automation and safety checks

2. **Workflow Integration Update**
   - Replace branch-based workflow with worktree workflow
   - Update commit and merge procedures
   - Add environment synchronization

3. **Recovery Procedures Update**
   - Worktree-specific recovery options
   - Cross-worktree state management
   - Isolation failure handling

#### 5.3 Development Agents Updates

##### 5.3.1 React Developer Agent
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/development/react-developer.md`

**New Requirements:**
1. **Working Directory Verification**
   - Always check current working directory at startup
   - Verify worktree environment setup
   - Check for proper .env files

2. **Environment Awareness**
   - Verify Node.js dependencies in current worktree
   - Check Docker configuration for worktree
   - Validate development server ports

3. **Development Workflow Updates**
   - Update development server startup procedures
   - Add worktree-specific testing procedures
   - Include cleanup responsibilities

##### 5.3.2 Backend Developer Agent
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/implementation/backend-developer.md`

**New Requirements:**
1. **Database Connection Management**
   - Verify database connection strings in worktree
   - Check for proper environment variables
   - Validate TestContainers configuration

2. **API Development in Isolation**
   - Port management for parallel API development
   - Environment-specific configuration
   - Testing isolation procedures

##### 5.3.3 Test Developer Agent
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/testing/test-developer.md`

**New Requirements:**
1. **Test Environment Setup**
   - Verify test database configuration in worktree
   - Check TestContainers setup
   - Validate test isolation

2. **Cross-Worktree Test Management**
   - Prevent test interference between worktrees
   - Manage test database instances
   - Coordinate test cleanup

#### 5.4 Utility Agents Updates

##### 5.4.1 Librarian Agent
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/utility/librarian.md`

**New Responsibilities:**
1. **Worktree Documentation Management**
   - Track documentation across worktrees
   - Manage cross-worktree file organization
   - Update file registry with worktree context

2. **Cleanup Documentation**
   - Document worktree lifecycle in file registry
   - Track cleanup activities
   - Maintain worktree status documentation

##### 5.4.2 Test Executor Agent
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/testing/test-executor.md`

**New Requirements:**
1. **Worktree-Aware Test Execution**
   - Verify testing in correct worktree
   - Prevent cross-worktree test interference
   - Report results with worktree context

### Training Materials Creation

#### 5.5 Quick Reference Documents
**New Files to Create:**

1. **AI Agent Worktree Quick Reference**
   **File**: `/home/chad/repos/witchcityrope-react/docs/guides-setup/ai-agent-worktree-quick-reference.md`
   - Common commands for each agent
   - Troubleshooting checklist
   - Environment verification steps

2. **Worktree Troubleshooting Guide**
   **File**: `/home/chad/repos/witchcityrope-react/docs/guides-setup/worktree-troubleshooting.md`
   - Common issues and solutions
   - Environment sync problems
   - Cleanup recovery procedures

3. **Agent Delegation with Worktrees**
   **File**: `/home/chad/repos/witchcityrope-react/docs/standards-processes/agent-delegation-worktree-context.md`
   - How to pass worktree context in delegations
   - Required information for each agent type
   - Verification procedures

### Documentation Update Verification

#### 5.6 Verification Checklist
**For Each Agent Documentation Update:**

1. **Startup Procedure Updates**
   - [ ] Working directory verification added
   - [ ] Environment file checks included
   - [ ] Dependency verification procedures added

2. **Workflow Integration**
   - [ ] Worktree context awareness documented
   - [ ] Cross-worktree coordination procedures added
   - [ ] Cleanup responsibilities defined

3. **Error Handling**
   - [ ] Worktree-specific error procedures documented
   - [ ] Recovery options updated
   - [ ] Escalation procedures defined

## Implementation Success Metrics

### Technical Metrics
- **Isolation Achievement**: 100% elimination of branch context sharing between Claude Code sessions
- **Environment Setup Time**: <5 minutes for new worktree creation
- **Cleanup Efficiency**: >95% automatic cleanup success rate
- **Disk Space Management**: <15GB total worktree space maintained

### Operational Metrics
- **Agent Adaptation**: 100% of agents successfully operating in worktree environment within 1 week
- **Documentation Completeness**: All agent documentation updated with worktree procedures
- **Error Reduction**: <5% worktree-related issues after first week

### Process Metrics
- **Migration Success**: All existing branches successfully migrated or merged
- **Workflow Continuity**: Zero development delays during transition
- **Training Effectiveness**: All agents following worktree procedures correctly

## Risk Mitigation

### High Risk: Agent Confusion During Transition
**Mitigation:**
- Phase 5 comprehensive training before implementation
- Quick reference guides for all agents
- Rollback procedures to branch-based workflow if needed

### Medium Risk: Environment Synchronization Issues
**Mitigation:**
- Automated environment setup scripts
- Verification procedures for each agent
- Clear troubleshooting documentation

### Medium Risk: Disk Space Accumulation
**Mitigation:**
- Automated cleanup procedures
- Monitoring and alerting systems
- Emergency cleanup protocols

## Next Steps

1. **Immediate (Next Session)**
   - Begin Phase 1 documentation updates
   - Create automation scripts foundation
   - Update git-manager agent definitions

2. **Week 1**
   - Complete all documentation updates
   - Test automation scripts
   - Prepare migration procedures

3. **Week 2**
   - Execute current branch migration
   - Implement worktree workflow
   - Begin agent training verification

4. **Week 3**
   - Full workflow testing
   - Refinement based on initial usage
   - Documentation of lessons learned

---

**Implementation Owner**: Git Manager Agent (Primary), Librarian Agent (Supporting)  
**Review Required**: After Phase 1 completion, before Phase 2 migration  
**Success Criteria**: All agents operating successfully in worktree environment with automated cleanup functioning