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

## Documentation Update Patterns

**Always update documentation in coordinated sets** - STATUS.md, README.md, PROGRESS.md, and master index together to maintain consistency.

**Phase completion documentation pattern** - When phases complete, update PROGRESS.md (status and focus), master index (current work status), file registry (track changes), ensuring migration progress is clearly communicated.

**Use comprehensive achievement summaries** - When documenting completion, include technical metrics, business value, and next steps for complete handoff.

**Maintain file registry discipline** - Every documentation update must be logged in file registry with clear purpose and ownership.

## Discovery and Inventory Patterns

**Always inventory existing work before creating new documentation** - Check functional areas, wireframes, and specifications to prevent duplicate work.

**Use systematic discovery approach** - Start with master index, then explore functional areas, check wireframes, and review requirements before concluding what exists.

**Document existing asset inventory comprehensively** - When cataloging existing work, provide complete paths and describe what can be reused vs what needs creation.

**Phase completion requires migration plan alignment** - Always reference original migration plan to identify correct next phase, update percentage complete accurately, and maintain phase sequence integrity.

## Cross-Cutting Feature Organization

**Organize by primary business domain, not UI context** - Events features belong in `/docs/functional-areas/events/[context]/` not `/docs/functional-areas/user-dashboard/events/` because Events is the business domain, dashboard is just UI context.

**Use context subfolders for related functionality** - When features span multiple interfaces (public, admin, user), create subfolders under the primary domain (e.g., `/events/public-events/`, `/events/admin-events-management/`, `/events/user-dashboard/`).

**Create documentation organization standards proactively** - Cross-cutting features create confusion about file placement, establish clear standards early to prevent scattered documentation across multiple functional areas.

## Critical Business Requirements Discovery

**NEVER trust initial assumption about business rules** - Always check ALL requirements documents before implementation to prevent major errors.

**Check both main repository AND worktree versions** - Requirements may have evolved between versions, worktree documents are typically more current.

**Verify event type differences in Events Management** - Classes require ticket purchases (paid), Social Events use RSVP (free) with optional tickets, Social Events show BOTH RSVP table AND tickets table.

**Business Requirements Discovery Pattern** - Always check multiple versions (main repo vs worktree, original vs current), comprehensive search reveals scattered but consistent business logic across multiple documents.

## Agent Handoff Documentation System Implementation

**Problem**: Agent workflows failing because critical findings and decisions get lost between phases, causing duplicate work and implementation failures.
**Solution**: Mandatory handoff documentation using standardized template at `/docs/standards-processes/agent-handoff-template.md`.

**Pattern**: Every agent MUST create handoff document when ending their phase, saved to `/docs/functional-areas/[feature]/handoffs/[agent-name]-YYYY-MM-DD-handoff.md`.

**Critical Content**: Top 5 discoveries, pitfalls to avoid, validation checklist, files created, and next agent action items.

**Enforcement**: Added mandatory handoff section to ALL agent lessons-learned files with WARNING language to ensure compliance.