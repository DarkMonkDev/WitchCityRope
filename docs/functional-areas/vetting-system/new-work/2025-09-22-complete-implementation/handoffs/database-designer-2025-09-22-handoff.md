# Database Designer Handoff Document
<!-- Date: 2025-09-22 -->
<!-- Phase: Database Schema Design Complete -->
<!-- Next Phase: Backend Implementation -->
<!-- Owner: Database Designer -->

## Handoff Summary

**Completed Work**: Comprehensive database schema design for WitchCityRope Vetting System implementation
**Document Created**: `/docs/functional-areas/vetting-system/new-work/2025-09-22-complete-implementation/design/database-design.md`
**Status**: Ready for backend implementation
**Next Agent**: Backend Developer

## Work Completed

### 1. Schema Analysis and Design

**Existing Schema Review**:
- Analyzed 11 existing vetting entities from September 13, 2025 implementation
- Identified strengths: comprehensive audit trails, encrypted PII, PostgreSQL optimization
- Identified gaps: email templates, bulk operations, enhanced notes, new status workflow

**New Schema Design**:
- 4 new entities designed: VettingEmailTemplate, VettingBulkOperation, VettingBulkOperationItem, VettingBulkOperationLog
- Enhanced existing entities: VettingApplication (new status), VettingApplicationNote (automatic/manual distinction)
- Comprehensive Entity Relationship Diagram with all relationships mapped

### 2. Entity Framework Core Configurations

**Configuration Classes Created**:
- `VettingEmailTemplateConfiguration` - Template storage with versioning and validation
- `VettingBulkOperationConfiguration` - Bulk operation tracking with JSONB parameters
- Enhanced `VettingApplicationNoteConfiguration` - Manual vs automatic note distinction
- All configurations follow WitchCityRope patterns with proper UTC DateTime handling

**Critical EF Core Patterns Applied**:
- Simple `public Guid Id { get; set; }` properties (no initializers per lessons learned)
- PostgreSQL `timestamptz` for all DateTime properties
- JSONB with GIN indexes for flexible data
- Proper foreign key relationships with appropriate delete behaviors
- Check constraints for business rules validation

### 3. Performance Optimization Strategy

**Strategic Indexing**:
- Partial indexes for sparse data (active applications, running operations)
- Composite indexes for common query patterns (status + date combinations)
- GIN indexes for JSONB search capabilities (bulk operation parameters, skills/interests)
- Unique constraints for business rules (one application per user, unique template types)

**Query Optimization**:
- Efficient bulk operation candidate selection queries
- Template retrieval with caching support
- Application grid queries with enhanced filtering
- Materialized view design for reporting requirements

### 4. Migration Strategy

**Phased Migration Approach**:
1. **Phase 1**: Core schema updates (new tables, enhanced columns)
2. **Phase 2**: Index creation (performance and constraint indexes)
3. **Phase 3**: Data migration (seed templates, update existing notes)
4. **Phase 4**: Constraint application (check constraints, foreign keys)

**Rollback Procedures**:
- Complete rollback strategy documented
- Data preservation guaranteed during rollback
- Original functionality validation checklist provided

### 5. Security and Compliance

**Data Protection**:
- All existing PII encryption patterns maintained
- New entities designed without PII storage requirements
- Email templates store only template text, variables replaced at runtime
- Comprehensive audit trails for all administrative operations

**Access Control Integration**:
- Foreign keys to existing AspNetUsers table
- Role-based access control patterns maintained
- Row Level Security patterns documented for future multi-tenancy

## New Requirements Addressed

### 1. Email Template System (Business Requirement)

**Implementation**:
- `VettingEmailTemplate` entity with 6 template types
- Admin-manageable templates within vetting section
- Template versioning and change tracking
- Support for template variables with runtime replacement

**Template Types Supported**:
1. Application Received (confirmation)
2. Interview Approved (NEW status)
3. Application Approved (final approval)
4. Application On Hold (additional info needed)
5. Application Denied (final rejection)
6. Interview Reminder (bulk operation)

### 2. Bulk Operations Framework (Business Requirement)

**Implementation**:
- `VettingBulkOperation` entity for operation tracking
- `VettingBulkOperationItem` entity for individual item processing
- `VettingBulkOperationLog` entity for detailed debugging
- Support for configurable time thresholds

