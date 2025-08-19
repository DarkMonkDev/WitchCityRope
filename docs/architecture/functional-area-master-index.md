# Functional Area Master Index
<!-- Last Updated: 2025-08-19 - Vertical Slice Project Archived, All Value Extracted -->
<!-- Version: 1.3 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Purpose
This master index is maintained by the librarian agent to provide quick lookups of functional areas, their working folders, and current development status. This prevents unnecessary searching and ensures agents receive accurate file paths.

## Index Structure

| Functional Area | Base Path | Current Work Path | Description | Status | Last Updated |
|-----------------|-----------|-------------------|-------------|--------|--------------|
| **AI Workflow Orchestration** | `/docs/functional-areas/ai-workflow-orchstration/` | Technology Research Complete | Workflow automation and agent coordination with technology-researcher sub-agent integrated | Enhanced | 2025-08-17 |
| **API Data Alignment** | `/docs/functional-areas/api-data-alignment/` | `/docs/functional-areas/api-data-alignment/new-work/2025-08-19-dto-database-alignment-strategy/` | DTO alignment strategy for React migration - API DTOs as source of truth, TypeScript interface alignment requirements | Enhanced | 2025-08-19 |
| **Authentication** | `/docs/functional-areas/authentication/` | `/docs/functional-areas/authentication/new-work/2025-08-19-react-authentication-integration/` | React authentication integration complete - TanStack Query + Zustand + React Router v7 patterns validated, API integration working, testing complete | Enhanced | 2025-08-19 |
| ~~**Authentication-Identity**~~ | `/docs/_archive/authentication-identity-legacy-2025-08-12/` | **ARCHIVED** | Legacy authentication docs - ARCHIVED to prevent confusion | Archived | 2025-08-12 |
| **Events Management** | `/docs/functional-areas/events/` | N/A | Event creation, RSVP, ticketing | Active | 2025-08-12 |
| **Homepage** | `/docs/functional-areas/homepage/` | N/A | Landing page and main navigation entry point with complete workflow structure and design assets | Enhanced | 2025-08-19 |
| **Payment (PayPal/Venmo)** | `/docs/functional-areas/payment-paypal-venmo/` | N/A | Payment processing integration | Planning | 2025-08-12 |
| **User Management** | `/docs/functional-areas/user-management/` | `/docs/functional-areas/user-management/new-work/2025-08-12-admin-screen/` | Admin user management, member profiles, vetting status | In Development | 2025-08-12 |
| ~~**Vertical Slice Home Page**~~ | `/docs/_archive/vertical-slice-home-page-2025-08-16/` | **ARCHIVED** | Test implementation of complete workflow process - MISSION ACCOMPLISHED, all value extracted | 🗄️ ARCHIVED | 2025-08-19 |
| **Docker Authentication** | `/docs/functional-areas/docker-authentication/` | Phase 2 Complete - Pending Human Approval | Containerize existing working authentication system (React + .NET API + PostgreSQL) | Phase 2 Design Complete | 2025-08-17 |

## Active Development Work

### 🚀 CURRENT: NSwag Implementation Complete - Type Generation Operational
- **Phase**: Automated Type Generation Pipeline Implementation
- **Status**: ✅ COMPLETE - NSwag pipeline operational with 100% success rate
- **Type Generation**: @witchcityrope/shared-types package with automated OpenAPI to TypeScript
- **Quality Results**: 97 TypeScript errors → 0, 25% → 100% test pass rate
- **Manual Interface Elimination**: All DTO interfaces removed, full automation achieved
- **Cost Savings**: $6,600+ annually validated through automated solution
- **Process Improvement**: Architecture Discovery Phase 0 implemented
- **Next Action**: Team training on NSwag workflow and production deployment
- **Session**: 2025-08-19

### ✅ COMPLETED: Authentication System with NSwag Type Generation
- **Phase**: Complete React authentication integration with automated type safety
- **Status**: ✅ COMPLETE - Authentication system production-ready with generated types
- **Technology Stack**: TanStack Query v5 + Zustand + React Router v7 + Mantine v7 + Generated Types
- **API Integration**: httpOnly cookies + JWT + service-to-service auth with type safety
- **Performance**: EXCELLENT - All response times <200ms with type-safe API calls
- **Testing**: 100% SUCCESS - Contract-compliant test mocks with generated types
- **Type Safety**: Zero TypeScript errors, perfect API contract compliance
- **Documentation**: COMPREHENSIVE - Implementation patterns and NSwag integration guides
- **Session**: 2025-08-19

### 📋 COMPLETED: Infrastructure Testing Phase
- **Work Folder**: `/apps/web/src/components/forms/` + `/apps/web/src/styles/FormComponents.module.css`
- **Test Page**: `/mantine-forms` - Working demonstration of all form components
- **Components**: MantineTextInput, MantinePasswordInput, MantineTextarea, MantineSelect
- **CSS Solutions**: Placeholder visibility control, floating labels, tapered underlines
- **Form Validation**: Mantine use-form + Zod patterns PROVEN
- **Infrastructure**: ✅ COMPLETE - All components validated and working
- **Session**: 2025-08-18

### 📋 COMPLETED: Technology Research Phase
- **Work Folder**: `/docs/architecture/react-migration/adrs/`
- **ADR-004**: Mantine v7 UI framework selection (89/100 score vs Chakra UI 81/100)
- **Technology-Researcher**: New sub-agent created for architecture decisions
- **Documentation Consolidation**: Deployment, CI/CD, forms validation consolidated
- **Agent Alignment**: All development agents updated for Mantine v7
- **Status**: ✅ COMPLETE - Technology stack confirmed and documented
- **Session**: 2025-08-17

### 🗄️ ARCHIVED: Vertical Slice Home Page Implementation
- **Archive Location**: `/docs/_archive/vertical-slice-home-page-2025-08-16/`
- **Archive Date**: 2025-08-19
- **Archive Reason**: ✅ MISSION ACCOMPLISHED - All value extracted to authentication functional area
- **Value Preserved**: Authentication patterns, workflow validation, performance benchmarks all extracted
- **Status**: 🗄️ ARCHIVED - All critical information preserved in production-ready locations
- **Purpose Fulfilled**: Validated 5-phase workflow and React + .NET + PostgreSQL architecture
- **Key Extraction**: Service-to-service auth discovery, $6,600+ cost savings, 94-98% performance improvements
- **Archive Session**: 2025-08-19

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
| **Infrastructure Testing Phase** | `/apps/web/src/components/forms/` + `/apps/web/src/styles/` | 2025-08-18 | ACTIVE - Working components and CSS modules retained |
| **Technology Research Phase** | `/docs/architecture/react-migration/adrs/` | 2025-08-17 | ACTIVE - ADR-004 and consolidation documentation retained |
| **Vertical Slice Home Page** | `/docs/functional-areas/vertical-slice-home-page/` | 2025-08-16 | 🗄️ ARCHIVED to `/docs/_archive/vertical-slice-home-page-2025-08-16/` - All value extracted to authentication functional area |