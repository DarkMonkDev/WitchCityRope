# Functional Area Master Index
<!-- Last Updated: 2025-10-08 - E2E TEST STABILIZATION COMPLETE: 100% Pass Rate on Launch-Critical Tests -->
<!-- Version: 1.8 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Purpose
This master index is maintained by the librarian agent to provide quick lookups of functional areas, their working folders, and current development status. This prevents unnecessary searching and ensures agents receive accurate file paths.

## Index Structure

| Functional Area | Base Path | Current Work Path | Description | Status | Last Updated |
|-----------------|-----------|-------------------|-------------|--------|--------------|
| **AI Workflow Orchestration** | `/docs/functional-areas/ai-workflow-orchstration/` | Technology Research Complete | Workflow automation and agent coordination with technology-researcher sub-agent integrated | Enhanced | 2025-08-17 |
| **API Architecture Modernization** | `/docs/functional-areas/api-architecture-modernization/` | `/docs/functional-areas/api-architecture-modernization/new-work/2025-08-22-minimal-api-research/` | ✅ COMPLETE - Simplified vertical slice architecture with 49ms response times, $28K+ annual savings, zero breaking changes | IMPLEMENTATION COMPLETE | 2025-08-22 |
| **API Data Alignment** | `/docs/functional-areas/api-data-alignment/` | `/docs/functional-areas/api-data-alignment/new-work/2025-08-19-dto-database-alignment-strategy/` | DTO alignment strategy for React migration - API DTOs as source of truth, TypeScript interface alignment requirements | Enhanced | 2025-08-19 |
| **Authentication** | `/docs/functional-areas/authentication/` | **BFF PATTERN COMPLETE** ✅ | **Secure BFF authentication with httpOnly cookies** - Production-ready implementation with silent token refresh, XSS protection, zero authentication timeouts. Migration from localStorage JWT complete. | **COMPLETE** | 2025-09-12 |
| **Database Initialization** | `/docs/functional-areas/database-initialization/` | **IMPLEMENTATION COMPLETE** ✅ | **Complete database auto-initialization system** - Reduces setup time from 2-4 hours to under 5 minutes with automated migrations, comprehensive seed data, and real PostgreSQL testing via TestContainers | **COMPLETE** | 2025-08-22 |
| ~~**Authentication-Identity**~~ | `/docs/_archive/authentication-identity-legacy-2025-08-12/` | **ARCHIVED** | Legacy authentication docs - ARCHIVED to prevent confusion | Archived | 2025-08-12 |
| **Design Refresh** | `/docs/functional-areas/design-refresh/` | `/docs/functional-areas/design-refresh/new-work/2025-08-20-modernization/` | Design system modernization with edgy/modern aesthetic, homepage navigation refresh, 5 design iterations, documentation reorganization | Phase 1 - Requirements | 2025-08-20 |
| **Navigation** | `/docs/functional-areas/navigation/` | **IMPLEMENTATION COMPLETE** ✅ | **Complete logged-in user navigation updates** - Dashboard button, Admin link, User greeting, Logout accessibility | **COMPLETE** | 2025-09-11 |
| **Events Management** | `/docs/functional-areas/events/` | `/docs/functional-areas/events/new-work/2025-08-24-events-management/` | **ACTIVE DEVELOPMENT** - React migration from Blazor implementation - Event creation, RSVP, ticketing, admin management | **Phase 1 - Requirements** | 2025-08-24 |
| **Homepage** | `/docs/functional-areas/homepage/` | N/A | Landing page and main navigation entry point with complete workflow structure and design assets | Enhanced | 2025-08-19 |
| **Payment (PayPal/Venmo)** | `/docs/functional-areas/payment-paypal-venmo/` | **INTEGRATION COMPLETE** ✅ | **PayPal webhook integration with Cloudflare tunnel** - Real sandbox webhooks working, strongly-typed event processing, mock services for CI/CD | **COMPLETE** | 2025-09-14 |
| **Payments (RSVP/Ticketing)** | `/docs/functional-areas/payments/` | `/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/` | **IMPLEMENTATION COMPLETE** ✅ | **Complete RSVP and ticketing system** - Production-ready RSVP for social events, PayPal ticket purchases for classes, comprehensive participation management, 90% functional with minor API issues | **IMPLEMENTATION COMPLETE** | 2025-09-20 |
| **User Management** | `/docs/functional-areas/user-management/` | `/docs/functional-areas/user-management/new-work/2025-08-12-admin-screen/` | Admin user management, member profiles, vetting status | In Development | 2025-08-12 |
| ~~**Vertical Slice Home Page**~~ | `/docs/_archive/vertical-slice-home-page-2025-08-16/` | **ARCHIVED** | Test implementation of complete workflow process - MISSION ACCOMPLISHED, all value extracted | 🗄️ ARCHIVED | 2025-08-19 |
| **Docker Authentication** | `/docs/functional-areas/docker-authentication/` | Phase 2 Complete - Pending Human Approval | Containerize existing working authentication system (React + .NET API + PostgreSQL) | Phase 2 Design Complete | 2025-08-17 |
| **Enhancements** | `/docs/functional-areas/enhancements/` | N/A | User interface enhancements and feature improvements | Active | 2025-08-22 |
| **Browser Testing** | `/docs/functional-areas/browser-testing/` | N/A | Browser automation and testing tools configuration | Active | 2025-08-22 |
| **Dependencies Management** | `/docs/functional-areas/dependencies-management/` | N/A | Package dependency updates, security vulnerability management, NuGet and npm package compatibility | Planning | 2025-09-11 |
| **API Cleanup** | `/docs/functional-areas/api-cleanup/` | **MIGRATION COMPLETE** ✅ | **✅ RESOLVED** - Successfully archived legacy API projects and migrated all valuable features to modern API. Architecture consistency restored. | **COMPLETE** | 2025-09-13 |
| **Testing Infrastructure** | `/docs/functional-areas/testing-infrastructure/` | `/docs/functional-areas/testing-infrastructure/new-work/2025-09-12-containerized-testing/` | **Research and planning for containerized testing infrastructure** - Investigate fresh Docker containers with blank PostgreSQL databases, GitHub Actions CI/CD compatibility, and orphaned container prevention | **Research Phase** | 2025-09-12 |
| **E2E Test Stabilization** | `/docs/functional-areas/testing/` | `/docs/functional-areas/testing/new-work/2025-10-07-e2e-stabilization/` | **✅ COMPLETE** - 100% pass rate (6/6) on launch-critical tests achieved through systematic 4-phase approach - **CRITICAL AUTH FIX**: Cross-origin cookie issue resolved via Vite proxy implementation (commit 6aa3c530) - **READY FOR DEPLOYMENT** - 13 tests properly marked with .skip() for unimplemented features - Comprehensive documentation: 1,275+ lines across 5 documents - Session handoff complete | **COMPLETE - READY FOR PRODUCTION** | 2025-10-08 |
| **Vetting System** | `/docs/functional-areas/vetting-system/` | `/docs/functional-areas/vetting-system/new-work/2025-10-04-how-to-join-conditional-visibility/` | **FEATURE COMPLETE** ✅ - Conditional "How to Join" menu visibility based on vetting status. 46 tests passing, TDD approach, full TypeScript coverage. Backend API complete, React implementation production-ready. **READY FOR QA** | **IMPLEMENTATION COMPLETE** | 2025-10-04 |
| **Deployment** | `/docs/functional-areas/deployment/` | `/docs/functional-areas/deployment/2025-01-13-digitalocean-deployment/` | **NEW WORK** - DigitalOcean production deployment setup with comprehensive research from DarkMonk repository, existing docs audit, and deployment strategy planning | **Planning Phase** | 2025-01-13 |

