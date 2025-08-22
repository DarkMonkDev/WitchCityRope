# Workflow Update Checklist for Git Worktree Transition
<!-- Last Updated: 2025-08-22 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Executive Summary

This document provides a comprehensive checklist of all files requiring updates for the git worktree transition. Every file listed here MUST be updated to ensure successful transition from branch-based to worktree-based AI agent workflows.

**Critical Success Factor**: This checklist ensures no documentation is missed during the transition, preventing agent confusion and workflow failures.

## Primary Workflow Process Files

### 1. Core Orchestration Process
**Priority**: CRITICAL - UPDATE FIRST

#### 1.1 Workflow Orchestration Process (SINGLE SOURCE OF TRUTH)
**File**: `/home/chad/repos/witchcityrope-react/docs/standards-processes/workflow-orchestration-process.md`

**Required Updates:**
- [ ] Replace all branch creation references with worktree creation
- [ ] Update initialization procedures to include worktree setup
- [ ] Add environment synchronization requirements
- [ ] Update isolation mechanisms from branch-based to worktree-based
- [ ] Add cleanup procedures to finalization phase
- [ ] Update quality gate verification to include worktree environment checks
- [ ] Add worktree naming conventions for AI workflows
- [ ] Update agent delegation to include worktree context

**Impact**: THIS IS THE AUTHORITATIVE SOURCE - all other documents reference this

#### 1.2 Orchestrate Command Definition
**File**: `/home/chad/repos/witchcityrope-react/.claude/commands/orchestrate.md`

**Required Updates:**
- [ ] **Initialization Protocol Section**: Replace `git checkout -b` with worktree creation delegation
- [ ] **Git Workflow Section**: Update all git operations to use worktree patterns
- [ ] **Agent Delegation Requirements**: Add worktree context passing requirements
- [ ] **Quality Gate Enforcement**: Add worktree environment verification steps
- [ ] **Finalization Phase**: Add cleanup coordination with git-manager
- [ ] **Critical Delegation Rules**: Add worktree working directory verification requirements
- [ ] **File Path Protocol**: Update to handle worktree-relative paths
- [ ] **Progress Tracking**: Update to include worktree status

**Impact**: Direct user interface - orchestrator behavior depends on this

## Agent Definition Files

### 2. Primary Agent Updates
**Priority**: CRITICAL - AGENTS WON'T WORK WITHOUT THESE

#### 2.1 Git Manager Agent (MOST CRITICAL)
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/utility/git-manager.md`

**Required Updates:**
- [ ] **Add Complete Worktree Management Section**:
  - [ ] Worktree creation procedures with environment setup
  - [ ] Worktree status monitoring and reporting
  - [ ] Cleanup automation and safety procedures
  - [ ] Directory structure management
- [ ] **Update Workflow Process Section**:
  - [ ] Replace branch-based workflow with worktree workflow
  - [ ] Update commit and merge procedures for worktree context
  - [ ] Add environment synchronization procedures
- [ ] **Add AI Agent Specific Patterns**:
  - [ ] Automated worktree naming for AI workflows
  - [ ] Environment file synchronization
  - [ ] Docker port management for parallel development
  - [ ] Cleanup triggers and procedures
- [ ] **Update Recovery Procedures**:
  - [ ] Worktree-specific recovery options
  - [ ] Cross-worktree state management
  - [ ] Isolation failure handling
- [ ] **Add Automation Scripts Section**:
  - [ ] Worktree creation scripts
  - [ ] Status monitoring scripts
  - [ ] Cleanup automation scripts

**Impact**: PRIMARY WORKTREE MANAGER - entire system depends on this agent

#### 2.2 Librarian Agent
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/utility/librarian.md`

**Required Updates:**
- [ ] **Add Worktree Documentation Management**:
  - [ ] Track documentation across worktrees
  - [ ] Manage cross-worktree file organization
  - [ ] Update file registry with worktree context
- [ ] **Add Cleanup Documentation Responsibilities**:
  - [ ] Document worktree lifecycle in file registry
  - [ ] Track cleanup activities
  - [ ] Maintain worktree status documentation
- [ ] **Update File Registry Management**:
  - [ ] Add worktree creation/removal logging procedures
  - [ ] Update file tracking for worktree context
  - [ ] Add cleanup date tracking for worktrees

**Impact**: Documentation integrity depends on this agent understanding worktrees

### 3. Development Agent Updates
**Priority**: HIGH - DEVELOPMENT WILL FAIL WITHOUT THESE

