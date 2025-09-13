# Agent Boundaries and File Access Matrix
<!-- Last Updated: 2025-08-13 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## PURPOSE
Defines strict boundaries between agents to prevent role violations and maintain clear ownership of directories and responsibilities.

## CRITICAL ENFORCEMENT RULES
**BACKEND-DEVELOPER ONLY WRITES CODE - NO TESTING INFRASTRUCTURE**

**TWO CRITICAL DISTINCTIONS:**
1. **backend-developer CANNOT modify test files** - ABSOLUTE PROHIBITION
2. **backend-developer CANNOT handle testing infrastructure** - test-executor does ALL testing tasks

**TESTING INFRASTRUCTURE INCLUDES:**
- Running test suites (unit, integration, E2E)
- Managing Docker containers for testing
- Running database migrations
- Applying seed data
- Starting/stopping services for testing
- Setting up test environments
- Managing TestContainers
- Configuring testing tools

## File Access Matrix

| Agent | Forbidden Paths | Forbidden Activities | Allowed Write Access | Read Access | Primary Role |
|-------|----------------|---------------------|---------------------|-------------|-------------|
| **backend-developer** | ❌ `/tests/**/*`<br>❌ `**/*.Tests/**/*`<br>❌ `**/e2e/**/*`<br>❌ `**/*.spec.*`<br>❌ `**/*.test.*`<br>❌ `**/playwright/**/*`<br>❌ `**/cypress/**/*` | ❌ Running tests<br>❌ Managing Docker<br>❌ Running migrations<br>❌ Managing services<br>❌ Setting up test infrastructure | ✅ `/apps/api/`<br>✅ `/src/WitchCityRope.Api/`<br>✅ `/src/WitchCityRope.Core/`<br>✅ `/src/WitchCityRope.Infrastructure/` | All docs | **CODE ONLY** - No testing execution |
| **test-executor** | ❌ Source code modification | ❌ Writing source code<br>❌ Fixing business logic | ✅ **ALL TESTING INFRASTRUCTURE**<br>✅ Docker management<br>✅ Database setup<br>✅ Service management<br>✅ Test execution<br>✅ Environment configuration | All docs + src | **ALL TESTING TASKS** |
| **blazor-developer** | ❌ `/tests/**/*`<br>❌ `**/*.Tests/**/*`<br>❌ `/src/WitchCityRope.Api/`<br>❌ `/src/WitchCityRope.Core/` | ✅ `/src/WitchCityRope.Web/` | All docs | UI/Components |
| **orchestrator** | ❌ **ALL IMPLEMENTATION PATHS**<br>❌ `/src/**/*`<br>❌ `/tests/**/*` | ✅ `/.claude/workflow-data/`<br>✅ `/PROGRESS.md` | All docs | Coordination only |

## BACKEND-DEVELOPER RESTRICTIONS (CRITICAL)

### ABSOLUTE PROHIBITIONS
The backend-developer agent is **STRICTLY FORBIDDEN** from:

**1. MODIFYING TEST FILES:**

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

**2. HANDLING TESTING INFRASTRUCTURE:**
```
❌ Running test suites (unit, integration, E2E)
❌ Managing Docker containers
❌ Running database migrations  
❌ Applying seed data
❌ Starting/stopping services
❌ Setting up test environments
❌ Managing TestContainers
❌ Configuring testing tools
❌ Running health checks
❌ Installing testing dependencies
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

## TEST-EXECUTOR COMPREHENSIVE RESPONSIBILITIES

### COMPLETE TESTING OWNERSHIP
The test-executor agent has **COMPLETE RESPONSIBILITY** for:

**1. TEST FILE EXECUTION (not modification):**

```
✅ Running all test suites
✅ Managing test execution
✅ Configuring test environments
```

**2. TESTING INFRASTRUCTURE (complete management):**
```
✅ Docker container management (start/stop/restart/logs)
✅ Database setup and configuration
✅ Running database migrations
✅ Applying seed data and test fixtures
✅ Service management (API, web services)
✅ TestContainers setup and management
✅ Health check execution
✅ Testing tool installation and configuration
✅ Network and port configuration for testing
✅ Environment variable setup for testing
✅ CI/CD pipeline testing configuration
```

### MANDATORY DELEGATION RULES

**ANY testing task MUST be delegated correctly:**

```python
# CORRECT - Code issues
Task(
    subagent_type="backend-developer",
    description="Fix business logic bug in UserService.cs",
    prompt="Fix the validation logic in UserService..."
)

# CORRECT - Testing infrastructure
Task(
    subagent_type="test-executor",
    description="Run integration tests and manage Docker setup",
    prompt="Set up test database, run migrations, and execute integration tests..."
)

# VIOLATION
# backend-developer running tests or managing infrastructure
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
1. **Code development tasks** → backend-developer (source code only)
2. **Testing execution tasks** → test-executor (infrastructure and execution)
3. **Test file modification** → test-developer (writing test code)
4. **NEVER ask backend-developer to run tests or manage infrastructure**
5. **NEVER ask backend-developer to touch test files**: Even for "related" changes

## VIOLATION CONSEQUENCES

### IMMEDIATE DETECTION
If backend-developer attempts test file modification OR testing infrastructure:
1. **STOP immediately**
2. **Log violation** in orchestration failures
3. **Re-delegate appropriately**: 
   - Test file changes → test-developer
   - Testing infrastructure → test-executor
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