**Operations Supported**:
- Send reminder emails to Interview Approved applications
- Change status to On Hold for old applications
- Extensible framework for future operation types

### 3. Enhanced Notes System (Business Requirement)

**Implementation**:
- `IsAutomatic` field to distinguish manual vs system notes
- `NoteType` field for categorizing note types (Manual, StatusChange, BulkOperation, System, EmailSent, EmailFailed)
- Backward compatibility with existing notes (default to manual)
- Enhanced indexing for note queries

### 4. Single Application Constraint (Business Requirement)

**Implementation**:
- Unique constraint on `ApplicantId` where `DeletedAt` IS NULL
- Database-level enforcement preventing multiple applications
- Soft delete support maintaining audit trail

### 5. New Status Workflow (Business Requirement)

**Implementation**:
- Added "Interview Approved" status (value 5)
- Added "OnHold" status (value 12)
- Updated status transition matrix documentation
- Maintained backward compatibility with existing status values

## Database Artifacts Delivered

### 1. Complete DDL Scripts

**New Tables**:
```sql
-- VettingEmailTemplates table with constraints and indexes
-- VettingBulkOperations table with JSONB parameters
-- VettingBulkOperationItems table with success tracking
-- VettingBulkOperationLogs table with detailed logging
```

**Enhanced Tables**:
```sql
-- VettingApplications: New status values, single application constraint
-- VettingApplicationNotes: IsAutomatic and NoteType columns
```

### 2. Entity Framework Configurations

**Files to Create**:
- `VettingEmailTemplateConfiguration.cs`
- `VettingBulkOperationConfiguration.cs`
- `VettingBulkOperationItemConfiguration.cs`
- `VettingBulkOperationLogConfiguration.cs`

**Files to Enhance**:
- `VettingApplicationConfiguration.cs` (add single application constraint)
- `VettingApplicationNoteConfiguration.cs` (add new fields and indexes)

### 3. Performance Indexes

**Critical Indexes Created**:
- Bulk operation performance indexes
- Email template active lookup indexes
- Application grid filtering indexes
- JSONB search optimization indexes

### 4. Seed Data Requirements

**Email Templates**:
- 6 default templates with professional content
- Template variables properly defined
- Active status set for all default templates

**Configuration Data**:
- Default bulk operation thresholds (7 days for reminders, 14 days for on-hold)
- System user for seed data creation

## Backend Developer Handoff Requirements

### 1. Entity Models to Create

**New Entities**:
```csharp
// VettingEmailTemplate.cs
public class VettingEmailTemplate
{
    public Guid Id { get; set; } // Simple property, no initializer
    public EmailTemplateType TemplateType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public Guid UpdatedBy { get; set; }
    public int Version { get; set; } = 1;
    // Navigation properties...
}

// VettingBulkOperation.cs - Similar pattern
// VettingBulkOperationItem.cs - Similar pattern
// VettingBulkOperationLog.cs - Similar pattern
```

**Enhanced Entities**:
```csharp
// Add to VettingApplicationNote.cs
public bool IsAutomatic { get; set; } = false;
public string NoteType { get; set; } = "Manual";
```

### 2. Enum Definitions

**New Enums**:
```csharp
public enum EmailTemplateType
{
    ApplicationReceived = 1,
    InterviewApproved = 2,
    ApplicationApproved = 3,
    ApplicationOnHold = 4,
    ApplicationDenied = 5,
    InterviewReminder = 6
}

public enum BulkOperationType
{
    SendReminderEmails = 1,
    ChangeStatusToOnHold = 2
}

public enum BulkOperationStatus
{
    Running = 1,
    Completed = 2,
    Failed = 3
}

public enum NoteType
{
    Manual,
    StatusChange,
    BulkOperation,
    System,
    EmailSent,
    EmailFailed
}
```

**Enhanced Enums**:
```csharp
public enum ApplicationStatus
{
    // Existing values...
    InterviewApproved = 5, // NEW
    OnHold = 12 // NEW
}
```

### 3. Migration Requirements

**Migration Commands**:
```bash
# Create new migration
dotnet ef migrations add VettingSystemEmailTemplatesAndBulkOperations --project apps/api

# Apply migration to database
dotnet ef database update --project apps/api
```

