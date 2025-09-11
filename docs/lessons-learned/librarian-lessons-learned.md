# Librarian Lessons Learned

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 CRITICAL: WORKTREE COMPLIANCE - MANDATORY 🚨

### ALL WORK MUST BE IN THE SPECIFIED WORKTREE DIRECTORY

**VIOLATION = CATASTROPHIC FAILURE**

When given a Working Directory like:
`/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management`

**YOU MUST:**
- Write ALL files to paths within the worktree directory
- NEVER write to `/home/chad/repos/witchcityrope-react/` main repository
- ALWAYS use the full worktree path in file operations
- VERIFY you're in the correct directory before ANY file operation

**Example:**
- ✅ CORRECT: `/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/docs/...`
- ❌ WRONG: `/home/chad/repos/witchcityrope-react/docs/...`

**Why This Matters:**
- Worktrees isolate feature branches
- Writing to main repo pollutes other branches
- Can cause merge conflicts and lost work
- BREAKS the entire development workflow

## CRITICAL: Document Structure Prevention

**Never allow `/docs/docs/` folders** - This catastrophic pattern happened multiple times and breaks the entire documentation system.

**Always check canonical locations first** - Use `/docs/standards-processes/CANONICAL-DOCUMENT-LOCATIONS.md` before creating ANY files.

**Update file registry for EVERY operation** - No exceptions. Every file created/modified/deleted must be logged in `/docs/architecture/file-registry.md`.

## File Placement Rules

**Never create files in project root** - Only README, PROGRESS, ARCHITECTURE, CLAUDE belong there.

**Never create files in worktree root** - ALL files must go in proper subdirectories under docs/.

**Use functional areas structure** - All feature documentation goes in `/docs/functional-areas/[feature]/`.

**Check master index before searching** - Always consult `/docs/architecture/functional-area-master-index.md` first when agents ask for files.

## 🚨 CRITICAL: File Extraction and Analysis Placement 🚨

**When extracting versions or creating analysis files:**
- **Wireframe versions** → `/docs/functional-areas/[feature]/new-work/[date-feature]/design/wireframes/`
- **Analysis documents** → `/docs/functional-areas/[feature]/new-work/[date-feature]/requirements/`
- **NEVER in root** → Not in `/` or `/home/chad/repos/witchcityrope-react/.worktrees/[branch]/`
- **ALWAYS in proper subfolder** → Follow the established structure without exception

**Example for Events Management:**
- ❌ WRONG: `/volunteers-tab-version-1.html`
- ❌ WRONG: `/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/volunteers-tab-version-1.html`
- ✅ CORRECT: `/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/docs/functional-areas/events/new-work/2025-08-24-events-management/design/wireframes/volunteers-tab-version-1.html`

## Content Quality Standards

**Keep lessons learned concise** - Each lesson should be 1-3 sentences maximum focusing on actionable "what" and "why".

**Avoid project-specific implementation details** - Focus on patterns, not specific project history or extensive explanations.

**Archive outdated content immediately** - Don't let obsolete documentation accumulate in active directories.

## Emergency Prevention

**Treat duplicate key files as emergencies** - CLAUDE.md, PROGRESS.md, ARCHITECTURE.md duplicates require immediate resolution.

**Monitor for root directory pollution** - Any non-standard files in root = immediate cleanup required.

**Validate structure weekly** - Run structure validation checks to prevent accumulation of violations.

## Agent Coordination

**Provide exact file paths** - When agents request documents, always return absolute paths from master index.

**Update master index with changes** - Any new functional areas or structure changes must update the index immediately.

**Use proper archival process** - Move outdated content to `/_archive/` with explanatory README files.

## Quality Control Patterns

**Enforce naming conventions** - Use lowercase-with-hyphens for folders, descriptive names for files.

**Require metadata headers** - Every document needs date, version, owner, and status metadata.

**Consolidate duplicate content** - When finding similar documents, merge and archive duplicates immediately.

## 🚨 MASSIVE DUPLICATE IMPLEMENTATION DETECTION 🚨

**Events System Duplicate Crisis**: During investigation on 2025-09-07, discovered **MASSIVE duplicate implementations** of Events Management system:
- **34+ React components** already built (RSVP modals, ticket purchase, admin forms, etc.)
- **Multiple API service layers** (eventsManagement.service.ts, legacyEventsApi.service.ts)
- **Complete backend API** with GET endpoints operational
- **Extensive testing infrastructure** with 18 E2E tests
- **Full TDD implementation plan** with 50 pre-written tests
- **Working demo pages** at localhost already functional

**ROOT CAUSE**: Agents didn't check existing implementations before creating new ones. The system is in Phase 5 (Implementation) with substantial existing work available.

**PREVENTION**: ALWAYS check functional area master index and existing codebase before any development work. Use search tools to find existing implementations.

## Critical Pattern Recognition

**Lessons learned files getting bloated** - Keep them concise and actionable, avoid turning into project documentation or implementation guides. Target 50-75 lines maximum per file.

**Root directory accumulating files** - Any new files in root (except approved ones) require immediate investigation and relocation.

**Multiple archive folders** - Only one `_archive/` per directory level is allowed, consolidate immediately if multiples found.

## Meta-Learning: Lessons Learned File Maintenance

**Resist the temptation to document everything** - Lessons learned files should contain only essential patterns, not project history or implementation guides.

**Focus on prevention, not celebration** - Document what to avoid and how to avoid it, not detailed success stories or project achievements.

**One lesson per concept** - Don't create multiple sections for the same underlying issue with different project contexts.

**Ruthlessly edit for conciseness** - If a lesson takes more than 3 sentences to explain, it probably belongs in a different type of document.

## Lessons Learned File Management

**Problem**: Files becoming project documentation instead of prevention patterns.
**Solution**: Keep to prevention patterns only - "what went wrong and how to avoid it".

**Problem**: Status reports and celebrations added to lessons learned files.
**Solution**: Use /session-work/ for progress reports, lessons learned for mistakes only.

**Problem**: Agents don't understand difference between lessons learned and progress reports.
**Solution**: Enforce strict template with validation checklist for orchestrator reviews.

## Progress Update Patterns

**Update both main and functional area progress files** - When major milestones complete, update both /PROGRESS.md and the specific functional area progress file to maintain consistency.

**Use specific commit references** - Include exact commit hashes when documenting implementation completions for traceability.

**Document next steps clearly** - Always specify what the next phase requires and which team/agent should handle it.

**Mark completed phases with checkmarks and progress percentages** - Use visual indicators (✅ COMPLETE, 100%) to show clear completion status.

**Create implementation plans as deliverables** - When testing reveals gaps, document comprehensive implementation strategies with time estimates and pre-written tests.