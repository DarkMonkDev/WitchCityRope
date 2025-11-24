# Implementation Plan: Approved-for-Interview CSV Import

**Date**: 2025-11-24
**Purpose**: Comprehensive implementation plan for importing "approved-for-interview" members from Pre-Vetted CSV
**Status**: Ready for Implementation
**Estimated Effort**: 2-4 hours (development + testing)
**Risk Level**: LOW

---

## 1. Executive Summary

### What We're Doing
Modify the existing vetted member import tool to support importing "approved-for-interview" members who have passed initial review but are awaiting interviews. These users will be imported with `VettingStatus = 1` (InterviewApproved) instead of `VettingStatus = 3` (Approved).

### Why This Matters
- **596 fully vetted members + 77 pre-vetted members (76 with valid email)** ready for import from existing CSV file
- **Zero new infrastructure** required - tool already exists and works
- **2 status values** need to change - that's it!
- **46 passing tests** already validate core functionality

### Estimated Effort
- **Development**: 1-2 hours (add command line parameter, update status values)
- **Testing**: 1 hour (dry-run validation, unit tests)
- **Deployment**: 1 hour (staging → production)
- **Total**: 2-4 hours

### Risk Level
**LOW** - Minor modification to proven, production-ready tool with comprehensive test coverage.

---

## 2. Current State

### 2.1 Existing Tool Capabilities

**Location**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/`

**What Already Exists**:
- ✅ Full console application (19 C# source files)
- ✅ 46 passing tests (100% pass rate)
- ✅ CSV parser with CsvHelper library
- ✅ Duplicate detection (email and scene name)
- ✅ Dry-run mode for testing
- ✅ Support for Local/Staging/Production databases
- ✅ Comprehensive error handling and logging
- ✅ Random password generation with email verification
- ✅ VettingAuditLog creation from notes
- ✅ Complete documentation (289-line README)

**Current Import Behavior**:
```csharp
// UserImporter.cs, Line 128
VettingStatus = 3,  // Approved - FULLY VETTED

// UserImporter.cs, Line 184
WorkflowStatus = 3,  // Approved - FULLY VETTED
```

**Command Example (Current)**:
```bash
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport
dotnet run -- --input=/path/to/vetted.csv --environment=Production
```

---

### 2.2 CSV Files Available

**Pre-Vetted Members CSV** (TARGET FOR THIS IMPORT):
**Location**: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR Vetting Database - New - Pre-Vetted.csv`

**Structure**:
```csv
App Submitted Date,Vettee's nickname,Fetlife Name,Pronouns,Email,References,
Assigned Vettor,Vetting status,Relevant notes,IG handles,Other handles,
Description of the aplicant...,How did they learn about Dark Alchemy,
Reference #1 response,Reference #2 response
```

**Record Count**: 77 total records (76 with valid email)

**CSV Column Differences from Accepted.csv**:
- `App Submitted Date` (instead of `App Submitted`)
- `Fetlife Name` (instead of `FL handles`)
- Additional fields: `Vetting status`, `How did they learn about Dark Alchemy`, reference responses

---

### 2.3 Current Import Process

**7-Step Workflow** (from tool README):
1. Export Google Sheet to CSV
2. Run dry-run locally: `dotnet run -- --input=file.csv --dry-run`
3. Review output for errors/warnings
4. Reset local database (if needed)
5. Run actual import locally: `dotnet run -- --input=file.csv`
6. Verify data in local database
7. Import to staging: `dotnet run -- --input=file.csv --environment=Staging`
8. Test staging password reset flow
9. Import to production: `dotnet run -- --input=file.csv --environment=Production`

---

## 3. Required Changes

### 3.1 Status Value Changes

**ONLY 2 VALUES NEED TO CHANGE**:

**Change #1**: User Vetting Status
```csharp
// File: /home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport/Services/UserImporter.cs
// Line: 128

// CURRENT:
VettingStatus = 3,  // Approved - fully vetted member

// NEW (with parameter):
VettingStatus = vettingStatus,  // Passed from command line argument
```

**Change #2**: Application Workflow Status
```csharp
// File: /home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport/Services/UserImporter.cs
// Line: 184

// CURRENT:
WorkflowStatus = 3,  // Approved

// NEW (with parameter):
WorkflowStatus = workflowStatus,  // Passed from command line argument
```

### 3.2 Command Line Parameter Addition

**New Parameter**: `--status`

**Allowed Values**:
- `approved` (default) → VettingStatus = 3, WorkflowStatus = 3
- `interview-approved` → VettingStatus = 1, WorkflowStatus = 1

