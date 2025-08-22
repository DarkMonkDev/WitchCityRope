# Database Indexing Strategy

## Overview

This document outlines the database indexing strategy implemented for the WitchCityRope application to optimize query performance. The indexes have been designed based on analysis of the application's query patterns and data access requirements.

## Core Principles

1. **Foreign Key Indexes**: All foreign key columns have indexes for efficient JOIN operations
2. **Query Pattern Optimization**: Indexes are created based on observed query patterns in the application
3. **Composite Indexes**: Multi-column indexes are used for queries that filter or sort by multiple columns
4. **Covering Indexes**: Some indexes include additional columns to avoid key lookups
5. **Write Performance Balance**: Indexes are carefully chosen to balance read and write performance

## Index Strategy by Table

### Users Table

**Existing Indexes:**
- `PK_Users` - Primary key on Id
- `IX_Users_Email` - Unique index for email lookups
- `IX_Users_SceneName` - Unique index for scene name lookups
- `IX_Users_IsActive` - For filtering active/inactive users
- `IX_Users_CreatedAt` - For sorting by creation date

**New Performance Indexes:**
- `IX_Users_Role` - Frequently filtered by role (Organizer, Administrator, etc.)
- `IX_Users_Role_IsActive` - Composite for role-based queries on active users
- `IX_Users_UpdatedAt` - For audit and recent activity queries

**Rationale:**
- Role-based queries are common for authorization checks
- The composite index helps with queries like "find all active organizers"
- UpdatedAt helps with data synchronization and audit trails

### Events Table

**Existing Indexes:**
- `PK_Events` - Primary key on Id
- `IX_Events_EventType` - For filtering by event type
- `IX_Events_IsPublished` - For showing only published events
- `IX_Events_IsPublished_StartDate` - Composite for published upcoming events
- `IX_Events_StartDate` - For date-based queries

**New Performance Indexes:**
- `IX_Events_EndDate` - For queries involving event end times
- `IX_Events_CreatedAt` - For sorting newest events
- `IX_Events_UpdatedAt` - For recent modifications
- `IX_Events_Location` - For location-based searches and conflict checks
- `IX_Events_IsPublished_EventType_StartDate` - Composite for filtered event listings

**Rationale:**
- Location index supports conflict detection queries
- The triple composite index optimizes the main event listing query
- EndDate index helps with "currently running" event queries

### Registrations Table

**Existing Indexes:**
- `PK_Registrations` - Primary key on Id
- `IX_Registrations_EventId` - Foreign key index
- `IX_Registrations_UserId` - Foreign key index
- `IX_Registrations_UserId_EventId` - Unique constraint preventing double registration
- `IX_Registrations_Status` - For filtering by registration status
- `IX_Registrations_RegisteredAt` - For chronological queries

**New Performance Indexes:**
- `IX_Registrations_ConfirmedAt` - For confirmed registration reports
- `IX_Registrations_CancelledAt` - For cancellation analytics
- `IX_Registrations_UpdatedAt` - For recent changes
- `IX_Registrations_UserId_Status` - Composite for user registration queries
- `IX_Registrations_EventId_Status` - Composite for event attendance queries
- `IX_Registrations_EventId_Status_RegisteredAt` - Covering index for event counts

**Rationale:**
- Status-based composite indexes optimize common queries like "user's confirmed registrations"
- The triple composite serves as a covering index for counting confirmed attendees
- Timestamp indexes support reporting and analytics

### Payments Table

**Existing Indexes:**
- `PK_Payments` - Primary key on Id
- `IX_Payments_RegistrationId` - Unique foreign key index
- `IX_Payments_Status` - For payment status queries
- `IX_Payments_TransactionId` - Unique index for transaction lookup
- `IX_Payments_RefundTransactionId` - For refund tracking
- `IX_Payments_ProcessedAt` - For chronological queries

**New Performance Indexes:**
- `IX_Payments_PaymentMethod` - For payment method analytics
- `IX_Payments_UpdatedAt` - For recent changes
- `IX_Payments_RefundedAt` - For refund reporting
- `IX_Payments_Status_ProcessedAt` - Composite for status-based reports
- `IX_Payments_PaymentMethod_Status_ProcessedAt` - Covering index for reconciliation

**Rationale:**
- Payment method index supports financial reporting by payment type
- Composite indexes optimize common financial queries
- The triple composite serves reconciliation reports without key lookups

### VettingApplications Table

**Existing Indexes:**
- `PK_VettingApplications` - Primary key on Id
- `IX_VettingApplications_ApplicantId` - Foreign key index
- `IX_VettingApplications_Status` - For workflow queries
- `IX_VettingApplications_SubmittedAt` - For chronological processing

**New Performance Indexes:**
- `IX_VettingApplications_ReviewedAt` - For completed application queries
- `IX_VettingApplications_UpdatedAt` - For recent changes
- `IX_VettingApplications_Status_SubmittedAt` - Composite for workflow queue