**Migration Validation**:
- Verify all new tables created with proper constraints
- Verify all indexes created successfully
- Verify seed data inserted correctly
- Verify existing data unaffected

### 4. Service Layer Requirements

**New Services Needed**:
- `VettingEmailTemplateService` - Template CRUD operations
- `VettingBulkOperationService` - Bulk operation execution
- Enhanced `VettingApplicationService` - Single application rule enforcement

**Integration Points**:
- SendGrid email service integration for template rendering
- Background job service for bulk operation processing
- Authentication service for admin role verification

## Testing Requirements

### 1. Database Testing

**Unit Tests**:
- Entity configuration validation
- Constraint enforcement testing
- Index performance validation
- Migration up/down testing

**Integration Tests**:
- Single application constraint enforcement
- Bulk operation transaction handling
- Email template rendering with variables
- Performance testing with realistic data volumes

### 2. Critical Test Scenarios

**Business Rules Testing**:
- One application per user enforcement
- Valid status transitions only
- Automatic note creation on status changes
- Bulk operation eligibility filtering

**Performance Testing**:
- Bulk operations with 100+ applications
- Template queries under load
- Application grid with filtering
- Concurrent admin operations

## Risk Mitigation

### 1. Critical Risks Identified

**Database Migration Risk** (HIGH):
- **Risk**: Complex schema changes could fail in production
- **Mitigation**: Phased migration approach with rollback procedures
- **Validation**: Full testing on staging with production-like data

**Performance Risk** (MEDIUM):
- **Risk**: New indexes could impact write performance
- **Mitigation**: Strategic index design with monitoring
- **Validation**: Performance testing with realistic load

**Data Consistency Risk** (LOW):
- **Risk**: Single application constraint could cause application failures
- **Mitigation**: Graceful error handling with clear user messages
- **Validation**: Thorough testing of edge cases

### 2. Success Criteria

**Technical Success**:
- All migrations apply without errors
- All new entities properly mapped in EF Core
- All indexes created and performing optimally
- All constraints enforcing business rules correctly

**Performance Success**:
- Bulk operations complete within 10 seconds for 100 items
- Template queries respond within 500ms
- Application grid loads within 2 seconds
- No degradation in existing functionality performance

**Business Success**:
- Single application rule enforced at database level
- Email template system operational for admins
- Bulk operations save significant admin time
- Enhanced notes provide better audit trails

## Documentation References

### Primary Documents
- **Database Design**: `/docs/functional-areas/vetting-system/new-work/2025-09-22-complete-implementation/design/database-design.md`
- **Business Requirements**: `/docs/functional-areas/vetting-system/new-work/2025-09-22-complete-implementation/requirements/business-requirements.md`
- **Functional Specification**: `/docs/functional-areas/vetting-system/new-work/2025-09-22-complete-implementation/requirements/functional-specification.md`

### Standards References
- **Entity Framework Patterns**: `/docs/standards-processes/development-standards/entity-framework-patterns.md`
- **Database Developer Lessons**: `/docs/lessons-learned/database-designer-lessons-learned.md`
- **Backend Lessons**: `/docs/lessons-learned/backend-developer-lessons-learned.md`

### Existing Implementation
- **Vetting Entities**: `/apps/api/Features/Vetting/Entities/`
- **Entity Configurations**: `/apps/api/Features/Vetting/Entities/Configuration/`

## Next Steps for Backend Developer

### 1. Immediate Actions
1. **Read handoff documents** and understand requirements
2. **Review existing vetting entities** to understand current patterns
3. **Create new entity models** following lessons learned patterns
4. **Create EF Core configurations** using provided specifications
5. **Generate and test migration** in development environment

### 2. Implementation Sequence
1. **Entity Models** → **EF Configurations** → **Migration** → **Services** → **API Endpoints**
2. **Focus on single application constraint first** (critical business rule)
3. **Implement email template system second** (foundational for notifications)
4. **Add bulk operations framework last** (most complex feature)

### 3. Integration Points
- Coordinate with frontend developer on DTO generation
- Coordinate with test developer on test data requirements
- Validate email service integration early
- Confirm authentication service integration patterns

---

**Handoff Complete**: Database schema design ready for backend implementation
**Confidence Level**: High - All requirements addressed with proven patterns
**Estimated Implementation**: 3-5 days for experienced backend developer
**Next Phase**: Backend service and API development