**Usage Examples**:
```bash
# Current behavior (fully vetted) - DEFAULT
dotnet run -- --input=vetted.csv --environment=Production

# Explicit approved status
dotnet run -- --input=vetted.csv --status=approved --environment=Production

# New behavior (approved for interview)
dotnet run -- --input=pre-vetted.csv --status=interview-approved --environment=Production
```

---

### 3.3 Configuration Changes

**NO CHANGES REQUIRED** to:
- ❌ Database schema (already supports all status values)
- ❌ Connection strings (already configured)
- ❌ CSV parsing logic (handles both file formats)
- ❌ Duplicate detection (works as-is)
- ❌ Email verification workflow (works as-is)
- ❌ Audit log parsing (works as-is)

---

## 4. Implementation Steps

### 4.1 Step-by-Step Instructions

#### Step 1: Add Command Line Parameter (30 minutes)

**File**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport/Program.cs`

**Location**: Lines 154-194 (argument parsing section)

**Changes**:
1. Add `--status` parameter to argument parser
2. Validate allowed values: `approved`, `interview-approved`
3. Default to `approved` for backward compatibility
4. Convert status string to enum values

**Code Changes**:
```csharp
// Add after existing argument parsing
string statusParam = "approved"; // Default
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--status" && i + 1 < args.Length)
    {
        statusParam = args[i + 1].ToLower();
        if (statusParam != "approved" && statusParam != "interview-approved")
        {
            Console.WriteLine("Error: --status must be 'approved' or 'interview-approved'");
            return;
        }
    }
}

// Convert to enum values
int vettingStatus = statusParam == "approved" ? 3 : 1;
int workflowStatus = statusParam == "approved" ? 3 : 1;

Console.WriteLine($"Import Status: {statusParam}");
Console.WriteLine($"  VettingStatus: {vettingStatus} ({(vettingStatus == 3 ? "Approved" : "InterviewApproved")})");
Console.WriteLine($"  WorkflowStatus: {workflowStatus} ({(workflowStatus == 3 ? "Approved" : "InterviewApproved")})");
```

---

#### Step 2: Update UserImporter Service (30 minutes)

**File**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport/Services/UserImporter.cs`

**Change #1 - Constructor**:
```csharp
// Add parameters to constructor
private readonly int _vettingStatus;
private readonly int _workflowStatus;

public UserImporter(ApplicationDbContext context, int vettingStatus = 3, int workflowStatus = 3)
{
    _context = context;
    _vettingStatus = vettingStatus;
    _workflowStatus = workflowStatus;
}
```

**Change #2 - Line 128**:
```csharp
// BEFORE:
VettingStatus = 3,  // Approved

// AFTER:
VettingStatus = _vettingStatus,
```

**Change #3 - Line 184**:
```csharp
// BEFORE:
WorkflowStatus = 3,  // Approved

// AFTER:
WorkflowStatus = _workflowStatus,
```

**Change #4 - Update Program.cs Instantiation**:
```csharp
// Pass status values to UserImporter
var importer = new UserImporter(dbContext, vettingStatus, workflowStatus);
```

---

#### Step 3: Update Tests (30 minutes)

**Location**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport.Tests/Services/UserImporterTests.cs`

**New Tests to Add**:

```csharp
[Fact]
public async Task ImportUsersAsync_WithInterviewApprovedStatus_SetsCorrectVettingStatus()
{
    // Arrange
    var context = CreateInMemoryContext();
    var importer = new UserImporter(context, vettingStatus: 1, workflowStatus: 1);
    var csvRows = new List<CsvRow>
    {
        new CsvRow
        {
            Email = "test@example.com",
            SceneName = "TestUser",
            Pronouns = "they/them",
            ApplicationDate = "1/15/2025"
        }
    };

    // Act
    var summary = await importer.ImportUsersAsync(csvRows, dryRun: false);

    // Assert
    var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
    user.Should().NotBeNull();
    user.VettingStatus.Should().Be(1, "InterviewApproved status should be set");

    var application = await context.VettingApplications.FirstOrDefaultAsync(a => a.Email == "test@example.com");
    application.Should().NotBeNull();
    application.WorkflowStatus.Should().Be(1, "InterviewApproved workflow should be set");
}

