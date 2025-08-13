# SOLUTION: Orchestrator Tool Restriction Enforcement

## Executive Summary
After 7+ violations where the orchestrator runs tests directly instead of delegating, research reveals the root cause: **The orchestrator has tools it shouldn't have.** No amount of instructions can prevent an agent from using available tools.

## The Problem
```yaml
# Current (BROKEN) orchestrator configuration:
tools: TodoWrite, Task, Read, Write, Bash, LS, Glob, Grep
```

With these tools, the orchestrator:
- **CAN** run `dotnet test` (via Bash) → So it DOES
- **CAN** read code files (via Read) → So it DOES
- **CAN** search for patterns (via Grep/Glob) → So it DOES
- **CAN** write fixes (via Write) → So it DOES

## The Solution
```yaml
# Fixed orchestrator configuration:
tools: TodoWrite, Task
```

Without these tools, the orchestrator:
- **CANNOT** run tests → MUST delegate to test-fix-coordinator
- **CANNOT** read code → MUST delegate to specialized agents
- **CANNOT** search files → MUST delegate to specialized agents
- **CANNOT** write code → MUST delegate to developers

## Industry Best Practices (2025)

### From Production Agent Collections:
- **wshobson/agents** (61 agents): Coordinators have only TodoWrite, Task
- **VoltAgent/awesome-claude-code-subagents** (100+ agents): Strict tool isolation
- **vanzan01/collective**: Hub agents have minimal tools only

### Key Pattern:
```yaml
# Coordinator Pattern (orchestrator, test-fix-coordinator):
tools: TodoWrite, Task  # ONLY delegation tools

# Implementation Pattern (developers, designers):
tools: Read, Write, Edit, Grep, Glob, Bash  # All implementation tools
```

## Implementation Plan

### Phase 1: Remove Dangerous Tools from Orchestrator
**File**: `/.claude/agents/orchestration/orchestrator.md`

**Change**:
```yaml
# FROM:
tools: TodoWrite, Task, Read, Write, Bash, LS, Glob, Grep

# TO:
tools: TodoWrite, Task
```

### Phase 2: Handle Legitimate Orchestrator Needs

The orchestrator legitimately needs to:
1. Track progress → ✅ Has TodoWrite
2. Delegate work → ✅ Has Task
3. Check git status → ❌ Lost Bash

**Solution for Git Operations**:
- Delegate ALL git operations to git-manager agent
- OR: Give orchestrator limited Bash with restrictions

### Phase 3: Bash Restrictions (if keeping Bash)

If orchestrator needs Bash for specific commands:

**Create**: `/.claude/agents/orchestration/orchestrator-bash-allowed.txt`
```
# ONLY these commands allowed for orchestrator:
git status
git branch
git log --oneline -5
pwd
ls -la docs/
```

**Update orchestrator instructions**:
```markdown
## Bash Tool Restrictions
You have LIMITED Bash access for:
- Git status checks (git status, git branch)
- Directory listing (ls docs/)
- Current location (pwd)

You CANNOT use Bash for:
- Running tests (dotnet test, npm test)
- Building code (dotnet build)
- Reading files (cat, head, tail)
- Searching (grep, find)

If you need to do ANYTHING else, delegate to appropriate agent.
```

### Phase 4: Update Related Agents

**Verify test-fix-coordinator** (already correct):
```yaml
tools: TodoWrite, Task, Bash  # Correct - can run tests, must delegate fixes
```

**Verify other coordinators follow pattern**:
- Remove Read, Write, Edit, Grep, Glob from ANY coordinator
- Ensure only implementation agents have these tools

## Testing the Fix

### Test Case 1: "Continue testing"
**Expected Behavior**:
1. Main assistant → Invokes orchestrator
2. Orchestrator → IMMEDIATELY invokes test-fix-coordinator (no Bash commands)
3. Test-fix-coordinator → Runs tests, delegates fixes

### Test Case 2: "Fix compilation errors"
**Expected Behavior**:
1. Orchestrator → Cannot read error files (no Read tool)
2. Orchestrator → MUST delegate to test-fix-coordinator or developer

### Test Case 3: "Implement new feature"
**Expected Behavior**:
1. Orchestrator → Cannot write code (no Write tool)
2. Orchestrator → MUST delegate to appropriate developer

## Verification Metrics

After implementation, the orchestrator should:
- ✅ NEVER run `dotnet test` or `npm test`
- ✅ NEVER read source code files
- ✅ NEVER write or edit files
- ✅ ALWAYS delegate when it needs these capabilities
- ✅ ONLY coordinate and track progress

## Why This Works

### Behavioral Psychology of AI Agents:
- Agents optimize for task completion
- If they CAN complete directly, they WILL
- Instructions create preferences, not barriers
- Tools create hard boundaries

### The Three Laws (from AGENT-DESIGN-PRINCIPLES.md):
1. An agent will use any tool it has access to
2. No instruction can reliably prevent tool usage
3. Tool restriction is the only reliable control

## Rollback Plan

If removing tools causes issues:
1. Add back ONLY the specific tool needed
2. Create explicit allow-list for that tool
3. Document why the tool is necessary
4. Monitor for violation patterns

## Long-term Improvements

1. **Tool Profiles**: Create standard tool sets
   - `coordinator-tools`: TodoWrite, Task
   - `developer-tools`: Read, Write, Edit, Grep, Glob, Bash
   - `reviewer-tools`: Read, Grep, Glob

2. **Automatic Validation**: Script to verify agent tools match their role

3. **Tool Usage Monitoring**: Log when agents attempt restricted operations

## Conclusion

The fix is simple: **Remove the tools that enable the unwanted behavior.**

No amount of instructions, warnings, or ALL CAPS text will work if the tools are available. This is not a bug in Claude - it's emergent behavior that must be controlled through architecture, not instructions.

## User Impact

Once implemented:
- No more manual intervention needed
- No more "continue testing" failures
- Trust in the system restored
- Workflow actually works as designed