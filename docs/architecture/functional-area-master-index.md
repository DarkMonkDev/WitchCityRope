# Functional Area Master Index
<!-- Last Updated: 2025-08-13 17:30 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Purpose
This master index is maintained by the librarian agent to provide quick lookups of functional areas, their working folders, and current development status. This prevents unnecessary searching and ensures agents receive accurate file paths.

## Index Structure

| Functional Area | Base Path | Current Work Path | Description | Status | Last Updated |
|-----------------|-----------|-------------------|-------------|--------|--------------|
| **AI Workflow Orchestration** | `/docs/functional-areas/ai-workflow-orchstration/` | Completed Implementation | Workflow automation and agent coordination | Completed | 2025-08-12 |
| **Authentication** | `/docs/functional-areas/authentication/` | N/A | Login, logout, user sessions, JWT/cookie auth, service-to-service auth | Stable | 2025-08-13 |
| ~~**Authentication-Identity**~~ | `/docs/_archive/authentication-identity-legacy-2025-08-12/` | **ARCHIVED** | Legacy authentication docs - ARCHIVED to prevent confusion | Archived | 2025-08-12 |
| **Events Management** | `/docs/functional-areas/events/` | N/A | Event creation, RSVP, ticketing | Active | 2025-08-12 |
| **Home Page** | `/docs/functional-areas/home-page/` | N/A | Landing page and main navigation | Stable | 2025-08-12 |
| **Payment (PayPal/Venmo)** | `/docs/functional-areas/payment-paypal-venmo/` | N/A | Payment processing integration | Planning | 2025-08-12 |
| **User Management** | `/docs/functional-areas/user-management/` | `/docs/functional-areas/user-management/new-work/2025-08-12-admin-screen/` | Admin user management, member profiles, vetting status | In Development | 2025-08-12 |

## Active Development Work

### User Management Admin Screen Redesign
- **Work Folder**: `/docs/functional-areas/user-management/new-work/2025-08-12-admin-screen/`
- **Requirements**: `/docs/functional-areas/user-management/new-work/2025-08-12-admin-screen/requirements/business-requirements.md`
- **Functional Spec**: `/docs/functional-areas/user-management/new-work/2025-08-12-admin-screen/requirements/functional-spec.md`
- **Status**: Implementation Phase (Components and API developed)
- **Session**: 2025-08-12
- **Review Document**: `/docs/functional-areas/user-management/CONSOLIDATION_REVIEW_2025-08-12.md`

## Standard Document Locations by Type

### Requirements Documents
Pattern: `{base_path}/new-work/{date}-{feature}/requirements/business-requirements.md`

### Functional Specifications
Pattern: `{base_path}/new-work/{date}-{feature}/requirements/functional-spec.md`

### Technical Designs
Pattern: `{base_path}/new-work/{date}-{feature}/design/technical-design.md`

### Test Plans
Pattern: `{base_path}/new-work/{date}-{feature}/testing/test-plan.md`

### Current State Documentation
Pattern: `{base_path}/current-state/`

### Wireframes and Mockups
Pattern: `{base_path}/wireframes/`

## Supporting Documentation Areas

| Area | Base Path | Description | Owner | Status |
|------|-----------|-------------|-------|--------|
| **Lessons Learned** | `/docs/lessons-learned/` | Role-specific lessons by UI developers, backend developers, test writers, etc. | All Teams | Active |
| **Orchestration Failures** | `/docs/lessons-learned/orchestration-failures/` | Critical lessons about AI workflow orchestration failures and solutions - UPDATED 2025-08-13 to fix test-fix-coordinator references | AI Teams | Active |
| **Standards & Processes** | `/docs/standards-processes/` | Development standards, coding patterns, testing guidelines | All Teams | Active |
| **Agent Boundaries** | `/docs/standards-processes/agent-boundaries.md` | Strict agent file access matrix and boundary enforcement | AI Teams | Active |
| **Guides & Setup** | `/docs/guides-setup/` | Installation guides, Docker setup, environment configuration | DevOps/Setup | Active |
| **Architecture** | `/docs/architecture/` | System design, ADRs, this master index, file registry | Architect/Librarian | Active |
| **Archive** | `/docs/_archive/` | Historical documents, deprecated files, old session notes | Librarian | Archived |

## Agent File Access Matrix

| Agent | Read Access | Write Access | Primary Working Areas |
|-------|------------|--------------|----------------------|
| **Orchestrator** | All | `/.claude/workflow-data/`, `/PROGRESS.md` | Workflow coordination |
| **Business Requirements** | All docs | `*/requirements/business-requirements.md` | Requirements phase |
| **Functional Spec** | All docs | `*/requirements/functional-spec.md` | Design phase |
| **Blazor Developer** | All docs | `/src/WitchCityRope.Web/` | Implementation |
| **Backend Developer** | All docs | `/src/WitchCityRope.Api/`, `/src/WitchCityRope.Core/` **FORBIDDEN**: `/tests/**/*` | API/Business logic |
| **Test Developer** | All docs | **EXCLUSIVE**: `/tests/**/*`, `**/*.test.*`, `**/*.spec.*` | Test implementation |
| **Database Designer** | All docs | `/src/WitchCityRope.Infrastructure/Data/` | Data layer |
| **UI Designer** | All docs | `*/wireframes/`, `*/design/` | UI/UX design |
| **Librarian** | All | All docs | Documentation management |
| **Git Manager** | All | Version control ops | Git operations |

## Usage Instructions for Agents

1. **Orchestrator**: When starting a workflow, query this index first to get exact paths
2. **All Agents**: Never search for functional areas - use this index
3. **Librarian**: Update this index whenever functional areas change
4. **Pass Exact Paths**: Always pass the full path from this index to other agents

## Maintenance Notes

- This file is the SOURCE OF TRUTH for functional area locations
- Update immediately when new functional areas are created
- Mark deprecated areas clearly
- Include active work folders for current development
- Archive completed work paths to history section below

## History of Completed Work

| Feature | Work Path | Completion Date | Archived To |
|---------|-----------|-----------------|-------------|
| (None yet) | | | |