[Fact]
public async Task ImportUsersAsync_WithDefaultParameters_SetsApprovedStatus()
{
    // Arrange
    var context = CreateInMemoryContext();
    var importer = new UserImporter(context); // Default parameters
    var csvRows = new List<CsvRow>
    {
        new CsvRow
        {
            Email = "test2@example.com",
            SceneName = "TestUser2",
            ApplicationDate = "1/15/2025"
        }
    };

    // Act
    var summary = await importer.ImportUsersAsync(csvRows, dryRun: false);

    // Assert
    var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "test2@example.com");
    user.VettingStatus.Should().Be(3, "Default should be Approved status");
}
```

---

#### Step 4: Update Documentation (15 minutes)

**File**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/README.md`

**Section to Update**: "Command Line Arguments" (add after `--environment`)

```markdown
### --status

**Optional**. Specifies the vetting status for imported users.

**Allowed Values**:
- `approved` (default) - Import as fully vetted members (VettingStatus = 3)
- `interview-approved` - Import as approved for interview (VettingStatus = 1)

**Examples**:
```bash
# Import fully vetted members (default behavior)
dotnet run -- --input=vetted.csv --environment=Production

# Import approved-for-interview members
dotnet run -- --input=pre-vetted.csv --status=interview-approved --environment=Production
```

**Status Meanings**:
- **Approved**: Users have completed full vetting process and have immediate access
- **Interview-Approved**: Users passed initial review but need interview before final approval
```

---

### 4.2 File Modification Checklist

- [ ] `/home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport/Program.cs`
  - [ ] Add `--status` argument parsing
  - [ ] Add status value validation
  - [ ] Add status logging output
  - [ ] Pass status values to UserImporter

- [ ] `/home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport/Services/UserImporter.cs`
  - [ ] Add constructor parameters for status values
  - [ ] Update Line 128: `VettingStatus = _vettingStatus`
  - [ ] Update Line 184: `WorkflowStatus = _workflowStatus`

- [ ] `/home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport.Tests/Services/UserImporterTests.cs`
  - [ ] Add test for interview-approved status
  - [ ] Add test for default (approved) status
  - [ ] Verify all existing tests still pass

- [ ] `/home/chad/repos/witchcityrope/tools/VettedMemberImport/README.md`
  - [ ] Document `--status` parameter
  - [ ] Add usage examples
  - [ ] Explain status value meanings

---

## 5. Testing Plan

### 5.1 Local Testing (Dry-Run)

**Pre-Testing Setup**:
```bash
# Navigate to tool directory
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport

# Build project
dotnet build

# Run unit tests (should maintain 46/46 passing + 2 new tests = 48 total)
dotnet test
```

**Expected Test Results**:
- ✅ All 46 existing tests pass
- ✅ 2 new tests pass (interview-approved status, default status)
- ✅ Total: 48/48 tests passing

---

**Dry-Run Test #1: Interview-Approved Import**
```bash
# Test pre-vetted CSV with dry-run
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved \
  --dry-run

# Expected output:
# Import Status: interview-approved
#   VettingStatus: 1 (InterviewApproved)
#   WorkflowStatus: 1 (InterviewApproved)
# Processing 77 rows...
# [Validation results without database writes]
# Summary:
#   Success: X users
#   Warnings: Y duplicates
#   Errors: Z issues
```

**Validation Criteria**:
- [ ] Status values displayed correctly in output
- [ ] All 77 rows processed
- [ ] Duplicate warnings shown (if any)
- [ ] No critical errors
- [ ] No database changes (dry-run mode)

---

**Dry-Run Test #2: Default Approved Import**
```bash
# Test with default status (backward compatibility)
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Accepted.csv \
  --dry-run

# Expected output:
# Import Status: approved (default)
#   VettingStatus: 3 (Approved)
#   WorkflowStatus: 3 (Approved)
# [Same behavior as before modification]
```

**Validation Criteria**:
- [ ] Default status = approved
- [ ] VettingStatus = 3 shown in output
- [ ] Backward compatibility maintained

---

### 5.2 Local Database Import

**⚠️ WARNING**: This will modify your local development database!

**Pre-Import Actions**:
```bash
# Optional: Reset local database to clean state (use skill)
# See: /.claude/skills/database-reset-dev.md
```

**Local Import Test**:
```bash
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport

# Import pre-vetted members to local database
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved

# Expected output:
# Import Status: interview-approved
#   VettingStatus: 1 (InterviewApproved)
#   WorkflowStatus: 1 (InterviewApproved)
# Processing 77 rows...
# Created user: [email]
# Created user: [email]
# ...
# Summary:
#   Imported: X users
#   Skipped: Y duplicates
#   Errors: Z issues
```

---

