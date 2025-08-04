# WitchCityRope Documentation Reorganization Proposal V2
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 2.0 -->
<!-- Owner: Project Management Team -->
<!-- Status: Under Review -->

## Executive Summary

This revised proposal addresses the critical documentation challenges facing the WitchCityRope project:
- **315 scattered documentation files** with massive duplication
- **Lessons learned buried** in individual work tracks
- **No standardized naming conventions** or structure
- **Outdated documentation mixed with current**

We propose a streamlined, maintainable documentation system that:
- Reduces files by 70% while improving discoverability
- Uses **worker-role based** lessons learned organization
- Implements **git-based archiving** (no automated removal)
- Reuses existing folder structures wherever possible

## Proposed Documentation Structure

### Core Principles
1. **Single Source of Truth**: One authoritative location for each type of information
2. **Worker-Focused Organization**: Documentation organized by who uses it
3. **Git-Based Archiving**: Use git for history, remove completed work files
4. **Minimal Disruption**: Reuse existing structures where possible
5. **Clear Ownership**: Each document has an assigned maintainer

### New Folder Structure (Building on Existing)

```
WitchCityRope/
├── docs/
│   ├── 00-START-HERE.md              # Entry point with navigation
│   ├── functional-areas/             # [EXISTING] Feature-specific documentation
│   │   ├── _template/                # Template for new functional areas
│   │   ├── authentication/           # [EXISTING]
│   │   ├── events-management/        # [RENAME from events/]
│   │   ├── membership-vetting/       # [EXISTING]
│   │   ├── payments/
│   │   └── user-dashboard/           # [EXISTING]
│   ├── standards-processes/          # [EXISTING] Project-wide standards
│   │   ├── documentation-process/    # [EXISTING] This documentation system
│   │   ├── development-standards/
│   │   ├── testing-standards/
│   │   ├── ui-components/
│   │   └── deployment-process/
│   ├── architecture/                 # [EXISTING] System architecture
│   │   ├── decisions/                # ADRs
│   │   ├── diagrams/
│   │   └── current-state.md
│   ├── lessons-learned/              # [RENAME] Worker-role based learnings
│   │   ├── test-writers.md          # E2E, integration, unit test creators
│   │   ├── ui-developers.md         # Blazor component developers
│   │   ├── backend-developers.md    # C#, API, service developers
│   │   ├── wireframe-designers.md   # UI/UX designers
│   │   ├── database-developers.md   # PostgreSQL, migrations
│   │   ├── devops-engineers.md      # Docker, CI/CD, deployment
│   │   └── README.md                # Index and update instructions
│   └── completed-work-archive/       # Temporary holding before git removal
│       └── README.md                 # Instructions for archiving
├── tests/
│   └── e2e/
│       ├── screenshots/
│       │   ├── baseline/             # [EXISTING] Regression test baselines
│       │   └── current/              # Latest test runs
│       └── test-catalog.md           # Master list of all tests
└── PROGRESS.md                       # [EXISTING] Current work status only
```

## Functional Area Documentation Standard

Each functional area folder will contain these standardized files:

### Core Documentation Files
```
functional-areas/[area-name]/
├── README.md                    # Overview and navigation
├── current-state/
│   ├── business-requirements.md # What the system does (non-technical)
│   ├── functional-design.md     # How it's implemented (technical)
│   ├── user-flows.md           # User interaction patterns
│   ├── wireframes.md           # UI specifications
│   └── test-coverage.md        # All tests for this area
├── development-history.md       # Completed work log
├── new-work/                    # Active development only
│   ├── status.md               # Current session status (updated per session)
│   ├── requirements.md         # New/changed requirements
│   ├── design-changes.md       # Technical changes planned
│   ├── test-plan.md           # New tests to create
│   └── temp/                   # Temporary dev files (playwright scripts, etc.)
└── wireframes/                  # HTML wireframe files
```

### New Work Management - Per-Session Updates

When working in a functional area:

1. **Start of Session**: Update `new-work/status.md` with:
   ```markdown
   ## Session: 2025-08-04 10:30 AM
   ### Current Focus
   - Working on: [specific task]
   - Previous session ended at: [where you left off]
   
   ### Session Goals
   - [ ] Goal 1
   - [ ] Goal 2
   ```

2. **During Session**: Update status.md after each significant step:
   ```markdown
   ### Progress Update: 11:45 AM
   - Completed: [what was done]
   - Current state: [where things stand]
   - Next step: [what comes next]
   - Blockers: [any issues]
   ```

3. **End of Session**: Final update with handoff info:
   ```markdown
   ### Session End: 2:30 PM
   - Completed items: [list]
   - Files modified: [list with paths]
   - Tests status: [passing/failing]
   - **Next developer should**: [clear instructions]
   - Known issues: [any problems to be aware of]
   ```

