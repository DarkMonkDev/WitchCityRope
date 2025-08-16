# Lessons Learned System Documentation

## Overview

This document explains the WitchCityRope lessons learned system, how it's organized, and how to contribute to it. The system ensures knowledge is captured, organized by agent role, and actionable for future development.

## Purpose

The lessons learned system serves to:
- **Capture Knowledge**: Document insights from development, testing, and operations
- **Prevent Repeated Mistakes**: Learn from past issues and solutions
- **Accelerate Development**: Provide quick access to proven patterns and solutions
- **Maintain Quality**: Ensure consistent approaches across the team
- **Support Agents**: Each AI agent has specific lessons relevant to their role

## System Structure

### One File Per Agent Type

Each specialized agent has their own lessons learned file containing domain-specific knowledge:

| Agent Type | Lessons File | Purpose | Key Topics |
|------------|--------------|---------|------------|
| **backend-developer** | `backend-lessons-learned.md` | API and database development | REST APIs, PostgreSQL, authentication, services |
| **react-developer** | `frontend-lessons-learned.md` | React component development | Components, hooks, state management, routing |
| **ui-designer** | `ui-designer-lessons-learned.md` | UI/UX design and mockups | Wireframes, Chakra UI patterns, responsive design |
| **test-developer** | `test-developer-lessons-learned.md` | Writing test code | Test patterns, fixtures, mocking, coverage |
| **test-executor** | `test-executor-lessons-learned.md` | Running and validating tests | Test environments, CI/CD, health checks |
| **devops** | `devops-lessons-learned.md` | Infrastructure and deployment | Docker, PostgreSQL, monitoring, deployment |

### Why Separate Files?

- **Focused Knowledge**: Each agent only needs information relevant to their role
- **Reduced Cognitive Load**: Developers don't need to filter through unrelated lessons
- **Better Maintenance**: Updates to one domain don't affect others
- **Clear Ownership**: Each file has a clear purpose and audience

## Critical Cross-Cutting Lessons

While most lessons are domain-specific, these critical insights apply across all development:

### 1. Always Use Proper Delegation
**Context**: Agents should delegate to appropriate sub-agents rather than trying to do everything
**Learning**: Main agents coordinate, specialized agents implement
**Action**: Use Task tool to delegate to domain-specific agents

### 2. File Registry is Mandatory
**Context**: Files were being created without tracking, leading to orphaned files
**Learning**: Every file operation must be logged
**Action**: Update `/docs/architecture/file-registry.md` for EVERY file created/modified/deleted

### 3. Check Existing Documentation First
**Context**: Duplicate solutions were being created for solved problems
**Learning**: Existing patterns and solutions often already exist
**Action**: Search lessons learned and documentation before implementing new solutions

### 4. PostgreSQL Only
**Context**: Project migrated from SQLite to PostgreSQL
**Learning**: All database operations must use PostgreSQL patterns
**Action**: No SQLite references or patterns in new code

### 5. React Patterns, Not Blazor
**Context**: Project migrated from Blazor to React
**Learning**: All UI patterns must follow React best practices
**Action**: Use functional components, hooks, and React ecosystem tools

## How to Use This System

### For Agents

1. **At Startup**: Read your specific lessons learned file
2. **During Work**: Reference lessons when making decisions
3. **After Work**: Add new lessons learned to your file

### For Developers

1. **Before Starting**: Review relevant lessons for your task
2. **When Stuck**: Check if the problem has been solved before
3. **After Solving**: Document new lessons learned

## Contributing New Lessons

### When to Add a Lesson

Add a new lesson when you:
- Solve a difficult problem
- Discover a better approach
- Identify a pattern (good or bad)
- Learn something that would help others
- Find a critical issue or fix

### How to Format Lessons

Use this template for consistency:

```markdown
## [Brief Title of Lesson]

**Date**: YYYY-MM-DD
**Category**: [Architecture|Performance|Security|Testing|etc.]
**Severity**: [Critical|High|Medium|Low]

### Context
[Describe the situation that led to this lesson. What were you trying to accomplish? What was the environment?]

### What We Learned
[Clearly state the key insight or discovery. What worked? What didn't work? Why?]

### Action Items
- [ ] [Specific action to take based on this lesson]
- [ ] [Another action item]
- [ ] [Update documentation/processes if needed]

### Impact
[How does this affect future development? What should change?]

### References
- [Link to related issue/PR/documentation]
- [Link to external resource if applicable]

### Tags
#category #severity #component #pattern
```

## Template Fields Explained

### Required Fields

- **Date**: When the lesson was learned (YYYY-MM-DD format)
- **Context**: The situation that led to the lesson
- **What We Learned**: The actual insight or knowledge gained
- **Action Items**: Specific, actionable steps based on the lesson

### Optional Fields

- **Category**: Type of lesson (Architecture, Performance, Security, etc.)
- **Severity**: How critical this lesson is (Critical, High, Medium, Low)
- **Impact**: Broader implications for the project
- **References**: Links to related resources
- **Tags**: For searchability and categorization

## Standard Tags

Use these standardized tags for consistency:

- `#critical` - Must be addressed immediately
- `#security` - Security-related concerns
- `#performance` - Performance optimizations
- `#architecture` - System design decisions
- `#process` - Development workflow improvements
- `#tooling` - Tool-specific knowledge
- `#debugging` - Problem-solving techniques
- `#testing` - Testing strategy and implementation
- `#deployment` - Deployment and infrastructure
- `#communication` - Team coordination

## Maintenance

### Review Schedule

- **Weekly**: Add new lessons from the week's work
- **Monthly**: Review and consolidate similar lessons
- **Quarterly**: Archive obsolete lessons
- **Yearly**: Major review and reorganization

### Archiving

Move obsolete lessons to `/docs/archive/obsolete-lessons/` when they:
- No longer apply (e.g., Blazor lessons after React migration)
- Have been superseded by better approaches
- Refer to removed features or components

### Quality Standards

Good lessons are:
- **Specific**: Concrete details, not vague statements
- **Actionable**: Include clear action items
- **Contextual**: Provide enough background for understanding
- **Dated**: Include when the lesson was learned
- **Tagged**: Use standard tags for discoverability

## File Locations

```
/docs/lessons-learned/
├── LESSONS_LEARNED_SYSTEM.md         (this file - system documentation)
├── backend-lessons-learned.md        (backend-developer agent)
├── frontend-lessons-learned.md       (react-developer agent)
├── ui-designer-lessons-learned.md    (ui-designer agent)
├── test-developer-lessons-learned.md (test-developer agent)
├── test-executor-lessons-learned.md  (test-executor agent)
└── devops-lessons-learned.md         (devops agent)

/docs/archive/obsolete-lessons/       (archived/obsolete content)
```

## Getting Started

1. **Identify your role/agent type**
2. **Read the corresponding lessons file**
3. **Apply relevant lessons to your work**
4. **Document new lessons as you learn them**
5. **Share critical lessons with the team**

## Remember

- Each agent has ONE lessons file
- Every lesson should be actionable
- Check existing lessons before solving problems
- Document new insights promptly
- Keep lessons focused and relevant to the agent's role