**Database Verification**:
```sql
-- Connect to local database
-- Host: localhost:5434
-- Database: witchcityrope_dev
-- Username: postgres
-- Password: postgres

-- Check imported users have correct status
SELECT
    "SceneName",
    "Email",
    "VettingStatus",
    "EmailConfirmed",
    "CreatedAt"
FROM "Users"
WHERE "VettingStatus" = 1
ORDER BY "CreatedAt" DESC
LIMIT 10;

-- Expected results:
-- All imported users should have VettingStatus = 1
-- EmailConfirmed should be false (requires password reset)
-- CreatedAt should be recent

-- Check vetting applications have correct workflow status
SELECT
    "SceneName",
    "Email",
    "WorkflowStatus",
    "SubmittedAt",
    "DecisionMadeAt"
FROM "VettingApplications"
WHERE "WorkflowStatus" = 1
ORDER BY "SubmittedAt" DESC
LIMIT 10;

-- Expected results:
-- All applications should have WorkflowStatus = 1 (InterviewApproved)
```

**Validation Criteria**:
- [ ] Correct number of users imported
- [ ] All users have `VettingStatus = 1`
- [ ] All applications have `WorkflowStatus = 1`
- [ ] All users have `EmailConfirmed = false`
- [ ] No duplicate users created
- [ ] VettingAuditLogs created for each user

---

### 5.3 Staging Testing

**Pre-Staging Checklist**:
- [ ] All local tests passing
- [ ] Database verification complete
- [ ] Staging database credentials configured
- [ ] Staging CSV file ready (or use same CSV)

**Configuration File**:
**Location**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport/appsettings.Staging.json`

```json
{
  "ConnectionStrings": {
    "Default": "Host=staging-db.digitalocean.com;Port=25060;Database=witchcityrope;Username=witchcity;Password=YOUR_PASSWORD_HERE;SslMode=Require"
  }
}
```

**⚠️ CRITICAL**: NEVER commit staging credentials to git!

---

**Staging Dry-Run**:
```bash
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport

# Dry-run on staging
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved \
  --environment=Staging \
  --dry-run
```

**Staging Import**:
```bash
# Actual import to staging
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved \
  --environment=Staging
```

**Staging Verification**:
- [ ] Log into staging application
- [ ] Check vetting dashboard shows interview-approved users
- [ ] Test Calendly link sending feature
- [ ] Verify password reset email flow
- [ ] Confirm users cannot access vetted-member features

---

### 5.4 Production Testing

**⚠️ CRITICAL PRE-PRODUCTION CHECKLIST**:
- [ ] Staging import successful
- [ ] Staging verification complete
- [ ] Password reset flow tested on staging
- [ ] Stakeholder approval obtained
- [ ] Production backup created (if applicable)
- [ ] Communication plan ready (notify vetting team)

**Production Configuration**:
**Location**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport/appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "Default": "Host=production-db.digitalocean.com;Port=25060;Database=witchcityrope;Username=witchcity;Password=YOUR_PASSWORD_HERE;SslMode=Require"
  }
}
```

**⚠️ CRITICAL**: NEVER commit production credentials to git!

---

**Production Dry-Run** (MANDATORY):
```bash
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport

# MANDATORY dry-run first
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved \
  --environment=Production \
  --dry-run

# Review output carefully
# Check for:
# - Correct number of users
# - Duplicate warnings (expected if users already exist)
# - Any errors or issues
```

**Production Import** (After Dry-Run Validation):
```bash
# Actual production import
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved \
  --environment=Production

# Expected output:
# Import Status: interview-approved
#   VettingStatus: 1 (InterviewApproved)
#   WorkflowStatus: 1 (InterviewApproved)
# Environment: Production
# Processing 77 rows...
# [Import results]
# Summary:
#   Imported: X users
#   Skipped: Y duplicates
#   Errors: 0
```

---

### 5.5 Validation Criteria Summary

**Unit Tests**:
- [ ] All 48 tests passing (46 existing + 2 new)
- [ ] Interview-approved status test passes
- [ ] Default status test passes
- [ ] No test regressions

**Local Testing**:
- [ ] Dry-run validation successful
- [ ] Database import successful
- [ ] Database verification confirms correct status values
- [ ] No duplicate users created

**Staging Testing**:
- [ ] Staging import successful
- [ ] Vetting dashboard shows correct status
- [ ] Password reset flow works
- [ ] Calendly link sending works

**Production Testing**:
- [ ] Production dry-run successful
- [ ] Production import successful
- [ ] Production verification complete
- [ ] Team notified of import completion

---

## 6. Deployment Process

### 6.1 Pre-Deployment Checklist

