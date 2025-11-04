# Skills Architecture & Single Source of Truth Plan

**Date**: 2025-11-04
**Status**: Planning Document
**Critical Issue**: Preventing duplicate process documentation across Skills, Lessons Learned, Agent Definitions, and Process Docs

---

## Executive Summary

**Your Three Questions:**
1. Are container restart and staging deployment good Skills? → **YES, perfect candidates**
2. How do agents know what Skills are available? → **Discovery mechanism needed**
3. How to prevent duplicate processes in multiple places? → **CRITICAL - needs architecture**

**The Core Problem You Identified:**

```
Process documentation could exist in:
├── Agent definitions (test-executor.md)
├── Process docs (workflow-orchestration-process.md)
├── Lessons learned (test-executor-lessons-learned.md)
└── Skills (container-restart.md)

Result: 4 places to update, 4 chances for inconsistency
```

**This document provides:**
- Clear Skills vs Documentation boundaries
- Single source of truth architecture
- Discovery mechanism for agents
- Migration plan for existing duplicates

---

## Part 1: Container Restart & Staging Deploy as Skills

### YES - These Are Perfect Skills Candidates

**Why Container Restart is Ideal:**
- ✅ **Automation-focused**: Bash script with clear steps
- ✅ **Frequently needed**: Multiple agents try to do this
- ✅ **Error-prone**: Agents don't follow documented process
- ✅ **Single source**: One correct way to do it
- ✅ **Clear inputs/outputs**: No containers running → All containers healthy

**Why Staging Deploy is Ideal:**
- ✅ **Complex multi-step**: Perfect for automation
- ✅ **Critical process**: Mistakes affect production-adjacent environment
- ✅ **Already documented**: Exists in `/docs/functional-areas/deployment/`
- ✅ **Repeatable**: Same steps every time
- ✅ **Validation-friendly**: Can check success at each step

**Current Problem:**
```
Agent tries to restart containers:
1. Checks their lessons learned (might find partial info)
2. Checks their agent definition (might find different info)
3. Runs docker-compose up (WRONG - should use ./dev.sh)
4. Doesn't check compilation errors
5. E2E tests fail mysteriously

With Skill:
1. Agent says "restart containers"
2. Claude Code invokes container-restart skill
3. Skill runs correct process (./dev.sh, health checks, etc.)
4. Returns success/failure
```

### Proposed New Skills

#### Skill 1: container-restart.md

```yaml
---
name: container-restart
description: Restarts WitchCityRope Docker development containers using correct procedure. Handles container shutdown, rebuild with correct compose files, health checks, and compilation verification. Ensures environment is ready for development or testing.
---

# Container Restart Skill

**Purpose**: Restart Docker containers the RIGHT way.

**When to Use**:
- Before running E2E tests
- After code changes that need container rebuild
- When containers are unhealthy
- When "Element not found" errors appear in E2E tests (usually means container has compilation errors)

## The Correct Process

### DON'T Do This:
```bash
# ❌ WRONG - Missing dev overlay, doesn't check health
docker-compose up -d
```

### DO This:

```bash
#!/bin/bash
# Container Restart - Correct Procedure

echo "🔄 Restarting WitchCityRope Development Containers"
echo ""

# Step 1: Stop existing containers
echo "1. Stopping containers..."
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down

# Step 2: Start with development overlay (CRITICAL)
echo "2. Starting containers with dev overlay..."
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build

# Or use the helper script (preferred):
# ./dev.sh

# Step 3: Wait for containers to start
echo "3. Waiting for containers to initialize..."
sleep 10

# Step 4: Check container status
echo "4. Checking container status..."
docker ps --format "table {{.Names}}\t{{.Status}}" | grep witchcity

# Step 5: Check for compilation errors (CRITICAL)
echo "5. Checking for compilation errors..."
echo ""
echo "Web container logs:"
docker logs witchcity-web --tail 20 | grep -i error

echo ""
echo "API container logs:"
docker logs witchcity-api --tail 20 | grep -i error

# Step 6: Verify health endpoints
echo ""
echo "6. Verifying health endpoints..."
sleep 5  # Give services time to start

curl -f http://localhost:5173/health 2>/dev/null && echo "✅ Web service healthy" || echo "❌ Web service unhealthy"
curl -f http://localhost:5653/health 2>/dev/null && echo "✅ API service healthy" || echo "❌ API service unhealthy"
curl -f http://localhost:5653/health/database 2>/dev/null && echo "✅ Database healthy" || echo "❌ Database unhealthy"

