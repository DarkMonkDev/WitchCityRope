# API Cleanup Functional Area
<!-- Last Updated: 2025-09-12 -->
<!-- Version: 1.0 -->
<!-- Owner: Orchestrator Agent -->
<!-- Status: Active -->

## Overview
This functional area manages the critical architectural cleanup to resolve the duplicate API projects crisis discovered in the WitchCityRope codebase.

## Problem Statement
**CRITICAL ARCHITECTURAL ISSUE**: Two separate API projects exist simultaneously in the codebase:

1. **Modern API** at `/apps/api/` (port 5655)
   - **Status**: ACTIVE - Currently serving React frontend
   - **Architecture**: Simplified vertical slice pattern
   - **Technology**: .NET Minimal API
   - **Performance**: 49ms average response time

2. **Legacy API** at `/src/WitchCityRope.Api/` 
   - **Status**: DORMANT - Not actively used
   - **Architecture**: Enterprise patterns with MediatR/CQRS
   - **Contains**: Valuable features not yet extracted
   - **Risk**: Potential feature loss if not properly migrated

## Root Cause Analysis
During the React migration in August 2025, a new simplified API was created at `/apps/api/` instead of refactoring the existing API at `/src/WitchCityRope.Api/`. This left the legacy API in place, creating:

- **Development Confusion**: Two API projects to maintain
- **Feature Duplication Risk**: Unclear which API contains which features
- **Architecture Inconsistency**: Mixed patterns across the codebase
- **Maintenance Overhead**: Duplicate concerns and potential conflicts

## Objectives

### Primary Goals
1. **Complete Feature Inventory**: Comprehensive audit of all features in legacy API
2. **Value Assessment**: Identify which features must be preserved
3. **Safe Extraction**: Migrate valuable features to modern API
4. **Legacy Archival**: Safely remove legacy API after extraction
5. **Documentation Update**: Ensure all references point to single API

### Success Criteria
- [ ] Zero valuable features lost in migration
- [ ] Modern API contains all necessary functionality
- [ ] Legacy API safely archived with git history preserved
- [ ] React frontend continues working without breaking changes
- [ ] Single source of truth for API architecture

## Current Work

### Active Development
- **Work Path**: [`/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/`](./new-work/2025-09-12-legacy-feature-extraction/)
- **Status**: Phase 1 - Requirements Analysis (Documentation structure created)
- **Progress Document**: [progress.md](./new-work/2025-09-12-legacy-feature-extraction/progress.md)

### Next Steps
1. **Backend Developer**: Conduct comprehensive legacy API feature audit
2. **Business Requirements**: Assess feature value and extraction priority
3. **Orchestrator**: Coordinate multi-phase extraction workflow
4. **Human Review**: Approve requirements and approach before implementation

## Risk Assessment

### High Risks
- **Feature Loss**: Incomplete extraction leading to lost functionality
- **Breaking Changes**: Frontend integration issues during migration
- **Data Corruption**: Database inconsistencies during feature moves
- **Performance Degradation**: Poor extraction implementation

### Mitigation Strategies
- **Incremental Approach**: Extract one feature at a time
- **Comprehensive Testing**: Full test coverage for each extraction
- **Human Reviews**: Mandatory approval points throughout process
- **Rollback Planning**: Safe reversion procedures if issues arise

## Architecture Context

### Modern API (/apps/api/)
- **Pattern**: Vertical slice architecture
- **Benefits**: Simplified development, faster response times
- **Current Features**: Health, Auth, Events (basic), Users (basic)
- **Technology**: .NET 9 Minimal API + Entity Framework Core

### Legacy API (/src/WitchCityRope.Api/)
- **Pattern**: Enterprise patterns with MediatR/CQRS
- **Features**: Unknown comprehensive inventory needed
- **Technology**: .NET 9 with complex layering
- **Status**: Dormant but contains valuable features

## Documentation Standards

### File Organization
All work follows the standard functional area structure:
```
/docs/functional-areas/api-cleanup/
├── README.md                           # This overview
├── current-state/                      # Analysis of current API state
└── new-work/
    └── 2025-09-12-legacy-feature-extraction/
        ├── progress.md                 # Workflow tracking
        ├── requirements/               # Business requirements
        ├── design/                     # Technical design
        ├── implementation/             # Development tracking
        ├── testing/                    # Test plans
        ├── reviews/                    # Human review points
        └── handoffs/                   # Agent handoff docs
```

### Key Documents
- **Progress Tracking**: [progress.md](./new-work/2025-09-12-legacy-feature-extraction/progress.md)
- **Master Index**: [`/docs/architecture/functional-area-master-index.md`](../../architecture/functional-area-master-index.md)
- **File Registry**: [`/docs/architecture/file-registry.md`](../../architecture/file-registry.md)

## Team Coordination

### Agent Responsibilities
- **Orchestrator**: Overall workflow coordination and human reviews
- **Backend Developer**: Feature extraction and API development
- **Business Requirements**: Feature value assessment and prioritization
- **Test Developer**: Comprehensive testing strategy and implementation
- **Librarian**: Documentation maintenance and structure enforcement

### Human Review Points
1. **After Requirements Phase**: Feature extraction scope and priority approval
2. **After First Vertical Slice**: Technical approach validation
3. **Before Legacy Archival**: Final approval for safe removal

## Related Documentation
- **API Architecture**: [`/docs/functional-areas/api-architecture-modernization/`](../api-architecture-modernization/)
- **React Migration**: [`/docs/architecture/react-migration/`](../../architecture/react-migration/)
- **Agent Handoffs**: [`/docs/standards-processes/agent-handoff-template.md`](../../standards-processes/agent-handoff-template.md)

---

**Started**: 2025-09-12  
**Priority**: CRITICAL  
**Estimated Duration**: TBD after requirements analysis  
**Next Milestone**: Requirements completion and human review