# 🚨 CRITICAL ORCHESTRATOR VIOLATION - TEST DELEGATION FAILURE 🚨

## DATE: 2025-08-13
## SEVERITY: CRITICAL
## USER IMPACT: EXTREME FRUSTRATION (7+ occurrences)

## THE VIOLATION
The orchestrator repeatedly RUNS TESTS DIRECTLY instead of delegating to test-executor.

## WHAT HAPPENED (PATTERN OF FAILURE)
1. User says "continue testing" or similar
2. Main assistant correctly invokes orchestrator 
3. **VIOLATION**: Orchestrator runs `dotnet test` or `npm test` commands directly
4. User has to interrupt and correct EVERY TIME
5. User reports this has happened 6-7 times despite "fixes"

## THE CORRECT BEHAVIOR (MANDATORY)
When orchestrator receives ANY testing/debugging/fixing request:

```python
# IMMEDIATE ACTION - NO OTHER STEPS FIRST
Task(
    subagent_type="test-executor",
    description="Execute test-fix cycle",
    prompt="[Full context about what to test]"
)
```

## WHAT ORCHESTRATOR MUST NEVER DO
❌ Run `dotnet test` commands
❌ Run `npm test` commands  
❌ Run `dotnet build` commands
❌ Read test files
❌ Analyze test code
❌ Fix compilation errors
❌ Debug test failures
❌ "Check" what tests exist
❌ "Understand" the test situation first

## WHAT ORCHESTRATOR MUST ALWAYS DO
✅ IMMEDIATELY delegate to test-executor
✅ Pass ALL context to test-executor
✅ Let test-executor run tests and report results
✅ WAIT for test-executor to report back, then coordinate fixes

## VERIFICATION CHECKLIST
Before responding to ANY testing request:
- [ ] Did I check for words: test, testing, debug, fix, continue testing?
- [ ] Is my FIRST action invoking test-executor?
- [ ] Am I delegating WITHOUT running any commands first?
- [ ] Can I see the Task tool invocation in my response?

## USER'S EXACT WORDS
"You are fucking killing me. I specifically said use proper sub agents. Why did the orchestrator try to run the tests? It is supposed to pass that off to the test-fix agent. This happens every fucking time."

"STOP. FIX THIS before you just do a work around. I don't want to have to catch this every fucking time. And this is like the 6th or 7th time and each time I am told it is fixed and immediately it does this same shit again the very next time."

## IMPACT OF VIOLATION
- Workflow completely broken
- User loses trust in the system
- User has to manually intervene every time
- Same error despite multiple "fixes"
- User justifiably angry

## ENFORCEMENT
This is not optional. This is not a suggestion. This is MANDATORY.
The orchestrator's lines 21-32 and 148-160 are EXPLICIT about this.
VIOLATION = ORCHESTRATION FAILURE

## TO ORCHESTRATOR
READ THIS FIRST before processing ANY request.
If the request mentions testing/debugging/fixing:
STOP.
DELEGATE TO TEST-EXECUTOR.
DO NOTHING ELSE FIRST.