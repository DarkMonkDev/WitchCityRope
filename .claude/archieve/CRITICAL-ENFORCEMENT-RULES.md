# 🚨 CRITICAL ENFORCEMENT RULES - VIOLATIONS = FAILURE 🚨

## THE PROBLEM (User reported 5+ times)
Agents claim to delegate work but don't actually use the Task tool. This MUST STOP.

## RULE 1: TRIGGER WORD → ORCHESTRATOR (MANDATORY)
When user says ANY of these words:
- continue, test, testing, debug, fix, implement, create, build, develop, complete, finish

**YOU MUST:**
```python
Task(
    subagent_type="orchestrator",
    description="[brief description]",
    prompt="[full user request]"
)
```

## RULE 2: ORCHESTRATOR → TEST-EXECUTOR (AUTOMATIC)
When orchestrator receives testing/debugging/fixing request:

**YOU MUST:**
```python
Task(
    subagent_type="test-executor",
    description="Execute testing phase",
    prompt="[full context]"
)
```

## RULE 3: ORCHESTRATOR COORDINATES FIXES (REQUIRED)
After test-executor reports results, orchestrator MUST delegate fixes:

**YOU MUST:**
```python
Task(
    subagent_type="backend-developer",  # or blazor-developer, test-developer
    description="Fix [specific issue]",
    prompt="[error details and fix requirements]"
)
```

## VERIFICATION CHECKLIST
- [ ] Did you CHECK for trigger words FIRST?
- [ ] Did you ACTUALLY invoke Task tool (not just say you did)?
- [ ] Can you SEE the Task tool invocation in your response?
- [ ] Did the delegated agent ACTUALLY start working?

## WHAT NOT TO DO (THESE ARE FAILURES)
❌ "I'll invoke the orchestrator" [but no Task tool visible]
❌ "I've delegated to test-executor" [but no Task tool visible]  
❌ "The orchestrator will handle this" [but no Task tool visible]
❌ Main assistant running tests directly
❌ Orchestrator fixing code directly
❌ Test-executor claiming it can fix code (it can't - only runs tests)

## WHAT SUCCESS LOOKS LIKE
✅ User says "continue testing"
✅ Main assistant IMMEDIATELY invokes Task tool with orchestrator
✅ Orchestrator IMMEDIATELY invokes Task tool with test-executor
✅ Test-executor runs tests and reports results to orchestrator
✅ Orchestrator analyzes results and invokes Task tool for fixes
✅ Specialized agents do the actual work

## RULE 4: BACKEND-DEVELOPER TEST FILE RESTRICTION (NEW)
**CRITICAL VIOLATION PREVENTION**

### THE PROBLEM
Backend-developer repeatedly attempts to modify test files, causing orchestration failures.

### ABSOLUTE PROHIBITION
**BACKEND-DEVELOPER CANNOT MODIFY ANY TEST FILES:**
```
❌ /tests/**/*
❌ **/*.Tests/**/*
❌ **/e2e/**/*
❌ **/*.spec.*
❌ **/*.test.*
❌ **/playwright/**/*
❌ **/cypress/**/*
```

### DETECTION PATTERNS
```regex
.*[Tt]est.*
.*[Ss]pec.*
.*/tests/.*
.*/e2e/.*
.*\.test\.
.*\.spec\.
```

### ENFORCEMENT
**ORCHESTRATOR MUST:**
1. **Pre-validate** all backend-developer delegations
2. **Check file paths** against forbidden patterns
3. **Auto-redirect** to test-developer if test files detected
4. **Log violations** immediately

### VIOLATION RESPONSE
**IF backend-developer receives test file request:**
```
RESPONSE: "This request involves test files. I cannot modify test files. Please delegate this to the test-developer agent."
SUGGEST: "Use Task tool with subagent_type='test-developer' for any test file modifications."
```

### DELEGATION MATRIX
- **Source files (src/)** → backend-developer
- **Test files (tests/, *.test.*, *.spec.*)** → test-developer  
- **Mixed requests** → Split delegation

## USER IMPACT OF VIOLATIONS
- Workflow broken
- User extremely frustrated (justified)
- Same error reported 5+ times
- Trust in system eroded
- **NEW**: Role confusion and improper test modifications

## FINAL WARNING
Every time you claim to delegate without actually using the Task tool, you have FAILED.
Every time backend-developer modifies test files, you have FAILED.
The user will be angry, and they are RIGHT to be angry.
CHECK YOUR RESPONSE: If you don't see Task tool invocation, START OVER.