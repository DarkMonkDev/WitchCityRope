# Technical Requirements - API Cleanup and Feature Extraction

<!-- Created: 2025-09-12 -->
<!-- Status: DRAFT - Pending Human Review -->
<!-- Owner: Orchestrator -->

## Executive Summary

This document defines the detailed technical requirements for resolving the critical duplicate API architecture issue in WitchCityRope. We have identified TWO separate API projects that must be consolidated into a single, modern API architecture.

## Current State Analysis

### Architecture Duplication
The codebase currently contains two complete API implementations:

#### Modern API (Primary - To Be Retained)
- **Location**: `/apps/api/`
- **Port**: 5655
- **Technology**: ASP.NET Core Minimal API (.NET 9)
- **Architecture**: Vertical Slice Architecture with simplified patterns
- **Status**: ACTIVE - Currently serving React frontend
- **Performance**: Achieving <50ms response times
- **Features Implemented**:
  - ✅ Health checks
  - ✅ Authentication (JWT with httpOnly cookies)
  - ✅ Events management
  - ✅ Users management
  - ✅ Shared utilities

#### Legacy API (To Be Archived)
- **Location**: `/src/WitchCityRope.Api/`
- **Technology**: ASP.NET Core API with full infrastructure patterns
- **Architecture**: Traditional layered architecture with repositories
- **Status**: DORMANT - Not currently running
- **Features Available**:
  - ✅ Auth (full implementation)
  - ✅ CheckIn system
  - ✅ Dashboard features
  - ✅ Events (comprehensive)
  - ✅ Payments processing
  - ✅ Safety features
  - ✅ Vetting system

### React Frontend Configuration
- **Current API Target**: Port 5655 (Modern API)
- **Configuration Location**: `/apps/web/src/config/api.ts`
- **Environment Variables**: Using VITE_API_BASE_URL

## Technical Requirements

### Phase 1: Feature Analysis & Extraction Planning

#### 1.1 Legacy Feature Audit Requirements
**Objective**: Complete technical inventory of all legacy API features

**Technical Tasks**:
1. **CheckIn System Analysis**
   - Analyze `/src/WitchCityRope.Api/Features/CheckIn/`
   - Document data models and business logic
   - Identify database dependencies
   - Map API endpoints and contracts

2. **Safety Features Analysis**
   - Analyze `/src/WitchCityRope.Api/Features/Safety/`
   - Document safety protocols and workflows
   - Identify integration points with other features
   - Map data persistence requirements

3. **Vetting System Analysis**
   - Analyze `/src/WitchCityRope.Api/Features/Vetting/`
   - Document vetting workflows and state machines
   - Identify role-based access control requirements
   - Map approval processes and notifications

4. **Payment Processing Analysis**
   - Compare payment implementations between APIs
   - Document payment provider integrations
   - Identify PCI compliance requirements
   - Map transaction handling and reconciliation

**Deliverable**: Technical feature inventory with:
- Complete endpoint documentation
- Data model specifications
- Business logic flowcharts
- Integration dependency maps

#### 1.2 Modern API Compatibility Assessment
**Objective**: Ensure extracted features align with modern architecture

**Technical Requirements**:
1. **Architecture Pattern Validation**
   - Vertical slice architecture compliance
   - Simple service pattern (no MediatR/CQRS)
   - Direct Entity Framework usage
   - Minimal API endpoint patterns

2. **Performance Baseline**
   - Current: <50ms response times
   - Target: Maintain or improve performance
   - Monitoring: Application Insights integration

3. **Authentication Consistency**
   - JWT Bearer token implementation
   - httpOnly cookie management
   - Role-based authorization
   - Session management patterns

### Phase 2: Critical Feature Migration

#### 2.1 Migration Architecture Requirements

**Vertical Slice Implementation Pattern**:
```
/apps/api/Features/[FeatureName]/
├── Services/
│   └── [Feature]Service.cs      # Direct EF Core service
├── Endpoints/
│   └── [Feature]Endpoints.cs    # Minimal API registration
├── Models/
│   ├── [Request]Request.cs      # Request DTOs
│   └── [Response]Response.cs    # Response DTOs
└── Validation/
    └── [Request]Validator.cs    # FluentValidation
```

**Service Implementation Requirements**:
- Direct DbContext injection (no repositories)
- Simple Result<T> pattern for error handling
- Async/await throughout
- Proper cancellation token propagation

**Endpoint Registration Requirements**:
- Use MapGroup for feature grouping
- Implement proper OpenAPI documentation
- Include authorization policies
- Add rate limiting where appropriate

#### 2.2 Database Migration Requirements

**Entity Framework Core Constraints**:
- Use code-first migrations
- Maintain backward compatibility
- No breaking schema changes
- Proper index optimization

**Migration Safety**:
- Database backup before any migration
- Rollback scripts for all changes
- Test migrations in development first
- Document all schema modifications