4. **Work Completion**:
   - Merge content into `current-state/` files (replace, don't append)
   - Add summary to `development-history.md`
   - Move work files to `completed-work-archive/`
   - Update status.md: "No active development as of [date]"
   - Commit to git, then remove archived files

## Worker-Based Lessons Learned

Organized by role for easy access and updates:

### lessons-learned/test-writers.md
```markdown
# Lessons Learned - Test Writers
<!-- Last Updated: 2025-08-04 -->

## E2E Testing (Playwright)
- Always use data-testid attributes, not CSS selectors
- Create new test users for each test run
- [Specific lessons with examples]

## Integration Testing
- Use TestContainers for real PostgreSQL
- Always check DateTime is UTC
- [Specific lessons with examples]
```

### lessons-learned/ui-developers.md
```markdown
# Lessons Learned - UI Developers
<!-- Last Updated: 2025-08-04 -->

## Blazor Components
- Never use SignInManager directly in components
- Always use @rendermode="InteractiveServer" for forms
- [Specific lessons with examples]

## Syncfusion Components
- License key must be in Program.cs
- Use WCR validation components, not built-in
- [Specific lessons with examples]
```

Each role gets their own file containing only relevant lessons. Monthly review removes outdated items.

## Archive Process - Git-Based Approach

### What Gets Archived (via Git)
1. **Completed Sprint Documentation**: After merging into current-state
2. **Resolved Issue Documentation**: After fix verified in production
3. **Old Design Documents**: When features are redesigned
4. **Temporary Test Scripts**: After feature is complete

### Archive Process
1. **Complete Work**: Finish the functional area work
2. **Update Current State**: Merge all changes into current-state docs
3. **Move to Archive Folder**: `completed-work-archive/[date]-[feature]/`
4. **Commit to Git**: With clear message about what's being archived
5. **Remove Files**: Delete the archived files (git keeps history)
6. **Update Index**: Note in development-history.md what was archived

### NO Automated Archiving
- All archiving is **manual** and **intentional**
- Team lead reviews before archiving
- Git commit message explains what and why
- Can always retrieve from git history if needed

## Migration Approach - Reuse Existing Structure

### What We Keep (Already in Place)
✓ `functional-areas/` folder structure  
✓ `standards-processes/` folder  
✓ `architecture/` folder  
✓ `lessons-learned-troubleshooting/` → rename to `lessons-learned/`  
✓ Existing test structure in `tests/`  
✓ Core files: CLAUDE.md, PROGRESS.md, README.md

### What We Add (Minimal)
+ `00-START-HERE.md` navigation file  
+ `_template/` folder in functional-areas  
+ `new-work/` folders in existing functional areas  
+ `completed-work-archive/` for temporary holding  
+ Worker-based lessons learned files

### What We Consolidate (Using Expert Review)
Before consolidating, use specialized agents to:
1. **Identify current accurate information** vs outdated
2. **Merge only valid, current content**
3. **Flag conflicting information** for human review

Consolidation targets:
- 19 login documents → 1 in `functional-areas/authentication/`
- 15+ test reports → functional area `test-coverage.md` files
- 4 CLAUDE.md files → 1 in root (expert review for accuracy)
- Multiple status files → 1 PROGRESS.md

## Component Documentation System

Located in `standards-processes/ui-components/`:

```
ui-components/
├── component-catalog.md          # Master list
├── blazor-components/
│   ├── forms/
│   │   ├── WcrInputText.md     # Component documentation
│   │   ├── WcrInputNumber.md
│   │   └── examples/
│   ├── navigation/
│   └── data-display/
├── usage-guidelines.md
└── creation-process.md
```

Each component documented with:
- Location in codebase
- Purpose and use cases
- Properties/parameters
- Code examples
- Dependencies
- Common mistakes to avoid

## Implementation Safety Measures

### Before Making Changes
1. **Git Status Check**: Ensure all current work is committed
2. **Create Branch**: `git checkout -b docs-reorganization`
3. **Backup Critical Files**: Copy CLAUDE.md and other key files
4. **Expert Analysis**: Use specialized agents to verify current info

### During Migration
1. **One Functional Area at a Time**: Don't try to do everything at once
2. **Create Example First**: Show one completed area for approval
3. **Expert Review**: Have agents analyze for accuracy before consolidating
4. **Test Navigation**: Ensure all links work after moves

### Validation Steps
1. **No Information Lost**: Compare file counts and content
2. **All Links Work**: Test internal documentation links
3. **Team Review**: Have team verify their sections
4. **Git History Intact**: Verify can retrieve archived items

## Example Migration - ONE Functional Area

I will create ONE example (authentication area) showing:
1. Consolidated current-state files
2. Cleaned up new-work structure
3. Proper templates in place
4. Expert-reviewed content

This example will be provided for approval before proceeding with other areas.

## Benefits of This Approach

1. **Worker-Focused**: Each role has their own lessons learned file
2. **Git Safety**: Nothing permanently lost, just organized
3. **Minimal Disruption**: Reuses existing structure
4. **Session Handoffs**: Clear status for distributed team
5. **Expert Validation**: Ensures accuracy when consolidating
6. **Manual Control**: No automated surprises

## Questions to Resolve

1. **Site Map**: The `site-map.md` file - should we:
   - Keep it in architecture/ for reference?
   - Archive it as likely outdated?
   - Update it to current state?

2. **CLAUDE.md Consolidation**: With 4 different versions:
   - Which one is most current/accurate?
   - Should we merge all unique content?
   - Need expert agent analysis first?

3. **Test Documentation**: Currently scattered across:
   - Individual test folders
   - Docs folder
   - CLAUDE.md
   - Where should the master test list live?

## Next Steps

1. **Review and approve** this proposal
2. **Answer the open questions** above
3. **Create ONE example** functional area for approval
4. **Expert analysis** of content to consolidate
5. **Begin phased migration** after approval
6. **Update team** on new structure

---

*This revised proposal incorporates all feedback: worker-based lessons learned, git-based archiving with manual control, session-based status updates, and emphasis on expert review when consolidating.*