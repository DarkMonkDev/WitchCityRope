# Automatic Orchestration Trigger
<!-- Last Updated: 2025-08-12 -->
<!-- Status: Active -->
<!-- Purpose: Ensure orchestrator is automatically invoked for development tasks -->

## Recognition Patterns

When ANY of these patterns are detected, IMMEDIATELY invoke the orchestrator:

### Development Keywords
- implement, create, build, develop, add, modify, refactor
- feature, component, screen, page, system, module
- fix, bug, issue, problem, error
- update, enhance, improve, optimize

### Multi-Step Indicators
- "that allows", "that can", "with ability to", "that supports"
- "admin screen", "user interface", "management system"
- Any request with multiple verbs or requirements
- Any request mentioning multiple user actions

## Automatic Invocation Rule

```
IF request contains (development_keyword AND multi_step_indicator)
OR request_complexity > single_action
THEN invoke_orchestrator()
```

## Examples That MUST Trigger Orchestrator

✅ "Implement a user management admin screen"
✅ "Fix the login bug where users can't reset passwords"
✅ "Add a new field to the user profile"
✅ "Create a reporting dashboard"
✅ "Refactor the event management system"
✅ "Build a feature for..."
✅ "Develop a component that..."

## Examples That DON'T Need Orchestrator

❌ "What is the current git branch?"
❌ "Show me the user service code"
❌ "Explain how authentication works"
❌ "Run the tests"
❌ "Check the logs"

## Override Commands

- "Don't use orchestrator" - Bypass orchestration
- "Direct implementation" - Skip workflow
- "Quick fix" - Minimal process

## Integration Instructions

When a multi-step development task is detected:
1. Immediately delegate to orchestrator agent
2. Pass the full request as the scope
3. Let orchestrator manage entire workflow
4. Do NOT proceed with direct implementation

## Verification

The orchestrator MUST:
- Take control immediately
- Determine work type
- Create branch and folders
- Start Phase 1
- PAUSE for human reviews at designated points

---

This configuration ensures the orchestrator is ALWAYS used for development work without requiring explicit invocation.