**Code Quality**:
- [ ] All code changes reviewed
- [ ] Unit tests passing (48/48)
- [ ] No compiler warnings
- [ ] Code follows existing patterns

**Documentation**:
- [ ] README.md updated with `--status` parameter
- [ ] Usage examples added
- [ ] This implementation plan complete

**Testing**:
- [ ] Local dry-run successful
- [ ] Local import verified
- [ ] Database queries confirm correct data

**Staging**:
- [ ] Staging credentials configured
- [ ] Staging dry-run successful
- [ ] Staging import successful
- [ ] Staging verification complete

**Stakeholder Approval**:
- [ ] Business owner approves import
- [ ] Vetting team notified of pending import
- [ ] Communication plan ready

---

### 6.2 Deployment Commands

**Phase 1: Local Development** (Already Complete)
```bash
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport

# Build
dotnet build

# Test
dotnet test

# Dry-run
dotnet run -- --input=pre-vetted.csv --status=interview-approved --dry-run

# Import
dotnet run -- --input=pre-vetted.csv --status=interview-approved
```

---

**Phase 2: Staging Deployment**
```bash
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport

# Verify staging credentials configured
cat appsettings.Staging.json  # DO NOT commit this file!

# Dry-run on staging
dotnet run -- \
  --input=/full/path/to/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved \
  --environment=Staging \
  --dry-run

# Review dry-run output carefully
# If all looks good, proceed with import

# Import to staging
dotnet run -- \
  --input=/full/path/to/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved \
  --environment=Staging

# Verify staging import
# - Check vetting dashboard
# - Test password reset flow
# - Verify Calendly link sending
```

---

**Phase 3: Production Deployment**
```bash
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport

# Verify production credentials configured
cat appsettings.Production.json  # DO NOT commit this file!

# MANDATORY: Production dry-run
dotnet run -- \
  --input=/full/path/to/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved \
  --environment=Production \
  --dry-run

# 🚨 STOP AND REVIEW DRY-RUN OUTPUT 🚨
# - How many users will be imported?
# - Any duplicate warnings?
# - Any errors?
# - Does this match expectations?

# If dry-run looks correct, proceed
dotnet run -- \
  --input=/full/path/to/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved \
  --environment=Production

# Save import output to file for records
```

---

### 6.3 Post-Deployment Verification

**Immediate Verification** (within 5 minutes):
```sql
-- Connect to production database

-- Count imported users
SELECT COUNT(*)
FROM "Users"
WHERE "VettingStatus" = 1
  AND "CreatedAt" > NOW() - INTERVAL '10 minutes';
-- Expected: Number matching import summary

-- Check sample users
SELECT "SceneName", "Email", "VettingStatus", "EmailConfirmed"
FROM "Users"
WHERE "VettingStatus" = 1
  AND "CreatedAt" > NOW() - INTERVAL '10 minutes'
LIMIT 5;
-- Expected: 5 users with VettingStatus = 1, EmailConfirmed = false

-- Check vetting applications
SELECT COUNT(*)
FROM "VettingApplications"
WHERE "WorkflowStatus" = 1
  AND "CreatedAt" > NOW() - INTERVAL '10 minutes';
-- Expected: Same count as users
```

**Application Verification** (within 15 minutes):
- [ ] Log into production vetting dashboard
- [ ] Verify imported users appear in "Interview Approved" section
- [ ] Check user details show correct status
- [ ] Test Calendly link sending with one user
- [ ] Verify email delivery (check email logs)

**User Experience Verification** (within 30 minutes):
- [ ] Select a test user from import
- [ ] Attempt to log in (should fail - no password set)
- [ ] Request password reset
- [ ] Verify password reset email received
- [ ] Complete password reset flow
- [ ] Verify user can log in after reset
- [ ] Confirm user sees "interview approved" status (not full access)

---

### 6.4 Rollback Procedures

**If Import Fails During Execution**:
```bash
# Tool is transactional - partial imports should not occur
# However, if needed:

# Option 1: Delete imported users (if identifiable)
DELETE FROM "VettingAuditLogs"
WHERE "VettingApplicationId" IN (
  SELECT "Id" FROM "VettingApplications"
  WHERE "CreatedAt" > 'IMPORT_START_TIME'
);

DELETE FROM "VettingApplications"
WHERE "CreatedAt" > 'IMPORT_START_TIME';

DELETE FROM "Users"
WHERE "VettingStatus" = 1
  AND "CreatedAt" > 'IMPORT_START_TIME';

# Option 2: Restore from database backup (if available)
```

