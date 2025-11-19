# Database Designer Handoff - Vetted Member Import
**Date**: 2025-11-18
**Phase**: Phase 1A - Database Schema Review
**Feature**: One-Time Vetted Member Import Tool

## 🎯 CRITICAL TASKS

1. **Review Existing Schemas**: ApplicationUser, VettingApplication, VettingAuditLog ✅ COMPLETE
2. **Verify Import Compatibility**: Ensure schemas can support historical data import ✅ COMPLETE
3. **Document Constraints**: Any database constraints that affect import process ✅ COMPLETE
4. **Migration Analysis**: Determine if any schema changes needed ✅ COMPLETE

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| Orchestrator Handoff | `/docs/functional-areas/member-import/handoffs/orchestrator-2025-11-18-handoff.md` | Requirements Summary |

## 🚨 KNOWN CONSIDERATIONS

1. **EmailVerified Field**: Must support setting to false for imported users ✅ VERIFIED
2. **VettingStatus**: Must support setting to Approved (3) directly ✅ VERIFIED
3. **Historical Data**: VettingApplication and VettingAuditLog need to support import ✅ VERIFIED

## ✅ VALIDATION CHECKLIST

- [x] ApplicationUser schema supports EmailConfirmed = false
- [x] VettingApplication schema supports historical data
- [x] VettingAuditLog schema supports note imports
- [x] No constraints preventing direct vetting status assignment
- [x] Document any required schema changes

## 📝 DELIVERABLES

1. ✅ Schema review document: `/docs/functional-areas/member-import/database-schema-review.md`
2. ✅ List of any migration changes needed: NONE REQUIRED
3. ✅ Constraint documentation: Detailed in schema review
4. ✅ Handoff to backend-developer for implementation

## 🎉 FINDINGS SUMMARY

### Schema Compatibility: ✅ FULLY COMPATIBLE

**No database migrations needed**. The current schema fully supports the import requirements.

### Critical Findings for Backend Developer

1. **Duplicate Detection**: MUST check BOTH email AND scene name (both have unique constraints)
2. **Transaction Per User**: Recommended for atomicity and rollback capability
3. **User Lookup**: Pre-load admin users for VettingAuditLog.PerformedBy (RESTRICT constraint requires valid user)
4. **Date Parsing**: Convert all dates to UTC before storing in PostgreSQL timestamptz columns
5. **Foreign Key Constraints**:
   - `VettingApplication.UserId` → CASCADE DELETE
   - `VettingAuditLog.PerformedBy` → RESTRICT DELETE (user cannot be deleted if referenced in audit log)

### Database Constraints to Handle

**UNIQUE Constraints** (will throw exceptions on duplicates):
- `Users.Email` (from ASP.NET Identity)
- `Users.SceneName`
- `VettingApplications.UserId` (one application per user)

**Required Fields** (cannot be null):
- `ApplicationUser`: Email, SceneName, VettingStatus, CreatedAt, UpdatedAt, Role
- `VettingApplication`: SceneName, Email, WorkflowStatus, SubmittedAt
- `VettingAuditLog`: ApplicationId, Action, PerformedBy, PerformedAt

### Recommended Import Flow

1. Pre-load existing emails, scene names, admin users into memory
2. For each Google Sheet row:
   - Check duplicates (skip if exists)
   - Begin transaction
   - Create ApplicationUser (EmailConfirmed=false, VettingStatus=3)
   - Create VettingApplication (WorkflowStatus=3)
   - Parse notes and create VettingAuditLog entries
   - Commit transaction
   - Log success/failure
3. Post-import validation queries

### Performance Optimizations

- Batch duplicate detection (2 queries total vs 280 individual queries)
- Pre-load admin users for audit log lookup
- Use connection pooling
- Use AsNoTracking() for read-only queries

---

**Status**: ✅ COMPLETE - Phase 1A finished, schema review delivered
**Next Agent**: backend-developer (import tool implementation - Phase 1B)
**Deliverable**: `/docs/functional-areas/member-import/database-schema-review.md`
