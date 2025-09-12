# API Cleanup and Feature Extraction Session Prompt

<!-- Last Updated: 2025-09-12 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Session Prompt - Ready for Execution -->

## CRITICAL SESSION OBJECTIVE

You are Claude Code starting a FRESH session to resolve the **DUPLICATE API PROJECTS CRISIS**. This session is specifically designed to handle the critical architectural issue where WitchCityRope has TWO separate API projects causing development confusion and architectural chaos.

## MANDATORY PRE-WORK CHECKLIST

Before starting ANY work, you MUST:

1. **Read the Investigation Report**: `/docs/architecture/critical-issue-duplicate-apis-investigation.md` 
   - This contains the complete forensic analysis of the duplicate API situation
   - Understand the timeline, root cause, and technical differences
   - Review the recommended resolution plan

2. **Read Backend Lessons Learned**: `/docs/lessons-learned/backend-lessons-learned.md`
   - Contains critical lessons about which API is active and which is legacy
   - Section: "CRITICAL FIX: Duplicate API Projects Detection" (line 205)
   - Section: "Minimal API Event Update Implementation" shows modern API architecture

3. **Verify Current State**:
   ```bash
   # Verify which API is actually running
   curl -s http://localhost:5655/health || echo "Modern API not running"
   curl -s http://localhost:5653/health || echo "Legacy API not running"
   
   # Check React frontend configuration
   grep -r "5655\|5653" apps/web/src/config/
   ```

## THE DUPLICATE API SITUATION

### Primary Active API (KEEP THIS ONE)
- **Location**: `/apps/api/` 
- **Port**: 5655
- **Technology**: ASP.NET Core Minimal API (.NET 9)
- **Architecture**: Vertical Slice Architecture  
- **Status**: **CURRENTLY SERVING REACT FRONTEND**
- **Performance**: 49ms response times achieved
- **Features**: Health, Authentication, Events, Users
- **Creation**: August 15, 2025 for React migration

### Legacy API (ARCHIVE THIS ONE)
- **Location**: `/src/WitchCityRope.Api/`
- **Port**: Not currently running
- **Technology**: ASP.NET Core API (.NET)  
- **Architecture**: Full-featured API with Infrastructure patterns
- **Status**: **DORMANT BUT CONTAINS VALUABLE FEATURES**
- **Features**: Auth, CheckIn, Dashboard, Events, Payments, Safety, Vetting
- **Creation**: June 28, 2025 for Blazor Server

### Root Cause
During React migration in August 2025, a NEW simplified API was created at `/apps/api/` instead of refactoring the existing API at `/src/WitchCityRope.Api/`. The legacy API was never cleaned up, creating architectural duplication.

## SESSION WORK PLAN

### Phase 1: Feature Extraction Analysis (Day 1)

#### 1.1 Legacy API Feature Inventory
Create comprehensive inventory of ALL features in legacy API that DON'T exist in modern API:

**Investigation Tasks**:
- **CheckIn System**: Analyze `/src/WitchCityRope.Api/Features/CheckIn/`
- **Safety Features**: Analyze `/src/WitchCityRope.Api/Features/Safety/`
- **Vetting System**: Analyze `/src/WitchCityRope.Api/Features/Vetting/`
- **Advanced Payment Features**: Compare payment implementations
- **Advanced Dashboard Features**: Identify dashboard functionality gaps

**Deliverable**: Create `/docs/architecture/legacy-api-feature-analysis-2025-09-12.md` with:
- Complete feature inventory with descriptions
- Business criticality assessment (Critical/Important/Nice-to-Have)
- Implementation complexity estimates
- Dependencies between features

#### 1.2 Modern API Capability Assessment
Document exactly what the modern API CAN do:

**Investigation Tasks**:
- Review all endpoints in `/apps/api/Features/`
- Test all functionality via curl/API testing
- Document performance characteristics
- Identify any gaps compared to legacy API

