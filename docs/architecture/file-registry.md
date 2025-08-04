# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
> 
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

## Active File Registry

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-01-20 | /.gitignore | MODIFIED | Added session-work directory exclusion | File lifecycle management | PERMANENT | N/A |
| 2025-01-20 | /session-work/README.md | CREATED | Explain session work directory purpose | File lifecycle management | PERMANENT | N/A |
| 2025-01-20 | /session-work/ | CREATED | Directory for temporary session files | File lifecycle management | PERMANENT | N/A |
| 2025-01-20 | /CLAUDE.md | MODIFIED | Added mandatory file tracking section | File lifecycle management | PERMANENT | N/A |
| 2025-01-20 | /docs/architecture/file-registry.md | CREATED | Central tracking of all file operations | File lifecycle management | PERMANENT | N/A |
| 2025-01-20 | /docs/architecture/decisions/adr-001-pure-blazor-server.md | CREATED | Document architecture decision | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/architecture/decisions/adr-002-authentication-api-pattern.md | CREATED | Document auth pattern decision | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/architecture/decisions/adr-003-playwright-e2e-testing.md | CREATED | Document testing framework decision | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/lessons-learned/ui-developers.md | CREATED | UI-specific lessons from project | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/standards-processes/development-standards/blazor-server-patterns.md | CREATED | Blazor Server development standards | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/standards-processes/development-standards/entity-framework-patterns.md | CREATED | EF Core patterns and practices | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/standards-processes/development-standards/docker-development.md | CREATED | Docker development standards | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/standards-processes/testing/browser-automation/playwright-guide.md | CREATED | Playwright E2E testing guide | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/standards-processes/testing/integration-test-patterns.md | CREATED | PostgreSQL integration test patterns | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/guides-setup/database-setup.md | CREATED | PostgreSQL setup guide | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/guides-setup/docker-development.md | CREATED | Docker development setup | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/guides-setup/playwright-setup.md | CREATED | Playwright setup guide | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/_archive/deprecated-testing-tools.md | CREATED | Archive of Puppeteer/Stagehand docs | CLAUDE.md reorganization | ARCHIVED | N/A |
| 2025-01-20 | /docs/_archive/session-history/2025-sessions.md | CREATED | Archive of session notes from CLAUDE.md | CLAUDE.md reorganization | ARCHIVED | N/A |
| 2025-01-20 | /docs/_archive/CLAUDE-OLD-2025-01-20.md | CREATED | Archived original CLAUDE.md | CLAUDE.md reorganization | ARCHIVED | N/A |
| 2025-01-20 | /docs/claude-md-reorganization-tracking.md | DELETED | Temporary tracking for reorg task | CLAUDE.md reorganization | DELETED | 2025-01-20 |
| 2025-01-20 | /docs/lessons-learned/backend-developers.md | MODIFIED | Added auth and EF Core lessons | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/lessons-learned/devops-engineers.md | MODIFIED | Added Docker and deployment lessons | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/lessons-learned/test-writers.md | MODIFIED | Added testing patterns and migration | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/functional-areas/authentication/current-state/functional-design.md | MODIFIED | Added API endpoint auth pattern | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/functional-areas/authentication/development-history.md | MODIFIED | Added auth migration history | CLAUDE.md reorganization | PERMANENT | N/A |
| 2025-01-20 | /docs/functional-areas/events-management/current-state/functional-design.md | MODIFIED | Added event validation system | CLAUDE.md reorganization | PERMANENT | N/A |

## File Status Definitions

- **PERMANENT**: File is part of the project documentation and should remain
- **TEMPORARY**: File is for current session only, should be deleted/archived at session end
- **ACTIVE**: File is being actively worked on
- **ARCHIVED**: File has been moved to archive, no longer active
- **DELETED**: File has been removed from the project

## Session Work Directory Structure

```
/session-work/
├── YYYY-MM-DD/
│   ├── analysis/
│   ├── diagnostics/
│   ├── temp-docs/
│   └── README.md (explains session purpose)
└── README.md (explains this directory)
```

## Rules for File Management

1. **All files MUST be logged** - No exceptions
2. **Use descriptive names** - Include purpose and date in filename
3. **Set cleanup dates** - Temporary files should have review dates
4. **Review at session end** - Update status and clean up
5. **No root directory files** - Use appropriate subdirectories

## Cleanup Schedule

Files marked as TEMPORARY should be reviewed and cleaned up according to this schedule:
- Session files: End of session
- Analysis files: 7 days
- Diagnostic files: 30 days
- Test files: After test completion

## How to Update This Registry

```markdown
| 2025-01-20 | /path/to/your/file.md | CREATED | Brief description | Your task name | ACTIVE | 2025-01-27 |
```

Always add new entries at the top of the Active File Registry table.