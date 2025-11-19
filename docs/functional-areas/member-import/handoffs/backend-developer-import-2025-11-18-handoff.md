# Backend Developer Handoff - Import Tool
**Date**: 2025-11-18
**Phase**: Phase 1B - Console Application Development
**Feature**: One-Time Vetted Member Import Tool

## 🎯 CRITICAL TASKS

1. **Console Application**: Create at /tools/VettedMemberImport/
2. **Google Sheet Integration**: Read "Accepted" tab via MCP tool
3. **Import Logic**: Create users with VettingStatus = Approved
4. **Connection String Support**: Local, Staging, Production
5. **Dry-Run Mode**: Test without writing to database
6. **Error Handling**: Skip duplicates, log all errors
7. **Documentation**: Usage instructions and deployment guide

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| Orchestrator Handoff | `/docs/functional-areas/member-import/handoffs/orchestrator-2025-11-18-handoff.md` | Import Tool Requirements |
| Database Designer Handoff | `/docs/functional-areas/member-import/handoffs/database-designer-2025-11-18-handoff.md` | Schema constraints |

## 🚨 KNOWN REQUIREMENTS

1. **EmailVerified = false**: All imported users require password reset
2. **Duplicate Detection**: Skip existing email addresses
3. **VettingApplication**: Create historical records
4. **VettingAuditLog**: Import notes as audit entries
5. **140+ Users**: Must handle bulk import efficiently

## ✅ VALIDATION CHECKLIST

- [x] Console app builds and runs
- [x] Can read CSV data (via Google Sheet export)
- [x] Dry-run mode works without database changes
- [x] Connection strings configurable (Local, Staging, Production)
- [x] Duplicate detection prevents errors
- [x] Error logging comprehensive
- [x] Documentation complete

## 📝 DELIVERABLES

1. Console application code
2. Google Sheet integration
3. Import logic implementation
4. Dry-run mode
5. Error reporting
6. Usage documentation
7. Deployment guide

---

## ✅ IMPLEMENTATION COMPLETE

**Implementation Date**: 2025-11-18
**Status**: COMPLETE
**Next Agent**: test-developer (Phase 1C - import tests)

### Deliverables

1. **Console Application**: `/tools/VettedMemberImport/VettedMemberImport/`
   - `Program.cs` - Entry point with CLI handling
   - `appsettings.json` - Local configuration
   - `appsettings.Staging.json` - Staging configuration template
   - `appsettings.Production.json` - Production configuration template

2. **Services**:
   - `CsvReader.cs` - CSV parsing with CsvHelper
   - `DateParser.cs` - Date format handling (7/11, 07/11/22, etc.)
   - `UserImporter.cs` - Import orchestration and business logic

3. **Data Layer**:
   - `ApplicationDbContext.cs` - EF Core database context
   - `ApplicationUser.cs` - User entity model
   - `VettingApplication.cs` - Vetting application entity model
   - `VettingAuditLog.cs` - Audit log entity model

4. **Models**:
   - `CsvRow.cs` - CSV row mapping model
   - `ImportSummary.cs` - Import result tracking model

5. **Documentation**: `/tools/VettedMemberImport/README.md`
   - Complete usage instructions
   - Testing workflow
   - Configuration guide
   - Troubleshooting section

### Implementation Details

**CSV Parsing**:
- Uses CsvHelper library for robust CSV parsing
- Handles column name variations
- Supports multiple date formats (M/d, MM/dd, M/d/yy, etc.)
- Trims whitespace and handles missing fields gracefully

**Duplicate Detection**:
- Checks email address (case-insensitive)
- Checks scene name (case-insensitive)
- Skips duplicates with warning log

**Date Parsing Logic**:
- Defaults to 2022 for dates without years
- Handles 2-digit years (22 → 2022)
- Infers decision dates from notes or uses +30 days from submission

**Security**:
- Generates random BCrypt password hashes
- Creates email verification tokens
- Sets EmailVerified = false (requires password reset)
- Masks passwords in connection string logging

**Audit Log Parsing**:
- Extracts dates and actions from notes
- Identifies performer names (Chad, Georgia, Cass, Samantha, Vento)
- Creates structured audit log entries
- Fallback to default admin if no names found

**Error Handling**:
- Validates required fields (email, scene name)
- Skips rows with errors and logs them
- Provides detailed error messages with row numbers
- Returns non-zero exit code if errors occurred

**Dry-Run Mode**:
- Logs what would be created without database writes
- Tests CSV parsing and validation logic
- Verifies database connectivity
- Shows import summary without changes

### Build Verification

```bash
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport
dotnet build --no-restore
# Result: Build succeeded. 0 Warning(s), 0 Error(s)
```

### Usage Examples

```bash
# Dry run (local)
dotnet run -- --input=/path/to/vetted-members.csv --dry-run

# Actual import (local)
dotnet run -- --input=/path/to/vetted-members.csv

# Import to staging
dotnet run -- --input=/path/to/vetted-members.csv --environment=Staging

# Import to production
dotnet run -- --input=/path/to/vetted-members.csv --environment=Production
```

### Testing Approach for test-developer

The console application is designed to be testable:
1. **Separation of concerns**: CSV reading, parsing, and database operations are in separate services
2. **Dependency injection**: All services use constructor injection
3. **Async operations**: All database operations are async with CancellationToken support
4. **Error handling**: Try-catch blocks with detailed logging
5. **Dry-run mode**: Tests can verify logic without database writes

**Suggested Tests**:
- CSV parsing with various date formats
- Duplicate detection logic
- Audit log parsing from notes
- Error handling for missing required fields
- Date inference logic

### Known Limitations

1. **Manual CSV Export**: User must manually export Google Sheet to CSV (MCP tools not available in console apps)
2. **Admin Lookup**: Performer names in audit logs matched against existing admins in database (may create placeholder if none found)
3. **Default Values**: ExperienceLevel=2 (Intermediate), YearsExperience=1 used as defaults
4. **No Rollback**: If import fails partway, manually delete partial records or reset database