#### 3.1 React Developer Agent
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/development/react-developer.md`

**Required Updates:**
- [ ] **Add Mandatory Startup Procedures**:
  - [ ] Always verify current working directory at startup
  - [ ] Verify worktree environment setup
  - [ ] Check for proper .env files
- [ ] **Add Environment Awareness Section**:
  - [ ] Verify Node.js dependencies in current worktree
  - [ ] Check Docker configuration for worktree
  - [ ] Validate development server ports to avoid conflicts
- [ ] **Update Development Workflow**:
  - [ ] Update development server startup procedures
  - [ ] Add worktree-specific testing procedures
  - [ ] Include isolation verification steps
- [ ] **Add Error Handling**:
  - [ ] Wrong working directory detection and escalation
  - [ ] Environment sync issues resolution
  - [ ] Port conflict resolution

#### 3.2 Backend Developer Agent
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/implementation/backend-developer.md`

**Required Updates:**
- [ ] **Add Database Connection Management**:
  - [ ] Verify database connection strings in worktree
  - [ ] Check for proper environment variables
  - [ ] Validate TestContainers configuration
- [ ] **Add API Development in Isolation**:
  - [ ] Port management for parallel API development
  - [ ] Environment-specific configuration
  - [ ] Testing isolation procedures
- [ ] **Add Working Directory Verification**:
  - [ ] Mandatory working directory check at startup
  - [ ] Environment file validation
  - [ ] Dependency verification in worktree context

#### 3.3 Test Developer Agent
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/testing/test-developer.md`

**Required Updates:**
- [ ] **Add Test Environment Setup**:
  - [ ] Verify test database configuration in worktree
  - [ ] Check TestContainers setup
  - [ ] Validate test isolation
- [ ] **Add Cross-Worktree Test Management**:
  - [ ] Prevent test interference between worktrees
  - [ ] Manage test database instances
  - [ ] Coordinate test cleanup
- [ ] **Add Working Directory Awareness**:
  - [ ] Verify testing in correct worktree
  - [ ] Environment-specific test configuration
  - [ ] Test result reporting with worktree context

#### 3.4 Test Executor Agent
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/testing/test-executor.md`

**Required Updates:**
- [ ] **Add Worktree-Aware Test Execution**:
  - [ ] Execute tests in correct worktree context
  - [ ] Verify test environment isolation
  - [ ] Report results with worktree context
  - [ ] Coordinate parallel test execution
- [ ] **Add Test Environment Management**:
  - [ ] Verify test databases isolated per worktree
  - [ ] Manage TestContainers in worktree context
  - [ ] Coordinate test resource usage
  - [ ] Report test environment status

### 4. Planning and Design Agents
**Priority**: MEDIUM - NEED BASIC WORKING DIRECTORY AWARENESS

#### 4.1 Business Requirements Agent
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/planning/business-requirements.md`

**Required Updates:**
- [ ] **Add Basic Working Directory Awareness**:
  - [ ] Verify current working directory at startup
  - [ ] Report working directory in status updates
  - [ ] Escalate if working directory issues detected

#### 4.2 UI Designer Agent
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/design/ui-designer.md`

**Required Updates:**
- [ ] **Add Basic Working Directory Awareness**:
  - [ ] Verify current working directory at startup
  - [ ] Check for design files in correct worktree
  - [ ] Report working directory in status updates

#### 4.3 Database Designer Agent
**File**: `/home/chad/repos/witchcityrope-react/.claude/agents/design/database-designer.md`

**Required Updates:**
- [ ] **Add Working Directory Verification**:
  - [ ] Verify current working directory at startup
  - [ ] Check for database migration files in correct worktree
  - [ ] Validate database connection in worktree context

## Lessons Learned Files

### 5. Agent-Specific Lessons Learned
**Priority**: HIGH - AGENTS MUST READ THEIR LESSONS

#### 5.1 Orchestrator Lessons Learned
**File**: `/home/chad/repos/witchcityrope-react/docs/lessons-learned/orchestrator-lessons-learned.md`

**Required Updates:**
- [ ] **Add Worktree Orchestration Lessons**:
  - [ ] Worktree initialization requirements for each workflow
  - [ ] Environment synchronization between worktrees
  - [ ] Cleanup coordination patterns
  - [ ] Agent delegation with worktree context
- [ ] **Add Quality Gate Updates**:
  - [ ] Worktree environment verification requirements
  - [ ] Cross-worktree coordination patterns
  - [ ] Cleanup verification procedures

#### 5.2 Git Manager Lessons Learned
**File**: `/home/chad/repos/witchcityrope-react/docs/lessons-learned/devops-lessons-learned.md`

**Required Updates:**
- [ ] **Add Worktree Management Lessons**:
  - [ ] Worktree creation best practices
  - [ ] Environment synchronization techniques
  - [ ] Cleanup automation patterns
  - [ ] Safety check procedures
- [ ] **Add AI Agent Workflow Patterns**:
  - [ ] Automated naming conventions
  - [ ] Port management strategies
  - [ ] Directory structure standards
  - [ ] Recovery procedures

