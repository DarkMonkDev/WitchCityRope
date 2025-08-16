# Agent Lessons Learned Structure

## Overview
This document defines the standard structure for organizing lessons learned by agent role. The goal is **one file per agent type** to eliminate duplication and ensure each agent has focused, relevant guidance.

## Standard Structure

### Current Agent-Based Files

#### Development Agents
- **`backend-lessons-learned.md`** - API development, database patterns, Entity Framework, authentication
  - **Agent Role**: Backend Developer
  - **Scope**: Service architecture, database management, API design, authentication patterns
  - **Combined**: Includes database-specific lessons (PostgreSQL, migrations, performance)

- **`frontend-lessons-learned.md`** - React component development, UI patterns, state management  
  - **Agent Role**: Frontend Developer (React Developer)
  - **Scope**: React patterns, component architecture, state management, forms, authentication UI
  - **Replaces**: Blazor-specific patterns with React equivalents

- **`ui-designer-lessons-learned.md`** - Design patterns, wireframes, component design
  - **Agent Role**: UI Designer  
  - **Scope**: Wireframe standards, design patterns, accessibility, component consistency
  - **Focus**: Visual design, user experience, design system implementation

#### Testing Agents
- **`test-developer-lessons-learned.md`** - Writing test code, testing patterns
  - **Agent Role**: Test Developer (Test Writer)
  - **Scope**: Unit tests, integration tests, test patterns, mocking, validation testing
  - **Focus**: Creating reliable, maintainable test code

- **`test-executor-lessons-learned.md`** - Running tests, interpreting results, environment management
  - **Agent Role**: Test Executor
  - **Scope**: E2E testing execution, environment health checks, Docker container management
  - **Focus**: Test execution reliability, environment prerequisites

#### Infrastructure Agents  
- **`devops-lessons-learned.md`** - Docker, deployment, infrastructure
  - **Agent Role**: DevOps Engineer
  - **Scope**: Container management, deployment patterns, CI/CD, monitoring
  - **Focus**: Infrastructure reliability, development environment setup

## Agent Role Definitions

### Backend Developer
**Responsibilities**: 
- API endpoint development
- Database schema design and migrations
- Authentication and authorization implementation
- Service layer architecture
- Performance optimization

**Lessons Focus**: 
- Entity Framework patterns
- PostgreSQL optimization
- Authentication patterns
- Service architecture
- API design best practices

### Frontend Developer  
**Responsibilities**:
- React component development
- State management implementation
- UI/UX implementation
- Form handling and validation
- Client-side authentication

**Lessons Focus**:
- React patterns and hooks
- Component architecture
- State management (Zustand, Context)
- Form validation (React Hook Form + Zod)
- Modern CSS (Tailwind, responsive design)

### UI Designer
**Responsibilities**:
- Wireframe creation
- Design system definition
- Component specification  
- Accessibility standards
- Visual consistency

**Lessons Focus**:
- Wireframe standards
- Design patterns
- Component library usage
- Accessibility considerations
- Design system implementation

### Test Developer
**Responsibilities**:
- Writing unit tests
- Creating integration tests
- Developing test patterns
- Test data management
- Validation testing

**Lessons Focus**:
- Testing frameworks (Vitest, React Testing Library)
- Test patterns and best practices
- Mocking strategies
- Test isolation patterns
- Validation testing approaches

### Test Executor
**Responsibilities**:
- Running E2E test suites
- Environment health verification
- Test result interpretation
- Infrastructure troubleshooting
- Test execution reliability

**Lessons Focus**:
- E2E testing prerequisites
- Docker container health checks
- Environment troubleshooting
- Test execution patterns
- Failure diagnosis

### DevOps Engineer
**Responsibilities**:
- Container orchestration
- Deployment automation
- Infrastructure monitoring
- Environment configuration
- Performance optimization

**Lessons Focus**:
- Docker development patterns
- Container networking
- Database management
- CI/CD pipeline configuration
- Infrastructure troubleshooting

## Migration Notes

### Files Consolidated
- **`database-developers.md`** → Merged into `backend-lessons-learned.md`
  - **Reason**: Same agent (Backend Developer) handles both API and database concerns
  - **Content**: PostgreSQL patterns, migration strategies, performance optimization

### Files Renamed for Consistency
- **`ui-developers.md`** → `frontend-lessons-learned.md`
  - **Reason**: Reflects React migration, broader frontend scope
  - **Content**: Transformed Blazor patterns to React equivalents

- **`wireframe-designers.md`** → `ui-designer-lessons-learned.md`  
  - **Reason**: Clearer agent role definition
  - **Content**: Design patterns, wireframe standards, accessibility

- **`test-writers.md`** → `test-developer-lessons-learned.md`
  - **Reason**: Consistent naming pattern with other developer roles
  - **Content**: Test development patterns, frameworks, best practices

- **`devops-engineers.md`** → `devops-lessons-learned.md`
  - **Reason**: Consistent naming pattern (role-lessons-learned.md)
  - **Content**: Docker, deployment, infrastructure patterns

### Archived Content
- **`lessons-learned-troubleshooting/`** → Moved to `/docs/archive/`
  - **Reason**: Content duplicated in main lessons files, outdated patterns
  - **Contents**: EF Core errors, PostgreSQL lessons, test summaries

- **`orchestration-failures/`** → Moved to `/docs/archive/`
  - **Reason**: Specific to deprecated agent orchestration system
  - **Contents**: Agent delegation failures, tool restriction enforcement

## Usage Guidelines

### For Agents
1. **Backend Developer** → Read `backend-lessons-learned.md`
2. **Frontend Developer** → Read `frontend-lessons-learned.md`  
3. **UI Designer** → Read `ui-designer-lessons-learned.md`
4. **Test Developer** → Read `test-developer-lessons-learned.md`
5. **Test Executor** → Read `test-executor-lessons-learned.md`
6. **DevOps Engineer** → Read `devops-lessons-learned.md`

### For Documentation Updates
- **Add lessons to the appropriate agent file** based on the agent responsible for that work
- **Avoid duplication** across multiple files
- **Reference related files** using cross-links when appropriate
- **Update this structure guide** when adding new agent types

### Cross-References
- **Authentication**: Backend + Frontend coordination required
- **Testing**: Test Developer creates tests, Test Executor runs them
- **Deployment**: DevOps + Backend for API deployment, DevOps + Frontend for web deployment
- **UI Components**: Frontend Developer implements, UI Designer specifies

## Benefits of This Structure

1. **Focused Learning**: Each agent gets relevant lessons without noise
2. **Eliminates Duplication**: Single source of truth per domain
3. **Clear Ownership**: Obvious which agent owns which lessons
4. **Easier Maintenance**: Updates go to one specific file
5. **Better Searchability**: Agents know exactly where to look
6. **Scalable**: Easy to add new agent types as needed

## Maintenance Schedule

- **Monthly Review**: Check for outdated lessons, cross-reference accuracy
- **Quarterly Archive**: Move obsolete content to archive folders  
- **Annual Structure Review**: Evaluate if new agent types are needed
- **After Major Migrations**: Update all relevant agent files (e.g., Blazor → React)

---

*This structure was established during the React migration in August 2025 to consolidate scattered documentation and eliminate duplication across 15+ lessons learned files.*