echo ""
echo "🎉 Container restart complete"
echo ""
echo "Next steps:"
echo "  - If health checks failed: Review logs above"
echo "  - If compilation errors found: Fix source code and restart again"
echo "  - If all healthy: Proceed with development/testing"
```

## Why This Matters

**Common Mistake**: Running `docker-compose up` without dev overlay
**Result**: Wrong ports (5652 instead of 5173), environment issues

**Common Mistake**: Not checking container logs for compilation errors
**Result**: E2E tests fail with "Element not found" because container built but code didn't compile

**Common Mistake**: Running tests immediately after restart
**Result**: Services not ready, tests fail intermittently

## Usage Examples

### From test-executor
```
Before running E2E tests, I'll use the container-restart skill to ensure environment is healthy.
```

### From react-developer
```
After making code changes, I'll use the container-restart skill to rebuild containers.
```

### From backend-developer
```
I'll use the container-restart skill to apply the new API changes.
```

## Integration with Lessons Learned

**Lessons learned should reference this skill:**

```markdown
## Problem: E2E Tests Fail with "Element Not Found"

**Solution**: Container has compilation errors. Use `container-restart` skill.

The skill checks for compilation errors automatically.
```

## Output Format

```json
{
  "containerRestart": {
    "status": "success|failure",
    "containersRunning": 3,
    "healthChecks": {
      "web": "healthy",
      "api": "healthy",
      "database": "healthy"
    },
    "compilationErrors": {
      "web": [],
      "api": []
    },
    "readyForTesting": true
  }
}
```
```

#### Skill 2: staging-deploy.md

```yaml
---
name: staging-deploy
description: Deploys WitchCityRope to staging environment on DigitalOcean. Handles build, deployment, database migrations, health verification, and rollback if needed. Follows documented staging deployment process with all safety checks.
---

# Staging Deployment Skill

**Purpose**: Deploy to staging environment safely.

**When to Use**:
- After Phase 5 validation passes
- When deploying new features for testing
- After hotfixes that need staging verification
- When requested by user/orchestrator

## Prerequisites Check

```bash
#!/bin/bash

echo "📋 Staging Deployment Prerequisites"
echo ""

# 1. All tests must be passing
echo "1. Checking test status..."
if ! grep -q "Status: PASS" test-results/test-execution-report.md; then
    echo "❌ FAIL: Tests not passing"
    echo "   Run tests and achieve 100% pass rate before deploying"
    exit 1
fi
echo "✅ All tests passing"

# 2. Git must be clean
echo "2. Checking git status..."
if [ -n "$(git status --short)" ]; then
    echo "❌ FAIL: Uncommitted changes"
    echo "   Commit all changes before deploying"
    exit 1
fi
echo "✅ Git clean"

# 3. Must be on correct branch
echo "3. Checking branch..."
BRANCH=$(git branch --show-current)
if [ "$BRANCH" != "main" ] && [ "$BRANCH" != "staging" ]; then
    echo "⚠️  WARNING: Not on main or staging branch (on $BRANCH)"
    read -p "Continue anyway? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi
echo "✅ On branch: $BRANCH"

# 4. Staging environment accessible
echo "4. Checking staging environment..."
if ! curl -f https://staging.witchcityrope.com/health > /dev/null 2>&1; then
    echo "⚠️  WARNING: Staging environment not responding"
fi
echo "✅ Prerequisites complete"
```

## Deployment Process

```bash
#!/bin/bash

echo "🚀 Starting Staging Deployment"
echo ""

# Step 1: Build production assets
echo "1. Building production assets..."
cd apps/web
npm run build
if [ $? -ne 0 ]; then
    echo "❌ FAIL: Web build failed"
    exit 1
fi
cd ../..
echo "✅ Web built"

cd apps/api
dotnet publish -c Release -o ./publish
if [ $? -ne 0 ]; then
    echo "❌ FAIL: API build failed"
    exit 1
fi
cd ../..
echo "✅ API built"

# Step 2: Run database migrations (dry run first)
echo ""
echo "2. Testing database migrations..."
# Connect to staging database
STAGING_CONNECTION_STRING="..." # From secrets