**Deliverable**: Update feature analysis document with modern API capabilities comparison

#### 1.3 Feature Extraction Priority Matrix
**Decision Required**: For each legacy feature, determine:
- **EXTRACT**: Must be migrated to modern API (business critical)
- **ARCHIVE**: Document but don't migrate (historical value only)
- **DISCARD**: Obsolete or redundant functionality

### Phase 2: Critical Feature Migration (Days 2-4)

#### 2.1 Extract Critical Features (If Any Identified)
For features marked as EXTRACT in Phase 1:

**Migration Process**:
1. **Create Feature Branch**: `git checkout -b feature/extract-legacy-[feature-name]`
2. **Implement in Modern Architecture**: Follow vertical slice pattern in `/apps/api/Features/`
3. **Port Business Logic**: Extract domain logic from legacy API
4. **Create Tests**: Add comprehensive test coverage
5. **Test Integration**: Verify with React frontend
6. **Document**: Update API documentation

**Critical Requirements**:
- **NEVER modify legacy API** - only extract FROM it
- **Follow modern API patterns** - vertical slice architecture
- **Maintain performance** - 49ms response time targets
- **Test thoroughly** - comprehensive test coverage required

#### 2.2 Database Schema Verification
**CRITICAL**: Ensure both APIs use compatible database schemas:

**Verification Tasks**:
- Compare Entity Framework models between both APIs
- Identify schema conflicts or differences
- Test database operations from both APIs (if needed for extraction)
- Document any schema migrations required

**Safety Measures**:
- **NEVER run both APIs simultaneously against same database**
- Create database backup before any schema changes
- Test all migrations in development environment first

### Phase 3: Legacy API Archival (Day 5)

#### 3.1 Comprehensive Documentation
Before archiving, create complete documentation:

**Required Documentation**:
- **API Documentation**: Export all endpoint documentation
- **Business Logic Documentation**: Document complex business rules
- **Database Schema**: Document all tables, relationships, constraints
- **Configuration Guide**: Document environment setup and configuration
- **Migration Guide**: Document what was extracted and why

**Deliverable**: `/docs/_archive/api-legacy-2025-09-12/LEGACY-API-COMPLETE-DOCUMENTATION.md`

#### 3.2 Safe Archival Process
**Archive Location**: `/docs/_archive/api-legacy-2025-09-12/`

**Archival Steps**:
1. **Create Archive Directory**: With proper README explaining archival
2. **Move Entire API Project**: `git mv src/WitchCityRope.Api docs/_archive/api-legacy-2025-09-12/src/`
3. **Update Build Configurations**: Remove from solution files, Docker configurations
4. **Clean Up References**: Remove from any documentation, scripts, guides
5. **Add Deprecation Warnings**: In any files that reference legacy API
6. **Update File Registry**: Log all moves and deletions

#### 3.3 Reference Cleanup
**Critical Cleanup Tasks**:
- **Remove from Docker Compose**: Clean up any legacy API services
- **Update Documentation**: Remove references from all active documentation
- **Clean Port References**: Remove port configurations for legacy API
- **Update Development Guides**: Ensure all guides reference only modern API
- **Clean Test Configurations**: Remove legacy API test references

### Phase 4: Documentation and Validation (Day 6)

#### 4.1 Architecture Documentation Update
**Required Updates**:
- **Update Architecture Overview**: Remove dual API references
- **Update API Documentation**: Document final modern API capabilities
- **Update Development Guides**: Single API development patterns
- **Update Deployment Guides**: Remove legacy API deployment steps

#### 4.2 Agent Instruction Updates
**Critical Agent Instruction Updates**:
- **Backend Developer Lessons**: Update to reference ONLY `/apps/api/`
- **CLAUDE.md**: Update API references to single API
- **Architecture Guides**: Remove dual API confusion
- **Test Documentation**: Update test procedures for single API

