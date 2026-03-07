# VettedMemberImport Tool - Complete Usage Guide
<!-- Date: 2025-11-30 -->
<!-- Version: 2.1 -->
<!-- Owner: Backend Team -->
<!-- Status: Active -->

## Overview

The VettedMemberImport tool is a console application for importing vetted members from Google Sheet exports (CSV) into the WitchCityRope database. It supports two distinct import modes for different stages in the vetting workflow.

**Tool Location**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/`

**Purpose**:
- Import historical vetting data from Google Sheets
- Create user accounts with proper vetting status
- Generate vetting applications and audit logs
- Support both fully-vetted and interview-approved member imports

**Capabilities**:
- CSV parsing with CsvHelper library
- Duplicate detection (email and scene name)
- Dry-run mode for validation
- Multi-environment support (Local/Staging/Production)
- Comprehensive error handling and logging
- Random password generation with email verification workflow

## Prerequisites

### Required Software
- .NET 10.0 SDK or higher
- PostgreSQL database access
- Terminal/command line access

### Required Access
- **Local Development**: PostgreSQL on localhost:5434
- **Staging**: DigitalOcean database credentials
- **Production**: DigitalOcean database credentials (separate instance)

### Required Files
- CSV export from Google Sheet
- Database connection strings configured

## CSV File Types

The tool supports two CSV file types, each with different import purposes:

### Type 1: Accepted Members (Fully Vetted)

**File Name**: `WCR Vetting Database - New - Accepted.csv`
**Location**: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/`

**Purpose**: Import members who have completed the full vetting process

**Import Results**:
- VettingStatus: 3 (Approved)
- WorkflowStatus: 3 (Approved)
- Role: VettedMember
- Access Level: Full vetted-member features

**Record Count**: 596 members

**Use When**: Importing members with completed vetting interviews and approvals

---

### Type 2: Pre-Vetted Members (Approved for Interview)

**File Name**: `WCR Vetting Database - New - Pre-Vetted.csv`
**Location**: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/`

**Purpose**: Import members approved for interview but not yet fully vetted

**Import Results**:
- VettingStatus: 1 (InterviewApproved)
- WorkflowStatus: 1 (InterviewApproved)
- Role: Member
- Access Level: Limited access, awaiting interview

**Post-Import Requirements**:
- Admin must send Calendly interview invitation links
- Members must complete interviews before full approval
- Vetting team must conduct interviews and make final decisions

**Use When**: Importing members who passed initial screening but need interviews

---

### CSV Column Structure

**Accepted.csv Columns**:
```
App Submitted, Vettee's nickname, Fetlife Name, Pronouns, Email,
References, Assigned Vettor, Relevant notes, Description of the applicant...
```

**Pre-Vetted.csv Columns**:
```
App Submitted Date, Vettee's nickname, Fetlife Name, Pronouns, Email,
References, Assigned Vettor, Vetting status, Relevant notes, IG handles,
Other handles, Description of the applicant..., How did they learn about Dark Alchemy,
Reference #1 response, Reference #2 response
```

**Key Differences**:
- Pre-Vetted has additional fields: `Vetting status`, `How did they learn about Dark Alchemy`, reference responses
- Column names may differ slightly (e.g., "App Submitted Date" vs "App Submitted")
- Tool handles both formats automatically

## Import Modes

### Mode 1: Fully Vetted Members

**Command Line Parameter**: `--status=approved` (or omit for default)

**When to Use**:
- Importing from Accepted.csv
- Members have completed full vetting process
- Members should have immediate access to vetted-member features
- Historical imports of approved members

**What It Does**:
1. Creates user accounts with VettingStatus=3 (Approved)
2. Assigns VettedMember role
3. Sets WorkflowStatus=3 (Approved) on vetting applications
4. Generates random password (user must reset via email)
5. Creates audit log entries from notes

**User Permissions After Import**:
- Access to vetted-member events
- Ability to register for workshops
- Full community feature access
- Can view vetted-member content

**Post-Import Workflow**:
1. Users receive "NewWebsiteUser" email
2. Users click password reset link
3. Users set password and email verification completes
4. Users immediately have full access

---

### Mode 2: Interview-Approved Members

**Command Line Parameter**: `--status=interview-approved`

**When to Use**:
- Importing from Pre-Vetted.csv
- Members passed initial screening
- Members need interviews before full approval
- Preparing for interview scheduling phase

**What It Does**:
1. Creates user accounts with VettingStatus=1 (InterviewApproved)
2. Assigns Member role (not VettedMember)
3. Sets WorkflowStatus=1 (InterviewApproved) on vetting applications
4. Generates random password (user must reset via email)
5. Creates audit log entries from notes

**User Permissions After Import**:
- Limited account access
- Cannot register for vetted-member events
- Can view public content
- Awaiting interview completion

**Post-Import Workflow**:
1. Admin reviews imported users in vetting dashboard
2. Admin sends Calendly invitation emails to users
3. Users receive "NewWebsiteUser" email and set passwords
4. Users receive Calendly invitation and schedule interviews
5. Vetting team conducts interviews
6. Admin updates status to Approved (VettingStatus=3) after successful interview
7. Users gain full vetted-member access

## Command Line Reference

### Basic Syntax

```bash
dotnet run -- --input <file> [--status <mode>] [--environment <env>] [--dry-run]
```

**IMPORTANT**: Arguments must use space-separated format (e.g., `--input file.csv`), NOT equals format (e.g., `--input=file.csv`). The tool's command line parser does not support the equals sign format.

### Parameters

#### --input <file> (REQUIRED)

Path to CSV file exported from Google Sheet.

**Examples**:
```bash
# Relative path
--input vetted-members.csv

