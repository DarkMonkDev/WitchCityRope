# Quality Gates for Vertical Slice Workflow

## MANDATORY Quality Checkpoints

### Phase 4: Testing & Validation
**MANDATORY Agent**: `lint-validator`
- **When**: After all implementation complete, before code review
- **Command**: Orchestrator MUST delegate to lint-validator
- **Pass Criteria**: Zero critical errors, all TypeScript types valid
- **Failure Action**: Delegate fixes to appropriate developer agents
- **Cannot Proceed Until**: Lint validation passes

### Phase 5: Finalization  
**MANDATORY Agent**: `prettier-formatter`
- **When**: Before final code review and documentation
- **Command**: Orchestrator MUST delegate to prettier-formatter
- **Pass Criteria**: All code formatted consistently
- **Failure Action**: Re-run formatter until consistent
- **Cannot Proceed Until**: All code properly formatted

## Workflow Enforcement

```
Implementation 
    ↓
Testing (test-executor)
    ↓
✅ QUALITY GATE: Lint Validation (lint-validator) - MANDATORY
    ↓
Code Review (code-reviewer)
    ↓
✅ QUALITY GATE: Code Formatting (prettier-formatter) - MANDATORY
    ↓
Final Documentation & Approval
```

## Delegation Examples

### Correct Workflow (Phase 4):
```
1. Orchestrator: "Delegating to test-executor for test execution"
2. Orchestrator: "Tests complete. Delegating to lint-validator for quality check"
3. lint-validator: "Found 3 ESLint errors, 2 TypeScript issues"
4. Orchestrator: "Delegating fixes to react-developer"
5. Orchestrator: "Re-running lint-validator to verify fixes"
6. lint-validator: "All checks pass"
```

### Correct Workflow (Phase 5):
```
1. Orchestrator: "Delegating to prettier-formatter for code formatting"
2. prettier-formatter: "Formatted 12 files"
3. Orchestrator: "Code formatting complete, proceeding to documentation"
```

## Common Mistakes to Avoid

❌ **WRONG**: Skipping lint validation after implementation
❌ **WRONG**: Proceeding to final review without formatting
❌ **WRONG**: Having developers format code instead of prettier-formatter
❌ **WRONG**: Running quality checks only if errors are visible

✅ **RIGHT**: Always run lint-validator in Phase 4
✅ **RIGHT**: Always run prettier-formatter in Phase 5
✅ **RIGHT**: Quality agents are MANDATORY, not optional
✅ **RIGHT**: Document all quality check results

## Success Criteria

The vertical slice is only complete when:
1. ✅ All tests pass (test-executor)
2. ✅ Lint validation passes (lint-validator) 
3. ✅ Code review complete (code-reviewer)
4. ✅ Code properly formatted (prettier-formatter)
5. ✅ Documentation updated (librarian)
6. ✅ Lessons learned captured

Quality agents are NOT optional - they are MANDATORY gates in the workflow.
