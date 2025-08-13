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

## RULE 2: ORCHESTRATOR → TEST-FIX-COORDINATOR (AUTOMATIC)
When orchestrator receives testing/debugging/fixing request:

**YOU MUST:**
```python
Task(
    subagent_type="test-fix-coordinator",
    description="Execute test-fix cycle",
    prompt="[full context]"
)
```

## RULE 3: TEST-FIX-COORDINATOR → OTHER AGENTS (REQUIRED)
Test-fix-coordinator MUST delegate fixes:

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
❌ "I've delegated to test-fix-coordinator" [but no Task tool visible]  
❌ "The orchestrator will handle this" [but no Task tool visible]
❌ Main assistant running tests directly
❌ Orchestrator fixing code directly
❌ Test-fix-coordinator claiming it doesn't have Task tool

## WHAT SUCCESS LOOKS LIKE
✅ User says "continue testing"
✅ Main assistant IMMEDIATELY invokes Task tool with orchestrator
✅ Orchestrator IMMEDIATELY invokes Task tool with test-fix-coordinator
✅ Test-fix-coordinator runs tests and invokes Task tool for fixes
✅ Specialized agents do the actual work

## USER IMPACT OF VIOLATIONS
- Workflow broken
- User extremely frustrated (justified)
- Same error reported 5+ times
- Trust in system eroded

## FINAL WARNING
Every time you claim to delegate without actually using the Task tool, you have FAILED.
The user will be angry, and they are RIGHT to be angry.
CHECK YOUR RESPONSE: If you don't see Task tool invocation, START OVER.