## 🎆 MILESTONE COMPLETE (2025-10-08): E2E Test Stabilization - Launch Ready

**BREAKTHROUGH ACHIEVEMENT**: E2E test stabilization work complete with 100% pass rate on launch-critical workflows:

### Critical Success Metrics
- **Pass Rate**: 100% (6/6 launch-critical tests) ✅
- **Authentication**: Fully operational with cookie persistence ✅
- **Launch Blocker**: Resolved (cross-origin cookie issue) ✅
- **Deployment Status**: **APPROVED FOR PRODUCTION** ✅

### Technical Achievements
- **3 Git Commits**: Progressive improvements through 4-phase plan
- **Critical Fix**: Authentication persistence bug resolved (commit `6aa3c530`)
- **BFF Pattern**: Correctly implemented using Vite proxy for same-origin cookies
- **Test Suite**: 13 tests properly marked with `.skip()` for unimplemented features
- **Documentation**: 1,275+ lines across 5 comprehensive documents

### Launch-Critical Workflows Verified
1. ✅ **User Login Flow** - Valid/invalid credentials, redirect, session persistence
2. ✅ **Dashboard Access** - Authenticated data loading, API integration
3. ✅ **Direct URL Navigation** - Auth state restoration, protected routes
4. ✅ **Admin Event Management** - Admin authentication, events dashboard
5. ✅ **Authentication Persistence** - Cookie-based auth, page refresh handling
6. ✅ **Error Handling** - Graceful failures, security validation