#### 2.3 Testing Requirements

**Unit Testing**:
- xUnit test framework
- In-memory database for service tests
- Moq for dependency mocking
- FluentAssertions for readable tests

**Integration Testing**:
- WebApplicationFactory for API tests
- TestContainers for PostgreSQL
- Complete endpoint coverage
- Authentication/authorization testing

**Performance Testing**:
- Load testing with NBomber
- Response time validation
- Memory leak detection
- Concurrent user simulation

### Phase 3: Legacy API Archival

#### 3.1 Documentation Requirements

**Required Documentation**:
1. **API Documentation Export**
   - OpenAPI/Swagger specification
   - Postman collection export
   - Authentication flow diagrams
   - Error code reference

2. **Business Logic Documentation**
   - Complex calculation explanations
   - Business rule documentation
   - Workflow state machines
   - Integration specifications

3. **Database Documentation**
   - Complete ERD diagram
   - Table descriptions
   - Stored procedures (if any)
   - Index strategies

#### 3.2 Archival Technical Requirements

**Git Operations**:
```bash
# Create archive branch
git checkout -b archive/legacy-api-2025-09-12

# Move to archive location
git mv src/WitchCityRope.Api docs/_archive/api-legacy-2025-09-12/

# Update solution file
# Remove project reference from WitchCityRope.sln

# Clean Docker configurations
# Remove from docker-compose files
```

**Build System Cleanup**:
- Remove from solution file
- Clean CI/CD pipelines
- Update deployment scripts
- Remove from Docker builds

### Phase 4: Validation & Finalization

#### 4.1 Integration Testing Requirements

**Frontend Validation**:
- All existing features working
- Authentication flow intact
- No broken API calls
- Performance maintained

**API Contract Validation**:
- Response format consistency
- Error handling patterns
- Status code compliance
- CORS configuration

#### 4.2 Documentation Update Requirements

**Update All References**:
- Architecture documents
- Development guides
- Deployment procedures
- Agent instructions
- Test documentation

## Implementation Approach

### Execution Strategy

#### Week 1: Analysis & Planning
**Days 1-2**: Legacy Feature Analysis
- Deep dive into CheckIn, Safety, Vetting features
- Document all business logic and data models
- Create extraction priority matrix

**Day 3**: Modern API Enhancement Planning
- Design vertical slice implementations
- Plan database migrations
- Create test strategies

#### Week 2: Implementation
**Days 4-5**: Critical Feature Extraction
- Implement high-priority features in modern API
- Create comprehensive tests
- Validate with frontend

**Day 6**: Legacy API Archival
- Complete documentation
- Move to archive location
- Clean all references

### Risk Mitigation

#### Technical Risks
1. **Data Loss Risk**
   - Mitigation: Complete database backups
   - Recovery: Rollback procedures ready

2. **Frontend Breaking Risk**
   - Mitigation: Comprehensive integration testing
   - Recovery: Feature flags for rollback

3. **Performance Degradation Risk**
   - Mitigation: Performance benchmarking
   - Recovery: Code optimization strategies

### Success Criteria

#### Technical Success Metrics
- [ ] Zero duplicate API references in codebase
- [ ] All tests passing (100% of existing tests)
- [ ] Performance maintained (<50ms response times)
- [ ] No frontend functionality regression
- [ ] Complete documentation trail

#### Architecture Success Metrics
- [ ] Single API architecture established
- [ ] Vertical slice pattern consistency
- [ ] Clean separation of concerns
- [ ] Simplified maintenance surface

## Recommended Approach Adjustments

### Key Architectural Decisions

1. **Prioritize Simplicity Over Feature Parity**
   - Not all legacy features may be needed
   - Focus on active business requirements
   - Archive rather than migrate unused features

2. **Maintain Modern Architecture Patterns**
   - Strict vertical slice adherence
   - No repository pattern regression
   - Simple, direct service implementations

3. **Frontend-First Validation**
   - Test every change with React frontend
   - Maintain API contract compatibility
   - Use feature flags for gradual rollout

### Migration Best Practices

1. **Incremental Migration**
   - One feature at a time
   - Complete testing before next feature
   - Maintain working state throughout

2. **Documentation-First Approach**
   - Document before implementation
   - Update docs with every change
   - Create migration guides

3. **Continuous Validation**
   - Run tests after each change
   - Monitor performance metrics
   - Validate with stakeholders

## Approval Required

**This technical requirements document requires approval before proceeding to implementation.**

### Review Checklist
- [ ] Technical approach approved
- [ ] Risk mitigation strategies accepted
- [ ] Timeline and phases confirmed
- [ ] Success criteria agreed upon
- [ ] Resource allocation confirmed

---

**Status**: AWAITING HUMAN REVIEW
**Next Step**: Upon approval, proceed to Phase 1 implementation
**Contact**: Development Team Lead for questions