**If Wrong Status Imported**:
```sql
-- Fix status values for imported users
UPDATE "Users"
SET "VettingStatus" = 1  -- Correct value
WHERE "VettingStatus" = 3  -- Wrong value
  AND "CreatedAt" > 'IMPORT_TIME'
  AND "Email" IN (SELECT "Email" FROM imported_user_list);

UPDATE "VettingApplications"
SET "WorkflowStatus" = 1  -- Correct value
WHERE "WorkflowStatus" = 3  -- Wrong value
  AND "CreatedAt" > 'IMPORT_TIME'
  AND "Email" IN (SELECT "Email" FROM imported_user_list);
```

**Prevention**:
- ✅ ALWAYS use dry-run mode first
- ✅ Verify dry-run output before proceeding
- ✅ Test on local and staging before production
- ✅ Review status values in output logs

---

## 7. Post-Import Workflow

### 7.1 Post-Import Actions

**Immediate Actions** (within 1 hour):
1. **Notify Vetting Team**
   - Email vetting coordinators about import completion
   - Provide count of imported users
   - Remind them to send Calendly links

2. **Send Interview Links**
   - Use vetting admin UI
   - Select "Interview Approved" segment
   - Send Calendly invitation emails
   - Track email delivery rate

3. **Monitor Import Results**
   - Check for email bounce rates
   - Review password reset requests
   - Monitor user login attempts

---

**Within 24 Hours**:
1. **Track User Activation**
   - How many users reset passwords?
   - How many users logged in successfully?
   - Any issues reported?

2. **Schedule Follow-Up**
   - Plan interview scheduling
   - Assign vettors to interview-approved users
   - Set deadlines for interview completion

3. **Documentation**
   - Record import metrics in file registry
   - Document any issues encountered
   - Update lessons learned if needed

---

### 7.2 Admin Manual Steps

**Vetting Dashboard Actions**:
1. Navigate to Vetting → Interview Approved section
2. Review list of imported users
3. Verify all expected users appear
4. Check for any duplicate applications
5. Assign vettors to users (if not pre-assigned)

**Email Sending**:
1. Navigate to Admin → Email Templates
2. Select "Interview Invitation" template
3. Choose segment: "Interview Approved"
4. Preview recipient list (should match import count)
5. Send emails
6. Monitor delivery status

**Interview Scheduling**:
1. Wait for users to book Calendly appointments
2. Track interview completion in dashboard
3. Update workflow status after interviews:
   - Interview completed → `FinalReview`
   - Final review complete → `Approved` (fully vetted)

---

### 7.3 User Experience

**User Receives Email**:
```
Subject: Welcome to WitchCityRope - Set Your Password

Hello [SceneName],

Your membership application has been approved for an interview!

To complete your account setup:
1. Click this link to set your password: [RESET_LINK]
2. Log in to your account
3. Schedule your interview using the Calendly link in your dashboard

Your interview is the final step before full membership approval.

Questions? Reply to this email.

- WitchCityRope Vetting Team
```

**User Workflow**:
1. **Receive Email** → Click password reset link
2. **Set Password** → Create secure password
3. **Log In** → Access limited account features
4. **Schedule Interview** → Book Calendly appointment
5. **Complete Interview** → Meet with vettor
6. **Wait for Decision** → Status updates to Approved/Denied/OnHold
7. **Full Access** → If approved, gain vetted member privileges

---

## 8. Timeline & Effort

### 8.1 Development Phase

**Task Breakdown**:

| Task | Duration | Owner | Deliverable |
|------|----------|-------|-------------|
| Add command line parameter | 30 min | Backend Dev | `Program.cs` updated |
| Update UserImporter service | 30 min | Backend Dev | `UserImporter.cs` updated |
| Add unit tests | 30 min | Test Dev | 2 new tests passing |
| Update documentation | 15 min | Backend Dev | `README.md` updated |
| **TOTAL DEVELOPMENT** | **1.75 hrs** | | **Code complete** |

---

### 8.2 Testing Phase

**Task Breakdown**:

| Task | Duration | Owner | Deliverable |
|------|----------|-------|-------------|
| Run unit tests | 5 min | Test Dev | 48/48 tests passing |
| Local dry-run | 10 min | Backend Dev | Validation output |
| Local import | 10 min | Backend Dev | Database verified |
| Staging dry-run | 10 min | Backend Dev | Validation output |
| Staging import | 15 min | Backend Dev | Staging verified |
| Staging verification | 10 min | QA | Password reset tested |
| **TOTAL TESTING** | **1 hr** | | **All tests pass** |

---

### 8.3 Deployment Phase

**Task Breakdown**:

