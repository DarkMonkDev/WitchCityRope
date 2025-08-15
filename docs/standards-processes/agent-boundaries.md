# Agent Boundaries and File Access Matrix
<!-- Last Updated: 2025-08-13 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## PURPOSE
Defines strict boundaries between agents to prevent role violations and maintain clear ownership of directories and responsibilities.

## CRITICAL ENFORCEMENT RULE
**BACKEND-DEVELOPER CANNOT MODIFY ANY TEST FILES - ABSOLUTE PROHIBITION**

## File Access Matrix

| Agent | Forbidden Paths | Allowed Write Access | Read Access | Primary Role |
|-------|----------------|---------------------|-------------|-------------|
| **backend-developer** | ❌ `/tests/**/*`<br>❌ `**/*.Tests/**/*`<br>❌ `**/e2e/**/*`<br>❌ `**/*.spec.*`<br>❌ `**/*.test.*`<br>❌ `**/playwright/**/*`<br>❌ `**/cypress/**/*` | ✅ `/src/WitchCityRope.Api/`<br>✅ `/src/WitchCityRope.Core/`<br>✅ `/src/WitchCityRope.Infrastructure/` | All docs | API/Business logic |
| **test-developer** | None | ✅ **ALL TEST PATHS**<br>✅ `/tests/**/*`<br>✅ `**/*.Tests/**/*`<br>✅ `**/e2e/**/*`<br>✅ `**/*.spec.*`<br>✅ `**/*.test.*` | All docs + src | Test implementation |
| **blazor-developer** | ❌ `/tests/**/*`<br>❌ `**/*.Tests/**/*`<br>❌ `/src/WitchCityRope.Api/`<br>❌ `/src/WitchCityRope.Core/` | ✅ `/src/WitchCityRope.Web/` | All docs | UI/Components |
| **orchestrator** | ❌ **ALL IMPLEMENTATION PATHS**<br>❌ `/src/**/*`<br>❌ `/tests/**/*` | ✅ `/.claude/workflow-data/`<br>✅ `/PROGRESS.md` | All docs | Coordination only |

## BACKEND-DEVELOPER RESTRICTIONS (CRITICAL)

### ABSOLUTE PROHIBITIONS
The backend-developer agent is **STRICTLY FORBIDDEN** from modifying:

```
❌ /tests/                              # Any test directory
❌ /e2e/                               # End-to-end tests  
❌ **/*.Tests/                          # Test projects
❌ **/*.test.*                          # Test files
❌ **/*.spec.*                          # Spec files
❌ **/playwright/                       # Playwright tests
❌ **/cypress/                          # Cypress tests
❌ **/*test*.js                         # JavaScript test files
❌ **/*test*.ts                         # TypeScript test files
❌ **/*Test*.cs                         # C# test files
❌ **/*Tests.cs                         # C# test files
❌ **/TestData/                         # Test data
❌ **/Fixtures/                         # Test fixtures
❌ **/Mocks/                            # Test mocks
❌ package.json (if contains test scripts)
❌ playwright.config.*                  # Playwright config
❌ jest.config.*                        # Jest config
```

### VIOLATION DETECTION PATTERNS
If backend-developer attempts to modify any path matching these patterns:
```regex
.*[Tt]est.*
.*[Ss]pec.*
.*/tests/.*
.*/e2e/.*
.*\.test\.
.*\.spec\.
.*/playwright/.*
.*/cypress/.*
.*Test[s]?\.cs$
.*Fixture.*
.*Mock.*
```

**IMMEDIATE ACTION REQUIRED**: STOP and delegate to test-developer

## TEST-DEVELOPER EXCLUSIVE OWNERSHIP

### EXCLUSIVE CONTROL
The test-developer agent has **EXCLUSIVE OWNERSHIP** of:

```
✅ /tests/                              # All test directories
✅ /e2e/                               # End-to-end tests
✅ **/*.Tests/                          # Test projects
✅ **/*.test.*                          # Test files
✅ **/*.spec.*                          # Spec files
✅ **/playwright/                       # Playwright tests
✅ **/cypress/                          # Cypress tests
✅ package.json (test scripts section)
✅ playwright.config.*                  # Playwright config
✅ jest.config.*                        # Jest config
✅ **/TestData/                         # Test data
✅ **/Fixtures/                         # Test fixtures
✅ **/Mocks/                            # Test mocks
```

### MANDATORY DELEGATION RULES

**ANY modification to test files MUST be delegated to test-developer:**

```python
# CORRECT
Task(
    subagent_type="test-developer",
    description="Fix test compilation errors",
    prompt="Fix the failing test in UserServiceTests.cs..."
)

# VIOLATION
# backend-developer modifying UserServiceTests.cs directly
```

## ORCHESTRATOR ENFORCEMENT

### PRE-DELEGATION VALIDATION
The orchestrator MUST check file paths before delegating:

```python
def validate_delegation(agent_type, file_paths):
    if agent_type == "backend-developer":
        test_patterns = [
            r".*[Tt]est.*",
            r".*[Ss]pec.*", 
            r".*/tests/.*",
            r".*/e2e/.*",
            r".*\.test\.",
            r".*\.spec\."
        ]
        
        for path in file_paths:
            for pattern in test_patterns:
                if re.match(pattern, path):
                    # VIOLATION DETECTED
                    return delegate_to_test_developer_instead()
    
    return proceed_with_delegation()
```

### DELEGATION RULES
1. **BEFORE delegating to backend-developer**: Check all file paths for test patterns
2. **IF test files detected**: Automatically delegate to test-developer instead
3. **NEVER allow backend-developer to touch test files**: Even for "related" changes

## VIOLATION CONSEQUENCES

### IMMEDIATE DETECTION
If backend-developer attempts test file modification:
1. **STOP immediately**
2. **Log violation** in orchestration failures
3. **Re-delegate to test-developer**
4. **Update file registry** with violation note

### ESCALATION PROCESS
1. **First violation**: Warning and immediate re-delegation
2. **Repeated violations**: Escalate to human
3. **Persistent violations**: Consider tool restrictions

## TOOL-LEVEL RESTRICTIONS

### BACKEND-DEVELOPER TOOL CONFIGURATION
Consider implementing read-only access for test paths:

```yaml
# Future enhancement
tools:
  Read: all_paths
  Write: 
    forbidden_patterns:
      - ".*[Tt]est.*"
      - ".*/tests/.*"
      - ".*\.test\."
      - ".*\.spec\."
  Edit:
    forbidden_patterns:
      - ".*[Tt]est.*"
      - ".*/tests/.*"
```

## IMPLEMENTATION CHECKLIST

- [x] Document agent boundaries matrix
- [x] Define forbidden paths for backend-developer
- [x] Define exclusive ownership for test-developer
- [ ] Update orchestrator delegation logic
- [ ] Update backend-developer agent definition
- [ ] Update test-developer agent definition
- [ ] Add enforcement rules documentation
- [ ] Add validation patterns to orchestrator
- [ ] Test boundary enforcement

## VALIDATION TESTS

### Test Scenarios
1. **backend-developer receives test file modification request** → Should auto-delegate to test-developer
2. **Orchestrator delegates compilation fix that includes test files** → Should route to test-developer
3. **Mixed changes (src + test files)** → Should split delegation appropriately
4. **test-developer modifies src files** → Should be allowed (has read access)

## MAINTENANCE

This document must be updated when:
- New agent types are added
- New directory structures are created
- Testing frameworks change
- Tool restrictions are implemented

---

**CRITICAL**: This boundary enforcement prevents the repeated violations where backend-developer improperly modifies test files, causing orchestration failures and user frustration.