**Rationale:**
- The composite index optimizes the vetting queue query
- Timestamp indexes support SLA monitoring and reporting

### IncidentReports Table

**Existing Indexes:**
- `PK_IncidentReports` - Primary key on Id
- `IX_IncidentReports_EventId` - Foreign key index
- `IX_IncidentReports_ReporterId` - Foreign key index
- `IX_IncidentReports_Status` - For workflow queries
- `IX_IncidentReports_Severity` - For priority filtering
- `IX_IncidentReports_IsAnonymous` - For anonymous report queries
- `IX_IncidentReports_ReportedAt` - For chronological queries

**New Performance Indexes:**
- `IX_IncidentReports_UpdatedAt` - For recent activity
- `IX_IncidentReports_ResolvedAt` - For resolution reporting
- `IX_IncidentReports_Status_Severity_ReportedAt` - Composite for incident queue
- `IX_IncidentReports_EventId_Status` - Composite for event safety metrics

**Rationale:**
- The triple composite optimizes the incident management dashboard
- Event-status composite helps with event safety scoring
- Resolution timestamp supports SLA reporting

### Supporting Tables

**IncidentActions:**
- Added indexes on PerformedAt, ActionType, and composite on IncidentReportId + PerformedAt
- Supports audit trail queries and action type analytics

**IncidentReviews:**
- Added indexes on ReviewedAt and RecommendedSeverity
- Optimizes review workflow and severity distribution queries

**VettingReviews:**
- Added indexes on ReviewedAt, Recommendation, and composite on VettingApplicationId + ReviewedAt
- Supports reviewer performance metrics and application timeline queries

**EventOrganizers:**
- Existing index on UserId already supports the many-to-many relationship efficiently

## Query Pattern Analysis

### Most Common Query Patterns Optimized:

1. **Event Listings**
   - Filter: IsPublished = true, StartDate >= now, EventType = X
   - Optimized by: `IX_Events_IsPublished_EventType_StartDate`

2. **User Registrations**
   - Filter: UserId = X, Status = 'Confirmed'
   - Optimized by: `IX_Registrations_UserId_Status`

3. **Event Attendance Count**
   - Filter: EventId = X, Status = 'Confirmed'
   - Optimized by: `IX_Registrations_EventId_Status`

4. **Payment Reconciliation**
   - Filter: PaymentMethod = X, Status = Y, ProcessedAt between dates
   - Optimized by: `IX_Payments_PaymentMethod_Status_ProcessedAt`

5. **Location Conflicts**
   - Filter: Location = X, date ranges overlap
   - Optimized by: `IX_Events_Location` with StartDate/EndDate

6. **Vetting Queue**
   - Filter: Status = 'Pending', order by SubmittedAt
   - Optimized by: `IX_VettingApplications_Status_SubmittedAt`

7. **Incident Dashboard**
   - Filter: Status IN ('Open', 'InReview'), order by Severity, ReportedAt
   - Optimized by: `IX_IncidentReports_Status_Severity_ReportedAt`

## Performance Considerations

### Index Maintenance Impact

1. **Write Performance**: The additional indexes will slightly impact INSERT/UPDATE/DELETE operations
2. **Storage**: Estimated 15-20% increase in database size due to indexes
3. **Maintenance**: Indexes should be rebuilt periodically during low-usage periods

### Monitoring Recommendations

1. **Query Performance**: Monitor slow query logs regularly
2. **Index Usage**: Use database tools to verify indexes are being utilized
3. **Fragmentation**: Check index fragmentation monthly and rebuild as needed
4. **Missing Indexes**: Review query plans for missing index suggestions

### Future Optimization Opportunities

1. **Filtered Indexes**: Consider filtered indexes for soft-deleted records
2. **Partial Indexes**: For SQLite, consider partial indexes on large tables
3. **Index Compression**: When migrating to SQL Server/PostgreSQL
4. **Columnstore Indexes**: For analytical queries on historical data

## Implementation Notes

### Migration Execution

1. The migration `20250627123615_AddPerformanceIndexes` adds all performance indexes
2. Index creation is done online where supported to minimize downtime
3. Rollback script is included in the Down() method

### Testing Strategy

1. **Performance Testing**: Measure query performance before/after index creation
2. **Load Testing**: Verify system behavior under concurrent load
3. **Index Effectiveness**: Use EXPLAIN PLAN to verify index usage

### Maintenance Schedule

1. **Weekly**: Review slow query logs
2. **Monthly**: Check index fragmentation levels
3. **Quarterly**: Analyze index usage statistics
4. **Annually**: Review and optimize indexing strategy

## Conclusion

This indexing strategy provides a solid foundation for application performance while maintaining a balance between read and write operations. The indexes are designed to support current query patterns while allowing flexibility for future enhancements. Regular monitoring and maintenance will ensure continued optimal performance as the application scales.