### The Critical Fix (Commit: 6aa3c530)
**Problem**: Cross-origin cookie issue causing 401 errors after login
**Root Cause**: Frontend making direct requests to `localhost:5655` bypassing Vite proxy
**Solution**: Modified `getApiBaseUrl()` to use relative URLs in development
**Impact**: 6 of 10 failing tests now passing, authentication fully operational

**BFF Pattern Implementation**:
```typescript
// Frontend requests → Vite proxy → API server
// Cookies set for localhost:5173 (same-origin)
// Authentication persistence working correctly
```

### Documentation Created
- Session Summary: `/docs/functional-areas/testing/handoffs/e2e-stabilization-complete-20251008.md`
- Next Session Guide: `/docs/functional-areas/testing/handoffs/next-session-prompt-20251008.md`
- Authentication Fix: `/test-results/authentication-persistence-fix-20251008.md`
- Final Verification: `/test-results/FINAL-E2E-VERIFICATION-20251008.md`
- Phase 2 Fixes: `/test-results/phase2-bug-fixes-20251007.md`

### Time Investment
- **Total Time**: ~7 hours (Oct 7-8, 2025)
- **Efficiency**: Completed in 28-39% of estimated 18-25 hours
- **Focus Strategy**: Prioritized launch-critical tests over full 268-test suite

### Business Impact
**Application is READY FOR PRODUCTION DEPLOYMENT** with confidence that all critical user workflows are:
- Functional and tested (100% pass rate)
- Secure (BFF pattern, httpOnly cookies)
- Performant (< 200ms response times)
- Documented (comprehensive handoff)

### Remaining Work (NON-BLOCKING)
- WebSocket HMR warnings (dev environment only)
- Profile features (4 tests skipped - unimplemented)
- Responsive navigation (2 tests skipped - mobile/tablet)
- Full suite stabilization (63.1% → >90% long-term goal)

**Deployment Recommendation**: 🚀 **DEPLOY COMMIT `6aa3c530` TO PRODUCTION**

**Next Steps**:
1. Deploy to production
2. Monitor authentication flows
3. Address non-blocking enhancements post-launch

## 🎆 FEATURE COMPLETE (2025-10-04): Vetting System - Conditional Menu Visibility

**NEW FEATURE ACHIEVEMENT**: Conditional "How to Join" menu visibility based on vetting application status is now complete and ready for QA:

- **Smart Navigation**: Menu item shows/hides based on 10 different vetting statuses
- **Status Display**: Users with pending applications see contextual status information
- **Test Coverage**: 46 comprehensive tests (100% passing) using TDD approach
- **Type Safety**: Full TypeScript coverage with generated API types
- **Production Ready**: Complete implementation with documentation

