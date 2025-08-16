# Lessons Learned - Index
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 1.0 -->
<!-- Owner: All Teams -->
<!-- Status: Active -->

## Overview

This folder contains role-specific lessons learned from the WitchCityRope project. Each file is maintained by practitioners in that role and contains hard-won knowledge about what works and what doesn't.

## Available Guides by Role

### 🎨 [UI Developers](ui-developers.md)
Blazor components, Syncfusion, render modes, Docker hot reload issues

### 🔧 [Backend Developers](backend-lessons-learned.md)
REST API patterns, service architecture, database integration, authentication, deployment

### 🧪 [Test Writers](test-writers.md)
E2E with Playwright, integration testing, unit testing patterns

### 🎯 [Wireframe Designers](wireframe-designers.md)
Design standards, responsive patterns, handoff to developers

### 🚀 [DevOps Engineers](devops-engineers.md)
Docker, CI/CD, deployment, monitoring, troubleshooting

## How to Use These Files

1. **Starting Work**: Read your role's file before beginning any task
2. **Hit a Problem**: Check if it's already documented with a solution
3. **Learned Something**: Add it to your role's file immediately
4. **Monthly Review**: Remove outdated lessons, update existing ones

## Contributing Guidelines

### Adding New Lessons
```markdown
### Brief Problem Description
**Issue**: What went wrong or was confusing  
**Solution**: How to fix or avoid it
```example code or commands```
**Applies to**: When this lesson is relevant
```

### Format Requirements
- Keep examples concise but complete
- Include "Applies to" for context
- Add code examples where helpful
- Mark deprecated approaches clearly

### What Makes a Good Lesson
✅ **Good**: "SignInManager fails in Blazor components with 'Headers are read-only' error"
❌ **Bad**: "Authentication doesn't work"

✅ **Good**: Specific solution with code example
❌ **Bad**: "Just restart everything"

## Review Process

**Monthly Review** (First Monday of each month):
1. Each role owner reviews their file
2. Remove lessons no longer applicable
3. Update examples to current patterns
4. Add new discoveries from past month

**Criteria for Removal**:
- Technology no longer used (e.g., MudBlazor)
- Pattern replaced by better approach
- Issue fixed in framework update
- No longer relevant to project

## Cross-Role Lessons

Some lessons apply to multiple roles. These should be:
1. Added to each relevant role file
2. Kept in sync during reviews
3. Tagged with "See also: [other-role].md"

## Historical Lessons

When removing lessons, consider if they have historical value:
- Major architectural decisions
- Significant debugging victories
- Patterns that might return

These can be moved to `history/` subfolder with date prefix.

## Questions or Suggestions?

- Can't find what you need? Ask in team chat
- Think a lesson needs updating? Create a PR
- Want to add a new role file? Discuss with team lead

---

*Remember: These lessons learned are a living document. The best lesson is one that prevents someone else from hitting the same problem.*