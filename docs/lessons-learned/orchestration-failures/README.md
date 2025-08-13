# Orchestration Failures - Critical Lessons Learned
<!-- Last Updated: 2025-08-12 -->
<!-- Version: 1.0 -->
<!-- Owner: AI Team/Librarian -->
<!-- Status: Active -->

## Purpose
This directory contains CRITICAL lessons learned from AI workflow orchestration failures. These are high-impact failures that have caused user frustration, workflow violations, and system reliability issues.

**ALL AI AGENTS MUST READ THESE BEFORE STARTING WORK**

## Current Failure Patterns

### 1. Orchestrator Auto-Invocation Failures
- **File**: `2025-08-12-orchestrator-not-invoked.md`
- **Issue**: Main Claude assistant fails to recognize trigger words and attempts direct work
- **Impact**: Workflow violations, user frustration
- **Status**: UNRESOLVED - requires main assistant training

### 2. Test Delegation Failures  
- **File**: `2025-08-12-test-coordinator-delegation-failure.md`
- **Issue**: Orchestrator bypasses test-fix-coordinator and attempts direct testing
- **Impact**: Improper agent usage, repeated failures
- **Status**: RESOLVED - tools removed, delegation rules strengthened

## Critical Action Items

### For Main Claude Assistant
1. **MANDATORY**: Check for trigger words BEFORE any other action
2. **TRIGGER WORDS**: continue, test, debug, fix, implement, create, build, develop, complete, finish, finalize
3. **WHEN IN DOUBT**: ALWAYS invoke orchestrator
4. **PATTERN**: Use `Task: orchestrator` for ALL development requests

### For Orchestrator Agent
1. **NEVER** run tests directly (`dotnet test`, `npm test`)
2. **NEVER** fix code directly
3. **ALWAYS** delegate to test-fix-coordinator for testing work
4. **READ** these lessons learned on startup

### For All Agents
1. Read orchestration failure lessons on startup
2. Follow delegation patterns strictly  
3. Report new failure patterns immediately
4. Add new failures to this directory

## Escalation Process

If you encounter orchestration failures:
1. **STOP** the current approach immediately
2. **DOCUMENT** the failure pattern in this directory
3. **UPDATE** file registry with new lesson learned
4. **NOTIFY** user of the issue and solution
5. **PREVENT** future occurrences through agent updates

## Directory Contents

| File | Date | Issue Type | Status | Impact |
|------|------|------------|--------|---------|
| `2025-08-12-orchestrator-not-invoked.md` | 2025-08-12 | Auto-invocation failure | Unresolved | High |
| `2025-08-12-test-coordinator-delegation-failure.md` | 2025-08-12 | Delegation bypass | Resolved | Medium |

## Success Metrics

- Zero orchestrator auto-invocation failures
- Zero test delegation bypasses  
- User satisfaction with AI workflow
- Proper agent usage patterns
- Reduced workflow violations

## Related Documentation

- **AI Workflow Orchestration**: `/docs/functional-areas/ai-workflow-orchstration/`
- **Agent Definitions**: `/.claude/agents/`
- **Project Instructions**: `/CLAUDE.md`
- **File Registry**: `/docs/architecture/file-registry.md`

---

**WARNING**: These failures have caused significant user frustration. The patterns documented here MUST be prevented through proper agent behavior and system adherence to established workflows.