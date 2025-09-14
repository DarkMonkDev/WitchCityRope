# Agent File Reference Fixes Report
Date: 2025-09-14
Status: CRITICAL FIXES REQUIRED

## Summary
Found 25 file reference issues across agent configurations that prevent proper workflow operation.

## Issues Identified and Fixes Required

### 1. Missing Files That Don't Exist (Need Removal from Agent Configs)

These files are referenced but don't exist in the project:

#### database-designer.md
- **REMOVE**: `/docs/lessons-learned/database-developers.md`
- **REPLACE WITH**: `/docs/lessons-learned/database-designer-lessons-learned.md`

#### ui-designer.md
- **REMOVE**: `/docs/ARCHITECTURE.md` 
- **REPLACE WITH**: `/home/chad/repos/witchcityrope-react/ARCHITECTURE.md`
- **REMOVE**: `/docs/standards-processes/form-fields-and-validation-standards.md`
- **REMOVE**: `/docs/standards-processes/development-standards/react-patterns.md`

#### react-developer.md
- **REMOVE**: `/docs/ARCHITECTURE.md`
- **REPLACE WITH**: `/home/chad/repos/witchcityrope-react/ARCHITECTURE.md`
- **REMOVE**: `/docs/standards-processes/form-fields-and-validation-standards.md`
- **REMOVE**: `/docs/functional-areas/authentication/jwt-service-to-service-auth.md`
- **FIX MALFORMED**: `1. Read the Docker Operations Guide at: /docs/guides-setup/docker-operations-guide.md`
  - Should be just the path without the "1. Read..." prefix

#### backend-developer.md
- **FIX MALFORMED**: `1. Read the Docker Operations Guide at: /docs/guides-setup/docker-operations-guide.md`

#### blazor-developer.md (DEPRECATED)
- **REMOVE**: `/docs/lessons-learned/ui-developers.md`
- **REPLACE WITH**: `/docs/lessons-learned/ui-designer-lessons-learned.md`
- **REMOVE**: `/docs/standards-processes/form-fields-and-validation-standards.md`
- **REMOVE**: `/docs/functional-areas/authentication/jwt-service-to-service-auth.md`

#### code-reviewer.md
- **REMOVE**: `/docs/lessons-learned/ui-developers.md`
- **REPLACE WITH**: `/docs/lessons-learned/ui-designer-lessons-learned.md`
- **REMOVE**: `/docs/lessons-learned/database-developers.md`
- **REPLACE WITH**: `/docs/lessons-learned/database-designer-lessons-learned.md`
- **ADD**: `/docs/lessons-learned/code-reviewer-lessons-learned.md` (now created)

#### functional-spec.md
- **REMOVE**: `/docs/lessons-learned/ui-developers.md`
- **REPLACE WITH**: `/docs/lessons-learned/ui-designer-lessons-learned.md`
- **REMOVE**: `/docs/lessons-learned/database-developers.md`
- **REPLACE WITH**: `/docs/lessons-learned/database-designer-lessons-learned.md`

#### test-developer.md
- **FIX MALFORMED**: `1. Read the Docker Operations Guide at: /docs/guides-setup/docker-operations-guide.md`

#### test-executor.md
- **REMOVE**: `/docs/guides-setup/docker/docker-development.md`
- **REMOVE MALFORMED**: `4. Read '/home/chad/repos/witchcityrope-react/docs/standards-processes/development-standards/docker-development.md'`
- **REMOVE MALFORMED**: `5. Read '/home/chad/repos/witchcityrope-react/docs/standards-processes/testing/TESTING_GUIDE.md'`
- **FIX MALFORMED**: `1. Read the Docker Operations Guide at: /docs/guides-setup/docker-operations-guide.md`

### 2. Correct File Paths for All Agents

All agents should reference their lessons learned files with absolute paths:
```
/home/chad/repos/witchcityrope-react/docs/lessons-learned/[agent-name]-lessons-learned.md
```

### 3. Files That Should Exist But Don't

These files are referenced by multiple agents but don't exist:
- `/docs/standards-processes/form-fields-and-validation-standards.md` (4 references)
- `/docs/standards-processes/development-standards/react-patterns.md` (1 reference)
- `/docs/functional-areas/authentication/jwt-service-to-service-auth.md` (2 references)

## Action Items

1. **Fix all agent configuration files** to use correct absolute paths
2. **Remove references to non-existent files**
3. **Consider creating the missing standards files** if they're actually needed
4. **Standardize all lessons learned references** to use full absolute paths

## Validation Script

Run the validation script again after fixes:
```bash
/home/chad/repos/witchcityrope-react/session-work/2025-09-14/agent-file-validation.sh
```

## Critical for Workflow

These fixes are ESSENTIAL because:
- Agents fail on startup when they can't read required files
- Incorrect paths cause delegation failures
- Missing lessons learned files prevent agents from learning from past mistakes
- The workflow orchestration system depends on agents reading correct files