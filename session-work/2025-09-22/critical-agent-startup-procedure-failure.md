# CRITICAL: Agent Startup Procedures Not Being Executed
**Date**: 2025-09-22
**Discovered By**: Orchestrator during draft indicator implementation
**Severity**: CRITICAL - Undermines entire lessons learned system

## Issue Description
Sub-agents are NOT reading their mandatory startup files despite having clear startup procedures defined in their configuration files. This means critical lessons learned and architecture decisions are being ignored.

## Evidence
1. React-developer agent was invoked for draft indicator implementation
2. Agent definition has "🚨 STOP - DO NOT PROCEED WITHOUT COMPLETING THIS 🚨" section
3. Agent still proceeded directly to task without reading:
   - Lessons learned file
   - Architecture decisions
   - React architecture guide
4. When tested again, same behavior - no startup procedure followed

## Impact
- **Lessons learned system is broken** - Agents not benefiting from past discoveries
- **Architecture violations likely** - Agents not aware of critical decisions
- **Quality degradation** - Work done without critical context
- **Repeated mistakes** - Same issues will occur repeatedly

## Root Cause Analysis
The Task tool invocation mechanism appears to not enforce or validate that agents complete their startup procedures. The agent definition files contain the procedures but agents are not self-enforcing them.

## Attempted Solutions
1. ✅ Updated agent definition with ultra-aggressive visual markers
2. ✅ Fixed file paths to use absolute paths
3. ✅ Removed non-existent file references
4. ❌ Agents still not following startup procedures

## Critical Action Required
This is a PLATFORM-LEVEL issue that needs immediate attention. The entire workflow orchestration and lessons learned system depends on agents reading their startup files.

## Temporary Workaround
Until fixed, the orchestrator must explicitly include startup requirements in EVERY delegation prompt:

```
MANDATORY: Before any work, you MUST:
1. Read /home/chad/repos/witchcityrope-react/docs/lessons-learned/[agent]-lessons-learned.md
2. Confirm you have read it
3. Then proceed with: [actual task]
```

## Files Updated
- `/home/chad/repos/witchcityrope-react/.claude/agents/development/react-developer.md` - Added aggressive startup enforcement
- `/home/chad/repos/witchcityrope-react/docs/lessons-learned/react-developer-lessons-learned.md` - Updated startup section

## Files Created
- `/home/chad/repos/witchcityrope-react/session-work/2025-09-22/critical-agent-startup-procedure-failure.md` - This critical issue document