dotnet ef migrations script --idempotent --output migration.sql
echo "✅ Migration script generated"
echo "   Review: migration.sql"

read -p "Apply migrations to staging? (y/N) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Deployment cancelled"
    exit 1
fi

# Apply migrations
dotnet ef database update --connection "$STAGING_CONNECTION_STRING"
if [ $? -ne 0 ]; then
    echo "❌ FAIL: Migration failed"
    exit 1
fi
echo "✅ Migrations applied"

# Step 3: Deploy to DigitalOcean App Platform
echo ""
echo "3. Deploying to DigitalOcean..."

# Trigger deployment via doctl or App Platform API
doctl apps create-deployment <app-id>

echo "✅ Deployment triggered"
echo "   Monitor: https://cloud.digitalocean.com/apps/<app-id>"

# Step 4: Wait for deployment
echo ""
echo "4. Waiting for deployment to complete..."
# Poll deployment status
for i in {1..30}; do
    STATUS=$(doctl apps get <app-id> --format Status --no-header)
    if [ "$STATUS" == "ACTIVE" ]; then
        echo "✅ Deployment complete"
        break
    fi
    echo "   Status: $STATUS (attempt $i/30)"
    sleep 10
done

# Step 5: Health checks
echo ""
echo "5. Running health checks..."

sleep 30  # Give services time to stabilize

curl -f https://staging.witchcityrope.com/health || {
    echo "❌ FAIL: Web health check failed"
    echo "   Consider rollback"
    exit 1
}
echo "✅ Web healthy"

curl -f https://api.staging.witchcityrope.com/health || {
    echo "❌ FAIL: API health check failed"
    echo "   Consider rollback"
    exit 1
}
echo "✅ API healthy"

curl -f https://api.staging.witchcityrope.com/health/database || {
    echo "❌ FAIL: Database health check failed"
    echo "   Consider rollback"
    exit 1
}
echo "✅ Database healthy"

# Step 6: Smoke tests
echo ""
echo "6. Running smoke tests..."

# Test critical endpoints
curl -f https://staging.witchcityrope.com/ || echo "⚠️  Homepage failed"
curl -f https://api.staging.witchcityrope.com/api/events || echo "⚠️  Events API failed"

echo ""
echo "🎉 Staging Deployment Complete"
echo ""
echo "Next steps:"
echo "  1. Manually test critical user flows"
echo "  2. Review logs: doctl apps logs <app-id>"
echo "  3. Monitor for errors: https://staging.witchcityrope.com"
echo "  4. If issues found, run staging-rollback skill"
```

## Rollback Procedure

```bash
#!/bin/bash

echo "⏮️  Rolling Back Staging Deployment"
echo ""

# Rollback to previous deployment
doctl apps create-deployment <app-id> --previous

echo "✅ Rollback initiated"
echo "   Monitor: https://cloud.digitalocean.com/apps/<app-id>"

# Wait and verify
sleep 60
curl -f https://staging.witchcityrope.com/health && echo "✅ Previous version restored"
```

## Integration with Process Docs

**Process docs should reference this skill:**

```markdown
# Staging Deployment Process

After Phase 5 validation passes:

1. Run `staging-deploy` skill
2. The skill handles:
   - Prerequisites check
   - Build process
   - Database migrations
   - Deployment
   - Health verification
3. Manual testing of critical flows
4. If issues: Use `staging-rollback` skill

See: `.claude/skills/staging-deploy.md` for automation details
```

## Output Format

```json
{
  "stagingDeploy": {
    "status": "success|failure",
    "build": {
      "web": "success",
      "api": "success"
    },
    "migrations": {
      "applied": 3,
      "status": "success"
    },
    "deployment": {
      "triggeredAt": "2025-11-04T14:30:00Z",
      "completedAt": "2025-11-04T14:35:00Z",
      "duration": "5m"
    },
    "healthChecks": {
      "web": "healthy",
      "api": "healthy",
      "database": "healthy"
    },
    "smokeTests": {
      "passed": 8,
      "failed": 0
    },
    "url": "https://staging.witchcityrope.com",
    "rollbackAvailable": true
  }
}
```
```

---

## Part 2: Agent Skill Discovery Mechanism

### The Discovery Problem

**Current State**: Agents don't know what Skills exist

**Impact**:
- Agents reinvent the wheel
- Don't use available automation
- Follow wrong processes