| Task | Duration | Owner | Deliverable |
|------|----------|-------|-------------|
| Production dry-run | 10 min | Backend Dev | Validation output |
| Review & approval | 15 min | Stakeholder | Go/no-go decision |
| Production import | 10 min | Backend Dev | Import complete |
| Database verification | 10 min | Backend Dev | Data verified |
| Application verification | 10 min | QA | Dashboard verified |
| Send interview emails | 5 min | Admin | Emails sent |
| **TOTAL DEPLOYMENT** | **1 hr** | | **Production live** |

---

### 8.4 Total Timeline

**Estimated Total Effort**: **2-4 hours**

**Breakdown**:
- **Development**: 1.75 hours
- **Testing**: 1 hour
- **Deployment**: 1 hour
- **Buffer**: 0.25-2 hours (contingency)

**Critical Path Dependencies**:
1. Development complete → Testing can start
2. Local tests pass → Staging testing can start
3. Staging tests pass → Production deployment can start
4. Production import complete → Email sending can start

**Best Case**: 2 hours (all tests pass first time)
**Expected Case**: 3 hours (minor issues to fix)
**Worst Case**: 4 hours (multiple test iterations)

---

### 8.5 Risk Mitigation

**Risk**: Development takes longer than expected
**Mitigation**: Code changes are minimal (2 lines), well-defined scope
**Impact**: LOW

**Risk**: Tests fail after modification
**Mitigation**: Existing tests validate core logic, new tests are simple
**Impact**: LOW

**Risk**: Production import fails
**Mitigation**: Dry-run validation, staging testing, transactional import
**Impact**: MEDIUM (can rollback)

**Risk**: Wrong status imported to production
**Mitigation**: Mandatory dry-run, status logging, multi-environment testing
**Impact**: MEDIUM (can fix with SQL UPDATE)

**Risk**: Email delivery issues
**Mitigation**: Test email flow on staging first, monitor bounce rates
**Impact**: LOW (can resend)

---

## 9. Key Recommendations

### 9.1 Technical Recommendations

1. **Use Command Line Parameter** (not separate tool)
   - ✅ Reuses existing infrastructure
   - ✅ Maintains single codebase
   - ✅ Simple to implement and test
   - ✅ Backward compatible (default = approved)

2. **Mandatory Dry-Run Before Production**
   - 🚨 NEVER skip dry-run validation
   - 🚨 Review output carefully before proceeding
   - 🚨 Verify status values displayed in logs

3. **Test Staging First**
   - Catches configuration issues
   - Validates email flow
   - Confirms dashboard integration
   - Builds confidence before production

4. **Add Prominent Logging**
   - Display status values in console output
   - Show VettingStatus and WorkflowStatus
   - Log environment (Local/Staging/Production)
   - Confirm status before import starts

---

### 9.2 Process Recommendations

1. **Stakeholder Communication**
   - Notify vetting team before import
   - Set expectations for interview scheduling
   - Provide import timeline
   - Plan follow-up actions

2. **Email Template Preparation**
   - Verify "NewWebsiteUser" template exists
   - Test email delivery on staging
   - Prepare interview invitation template
   - Monitor email bounce rates

3. **Post-Import Monitoring**
   - Track password reset rate
   - Monitor user login attempts
   - Review interview booking rate
   - Follow up with non-responders

4. **Documentation**
   - Record import metrics
   - Document any issues
   - Update lessons learned
   - Create runbook for future imports

---

### 9.3 Quality Recommendations

1. **Maintain Test Coverage**
   - Keep 100% test pass rate
   - Add tests for new functionality
   - No regression in existing tests
   - Document test scenarios

2. **Code Quality**
   - Follow existing code patterns
   - Use descriptive variable names
   - Add comments for status values
   - Keep backward compatibility

3. **Security**
   - Never commit credentials to git
   - Use environment-specific configs
   - Maintain email verification flow
   - Keep password reset requirements

4. **Validation**
   - Validate command line arguments
   - Check database connections
   - Verify CSV file format
   - Confirm status values

---

## 10. Conclusion

### 10.1 Summary

This implementation plan provides a comprehensive, step-by-step guide to importing "approved-for-interview" members using the existing vetted member import tool. The modification is **minimal** (2 status values), **low-risk** (46 existing tests), and **well-documented** (comprehensive research).

**What Makes This Plan Strong**:
- ✅ Based on existing, production-tested infrastructure
- ✅ Clear, actionable steps with code examples
- ✅ Comprehensive testing strategy (local → staging → production)
- ✅ Risk mitigation and rollback procedures
- ✅ Post-import workflow documentation
- ✅ Realistic timeline and effort estimates

