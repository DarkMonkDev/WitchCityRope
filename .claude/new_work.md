# Starting New Work - Claude Code Workflow
<!-- Last Updated: 2025-08-04 -->
<!-- Purpose: Guide for Claude Code when starting new development work -->

## Overview
This guide ensures consistent workflow when starting new development work on the WitchCityRope project, following the documentation structure established in August 2025.

## Pre-Work Checklist

### 1. Understand the Request
- [ ] Read the user's request completely
- [ ] Identify which functional area(s) are affected
- [ ] Determine if this is new feature work or maintenance
- [ ] Check if similar work has been done before

### 2. Review Current State
```bash
# Check project status
cat PROGRESS.md | grep -A 10 "Current Status"

# Review relevant functional area
ls docs/functional-areas/

# Check for existing work in the area
cat docs/functional-areas/[relevant-area]/new-work/status.md
```

### 3. Check Documentation
- [ ] Review `/docs/functional-areas/[area]/current-state/` for existing implementation
- [ ] Check `/docs/lessons-learned/` for relevant role-based guidance
- [ ] Read `/docs/standards-processes/` for applicable standards

## New Feature Workflow

### Step 1: Create or Update Feature Documentation
```bash
# If new functional area needed
cp -r docs/functional-areas/_template docs/functional-areas/new-feature-name

# Update the status file
echo "## New Work: [Feature Name]
Date Started: $(date +%Y-%m-%d)
Status: In Progress
Developer: Claude Code

### Description
[What is being implemented]

### Technical Approach
[How it will be implemented]

### Progress
- [ ] Requirements gathered
- [ ] Technical design documented
- [ ] Implementation started
- [ ] Tests written
- [ ] Documentation updated
" > docs/functional-areas/[feature]/new-work/status.md
```

### Step 2: Use TodoWrite for Task Tracking
Always break down work into trackable tasks:
```
1. Analyze requirements
2. Review existing code
3. Create/update documentation
4. Implement feature
5. Write tests
6. Update user documentation
```

### Step 3: Follow Development Standards
- [ ] Review coding standards in `/docs/standards-processes/`
- [ ] Use WCR validation components for forms
- [ ] Follow Blazor Server patterns (no Razor Pages)
- [ ] Ensure proper error handling
- [ ] Add appropriate logging

### Step 4: Update as You Work
```bash
# Update status regularly
vim docs/functional-areas/[feature]/new-work/status.md

# Document decisions
vim docs/functional-areas/[feature]/new-work/decisions.md

# Track issues encountered
vim docs/functional-areas/[feature]/new-work/issues.md
```

## Bug Fix Workflow

### Step 1: Document the Issue
```bash
# Create issue documentation
echo "## Bug: [Description]
Date: $(date +%Y-%m-%d)
Reported By: [User]
Severity: [High/Medium/Low]

### Symptoms
[What is happening]

### Expected Behavior
[What should happen]

### Investigation
[Steps taken to understand the issue]

### Root Cause
[Why it's happening]

### Fix Applied
[What was changed]
" > docs/functional-areas/[area]/new-work/bug-$(date +%Y%m%d).md
```

### Step 2: Check Related Areas
- [ ] Search for similar issues in `/docs/lessons-learned/`
- [ ] Check if bug affects other functional areas
- [ ] Review test coverage in affected area

### Step 3: Apply Fix
- [ ] Create minimal fix following existing patterns
- [ ] Add test to prevent regression
- [ ] Update relevant documentation

## Code Modification Workflow

### Before Making Changes
1. **Understand Current Implementation**
   ```bash
   # Read current implementation docs
   cat docs/functional-areas/[area]/current-state/functional-design.md
   
   # Check for known issues
   cat docs/functional-areas/[area]/current-state/known-issues.md
   ```

2. **Check Standards**
   - Architecture patterns in `ARCHITECTURE.md`
   - Coding standards in `/docs/standards-processes/`
   - UI patterns in `/docs/lessons-learned/ui-developers.md`

3. **Plan the Change**
   - Document intended changes
   - Consider impact on other areas
   - Plan test strategy

### During Implementation
1. **Follow Patterns**
   - Use existing code as reference
   - Maintain consistency with current style
   - Respect service boundaries (Web→API→DB)

2. **Test Continuously**
   ```bash
   # Run affected tests frequently
   dotnet test tests/WitchCityRope.Core.Tests/
   
   # Check for compilation issues
   dotnet build
   ```

3. **Update Documentation**
   - Update technical design if architecture changes
   - Add to lessons learned if discovering gotchas
   - Update test coverage documentation

### After Implementation
1. **Run Full Test Suite**
   ```bash
   # Health checks first
   dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"
   
   # Then full suite
   dotnet test
   ```

2. **Update Status**
   ```bash
   # Update feature status
   vim docs/functional-areas/[area]/new-work/status.md
   
   # Update PROGRESS.md if significant
   vim PROGRESS.md
   ```

3. **Document Completion**
   - Mark TodoWrite tasks as complete
   - Update any affected documentation
   - Note any follow-up work needed

## Common Patterns by Work Type

### UI Work
1. Check `/docs/lessons-learned/ui-developers.md`
2. Use WCR validation components
3. Follow Blazor Server patterns
4. Test with `./restart-web.sh` for hot reload

### API Work
1. Check `/docs/lessons-learned/api-developers.md`
2. Follow REST conventions
3. Include proper validation
4. Update API documentation

### Database Work
1. Check migration history
2. Use UTC for all DateTime
3. Update seed data if needed
4. Test with integration tests

### Testing Work
1. Check `/docs/standards-processes/testing/`
2. Follow existing test patterns
3. Ensure unique test data
4. Run health checks first

## Important Reminders

### Always Remember
- ✅ Start with documentation review
- ✅ Use TodoWrite for task tracking
- ✅ Update status as you work
- ✅ Follow established patterns
- ✅ Test continuously
- ✅ Document decisions and issues

### Never Forget
- ❌ Don't create Razor Pages
- ❌ Don't bypass API for data access
- ❌ Don't use MudBlazor (Syncfusion only)
- ❌ Don't commit without testing
- ❌ Don't leave documentation outdated

## Quick Command Reference

```bash
# Start work session
cat docs/00-START-HERE.md
cat PROGRESS.md

# Check feature area
ls docs/functional-areas/
cat docs/functional-areas/[area]/README.md

# Update status
vim docs/functional-areas/[area]/new-work/status.md

# Run tests
dotnet test --filter "Category=HealthCheck"
dotnet test

# Restart after changes
./restart-web.sh
```

---

**Remember**: Good documentation and consistent workflow make future development easier. Always leave the codebase better than you found it.