#### 5.3 Librarian Lessons Learned
**File**: `/home/chad/repos/witchcityrope-react/docs/lessons-learned/librarian-lessons-learned.md`

**Required Updates:**
- [ ] **Add Worktree Documentation Lessons**:
  - [ ] Cross-worktree file organization patterns
  - [ ] File registry updates for worktree lifecycle
  - [ ] Cleanup documentation requirements
  - [ ] Progress tracking across worktrees

#### 5.4 Development Agent Lessons Learned
**Files**:
- `/home/chad/repos/witchcityrope-react/docs/lessons-learned/react-developer-lessons-learned.md`
- `/home/chad/repos/witchcityrope-react/docs/lessons-learned/backend-lessons-learned.md`
- `/home/chad/repos/witchcityrope-react/docs/lessons-learned/test-developer-lessons-learned.md`

**Required Updates for All:**
- [ ] **Add Working Directory Awareness Lessons**:
  - [ ] Working directory verification importance
  - [ ] Environment file synchronization
  - [ ] Dependency installation in worktree context
  - [ ] Port conflict resolution
- [ ] **Add Isolation Best Practices**:
  - [ ] Database connection string awareness
  - [ ] Docker configuration for worktrees
  - [ ] Cross-worktree coordination
  - [ ] Error escalation procedures

## Project Configuration Files

### 6. CLAUDE.md Updates
**Priority**: HIGH - AGENTS AND USERS READ THIS

#### 6.1 Main Project Instructions
**File**: `/home/chad/repos/witchcityrope-react/CLAUDE.md`

**Required Updates:**
- [ ] **Update Git Workflow Section**:
  - [ ] Replace branch-based instructions with worktree instructions
  - [ ] Add worktree directory structure explanation
  - [ ] Update development workflow patterns
- [ ] **Add Worktree Quick Start Section**:
  - [ ] Basic worktree commands for users
  - [ ] Environment setup procedures
  - [ ] Troubleshooting common issues
- [ ] **Update Agent Interaction Guidelines**:
  - [ ] Worktree context passing requirements
  - [ ] Working directory verification importance
  - [ ] Cleanup coordination procedures

### 7. Process Documentation Updates

#### 7.1 GitHub Push Instructions
**File**: `/home/chad/repos/witchcityrope-react/docs/standards-processes/GITHUB-PUSH-INSTRUCTIONS.md`

**Required Updates:**
- [ ] **Update Push Workflow**:
  - [ ] Working from worktree context
  - [ ] Merge procedures from worktree to main
  - [ ] Cleanup after successful push
- [ ] **Add Safety Procedures**:
  - [ ] Verify working in correct worktree before push
  - [ ] Environment validation before merge
  - [ ] Post-merge cleanup verification

#### 7.2 Documentation Process
**File**: `/home/chad/repos/witchcityrope-react/docs/standards-processes/documentation-process/DOCUMENTATION_GUIDE.md`

**Required Updates:**
- [ ] **Add Worktree Documentation Standards**:
  - [ ] Cross-worktree documentation consistency
  - [ ] Worktree-specific documentation lifecycle
  - [ ] Cleanup documentation requirements

## New Documentation Required

### 8. New Files to Create
**Priority**: HIGH - ESSENTIAL FOR TRAINING

#### 8.1 AI Agent Worktree Quick Reference
**File**: `/home/chad/repos/witchcityrope-react/docs/guides-setup/ai-agent-worktree-quick-reference.md`

**Content Required:**
- [ ] Quick commands for each agent type
- [ ] Working directory verification checklist
- [ ] Environment setup validation
- [ ] Common troubleshooting steps
- [ ] Escalation procedures

#### 8.2 Worktree Environment Setup Guide
**File**: `/home/chad/repos/witchcityrope-react/docs/standards-processes/worktree-environment-setup.md`

**Content Required:**
- [ ] Environment file synchronization procedures
- [ ] Docker configuration for parallel development
- [ ] Node.js dependency management
- [ ] Database connection configuration
- [ ] Port management strategies

#### 8.3 Worktree Troubleshooting Guide
**File**: `/home/chad/repos/witchcityrope-react/docs/guides-setup/worktree-troubleshooting.md`

**Content Required:**
- [ ] Common worktree issues and solutions
- [ ] Environment synchronization problems
- [ ] Cleanup recovery procedures
- [ ] Agent working directory issues
- [ ] Port conflict resolution

#### 8.4 Agent Delegation with Worktree Context
**File**: `/home/chad/repos/witchcityrope-react/docs/standards-processes/agent-delegation-worktree-context.md`

**Content Required:**
- [ ] How to pass worktree context in delegations
- [ ] Required information for each agent type
- [ ] Verification procedures
- [ ] Error handling patterns

