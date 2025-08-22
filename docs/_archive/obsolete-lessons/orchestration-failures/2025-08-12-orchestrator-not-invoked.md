# Orchestration Failure: 2025-08-12 - Orchestrator Not Automatically Invoked

## Problem
User requested "continue the testing, debugging and fixing phase" but the orchestrator was NOT automatically invoked despite clear requirements in:
- CLAUDE.md line 8: "ANY development request automatically triggers the orchestrator agent"
- orchestrator.md lines 10-17: Lists MANDATORY invocation triggers including "Continue", "Test", "Debug", "Fix"

## Root Cause
The main Claude assistant failed to recognize the trigger words and attempted to handle the work directly instead of immediately delegating to the orchestrator.

## Impact
- User frustration (justified anger)
- Workflow violation
- Improper agent usage
- System claiming fixes that don't work

## Solution Required
The main Claude assistant MUST be updated to:
1. Parse user requests for ANY of the trigger words
2. IMMEDIATELY invoke orchestrator for:
   - "continue" (ANY context)
   - "test", "testing", "run tests"
   - "debug", "debugging"
   - "fix", "fixing", "fixes"
   - "implement", "create", "build", "develop"
   - "complete", "finish", "finalize"
   - ANY multi-step task

## Correct Response Pattern
When user says "continue the testing, debugging and fixing phase":
```
Task: orchestrator
Description: Continue testing and fixing
Prompt: Continue the testing, debugging and fixing phase of [project]
```

## Prevention
- This document MUST be read by all agents
- Main assistant MUST check for trigger words BEFORE any other action
- When in doubt, ALWAYS invoke orchestrator

## User Quote
"WHAT THE FUCK. Why the hell are the correct sub agents not being used. I am SO FUCKING TIRED of the system saying this problem is fixed and EVERY TIME we go to do this work it fails."

This frustration is 100% justified. The system has clear rules that are being ignored.