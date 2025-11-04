# Standards Index

**Purpose**: Central reference for all WitchCityRope development standards and patterns.
**Quick Start**: Find the standard you need based on your current task.

## How to Use This Index

1. **Find your task category** below (Backend, Frontend, Testing, etc.)
2. **Read the "When to Read" column** to identify relevant standards
3. **Only read standards needed for your current task** (Just-in-Time Learning)

## Core Standards (All Developers)

| Document | Purpose | When to Read | Size |
|----------|---------|--------------|------|
| [CODING_STANDARDS.md](./CODING_STANDARDS.md) | SOLID principles, documentation, naming conventions | All tasks | 469 lines |
| [documentation-organization-standard.md](./documentation-organization-standard.md) | File organization and documentation structure | Creating/organizing docs | ~300 lines |

## Backend Development Standards

| Document | Purpose | When to Read | Size |
|----------|---------|--------------|------|
| [backend/service-layer-patterns.md](./backend/service-layer-patterns.md) | Service implementation patterns, API controllers | Implementing services/endpoints | 206 lines |
| [backend/api-design-patterns.md](./backend/api-design-patterns.md) | REST API design, HTTP methods, endpoint conventions | Designing/implementing API endpoints | 400 lines |
| [backend/error-handling-patterns.md](./backend/error-handling-patterns.md) | Result pattern, structured logging, exception handling | Implementing error handling | 160 lines |
| [backend/database-patterns.md](./backend/database-patterns.md) | EF Core query patterns, migrations reference | Database queries, data access | 100 lines |
| [backend/vertical-slice-architecture.md](./backend/vertical-slice-architecture.md) | Feature-based code organization patterns | Understanding project structure, new features | 150 lines |
| [backend/performance-standards.md](./backend/performance-standards.md) | Performance benchmarks, query optimization, caching | Performance work, optimization | 162 lines |
| [backend/security-patterns.md](./backend/security-patterns.md) | Input validation, sanitization, security checklist | Handling user input, auth | 175 lines |
| [backend/database-migrations-guide.md](./backend/database-migrations-guide.md) | EF Core migrations, seed data | Database changes | 627 lines |
| [backend/vertical-slice-implementation-guide.md](./backend/vertical-slice-implementation-guide.md) | Complete vertical slice implementation | Detailed feature development | 736 lines |
| [development-standards/entity-framework-patterns.md](./development-standards/entity-framework-patterns.md) | Comprehensive EF Core patterns | Advanced database queries | 409 lines |
| [development-standards/authentication-patterns.md](./development-standards/authentication-patterns.md) | Authentication service patterns | Auth implementation | 449 lines |

## Frontend Development Standards

| Document | Purpose | When to Read | Size |
|----------|---------|--------------|------|
| [frontend/react-patterns.md](./frontend/react-patterns.md) | React component patterns, hooks, best practices | React component development | 242 lines |
| [frontend/typescript-patterns.md](./frontend/typescript-patterns.md) | TypeScript type safety, DTOs, type guards | Working with TypeScript types | 350 lines |
| [frontend/mantine-ui-standards.md](./frontend/mantine-ui-standards.md) | Mantine v7 component usage, theming, UI consistency | UI implementation with Mantine | 250 lines |
| [frontend/routing-patterns.md](./frontend/routing-patterns.md) | React Router v7, navigation, protected routes | Implementing routes/navigation | 300 lines |
| [frontend/state-management-patterns.md](./frontend/state-management-patterns.md) | Zustand, React Query, Context patterns | State management implementation | 400 lines |
| [development-standards/react-patterns.md](./development-standards/react-patterns.md) | Legacy location (use frontend/ above) | Backward compatibility | 242 lines |
| [ui-implementation-standards.md](./ui-implementation-standards.md) | UI component standards | UI implementation | ~300 lines |
| [forms-standardization.md](./forms-standardization.md) | Form patterns and validation | Form implementation | ~200 lines |

## Testing Standards

| Document | Purpose | When to Read | Size |
|----------|---------|--------------|------|
| [testing/test-standards.md](./testing/test-standards.md) | Test organization, naming, data management | Writing tests | 219 lines |
| [testing/TESTING_GUIDE.md](./testing/TESTING_GUIDE.md) | Comprehensive testing guide | Test strategy, setup | ~800 lines |
| [testing/E2E_TESTING_PATTERNS.md](./testing/E2E_TESTING_PATTERNS.md) | Playwright E2E patterns | E2E testing | ~400 lines |
| [testing/integration-test-patterns.md](./testing/integration-test-patterns.md) | Integration testing patterns | Integration tests | ~300 lines |
| [testing/docker-only-testing-standard.md](./testing/docker-only-testing-standard.md) | Docker testing requirements | Test environment setup | ~200 lines |

## Architecture Standards