# Absolute path
--input /home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Accepted.csv

# Path with spaces (use quotes)
--input "/path/with spaces/file.csv"
```

---

#### --status <mode> (OPTIONAL)

Controls vetting status of imported users.

**Values**:
- `approved` - Fully vetted members (default)
- `interview-approved` - Approved for interview

**Default**: `approved` (maintains backward compatibility)

**Examples**:
```bash
# Default (fully vetted)
dotnet run -- --input accepted.csv

# Explicit approved
dotnet run -- --input accepted.csv --status approved

# Interview-approved
dotnet run -- --input pre-vetted.csv --status interview-approved
```

**Status Comparison**:

| Status | VettingStatus | WorkflowStatus | Role | Access Level |
|--------|---------------|----------------|------|--------------|
| `approved` | 3 | 3 | VettedMember | Full access |
| `interview-approved` | 1 | 1 | Member | Limited access |

---

#### --environment <env> (OPTIONAL)

Target environment for import.

**Values**:
- `Development` - Local database (default)
- `Staging` - Staging environment
- `Production` - Production environment

**Default**: `Development`

**Examples**:
```bash
# Local (default)
dotnet run -- --input file.csv

# Staging
dotnet run -- --input file.csv --environment Staging

# Production
dotnet run -- --input file.csv --environment Production
```

**Configuration Files**:
- Development: `appsettings.json`
- Staging: `appsettings.Staging.json`
- Production: `appsettings.Production.json`

**Staging/Production Connection String Setup**:

Before running against Staging or Production, you MUST configure the connection string in the appropriate `appsettings.[Environment].json` file:

```json
{
  "ConnectionStrings": {
    "Default": "Host=<hostname>;Port=25060;Database=<database>;Username=<username>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

**IMPORTANT**:
- Use direct PostgreSQL port **25060**
- For staging: Database name is `witchcityrope_staging`, user is `witchcity_staging`
- For production: Database name is `witchcityrope_production`, user is `witchcity_production`
- SSL Mode and Trust Server Certificate are required for DigitalOcean connections
- Get credentials from DigitalOcean database dashboard or team secrets manager

---

#### --dry-run (OPTIONAL)

Test import without writing to database.

**Purpose**:
- Validate CSV file format
- Check for duplicate records
- Identify errors before actual import
- Review import summary without database changes

**Examples**:
```bash
# Dry-run locally
dotnet run -- --input file.csv --dry-run

# Dry-run with interview-approved status
dotnet run -- --input pre-vetted.csv --status interview-approved --dry-run

# Dry-run on staging
dotnet run -- --input file.csv --environment Staging --dry-run
```

**Output**: Same as actual import but with "Dry Run: True" and no database writes

---

#### --help or -h (OPTIONAL)

Display help message with all available parameters.

```bash
dotnet run -- --help
```

## Step-by-Step Import Procedures

### Procedure 1: Importing Fully Vetted Members

**Purpose**: Import members who completed full vetting process from Accepted.csv

#### Step 1: Preparation

1. **Export CSV from Google Sheet**:
   - Open Google Sheet (Spreadsheet ID: 1HYa3wGFn3My0ehh7IQdF2as0eqe4lQ4Fpjhr7F8svCo)
   - Select "Accepted" sheet
   - File → Download → CSV (.csv)
   - Save as `WCR Vetting Database - New - Accepted.csv`

2. **Navigate to tool directory**:
   ```bash
   cd /home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport
   ```

3. **Build the project** (first time or after code changes):
   ```bash
   dotnet build
   ```

---

#### Step 2: Dry-Run Validation

**Purpose**: Validate CSV before actual import

```bash
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Accepted.csv \
  --dry-run
```

**Review Output**:
- Total records processed
- Success count
- Duplicate warnings
- Error messages
- No database changes made

**Expected Results**:
- All 596 rows processed
- Some duplicates expected (already imported users)
- No critical errors
- Import summary shows what would be imported

**If Errors Found**: Fix CSV file and re-run dry-run until clean

---

#### Step 3: Local Import

**Purpose**: Import to local development database for testing

**Prerequisites**:
- Local Docker containers running (`./dev.sh`)
- Database accessible on localhost:5434

```bash
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Accepted.csv
```

**Expected Output**:
```
=== Vetted Member Import Tool ===

Environment: Development
Input File: [path]
Import Status: approved
  VettingStatus: 3 (Approved)
  WorkflowStatus: 3 (Approved)
Dry Run: False

Database connection successful
Read 596 rows from CSV file

=== IMPORT SUMMARY ===
Total Records: 596
Successful: 140
Skipped (Duplicates): 2
Errors: 0

IMPORT COMPLETE
```

---

#### Step 4: Local Database Verification

**Connect to database**:
```bash
psql -h localhost -p 5434 -U postgres -d witchcityrope_dev
```

**Verify user accounts**:
```sql
SELECT
    "SceneName",
    "Email",
    "VettingStatus",
    "EmailConfirmed",
    "CreatedAt"
FROM "Users"
WHERE "VettingStatus" = 3
  AND "CreatedAt" > NOW() - INTERVAL '1 hour'
LIMIT 10;
```

**Expected Results**:
- VettingStatus = 3 (Approved)
- EmailConfirmed = false (requires password reset)
- CreatedAt shows recent timestamp

**Verify vetting applications**:
```sql
SELECT
    "SceneName",
    "Email",
    "WorkflowStatus",
    "SubmittedAt"
FROM "VettingApplications"
WHERE "WorkflowStatus" = 3
  AND "CreatedAt" > NOW() - INTERVAL '1 hour'
LIMIT 10;
```

**Expected Results**:
- WorkflowStatus = 3 (Approved)
- Matching user count

**Check roles**:
```sql
SELECT
    u."SceneName",
    u."Email",
    r."Name" as "RoleName"
FROM "Users" u
JOIN "UserRoles" ur ON u."Id" = ur."UserId"
JOIN "Roles" r ON ur."RoleId" = r."Id"
WHERE u."VettingStatus" = 3
  AND u."CreatedAt" > NOW() - INTERVAL '1 hour';
```

**Expected Results**: All users have "VettedMember" role

---

#### Step 5: Staging Import

**Prerequisites**:
- Staging database credentials configured in `appsettings.Staging.json`
- Staging environment accessible

**Dry-Run on Staging**:
```bash
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Accepted.csv \
  --environment=Staging \
  --dry-run
```

**Review dry-run output** carefully before proceeding

**Actual Staging Import**:
```bash
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Accepted.csv \
  --environment=Staging
```

---

#### Step 6: Staging Verification

**Test password reset flow**:
1. Select a test user from import
2. Navigate to staging application login page
3. Click "Forgot Password"
4. Enter test user email
5. Verify password reset email received
6. Complete password reset
7. Log in with new password
8. Verify full vetted-member access

**Check vetting dashboard**:
1. Log in as admin
2. Navigate to Vetting Dashboard
3. Verify imported users appear in "Approved" section
4. Check user details show correct status

---

#### Step 7: Production Import

**CRITICAL PRE-PRODUCTION CHECKLIST**:
- [ ] Staging import successful
- [ ] Staging verification complete
- [ ] Password reset flow tested
- [ ] Stakeholder approval obtained
- [ ] Communication plan ready (notify vetting team)

**Production Dry-Run (MANDATORY)**:
```bash
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Accepted.csv \
  --environment=Production \
  --dry-run
```

**STOP AND REVIEW**:
- How many users will be imported?
- Any unexpected duplicates?
- Any errors?
- Does output match expectations?

**Actual Production Import** (after approval):
```bash
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Accepted.csv \
  --environment=Production
```

**Save output** to file for records

---

#### Step 8: Production Verification

**Database verification** (within 5 minutes):
```sql
-- Connect to production database

-- Count imported users
SELECT COUNT(*)
FROM "Users"
WHERE "VettingStatus" = 3
  AND "CreatedAt" > NOW() - INTERVAL '10 minutes';

-- Sample users
SELECT "SceneName", "Email", "VettingStatus", "EmailConfirmed"
FROM "Users"
WHERE "VettingStatus" = 3
  AND "CreatedAt" > NOW() - INTERVAL '10 minutes'
LIMIT 5;
```

**Application verification**:
- Log into production vetting dashboard
- Verify imported users appear
- Check user counts match import summary

**Email notification**:
- Notify vetting team of import completion
- Provide import count and summary

---

### Procedure 2: Importing Interview-Approved Members

**Purpose**: Import members approved for interview from Pre-Vetted.csv

#### Step 1: Preparation

1. **Export CSV from Google Sheet**:
   - Open Google Sheet
   - Select "Pre-Vetted" sheet
   - File → Download → CSV (.csv)
   - Save as `WCR Vetting Database - New - Pre-Vetted.csv`

2. **Navigate to tool directory**:
   ```bash
   cd /home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport
   ```

3. **Build the project**:
   ```bash
   dotnet build
   ```

---

#### Step 2: Dry-Run Validation

```bash
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved \
  --dry-run
```

**Review Output**:
- Verify status shows "interview-approved"
- Check VettingStatus: 1 (InterviewApproved)
- Check WorkflowStatus: 1 (InterviewApproved)
- Review import summary

---

#### Step 3: Local Import

```bash
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved
```

**Expected Output**:
```
Import Status: interview-approved
  VettingStatus: 1 (InterviewApproved)
  WorkflowStatus: 1 (InterviewApproved)
[Import results]
```

---

#### Step 4: Local Database Verification

```sql
-- Verify users have correct status
SELECT
    "SceneName",
    "Email",
    "VettingStatus",
    "EmailConfirmed"
FROM "Users"
WHERE "VettingStatus" = 1
  AND "CreatedAt" > NOW() - INTERVAL '1 hour'
LIMIT 10;
```

**Expected Results**: VettingStatus = 1 (InterviewApproved)

```sql
-- Verify applications have correct workflow status
SELECT
    "SceneName",
    "Email",
    "WorkflowStatus"
FROM "VettingApplications"
WHERE "WorkflowStatus" = 1
  AND "CreatedAt" > NOW() - INTERVAL '1 hour'
LIMIT 10;
```

**Expected Results**: WorkflowStatus = 1 (InterviewApproved)

```sql
-- Verify roles
SELECT
    u."SceneName",
    r."Name" as "RoleName"
FROM "Users" u
JOIN "UserRoles" ur ON u."Id" = ur."UserId"
JOIN "Roles" r ON ur."RoleId" = r."Id"
WHERE u."VettingStatus" = 1
  AND u."CreatedAt" > NOW() - INTERVAL '1 hour';
```

**Expected Results**: All users have "Member" role (NOT "VettedMember")

---

#### Step 5: Staging Import

```bash
# Dry-run
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved \
  --environment=Staging \
  --dry-run

# Actual import
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved \
  --environment=Staging
```

---

#### Step 6: Staging Verification

**Test limited access**:
1. Select test user from import
2. Complete password reset
3. Log in
4. Verify user CANNOT access vetted-member features
5. Verify user sees "interview pending" status

**Test vetting dashboard**:
1. Log in as admin
2. Navigate to Vetting Dashboard
3. Verify users appear in "Interview Approved" section
4. Check Calendly link sending feature

---

#### Step 7: Production Import

**Production Dry-Run**:
```bash
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved \
  --environment=Production \
  --dry-run
```

**Production Import**:
```bash
dotnet run -- \
  --input=/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR\ Vetting\ Database\ -\ New\ -\ Pre-Vetted.csv \
  --status=interview-approved \
  --environment=Production
```

---

#### Step 8: Post-Import Admin Actions

**CRITICAL**: These steps are REQUIRED after interview-approved import

1. **Review imported users**:
   - Log into production admin panel
   - Navigate to Vetting Dashboard → Interview Approved
   - Verify all expected users appear

2. **Send Calendly invitation emails**:
   - Select all interview-approved users
   - Use bulk email feature
   - Send "Interview Invitation" template
   - Include Calendly scheduling link

3. **Monitor interview scheduling**:
   - Track which users schedule interviews
   - Follow up with non-responders after 7 days
   - Coordinate vetting team availability

4. **Conduct interviews**:
   - Complete scheduled interviews
   - Document interview results
   - Make approval decisions

5. **Update user status after interviews**:
   - For approved users: Update VettingStatus to 3 (Approved)
   - Assign VettedMember role
   - Send approval notification
   - For denied users: Update VettingStatus to 4 (Denied)
   - Send denial notification with explanation

## Troubleshooting

### Cannot Connect to Database

**Symptoms**: "Cannot connect to database" error message

**Causes**:
- Incorrect connection string
- Database server not running
- Firewall blocking connection
- Wrong credentials

**Solutions**:
1. Verify connection string in `appsettings.[Environment].json`
2. Test database connectivity:
   ```bash
   psql -h HOST -p PORT -U USERNAME -d DATABASE
   ```
3. For local: Ensure Docker containers running (`./dev.sh`)
4. For staging/production: Check firewall rules and network access
5. Verify credentials are correct

---

### CSV File Not Found

**Symptoms**: "File not found" error

**Causes**:
- Incorrect file path
- File doesn't exist
- Permission issues
- Path with spaces not quoted

**Solutions**:
1. Verify file path is correct
2. Use absolute path: `/full/path/to/file.csv`
3. For paths with spaces, use quotes: `"/path/with spaces/file.csv"`
4. Check file exists: `ls -la /path/to/file.csv`
5. Verify file permissions: `chmod 644 file.csv`

---

### Duplicate Records

**Symptoms**: "Duplicate - Skipped" warnings in output

**Causes**:
- User already exists in database (by email or scene name)
- CSV contains duplicate rows
- Previous import included same users

**Solutions**:
1. Review skipped records in output
2. Verify duplicates are intentional (already imported)
3. For new duplicates: Check CSV for duplicate rows
4. If unintentional: Clean up duplicates in database before re-import
5. Expected for production imports (staging imports may create duplicates)

**Query to check existing users**:
```sql
SELECT "Email", "SceneName", "VettingStatus"
FROM "Users"
WHERE "Email" IN ('email1@example.com', 'email2@example.com');
```

---

### Date Parsing Failures

**Symptoms**: Warning messages about date parsing

**Causes**:
- Unexpected date format in CSV
- Empty date fields
- Invalid date values

**Solutions**:
1. Check "App Submitted" column format
2. Supported formats: `7/11`, `07/11`, `7/11/22`, `07/11/2022`
3. Tool handles most formats automatically
4. Warnings are informational, not blocking
5. If critical: Update CSV to use consistent date format

---

### Wrong Status Imported

**Symptoms**: Users have incorrect VettingStatus in database

**Causes**:
- Forgot `--status` parameter
- Used wrong status value
- Imported wrong CSV file

**Solutions**:
1. Verify console output shows correct status:
   ```
   Import Status: interview-approved
     VettingStatus: 1 (InterviewApproved)
   ```
2. Check `--status` parameter matches intended import
3. For Pre-Vetted.csv, MUST use `--status=interview-approved`
4. For Accepted.csv, omit `--status` or use `--status=approved`

**Fix wrong status in database**:
```sql
-- Update users to interview-approved (if imported as approved by mistake)
UPDATE "Users"
SET "VettingStatus" = 1
WHERE "VettingStatus" = 3
  AND "CreatedAt" > 'IMPORT_TIMESTAMP'
  AND "Email" IN (SELECT "Email" FROM imported_user_list);

UPDATE "VettingApplications"
SET "WorkflowStatus" = 1
WHERE "WorkflowStatus" = 3
  AND "CreatedAt" > 'IMPORT_TIMESTAMP'
  AND "Email" IN (SELECT "Email" FROM imported_user_list);
```

---

### Missing Roles

**Symptoms**: Users imported but don't have roles assigned

**Causes**:
- Role assignment logic failed
- Database constraints violated
- Roles table missing required roles

**Solutions**:
1. Verify roles exist in database:
   ```sql
   SELECT * FROM "Roles";
   ```
2. Expected roles: "Member", "VettedMember", "Admin", "Teacher"
3. Check import logs for role assignment errors
4. Manually assign roles if needed:
   ```sql
   -- Get user and role IDs
   SELECT u."Id", u."SceneName", r."Id", r."Name"
   FROM "Users" u, "Roles" r
   WHERE u."Email" = 'user@example.com'
     AND r."Name" = 'VettedMember';

   -- Assign role
   INSERT INTO "UserRoles" ("UserId", "RoleId")
   VALUES ('user-id', 'role-id');
   ```

## Security Notes

### Password Handling

- **Random password hashes** generated for all imported users
- Users CANNOT log in with these random passwords
- Users MUST reset password via email verification link
- Password reset is the ONLY way to set initial password
- This ensures users control their own passwords

### Email Verification

- All imported users have `EmailConfirmed = false`
- Email verification required before full access
- Password reset flow automatically verifies email
- Prevents unauthorized access to imported accounts

### Connection Strings

**CRITICAL SECURITY RULES**:
- NEVER commit `appsettings.Staging.json` to git
- NEVER commit `appsettings.Production.json` to git
- Add to `.gitignore`:
  ```
  appsettings.Staging.json
  appsettings.Production.json
  ```
- Store credentials in secure password manager
- Rotate database passwords regularly
- Use SSL/TLS for staging/production connections

### Data Privacy

- CSV files contain PII (personally identifiable information)
- Store CSV files securely
- Delete CSV files after successful import
- Don't commit CSV files to version control
- Limit access to import tool to authorized personnel

## Testing

### Unit Tests

**Location**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport.Tests/`

**Run Tests**:
```bash
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport
dotnet test
```

**Expected Results**:
- All 52 tests passing (100% pass rate)
- Tests for both import modes
- Tests for duplicate detection
- Tests for date parsing
- Tests for error handling

**Test Coverage**:
- User creation with correct status values
- VettingApplication creation
- VettingAuditLog parsing
- Duplicate detection logic
- Date format parsing
- CSV validation
- Error handling

### Integration Testing

**Local Testing Workflow**:
1. Reset local database: Use `database-reset-dev` skill
2. Run dry-run: Validate CSV without database changes
3. Run actual import: Import to local database
4. Verify database: Check users, applications, audit logs
5. Test password reset: Complete full user workflow
6. Test application features: Verify access levels

**Staging Testing Workflow**:
1. Run dry-run on staging
2. Import to staging
3. Test password reset flow
4. Test vetting dashboard
5. For interview-approved: Test Calendly link sending
6. Verify email delivery

## Quick Reference Card

### Common Commands

```bash
# Navigate to tool
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport

# Dry-run (local, fully vetted)
dotnet run -- --input accepted.csv --dry-run

# Dry-run (local, interview-approved)
dotnet run -- --input pre-vetted.csv --status interview-approved --dry-run

# Import locally (fully vetted)
dotnet run -- --input accepted.csv

# Import locally (interview-approved)
dotnet run -- --input pre-vetted.csv --status interview-approved

# Import to staging (interview-approved)
dotnet run -- --input pre-vetted.csv --status interview-approved --environment Staging

# Import to production (interview-approved, with dry-run first)
dotnet run -- --input pre-vetted.csv --status interview-approved --environment Production --dry-run
dotnet run -- --input pre-vetted.csv --status interview-approved --environment Production
```

### Status Values Reference

| Mode | --status | VettingStatus | WorkflowStatus | Role | Access |
|------|----------|---------------|----------------|------|--------|
| Fully Vetted | `approved` or omit | 3 | 3 | VettedMember | Full |
| Interview Approved | `interview-approved` | 1 | 1 | Member | Limited |

### CSV Files Reference

| CSV File | Status Parameter | Purpose | Post-Import Actions |
|----------|------------------|---------|---------------------|
| Accepted.csv | `approved` (default) | Fully vetted members | Send password reset emails |
| Pre-Vetted.csv | `interview-approved` | Interview-approved members | Send Calendly invitations |

### Database Verification Queries

```sql
-- Count users by vetting status
SELECT "VettingStatus", COUNT(*)
FROM "Users"
GROUP BY "VettingStatus";

-- Recent imports
SELECT "SceneName", "Email", "VettingStatus", "CreatedAt"
FROM "Users"
WHERE "CreatedAt" > NOW() - INTERVAL '1 hour'
ORDER BY "CreatedAt" DESC;

-- Check roles
SELECT u."SceneName", r."Name"
FROM "Users" u
JOIN "UserRoles" ur ON u."Id" = ur."UserId"
JOIN "Roles" r ON ur."RoleId" = r."Id"
WHERE u."CreatedAt" > NOW() - INTERVAL '1 hour';
```

---

## Document Metadata

**Version**: 2.1
**Last Updated**: 2025-11-30
**Status**: Active
**Owner**: Backend Team
**Related Documents**:
- Tool README: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/README.md`
- Implementation Plan: `/home/chad/repos/witchcityrope/docs/functional-areas/user-management/new-work/2025-11-24-approved-for-interview-import/implementation-plan.md`
- Member Import Files: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/`

---

## Changelog

### Version 2.1 (2025-11-30)
- **Fixed**: Command line argument format changed from equals sign (`--input=file.csv`) to space-separated (`--input file.csv`) throughout all examples
- **Added**: Staging/Production connection string setup section with PgBouncer configuration details
- **Added**: Important note clarifying that equals sign format is NOT supported by the command line parser

### Version 2.0 (2025-11-24)
- Initial comprehensive guide with full procedures for both import modes

---

**END OF USAGE GUIDE**