#### 4.3 Validation and Testing
**Comprehensive Validation**:
- **Frontend Integration Test**: Verify React app works with modern API only
- **All API Endpoints Test**: Verify no functionality lost
- **Documentation Review**: Ensure no broken references
- **Team Handoff**: Prepare clean handoff documentation

## TECHNICAL CONSTRAINTS AND WARNINGS

### NEVER DO THESE THINGS
- ❌ **NEVER run both APIs simultaneously** - Database conflicts possible  
- ❌ **NEVER modify legacy API** - It's read-only for extraction
- ❌ **NEVER add new features to legacy API** - It's being archived
- ❌ **NEVER break the modern API** - It's serving production traffic
- ❌ **NEVER skip testing** - Frontend integration must work

### ALWAYS DO THESE THINGS
- ✅ **ALWAYS test after each change** - Verify nothing breaks
- ✅ **ALWAYS backup before major changes** - Database safety first
- ✅ **ALWAYS use git branches** - For any extraction work  
- ✅ **ALWAYS document decisions** - Why extracted vs archived
- ✅ **ALWAYS update file registry** - Track all file operations

### ARCHITECTURE CONSTRAINTS
- **Modern API Architecture**: Vertical slice pattern ONLY
- **Performance Requirements**: Maintain <50ms response times
- **Authentication**: JWT Bearer tokens with httpOnly cookies
- **Database**: PostgreSQL with Entity Framework Core
- **Testing**: Comprehensive unit and integration tests required

## GOTCHAS AND COMMON ISSUES

### Port Configuration Issues
- **Problem**: Multiple port references scattered throughout codebase
- **Solution**: Use centralized configuration in `/apps/web/src/config/api.ts`
- **Warning**: Some test files may have hard-coded legacy ports

### Test File Legacy References
- **Problem**: Test files reference legacy API port 5653 instead of modern API port 5655
- **Solution**: Update test configurations to use environment variables
- **Files**: Check `/tests/`, `/apps/web/src/test/`, Playwright configurations

### Docker Container Confusion
- **Problem**: Multiple API containers or configurations
- **Solution**: Clean up docker-compose files to reference only modern API
- **Warning**: API container may need rebuild after changes

### Entity Framework Differences
- **Problem**: Legacy and modern APIs may have different EF models
- **Solution**: Carefully compare entity definitions before extraction
- **Warning**: Database schema conflicts possible if both APIs run

### Authentication Method Differences
- **Problem**: Different authentication implementations between APIs
- **Solution**: Stick with modern API authentication patterns
- **Warning**: Don't try to merge authentication systems

## SUCCESS CRITERIA

### Phase Completion Requirements

#### Phase 1 Success Criteria
- [ ] Complete legacy API feature inventory documented
- [ ] Modern API capability assessment completed  
- [ ] Feature extraction priority matrix created
- [ ] Business criticality assessment completed
- [ ] Implementation complexity estimates provided

#### Phase 2 Success Criteria  
- [ ] All CRITICAL features extracted and working in modern API
- [ ] All extracted features tested with React frontend
- [ ] Database schema conflicts resolved
- [ ] Performance targets maintained (< 50ms)
- [ ] Comprehensive test coverage for extracted features

#### Phase 3 Success Criteria
- [ ] Legacy API completely archived with documentation
- [ ] All references cleaned up from active codebase  
- [ ] Build configurations updated (no legacy API references)
- [ ] File registry updated with all moves and deletions
- [ ] Archive README created explaining what was archived and why

#### Phase 4 Success Criteria
- [ ] All documentation updated to reference single API
- [ ] Agent instructions updated  
- [ ] Frontend integration verified working
- [ ] No broken references or dead links
- [ ] Clean handoff documentation created

### Final Success Validation

**Zero Confusion Test**:
- [ ] New developer can start development without API confusion
- [ ] All documentation references single API consistently  
- [ ] No references to legacy API in active codebase
- [ ] Clear development environment setup instructions