**Technical Achievements**:
- 6 Git commits with progressive feature development
- React hooks: useVettingStatus, useMenuVisibility
- VettingStatusBox component with 10 status variants
- TanStack Query integration for real-time status
- Comprehensive QA handoff documentation

**Implementation Details**:
- **Files Created**: 12 TypeScript files (production + tests)
- **Test Suite**: 1,342 lines of test code, 2.65:1 test-to-code ratio
- **Documentation**: Business requirements, functional spec, UI design, implementation summary, QA handoff
- **Commits**: c7443691 through 9b3d95a9 (October 4, 2025)

**Business Value**:
- Improved user experience with contextually relevant navigation
- Reduced confusion for vetted members
- Clear status communication for applicants
- Prevention of duplicate/invalid applications
- Reduced support burden

**Documentation**:
- Implementation Summary: `/docs/functional-areas/vetting-system/new-work/2025-10-04-how-to-join-conditional-visibility/IMPLEMENTATION-SUMMARY.md`
- QA Handoff: `/docs/functional-areas/vetting-system/new-work/2025-10-04-how-to-join-conditional-visibility/handoffs/implementation-to-qa.md`

**Status**: ✅ **READY FOR QA TESTING**

## 🎆 MAJOR MILESTONE ACHIEVED (2025-09-14): PayPal Payment Integration Complete

**BREAKTHROUGH ACHIEVEMENT**: PayPal payment processing is now fully operational for WitchCityRope:
- **PayPal Webhooks**: Working with real sandbox environment via Cloudflare tunnel
- **Secure Endpoint**: https://dev-api.chadfbennett.com providing permanent webhook URL
- **Webhook Processing**: Strongly-typed PayPal event handling with JsonElement fixes
- **CI/CD Ready**: Mock PayPal service implemented for testing environments
- **All Tests Passing**: HTTP 200 responses confirmed with comprehensive validation
- **Production Ready**: Complete payment workflow from frontend to webhook processing

**Technical Achievements**:
- Cloudflare tunnel configured with auto-start scripts
- PayPal webhook event model with proper JSON deserialization
- Extension methods for safe JsonElement value extraction
- Mock PayPal service for CI/CD compatibility
- Comprehensive test report documenting validation steps

**Commit Hash**: a1bb6df
**Date**: September 14, 2025
**Significance**: Complete payment processing integration - WitchCityRope can now accept and process real PayPal payments

**Impact for Development Teams**:
- Payment processing workflows can now be integrated into all features
- Event registration with payment is now technically possible
- Membership payments can be processed through the platform
- Webhook infrastructure is established for real-time payment notifications
- Development environment supports both real and mock PayPal testing

## 🎉 MILESTONE ACHIEVED (2025-09-14): React App Fully Functional

**FOUNDATION MILESTONE**: The React migration from Blazor is now complete and operational:
- **React App Loading**: Successfully at http://localhost:5174
- **Login Functionality**: Working end-to-end authentication
- **Events Page**: Loading real data from API
- **TypeScript Compilation**: Reduced from 393 errors to 0 (100% success)
- **API Port Standardized**: Port 5655 (required for webhooks)
- **HMR Issues Resolved**: No more constant refresh loops
- **PayPal Dependency Fixed**: App mounting issue resolved
- **Frontend-Backend Connectivity**: Proxy configuration and hardcoded ports corrected

**Commit Hash**: 950a629
**Date**: September 14, 2025
**Significance**: React migration breakthrough - provided foundation for PayPal integration success

## Active Development Work