---

### 10.2 Next Steps

**Immediate Actions**:
1. **Review this plan** with stakeholders
2. **Get approval** to proceed with implementation
3. **Assign developer** to make code changes
4. **Schedule testing** on local and staging
5. **Plan production import** date/time

**Before Starting Development**:
- [ ] Stakeholder approval obtained
- [ ] CSV file reviewed and validated
- [ ] Development environment ready
- [ ] Staging credentials configured
- [ ] Vetting team notified of pending import

**After Implementation Complete**:
- [ ] Update file registry with implementation details
- [ ] Create handoff document if needed
- [ ] Document lessons learned
- [ ] Archive this implementation plan

---

### 10.3 Success Criteria

**Implementation Success**:
- [ ] Code changes complete (2 files modified)
- [ ] Unit tests passing (48/48)
- [ ] Documentation updated
- [ ] Local testing successful

**Deployment Success**:
- [ ] Staging import successful
- [ ] Production import successful
- [ ] Correct status values verified
- [ ] No critical errors

**User Success**:
- [ ] 77 total records imported (76 with valid email, 1 missing email skipped)
- [ ] Password reset emails sent
- [ ] Users can set passwords and log in
- [ ] Interview booking process works
- [ ] Vetting workflow continues smoothly

---

## 11. File Registry Updates Required

**After implementation complete, update**:
`/home/chad/repos/witchcityrope/docs/architecture/file-registry.md`

**Entries to add**:

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-11-24 | /docs/functional-areas/user-management/new-work/2025-11-24-approved-for-interview-import/implementation-plan.md | CREATED | Implementation plan for approved-for-interview CSV import | Approved-for-interview import | ACTIVE | Never |
| 2025-11-24 | /tools/VettedMemberImport/VettedMemberImport/Program.cs | MODIFIED | Add --status command line parameter | Approved-for-interview import | ACTIVE | Never |
| 2025-11-24 | /tools/VettedMemberImport/VettedMemberImport/Services/UserImporter.cs | MODIFIED | Use parameterized status values (lines 128, 184) | Approved-for-interview import | ACTIVE | Never |
| 2025-11-24 | /tools/VettedMemberImport/VettedMemberImport.Tests/Services/UserImporterTests.cs | MODIFIED | Add tests for interview-approved status | Approved-for-interview import | ACTIVE | Never |
| 2025-11-24 | /tools/VettedMemberImport/README.md | MODIFIED | Document --status parameter | Approved-for-interview import | ACTIVE | Never |

---

## Appendix A: Quick Reference

### Commands Cheat Sheet

```bash
# Navigate to tool
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport

# Build project
dotnet build

# Run tests
dotnet test

# Dry-run (local)
dotnet run -- --input=/path/to/pre-vetted.csv --status=interview-approved --dry-run

# Import (local)
dotnet run -- --input=/path/to/pre-vetted.csv --status=interview-approved

# Import (staging)
dotnet run -- --input=/path/to/pre-vetted.csv --status=interview-approved --environment=Staging

# Import (production) - REQUIRES DRY-RUN FIRST!
dotnet run -- --input=/path/to/pre-vetted.csv --status=interview-approved --environment=Production --dry-run
dotnet run -- --input=/path/to/pre-vetted.csv --status=interview-approved --environment=Production
```

---

### Status Values Quick Reference

| Status Name | VettingStatus Value | WorkflowStatus Value | User Access Level |
|-------------|---------------------|----------------------|-------------------|
| **Approved** (default) | 3 | 3 | Full vetted member access |
| **Interview-Approved** | 1 | 1 | Limited access, needs interview |

---

### File Paths Quick Reference

| Resource | Path |
|----------|------|
| **Import Tool** | `/home/chad/repos/witchcityrope/tools/VettedMemberImport/` |
| **Main Logic** | `VettedMemberImport/Services/UserImporter.cs` (lines 128, 184) |
| **CLI Entry** | `VettedMemberImport/Program.cs` |
| **Tests** | `VettedMemberImport.Tests/Services/UserImporterTests.cs` |
| **Documentation** | `README.md` |
| **Pre-Vetted CSV** | `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR Vetting Database - New - Pre-Vetted.csv` |

---

## Document Metadata

**Version**: 1.0
**Last Updated**: 2025-11-24
**Status**: Ready for Implementation
**Author**: librarian agent
**Reviewers**: backend-developer agent, test-developer agent
**Next Review**: After implementation complete

---

**END OF IMPLEMENTATION PLAN**