**Functionality Preservation Test**:
- [ ] React frontend works identically to before cleanup
- [ ] All business-critical features preserved
- [ ] Performance maintained or improved
- [ ] No regressions in user experience

## FILE REGISTRY REQUIREMENTS

You MUST update `/docs/architecture/file-registry.md` for EVERY file operation:

```markdown
| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-09-12 | /src/WitchCityRope.Api/ | ARCHIVED | Complete legacy API archival - contains CheckIn, Safety, Vetting features that were analyzed and deemed non-critical for current needs | API Cleanup Session | ARCHIVED | Keep in archive permanently |
| 2025-09-12 | /docs/_archive/api-legacy-2025-09-12/ | CREATED | Archive location for complete legacy API with documentation | API Cleanup Session | ACTIVE | Keep permanently |
| 2025-09-12 | /docs/architecture/legacy-api-feature-analysis-2025-09-12.md | CREATED | Complete analysis of legacy API features with extraction decisions | API Cleanup Session | ACTIVE | Keep permanently |
```

## EMERGENCY PROCEDURES

### If Something Breaks
1. **IMMEDIATELY stop work** and assess impact
2. **Check React frontend** - is it still working?
3. **Check modern API health** - `curl http://localhost:5655/health`
4. **Restore from git** if necessary - `git reset --hard HEAD~1`  
5. **Document the issue** in session notes
6. **Plan recovery strategy** before continuing

### If Database Issues Occur
1. **NEVER delete data** without backup
2. **Stop all API processes** immediately
3. **Create database dump** - `docker exec witchcity-postgres pg_dump`
4. **Restore clean state** if necessary
5. **Document the issue** and root cause

### If Performance Degrades
1. **Measure response times** - use curl/browser dev tools
2. **Compare to baseline** - should be < 50ms
3. **Identify bottlenecks** - slow queries, inefficient code
4. **Rollback changes** if necessary
5. **Document performance impact** and mitigation

## DELIVERABLES

### Required Documents
1. **Feature Analysis Report**: Complete analysis of legacy vs modern API features
2. **Extraction Decision Matrix**: What was extracted, archived, or discarded and why
3. **Archive Documentation**: Complete documentation of archived legacy API
4. **Architecture Update**: Updated architecture documents reflecting single API
5. **Migration Summary**: Summary of all changes made during cleanup

### Updated Files Expected
- `/docs/architecture/legacy-api-feature-analysis-2025-09-12.md`
- `/docs/_archive/api-legacy-2025-09-12/LEGACY-API-COMPLETE-DOCUMENTATION.md`  
- `/docs/architecture/API-ARCHITECTURE-OVERVIEW.md` (updated)
- `/docs/lessons-learned/backend-lessons-learned.md` (updated)
- `/docs/architecture/file-registry.md` (updated with all changes)

### Code Changes Expected
- Legacy API moved to archive location
- Modern API potentially enhanced with extracted features
- All build configurations cleaned up
- All documentation references updated

## FINAL REMINDERS

This session is **CRITICAL** for project health. The duplicate API situation creates:
- **Developer confusion** (which API to modify?)
- **Maintenance burden** (two codebases to maintain)
- **Architecture debt** (violates single responsibility)
- **Future problems** (scaling and deployment complexity)

**Your mission**: Leave the project with ONE clear, well-documented API that serves all business needs, with complete historical preservation of the legacy API in the archive.

**Success means**: Future developers will never be confused about which API to use or modify.

---

**CLASSIFICATION**: Critical Architectural Cleanup
**PRIORITY**: HIGH  
**ESTIMATED TIME**: 5-6 days
**COMPLEXITY**: Medium-High (requires careful analysis and safe migration)
**RISK LEVEL**: Medium (modern API must remain functional throughout)

**BEGIN SESSION**: Start with Phase 1 - Feature Extraction Analysis