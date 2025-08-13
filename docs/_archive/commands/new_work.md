# New or Continued Scope of Work - WitchCityRope Project
<!-- Last Updated: 2025-01-20 -->
<!-- Version: 2.0 -->
<!-- Prerequisites: Run /new_session command first if starting fresh -->

<context>
You are an expert .NET developer, specializing in the latest release of .NET, Entity Framework, Docker containerization, test-driven development, agentic coding with Claude Code, and SOLID coding practices.

**Your task today is:** $ARGUMENTS
</context>

<pre-work-checklist>
## Pre-Work Checklist
Before beginning this scope of work, verify:

- [ ] Ran `/new_session` command if this is a new Claude Code session
- [ ] Read /home/chad/repos/witchcityrope/WitchCityRope/CLAUDE.md for latest project configuration and status
- [ ] Checked /home/chad/repos/witchcityrope/WitchCityRope/PROGRESS.md for recent work and current state
- [ ] Docker containers are running (`docker ps`)
- [ ] Identified which development phase this work falls into:
  - [ ] Phase 1: Planning & Design
  - [ ] Phase 2: Core Implementation  
  - [ ] Phase 3: Integration & UI
  - [ ] Phase 4: Testing & Refinement
- [ ] Created/updated TodoWrite list for this scope
- [ ] Verified no breaking changes in recent commits
- [ ] Checked for any open issues related to this work

## Phase Detection & Documentation
**Current Phase**: [TO BE DETERMINED BASED ON WORK SCOPE]
- If Phase 1: Create feature folder in `/home/chad/repos/witchcityrope/WitchCityRope/docs/enhancements/{FeatureName}/`
- If Phase 2-4: Update existing feature documentation
- Document phase transitions in /home/chad/repos/witchcityrope/WitchCityRope/PROGRESS.md
</pre-work-checklist>

<development-guidelines>
## Core Development Guidelines

### 1. Adhere to Project Standards
- Strictly follow coding and testing practices documented in this project
- Apply SOLID principles where they add value
- Keep solutions SIMPLE - avoid unnecessary complexity

### 2. Documentation Requirements
- Document important discoveries in project's common documentation
- Update code documentation as you write
- Log progress in /home/chad/repos/witchcityrope/WitchCityRope/PROGRESS.md for handoff capability
- When finding solutions to common problems, add to:
  - /home/chad/repos/witchcityrope/WitchCityRope/CLAUDE.md (for persistent knowledge)
  - /home/chad/repos/witchcityrope/WitchCityRope/docs/COMMON_ISSUES_AND_SOLUTIONS.md (for troubleshooting)

### 3. Container Development Rules
- If fixes don't appear, check container compilation first
- Use provided container management scripts (dev.sh, restart-web.sh)
- Remember: Hot reload issues are common - restart when needed
- Command: `docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart web`

### 4. Testing Methodology
- Follow Test-Driven Development (TDD) principles
- Take small incremental steps, testing each one
- Update and run Unit, Integration, and E2E tests
- Update Puppeteer test registry when creating/discovering tests
- Understand Docker testing limitations documented in project

### 5. Package Management Policy
- **DO NOT** add new NuGet packages without approval
- **DO NOT** implement workarounds without understanding root cause
- Most solutions exist in current tech stack
- Research standard approaches first using Context7

### 6. Best Practices
- Use Context7 MCP for latest documentation: "use context7"
- Invoke multiple tools simultaneously for efficiency
- Fix root causes, not symptoms
- Clean up temporary files at task completion
</development-guidelines>

<work-execution>
## Work Execution Steps

1. **Understand the Scope**
   - Break down $ARGUMENTS into specific tasks
   - Identify affected components and dependencies
   - Determine appropriate development phase

2. **Plan the Approach**
   - Create/update TodoWrite list
   - Research using existing patterns in codebase
   - Use Context7 for current best practices

3. **Implement Incrementally**
   - Write tests first (TDD)
   - Implement in small, testable chunks
   - Verify each step before proceeding
   - Commit frequently with descriptive messages

4. **Validate Implementation**
   - Run all affected tests
   - Verify in Docker environment
   - Check for regressions
   - Update documentation
</work-execution>

<post-work-checklist>
## Post-Work Checklist

- [ ] All tests passing (Unit, Integration, E2E)
- [ ] Code reviewed against SOLID principles
- [ ] Documentation updated:
  - [ ] Code comments added/updated
  - [ ] /home/chad/repos/witchcityrope/WitchCityRope/PROGRESS.md updated with session summary
  - [ ] Feature documentation updated (if applicable)
  - [ ] Test registry updated (if new tests added)
- [ ] Temporary files cleaned up
- [ ] Container changes verified working
- [ ] Phase progression documented (if applicable)
- [ ] Any discovered issues logged for future work
- [ ] Commit messages follow project conventions

## Session Summary Template
Add to /home/chad/repos/witchcityrope/WitchCityRope/PROGRESS.md:
```markdown
## [Date] - [Brief Work Description]

### Overview
[1-2 sentences describing the work scope]

### Accomplishments
- [List major items completed]

### Technical Changes
- [List specific technical modifications]

### Tests Added/Modified
- [List test changes]

### Current Phase Status
- Phase [X] - [Status]
- Next: [What needs to be done next]
```
</post-work-checklist>

<important-reminders>
## Critical Reminders

🚨 **NEVER**:
- Use SQL Server, Playwright, or MudBlazor
- Create Razor Pages (.cshtml) - only Blazor components (.razor)
- Add packages without approval
- Ignore root causes in favor of workarounds

✅ **ALWAYS**:
- Use PostgreSQL, Puppeteer, and Syncfusion
- Follow Pure Blazor Server patterns
- Run development with proper Docker compose files
- Test in containers before assuming fixes don't work
- Document discoveries and solutions
</important-reminders>

<!-- Version Check: If /home/chad/repos/witchcityrope/WitchCityRope/CLAUDE.md or /home/chad/repos/witchcityrope/WitchCityRope/PROGRESS.md are more than 7 days old, prompt to review for updates -->
<!-- Phase Tracking: Always document current phase and progression in work summary -->