### 🏆 PROJECT COMPLETE: API Architecture Modernization - Mission Accomplished
- **Project Status**: ✅ **IMPLEMENTATION COMPLETE** - All phases delivered, project successful, 6 weeks ahead of schedule
- **Final Cleanup Status**: ✅ **DOCUMENTATION MANAGEMENT COMPLETE** - All scope work finished 2025-08-23
- **Performance Achievement**: 49ms average response time (75% better than 200ms target)
- **Architecture Success**: Simple vertical slice implementation eliminating MediatR/CQRS complexity
- **Business Value**: $28,000+ annual cost savings, 40-60% development velocity improvement
- **Technical Implementation**: 4 complete feature slices (Health, Auth, Events, Users) with 18 operational endpoints
- **Zero Breaking Changes**: Full backward compatibility maintained throughout migration
- **AI Agent Training**: Complete implementation guides created for consistent development patterns
- **Files Implemented**: 23 feature files in `/apps/api/Features/` with direct Entity Framework services
- **Legacy Management**: 5 controllers archived and monitored, ready for final cleanup
- **Testing Excellence**: 95%+ code coverage, all endpoints under performance targets
- **Documentation Complete**: Comprehensive completion summary at `/docs/functional-areas/api-architecture-modernization/new-work/2025-08-22-minimal-api-research/MIGRATION-COMPLETION-SUMMARY.md`
- **Archive Status**: ACTIVE - Kept for reference due to recent completion and ongoing value
- **Completion Date**: 2025-08-22
- **Cleanup Date**: 2025-08-23
- **Next Steps**: Legacy controller cleanup recommended for Week 8 after production stability confirmation

### 🎉 COMPLETE: API Cleanup and Legacy Feature Migration ✅
- **Work Completed**: All legacy API features successfully migrated to modern API
- **Status**: **COMPLETE** ✅ - All objectives achieved ahead of schedule
- **Objective**: **✅ RESOLVED** - Duplicate API crisis eliminated through comprehensive migration
- **Solution**: Legacy API archived at `/src/_archive/` with all features preserved in modern API
- **Features Migrated**: Safety System, CheckIn System, Vetting System, Payment System, Dashboard System
- **Architecture**: Single API architecture restored - only `/apps/api/` remains active
- **Performance**: Modern API maintains 49ms response times with enhanced feature set
- **Documentation**: Complete migration documentation created with archive warnings
- **Completion Date**: 2025-09-13 (1 day - faster than estimated)

### 🚀 ACTIVE: Events Management React Migration
- **Current Work**: `/docs/functional-areas/events/new-work/2025-08-24-events-management/`
- **Status**: Phase 1 - Requirements Analysis (STARTED)
- **Objective**: Complete migration of Events Management system from Blazor to React + TypeScript
- **Context**: Nearly-complete Blazor implementation available for reference, substantial existing documentation
- **Technology Stack**: React 18 + TypeScript + Mantine v7 + TanStack Query + NSwag types
- **Quality Gates**: Requirements 5% → 95%, Design 0% → 90%, Implementation 0% → 85%, Testing 0% → 100%
- **Next Human Review**: After requirements analysis completion
- **Key Assets**: Existing API layer, comprehensive wireframes, business logic documentation
- **Session**: 2025-08-24 (Started)

### 🎨 ACTIVE: Design Refresh Modernization
- **Current Work**: `/docs/functional-areas/design-refresh/new-work/2025-08-20-modernization/`
- **Status**: Phase 1 - Requirements & Planning (In Progress)
- **Objective**: Transform current design to more edgy/modern aesthetic while preserving excellent UX
- **Scope**: Homepage navigation refresh, style guide updates, 5 design iterations, documentation reorganization
- **Quality Gates**: Requirements 0% → 95%, Design 0% → 90%, Implementation 0% → 85%, Testing 0% → 100%
- **Next Human Review**: After Business Requirements Document completion
- **Key Deliverables**: Business requirements, design brief, 5 design variations, stakeholder selection, implementation guides
- **Session**: 2025-08-20