### Solution: Three-Tier Discovery

#### Tier 1: Skills Registry File

**Create**: `/.claude/skills/SKILLS-REGISTRY.md`

```markdown
# Skills Registry - Quick Reference

**Purpose**: Quick lookup of all available Skills for agents.

**When an agent needs to perform a task, check this registry first.**

---

## Container & Environment Management

### container-restart
**Use when**: Containers need restarting, before E2E tests, compilation errors suspected
**Auto-invoked by**: "restart containers", "rebuild environment"
**Output**: Container health status, compilation check results

### environment-health-check
**Use when**: Before running any tests, after code changes
**Auto-invoked by**: "check environment", "verify containers"
**Output**: Health status of all services (Web, API, Database)

---

## Deployment & Operations

### staging-deploy
**Use when**: Deploying to staging after Phase 5 validation
**Auto-invoked by**: "deploy to staging", "push to staging"
**Output**: Deployment status, health checks, rollback availability

### staging-rollback
**Use when**: Deployment has issues, need to revert
**Auto-invoked by**: "rollback staging", "revert deployment"
**Output**: Rollback status, previous version health

---

## Testing & Validation

### test-catalog-updater
**Use when**: After running ANY tests (unit, integration, E2E)
**Auto-invoked by**: "update test catalog", "record test results"
**Output**: Catalog update confirmation, metrics recorded

### phase-1-validator through phase-5-validator
**Use when**: Validating phase completion before transition
**Auto-invoked by**: "validate requirements", "check if design complete"
**Output**: Quality gate score, pass/fail, missing items

---

## Documentation & Organization

### handoff-document-generator
**Use when**: Transitioning between agents or phases
**Auto-invoked by**: "create handoff", "document for next agent"
**Output**: Handoff document location, TODO sections

### master-index-updater
**Use when**: Adding new features, updating feature documentation
**Auto-invoked by**: "update master index", "add to navigation"
**Output**: Index update confirmation, new entry location

### lessons-learned-validator
**Use when**: Adding or updating lessons learned entries
**Auto-invoked by**: "validate lesson format", "check lesson quality"
**Output**: Validation score, format compliance issues

---

## Workflow Automation

### quality-gate-calculator
**Use when**: Starting workflow, need to know quality standards
**Auto-invoked by**: "calculate quality gates", "what are requirements for hotfix"
**Output**: Required percentages by phase, rationale

---

## Usage Pattern

**As an agent, when you need to perform a task:**

1. **Check this registry**: Is there a Skill for this?
2. **Mention the task naturally**: "I'll restart the containers"
3. **Claude Code invokes the Skill**: Automatically based on context
4. **You receive the output**: Use it to continue your work

**You don't need to explicitly call Skills** - just mention the task and Claude Code handles invocation.

**Example**:
```
Agent: "Before running E2E tests, I need to ensure containers are healthy. I'll restart the containers."

Claude Code: *Invokes container-restart skill*

Skill Output: All containers healthy, no compilation errors

Agent: "Great, containers are ready. Now I'll run the E2E tests."
```
```

#### Tier 2: Agent Definition References

**Update each agent definition** to include relevant Skills:

```yaml
---
name: test-executor
tools: Bash, Read, Write, Glob
---

## Available Skills for Your Role

**You have access to these Skills** (auto-invoked by Claude Code when you mention the task):

### Container & Environment
- **container-restart** - When containers need restarting (before E2E tests, after compilation errors)
- **environment-health-check** - Before running any tests

### Testing
- **test-catalog-updater** - MANDATORY after running ANY tests
- **phase-4-validator** - To validate testing phase completion

### Documentation
- **handoff-document-generator** - When handing off to next agent

**How to use**: Mention the task naturally and Claude Code will invoke the appropriate skill.

See: `/.claude/skills/SKILLS-REGISTRY.md` for complete list

---

## YOUR CORE RESPONSIBILITY
...rest of agent definition...
```

#### Tier 3: Lessons Learned Cross-References

**Update lessons learned** to reference Skills:

```markdown
## Problem: Docker Containers Restarted Incorrectly

**Problem**: Agent ran `docker-compose up` without dev overlay.

Result: Wrong ports, environment issues, E2E tests failed.

**Solution**: Use the `container-restart` skill instead of manual restart.

The skill handles:
- Correct compose file overlay (docker-compose.dev.yml)
- Health checks
- Compilation error detection
- Service readiness verification

**Example**:
```
❌ Don't: Run docker-compose commands manually
✅ Do: Mention "restart containers" and let skill handle it
```

See: `/.claude/skills/container-restart.md` for automation details
```

---

## Part 3: Single Source of Truth Architecture

### The CRITICAL Problem

**Your observation is 100% correct:**

```
Process documentation spreading across:
1. Agent definitions (how to restart containers)
2. Process docs (deployment procedures)
3. Lessons learned (mistakes to avoid)
4. Skills (automation scripts)

Result: Maintenance nightmare, inconsistencies, confusion
```

### The Solution: Clear Boundaries

#### Rule 1: Skills = Executable Automation ONLY

**Skills contain:**
- ✅ Bash scripts (executable code)
- ✅ Step-by-step automation
- ✅ Input → Process → Output
- ✅ Technical details (commands, parameters)

**Skills do NOT contain:**
- ❌ Explanations of why process exists
- ❌ History of problems that led to process
- ❌ Alternative approaches (choose one correct way)
- ❌ Context about when to use (that's elsewhere)

**Example - container-restart.md**:
```bash
# ✅ IN SKILL: The automation
#!/bin/bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
sleep 10
curl -f http://localhost:5173/health
```

```markdown
# ❌ NOT IN SKILL: The why/context
Why we use dev overlay: Because in August 2025 we switched to...
Historical context: We used to use docker-compose up but...
Alternative approaches: You could also use docker stack deploy...
```

#### Rule 2: Lessons Learned = Prevention + References

**Lessons Learned contain:**
- ✅ What went wrong (specific problem)
- ✅ Why it went wrong (root cause)
- ✅ How to avoid it (solution)
- ✅ Reference to Skill for automation
- ✅ Example of wrong vs right

**Lessons Learned do NOT contain:**
- ❌ The automation script itself (that's in Skill)
- ❌ Detailed technical steps (reference Skill)
- ❌ Multiple versions of same process

**Example - test-executor-lessons-learned.md**:
```markdown
## Problem: E2E Tests Fail with "Element Not Found"

**Problem**: Containers show "Up" but E2E tests fail mysteriously.

Root cause: Container built successfully but code has compilation errors.

**Solution**: Always restart containers with compilation check before E2E tests.

Use the `container-restart` skill - it automatically checks for compilation errors.

**Example**:
```bash
# ❌ Wrong - Manual restart, no error check
docker-compose up -d
npm test

# ✅ Right - Use skill (mention task, skill invoked automatically)
"I'll restart containers to ensure compilation succeeded"
# container-restart skill runs, checks errors
npm test
```

**See**: `/.claude/skills/container-restart.md` for automation
```

#### Rule 3: Agent Definitions = Role + Tool Access + Skill References

**Agent Definitions contain:**
- ✅ Role description (what agent does)
- ✅ Tool list (what agent can use)
- ✅ List of relevant Skills (with brief description)
- ✅ Mandatory startup procedures
- ✅ Boundaries (what agent does NOT do)

**Agent Definitions do NOT contain:**
- ❌ Detailed process steps (reference Skill)
- ❌ Automation scripts (that's in Skill)
- ❌ Problem history (that's in Lessons Learned)

**Example - test-executor.md**:
```yaml
---
name: test-executor
tools: Bash, Read, Write, Glob
---

## Available Skills
- **container-restart** - Use before E2E tests
- **test-catalog-updater** - Use after ALL test runs

## YOUR CORE RESPONSIBILITY
Run tests, manage test environment, report results.

**Before E2E tests**: Restart containers (skill handles this)
**After ANY tests**: Update TEST_CATALOG (skill handles this)

See: Lessons learned for common pitfalls
See: Skills for automation details
```

#### Rule 4: Process Docs = Strategic Workflow + References

**Process Docs contain:**
- ✅ High-level workflow (phase sequence)
- ✅ When to do what (decision points)
- ✅ Phase transitions (handoffs)
- ✅ References to Skills for automation
- ✅ References to Lessons Learned for pitfalls

**Process Docs do NOT contain:**
- ❌ Detailed technical steps (reference Skill)
- ❌ Automation scripts (that's in Skill)
- ❌ Agent-specific instructions (that's in agent definition)

**Example - workflow-orchestration-process.md**:
```markdown
# Phase 4: Testing

**Goal**: Validate implementation with 100% test pass rate.

**Process**:
1. test-executor checks environment health
   - Uses `environment-health-check` skill
2. test-executor restarts containers if needed
   - Uses `container-restart` skill
3. test-executor runs all test suites
4. test-executor updates TEST_CATALOG
   - Uses `test-catalog-updater` skill
5. Orchestrator validates phase complete
   - Uses `phase-4-validator` skill

**Quality Gate**: 100% test pass rate (all work types)

See:
- `/.claude/skills/container-restart.md` for container automation
- `/docs/lessons-learned/test-executor-lessons-learned.md` for common issues
```

### Visual Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    SINGLE SOURCE OF TRUTH                    │
└─────────────────────────────────────────────────────────────┘

┌────────────────────┐
│   SKILLS           │  ← Executable automation (HOW TO DO)
│   - Bash scripts   │
│   - Step-by-step   │
│   - Input/Output   │
└─────────┬──────────┘
          │ Referenced by
          ↓
┌────────────────────────────────────────────────┐
│   LESSONS LEARNED        │   AGENT DEFINITIONS │
│   - What went wrong      │   - Role & tools    │
│   - Why & how to avoid   │   - Skill list      │
│   → Reference Skills     │   → Reference Skills│
└──────────┬───────────────┴──────────┬──────────┘
           │ Referenced by            │
           ↓                          ↓
     ┌────────────────────────────────────┐
     │   PROCESS DOCS                     │
     │   - Strategic workflow             │
     │   - When to do what                │
     │   → Reference Skills & Lessons     │
     └────────────────────────────────────┘

RULE: Each piece of information lives in EXACTLY ONE place
```

### Migration Strategy

#### Phase 1: Identify Duplicates (Week 1)

**Audit current documentation**:

```bash
# Search for container restart instructions
grep -r "docker-compose up" \
  .claude/agents/ \
  docs/lessons-learned/ \
  docs/standards-processes/ \
  docs/guides-setup/

# Search for deployment instructions
grep -r "deploy\|deployment" \
  .claude/agents/ \
  docs/lessons-learned/ \
  docs/standards-processes/ \
  docs/functional-areas/deployment/

# Create inventory
echo "# Duplicate Process Documentation" > duplicates-inventory.md
echo "" >> duplicates-inventory.md
echo "## Container Restart Process" >> duplicates-inventory.md
echo "Found in:" >> duplicates-inventory.md
# List all locations
```

**Expected findings**:
```
Container restart process found in:
1. test-executor.md (lines 150-160)
2. test-executor-lessons-learned.md (lines 450-470)
3. docker-operations-guide.md (lines 80-120)
4. workflow-orchestration-process.md (reference)

Deployment process found in:
1. staging-deployment-guide.md (full procedure)
2. librarian-lessons-learned.md (brief mention)
3. git-manager.md (deployment steps)
```

#### Phase 2: Create Skills (Week 1-2)

**For each duplicate process**:

1. **Create Skill with automation**:
   - File: `/.claude/skills/container-restart.md`
   - Content: Executable bash script
   - No explanations, just automation

2. **Verify skill works**:
   - Test bash script manually
   - Confirm output format
   - Document input requirements

#### Phase 3: Update References (Week 2)

**For each location where process existed**:

1. **Lessons Learned**: Replace with reference
   ```markdown
   ## Problem: Container Restart Done Wrong

   **Solution**: Use `container-restart` skill.

   See: `/.claude/skills/container-restart.md`
   ```

2. **Agent Definitions**: Add to Skills list
   ```yaml
   ## Available Skills
   - **container-restart** - Before E2E tests
   ```

3. **Process Docs**: Update to reference
   ```markdown
   Before testing: Restart containers (see `container-restart` skill)
   ```

#### Phase 4: Remove Duplication (Week 2)

**Delete detailed steps from**:
- ❌ Agent definitions (keep only "use X skill")
- ❌ Lessons learned (keep only problem + reference)
- ❌ Process docs (keep only when + reference)

**Keep only in**:
- ✅ Skills (the actual automation)

#### Phase 5: Validation (Week 3)

**Create validation script**:

```bash
#!/bin/bash
# Check for process duplication

echo "Checking for duplicate processes..."

# Look for detailed container restart instructions outside Skills
if grep -r "docker-compose.*up.*docker-compose.*dev" \
   .claude/agents/ docs/lessons-learned/ docs/standards-processes/ \
   --exclude-dir=.claude/skills/ | grep -v "See:"; then
    echo "❌ Found container restart details outside Skills"
    echo "   Should only exist in /.claude/skills/container-restart.md"
    exit 1
fi

echo "✅ No duplicate processes found"
```

---

## Implementation Priority

### Immediate (This Week)

1. **Create container-restart skill** (2 hours)
   - Most frequently needed
   - Currently causing E2E test failures
   - Clear automation opportunity

2. **Create SKILLS-REGISTRY.md** (1 hour)
   - Central discovery mechanism
   - Lists all 12 Skills (10 existing + 2 new)

3. **Update test-executor definition** (30 min)
   - Add Skills list
   - Reference container-restart

### Short-term (Next 2 Weeks)

4. **Create staging-deploy skill** (4 hours)
   - Complex but high-value
   - Prevents deployment mistakes

5. **Audit and document duplicates** (3 hours)
   - Find all duplicate processes
   - Create migration plan

6. **Update all agent definitions** (2 hours)
   - Add Skills lists to all 16 agents
   - Ensure consistency

### Medium-term (This Month)

7. **Migrate duplicates to Skills** (8 hours)
   - Create Skills for duplicate processes
   - Update references
   - Remove duplication

8. **Validation and testing** (4 hours)
   - Test all Skills work
   - Verify agents can use Skills
   - Check no important info lost

---

## Success Metrics

### Quantitative

- **Duplication**: Reduce process documentation locations from 4 → 1
- **Consistency**: 100% of processes have single source of truth
- **Discovery**: All agents have Skills list in definition
- **Automation**: 2 new Skills (container-restart, staging-deploy) created

### Qualitative

- **Agent behavior**: Agents use Skills instead of reinventing
- **Maintenance**: Update process once (in Skill), not 4 times
- **Reliability**: Processes executed correctly every time
- **Discoverability**: Agents know what automation is available

---

## Questions & Answers

### Q1: Won't Skills become too numerous?

**A**: Good problem to have. Solutions:
- Group by category in SKILLS-REGISTRY.md
- Each agent definition lists only relevant Skills
- Skills are auto-invoked, agents don't need to memorize

### Q2: What if a process needs explanation AND automation?

**A**: Split it:
- **Skill**: The automation (bash script)
- **Lessons Learned**: The explanation (why this way)
- **Both reference each other**

Example:
- `container-restart.md` (Skill): Just the script
- `test-executor-lessons-learned.md`: "Problem X happened. Use container-restart skill. Here's why that works."

### Q3: How do we prevent Skills from becoming duplicative?

**A**: Strict naming convention:
- One skill per process
- Descriptive name (container-restart, not dev-environment-setup)
- Check SKILLS-REGISTRY before creating new skill

### Q4: What about process variations (dev vs staging vs prod)?

**A**: Parameters or separate skills:

**Option 1 - Parameters**:
```yaml
---
name: container-restart
description: Restart containers (dev/staging/prod)
---

# Usage: bash container-restart.md <environment>

ENVIRONMENT=$1  # dev|staging|prod
```

**Option 2 - Separate Skills**:
```
container-restart-dev.md
container-restart-staging.md
container-restart-prod.md
```

Recommend: **Option 1** for similar processes, **Option 2** for significantly different processes.

---

## Conclusion

**Your three questions all tie together:**

1. **Container restart & staging deploy are perfect Skills** ✅
   - Automation-focused
   - Frequently needed
   - Currently causing problems

2. **Agents discover Skills via three-tier system** ✅
   - SKILLS-REGISTRY.md (central reference)
   - Agent definitions (relevant Skills per role)
   - Lessons learned (references to Skills)

3. **Single source of truth prevents duplication** ✅
   - Skills = Automation (executable)
   - Lessons Learned = Prevention (problems + references)
   - Agent Definitions = Role (capabilities + references)
   - Process Docs = Strategy (workflow + references)
   - Each process exists in EXACTLY ONE place

**Implementation**: Start with container-restart skill this week, it will prove the pattern.

**Success**: When agents naturally use Skills instead of manually performing processes, and you can update a process once instead of four times.