## Automation Scripts

### 9. New Scripts Required
**Priority**: MEDIUM - ENHANCES EFFICIENCY

#### 9.1 Worktree Management Scripts
**Directory**: `/home/chad/repos/witchcityrope-react/scripts/worktree/`

**Scripts to Create:**
- [ ] `create-worktree.sh` - Create new worktree with environment setup
- [ ] `worktree-status.sh` - Show status of all worktrees
- [ ] `cleanup-worktrees.sh` - Remove worktrees for merged branches
- [ ] `sync-environments.sh` - Synchronize environment files across worktrees

#### 9.2 Agent Integration Scripts
**Directory**: `/home/chad/repos/witchcityrope-react/.claude/scripts/`

**Scripts to Create:**
- [ ] `setup-agent-environment.sh` - Verify agent working directory
- [ ] `verify-worktree-environment.sh` - Check environment files and dependencies
- [ ] `validate-worktree-isolation.sh` - Ensure proper isolation

## Verification Checklist

### 10. Update Verification Requirements
**Priority**: CRITICAL - ENSURES QUALITY

#### 10.1 For Each Agent Documentation Update
- [ ] **Startup Procedure Updates**:
  - [ ] Working directory verification added
  - [ ] Environment file checks included
  - [ ] Dependency verification procedures added
- [ ] **Workflow Integration**:
  - [ ] Worktree context awareness documented
  - [ ] Cross-worktree coordination procedures added
  - [ ] Cleanup responsibilities defined
- [ ] **Error Handling**:
  - [ ] Worktree-specific error procedures documented
  - [ ] Recovery options updated
  - [ ] Escalation procedures defined

#### 10.2 For Each Process Document Update
- [ ] **Workflow Continuity**:
  - [ ] All branch references replaced with worktree references
  - [ ] Environment setup procedures updated
  - [ ] Cleanup procedures added
- [ ] **Agent Coordination**:
  - [ ] Delegation patterns updated
  - [ ] Context passing requirements added
  - [ ] Verification procedures included

### 11. Implementation Testing Checklist
**Priority**: CRITICAL - VALIDATES SUCCESS

#### 11.1 Before Go-Live Testing
- [ ] **Documentation Completeness**:
  - [ ] All files in this checklist updated
  - [ ] New documentation created
  - [ ] Scripts implemented and tested
- [ ] **Agent Training Validation**:
  - [ ] Each agent can find and read updated documentation
  - [ ] Working directory verification procedures work
  - [ ] Environment setup procedures validated
- [ ] **Workflow Integration Testing**:
  - [ ] Orchestrator can create worktrees via git-manager
  - [ ] Agents can work in assigned worktrees
  - [ ] Cleanup procedures function correctly

#### 11.2 Post-Implementation Verification
- [ ] **Operational Testing**:
  - [ ] Complete workflow runs successfully in worktree
  - [ ] Agent coordination works properly
  - [ ] Cleanup executes automatically
- [ ] **Performance Validation**:
  - [ ] Worktree creation time <5 minutes
  - [ ] Environment setup successful >95%
  - [ ] Cleanup success rate >95%

## Critical Success Factors

### 12. Must-Have Updates
**These CANNOT be skipped:**

1. **Git Manager Agent** - PRIMARY WORKTREE MANAGER
2. **Orchestrate Command** - USER INTERFACE
3. **Workflow Orchestration Process** - SINGLE SOURCE OF TRUTH
4. **All Development Agent Working Directory Verification** - PREVENTS FAILURES
5. **All Agent Lessons Learned** - KNOWLEDGE TRANSFER

### 13. Nice-to-Have Updates
**These enhance efficiency but aren't blockers:**

1. **Automation Scripts** - EFFICIENCY ENHANCERS
2. **Advanced Troubleshooting Guides** - SUPPORT MATERIALS
3. **Performance Optimization Documentation** - IMPROVEMENTS

## Implementation Schedule

### Phase 1: Core Updates (Days 1-2)
- [ ] Workflow orchestration process
- [ ] Orchestrate command
- [ ] Git manager agent
- [ ] Orchestrator lessons learned

### Phase 2: Agent Updates (Days 3-4)
- [ ] All development agents
- [ ] Librarian agent
- [ ] Test executor agent
- [ ] All agent lessons learned

### Phase 3: Documentation (Day 5)
- [ ] CLAUDE.md updates
- [ ] New documentation creation
- [ ] Process documentation updates

### Phase 4: Scripts and Testing (Day 6)
- [ ] Automation scripts
- [ ] Verification testing
- [ ] Integration validation

---

**Checklist Owner**: Librarian Agent  
**Review Required**: Before any implementation begins  
**Success Criteria**: All checklist items completed and verified before worktree transition