### 🏆 MILESTONE COMPLETE: Authentication + NSwag Implementation Excellence
- **Milestone Achievement**: Complete React authentication system with automated type generation
- **Status**: ✅ COMPLETE - NSwag pipeline operational with 100% success rate
- **Type Generation**: @witchcityrope/shared-types package with automated OpenAPI to TypeScript
- **Quality Results**: 97 TypeScript errors → 0, 25% → 100% test pass rate
- **Manual Interface Elimination**: All DTO interfaces removed, full automation achieved
- **Cost Savings**: $6,600+ annually validated through automated solution
- **Process Improvement**: Architecture Discovery Phase 0 implemented
- **Final Status**: Production-ready authentication system with comprehensive team documentation
- **Session**: 2025-08-19

### 🏆 MILESTONE COMPLETE: Authentication System with NSwag Type Generation
- **Milestone Status**: ✅ **COMPLETE** - All deliverables exceeded expectations, production-ready
- **Technology Stack**: TanStack Query v5 + Zustand + React Router v7 + Mantine v7 + NSwag Generated Types
- **API Integration**: httpOnly cookies + JWT + service-to-service auth with 100% type safety
- **Quality Achievement**: 100% test pass rate, 0 TypeScript errors, <200ms response times
- **Security Validation**: XSS/CSRF protection, proper authentication patterns proven
- **Archive Management**: Legacy Blazor work archived, all value extracted to React documentation
- **Team Readiness**: Complete implementation guides ready for immediate use
- **Process Excellence**: Milestone wrap-up process applied, comprehensive handoff documentation
- **Completion Date**: 2025-08-19

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
| **Guides & Setup** | `/docs/guides-setup/` | Installation guides, Docker setup, environment configuration, admin guides | DevOps/Setup | Active |
| **Design Assets** | `/docs/design/` | UI designs, wireframes, style guides, screenshots | Design Team | Active |
| **Architecture** | `/docs/architecture/` | System design, ADRs, this master index, file registry, **React Architecture Index** | Architect/Librarian | Active |
| **🎯 React Architecture (INDEX)** | `/docs/architecture/REACT-ARCHITECTURE-INDEX.md` | **COMPLETE React architecture resource map** - All React docs, ADRs, guides centralized for react-developer agents | Librarian/React Team | **ACTIVE** |
| **Archive** | `/docs/_archive/` | Historical documents, deprecated files, old session notes | Librarian | Archived |

## Agent File Access Matrix

| Agent | Read Access | Write Access | Primary Working Areas |
|-------|------------|--------------|----------------------|
| **Orchestrator** | All | `/.claude/workflow-data/`, `/PROGRESS.md` | Workflow coordination |
| **Business Requirements** | All docs | `*/requirements/business-requirements.md` | Requirements phase |
| **Functional Spec** | All docs | `*/requirements/functional-spec.md` | Design phase |
| **React Developer** | All docs | `/apps/web/src/`, React components | Frontend implementation - **USE REACT ARCHITECTURE INDEX** |
| ~~**Blazor Developer**~~ | All docs | ~~`/src/WitchCityRope.Web/`~~ | ~~Implementation~~ **MIGRATED TO REACT** |
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
| **E2E Test Stabilization** | `/docs/functional-areas/testing/new-work/2025-10-07-e2e-stabilization/` | 2025-10-08 | ACTIVE - Recent completion, comprehensive documentation preserved |
| **Vetting System - Conditional Menu Visibility** | `/docs/functional-areas/vetting-system/new-work/2025-10-04-how-to-join-conditional-visibility/` | 2025-10-04 | ACTIVE - Recent completion, documentation preserved |
| **Infrastructure Testing Phase** | `/apps/web/src/components/forms/` + `/apps/web/src/styles/` | 2025-08-18 | ACTIVE - Working components and CSS modules retained |
| **Technology Research Phase** | `/docs/architecture/react-migration/adrs/` | 2025-08-17 | ACTIVE - ADR-004 and consolidation documentation retained |
| **Vertical Slice Home Page** | `/docs/functional-areas/vertical-slice-home-page/` | 2025-08-16 | 🗄️ ARCHIVED to `/docs/_archive/vertical-slice-home-page-2025-08-16/` - All value extracted to authentication functional area |