| Document | Purpose | When to Read | Size |
|----------|---------|--------------|------|
| [architecture/microservices-patterns.md](./architecture/microservices-patterns.md) | Web + API microservices, service communication | Understanding system architecture | 350 lines |
| [architecture/docker-patterns.md](./architecture/docker-patterns.md) | Docker development patterns, container management | Docker/container work | 250 lines |
| [architecture-discovery-process.md](./architecture-discovery-process.md) | Understanding existing architecture | Exploring codebase | ~300 lines |
| [api-contract-validation.md](./api-contract-validation.md) | API contract validation | API changes | ~200 lines |

## Workflow Standards

| Document | Purpose | When to Read | Size |
|----------|---------|--------------|------|
| [workflow-orchestration-process.md](./workflow-orchestration-process.md) | AI workflow phases, handoffs | Complex development tasks | ~400 lines |
| [agent-handoff-template.md](./agent-handoff-template.md) | Agent handoff documentation | Creating handoff docs | ~100 lines |
| [GITHUB-PUSH-INSTRUCTIONS.md](./GITHUB-PUSH-INSTRUCTIONS.md) | Git workflow, commit standards | Committing code | ~200 lines |

## Infrastructure Standards

| Document | Purpose | When to Read | Size |
|----------|---------|--------------|------|
| [development-standards/docker-development.md](./development-standards/docker-development.md) | Docker development patterns | Container work | ~400 lines |
| [development-standards/port-configuration-management.md](./development-standards/port-configuration-management.md) | Port management | Port configuration | ~100 lines |

## Documentation Standards

| Document | Purpose | When to Read | Size |
|----------|---------|--------------|------|
| [documentation-process/DOCUMENTATION_GUIDE.md](./documentation-process/DOCUMENTATION_GUIDE.md) | Documentation best practices | Writing documentation | ~600 lines |
| [documentation-process/QUICK_REFERENCE.md](./documentation-process/QUICK_REFERENCE.md) | Quick documentation reference | Quick lookup | ~100 lines |

## CI/CD Standards

| Document | Purpose | When to Read | Size |
|----------|---------|--------------|------|
| [ci-cd/CI_CD_QUICK_REFERENCE.md](./ci-cd/CI_CD_QUICK_REFERENCE.md) | CI/CD quick reference | Deployment tasks | ~200 lines |
| [ci-cd/CI_CD_COMPREHENSIVE_GUIDE.md](./ci-cd/CI_CD_COMPREHENSIVE_GUIDE.md) | Complete CI/CD guide | CI/CD setup | ~800 lines |

## Special Topics

### MCP (Model Context Protocol) Servers
Location: `./MCP/`
- Quick references for Context7, Chrome DevTools, etc.
- Setup guides for Ubuntu MCP servers

### Validation
- [forms-validation-requirements.md](./forms-validation-requirements.md) - Validation requirements

### Migration Guides
- [testing/playwright-migration/](./testing/playwright-migration/) - Playwright migration documentation
- [testing/TestProjectMigration/](./testing/TestProjectMigration/) - Test project migration

## Usage Examples

### Backend Developer Tasks

**Task**: Implement new user management endpoint
```
Read:
1. CODING_STANDARDS.md (general principles)
2. backend/service-layer-patterns.md (service implementation)
3. backend/error-handling-patterns.md (error handling)
4. development-standards/entity-framework-patterns.md (database queries)
```

**Task**: Fix database query performance
```
Read:
1. backend/performance-standards.md (optimization techniques)
2. development-standards/entity-framework-patterns.md (EF Core patterns)
```

### Frontend Developer Tasks

**Task**: Create new user profile component
```
Read:
1. CODING_STANDARDS.md (general principles)
2. frontend/react-patterns.md (React patterns)
3. frontend/mantine-ui-standards.md (UI components)
4. frontend/typescript-patterns.md (type safety)
```

**Task**: Implement login form
```
Read:
1. frontend/react-patterns.md (React patterns)
2. forms-standardization.md (form patterns)
3. frontend/state-management-patterns.md (form state)
4. development-standards/authentication-patterns.md (auth integration)
```

**Task**: Add new API endpoint
```
Read:
1. backend/api-design-patterns.md (endpoint conventions)
2. backend/service-layer-patterns.md (service implementation)
3. backend/error-handling-patterns.md (error responses)
4. frontend/typescript-patterns.md (DTO types)
```

### Test Developer Tasks

**Task**: Write unit tests for service
```
Read:
1. testing/test-standards.md (test organization)
2. testing/TESTING_GUIDE.md (testing patterns)
```

**Task**: Create E2E test for registration flow
```
Read:
1. testing/E2E_TESTING_PATTERNS.md (E2E patterns)
2. testing/docker-only-testing-standard.md (test environment)
```

## Standards Maintenance

When you discover new patterns while working:
1. Update relevant standards document
2. Document the problem solved and solution applied
3. Update this index if adding new standards
4. Notify team of significant changes

## Related Resources

- **Lessons Learned**: `/docs/lessons-learned/` - Role-specific knowledge
- **Skills Registry**: `/.claude/skills/SKILLS-REGISTRY.md` - Automation procedures
- **Architecture**: `/docs/architecture/` - System architecture decisions
- **Functional Areas**: `/docs/functional-areas/` - Feature documentation
