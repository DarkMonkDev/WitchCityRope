# Librarian Lessons Learned

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## CRITICAL: Document Structure Prevention

**Never allow `/docs/docs/` folders** - This catastrophic pattern happened multiple times and breaks the entire documentation system.

**Always check canonical locations first** - Use `/docs/standards-processes/CANONICAL-DOCUMENT-LOCATIONS.md` before creating ANY files.

**Update file registry for EVERY operation** - No exceptions. Every file created/modified/deleted must be logged in `/docs/architecture/file-registry.md`.

## File Placement Rules

**Never create files in project root** - Only README, PROGRESS, ARCHITECTURE, CLAUDE belong there.

**Use functional areas structure** - All feature documentation goes in `/docs/functional-areas/[feature]/`.

**Check master index before searching** - Always consult `/docs/architecture/functional-area-master-index.md` first when agents ask for files.

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