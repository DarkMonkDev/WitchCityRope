# ORCHESTRATOR AUTO-INVOCATION TRIGGERS

## ⚠️ CRITICAL: MAIN ASSISTANT MUST CHECK THIS FIRST ⚠️

Before ANY other action, scan user message for these triggers:

### IMMEDIATE ORCHESTRATOR TRIGGERS
Any message containing these words/phrases MUST invoke orchestrator:

**Development Actions:**
- continue (in ANY context about work/development)
- implement, implementing, implementation
- create, creating, build, building
- develop, developing, development
- add, adding (features/functionality)

**Testing/Fixing:**
- test, testing, tests, run tests
- debug, debugging, debugger
- fix, fixing, fixes, bugfix
- resolve, resolving
- troubleshoot, troubleshooting

**Completion:**
- complete, completing, completion
- finish, finishing
- finalize, finalizing
- deploy, deploying

**Multi-Step Indicators:**
- "phase", "workflow", "process"
- "and then", "after that", "next"
- Lists of tasks
- Multiple components mentioned

### BYPASS PHRASES (Don't invoke orchestrator)
- "what is", "how does", "explain" (information only)
- "show me the code" (read-only)
- "where is" (search only)
- Single file edits with explicit paths
- Git operations only

### CORRECT INVOCATION PATTERN
```
User: "continue the testing phase"
Assistant: [IMMEDIATELY, no other text or actions first]

<Task>
  description: Continue testing phase
  subagent_type: orchestrator
  prompt: [Full user request]
</Task>

Orchestrator then: [IMMEDIATELY delegates to test-fix-coordinator WITHOUT PAUSE]
```

### SPECIAL RULE: TEST-FIX-COORDINATOR HANDOFF
**When orchestrator needs to delegate testing work:**
- ✅ **NO HUMAN REVIEW REQUIRED**
- ✅ **PASS DIRECTLY TO test-fix-coordinator**
- ✅ **NO PAUSE OR APPROVAL NEEDED**
- ✅ **AUTOMATIC DELEGATION**

### WRONG PATTERN (What keeps happening)
```
User: "continue the testing phase"
Assistant: I'll continue with the testing phase. Let me first check...
[Starts doing work directly instead of invoking orchestrator]
```

## THE RULE
1. Check message for triggers FIRST
2. If trigger found → Invoke orchestrator IMMEDIATELY
3. NO other actions before orchestrator invocation
4. NO "I'll help you..." preamble - just invoke

## User Frustration Prevention
Every time the orchestrator isn't invoked when it should be:
- User gets angry (rightfully)
- Work gets done wrong
- Agents aren't coordinated
- System claims "fixed" but isn't

STOP THE CYCLE: CHECK TRIGGERS FIRST!