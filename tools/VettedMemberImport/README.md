# Vetted Member Import Tool

One-time console application for importing approved vetted members from Google Sheet into the WitchCityRope database.

## Overview

This tool imports historical vetted member data from a Google Sheet export (CSV) into the database, creating:
- User accounts with EmailVerified=false (requires password reset)
- VettingApplication records with historical data
- VettingAuditLog entries parsed from notes

**Two Import Modes**:
1. **Fully Vetted Members** (default): VettingStatus=3, Role="VettedMember"
2. **Interview-Approved Members**: VettingStatus=1, Role="Member"

## Prerequisites

- .NET 9.0 SDK
- PostgreSQL database (Local, Staging, or Production)
- CSV export from Google Sheet (Spreadsheet ID: 1HYa3wGFn3My0ehh7IQdF2as0eqe4lQ4Fpjhr7F8svCo)

## CSV Files

The tool supports two CSV file types:

### 1. Accepted Members (Fully Vetted)
**File**: `WCR Vetting Database - New - Accepted.csv`
**Status**: VettingStatus=3 (Approved), Role="VettedMember"
**Use**: Import members who have completed vetting process
**Count**: 596 members

### 2. Pre-Vetted Members (Approved for Interview)
**File**: `WCR Vetting Database - New - Pre-Vetted.csv`
**Status**: VettingStatus=1 (InterviewApproved), Role="Member"
**Use**: Import members approved for interview but not yet fully vetted
**Count**: 77 total records (76 with valid email addresses, 1 missing email)
**Post-Import**: Admin must send Calendly interview links

**Location**: `/docs/functional-areas/member-import/Import-CSV-files/`

## Exporting Google Sheet to CSV

1. Open the Google Sheet
2. Select the appropriate sheet ("Accepted" or "Pre-Vetted")
3. Go to "File" → "Download" → "Comma-separated values (.csv)"
4. Save with descriptive name

## Configuration

### Connection Strings

Update the appropriate `appsettings.*.json` file with your database connection string:

**Local (Development)**:
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5434;Database=witchcityrope_dev;Username=postgres;Password=postgres"
  }
}
```

**Staging**:
```json
{
  "ConnectionStrings": {
    "Default": "Host=staging-db.digitalocean.com;Port=25060;Database=witchcityrope;Username=witchcity;Password=YOUR_PASSWORD_HERE;SslMode=Require"
  }
}
```

**Production**:
```json
{
  "ConnectionStrings": {
    "Default": "Host=production-db.digitalocean.com;Port=25060;Database=witchcityrope;Username=witchcity;Password=YOUR_PASSWORD_HERE;SslMode=Require"
  }
}
```

## Usage

### Basic Commands

```bash
# Navigate to tool directory
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport

# Restore packages
dotnet restore

# Build the application
dotnet build

# Show help
dotnet run -- --help
```

### Import Scenarios

#### Import Fully Vetted Members (Accepted.csv)
```bash
# Default behavior - fully vetted members
dotnet run -- --input="/docs/functional-areas/member-import/Import-CSV-files/WCR Vetting Database - New - Accepted.csv" --environment=Production

# Explicit status parameter
dotnet run -- --input="/path/to/accepted.csv" --status=approved --environment=Production
```

#### Import Interview-Approved Members (Pre-Vetted.csv)
```bash
dotnet run -- --input="/docs/functional-areas/member-import/Import-CSV-files/WCR Vetting Database - New - Pre-Vetted.csv" --status=interview-approved --environment=Production
```

#### Testing Before Production

**Dry-Run with Fully Vetted CSV**:
```bash
dotnet run -- --input=accepted.csv --dry-run
```

**Dry-Run with Interview-Approved CSV**:
```bash
dotnet run -- --input=pre-vetted.csv --status=interview-approved --dry-run
```

**Local Import Testing**:
```bash
# Test locally first
dotnet run -- --input=/path/to/vetted-members.csv

# Test with interview-approved status
dotnet run -- --input=/path/to/pre-vetted.csv --status=interview-approved
```

**Staging Import**:
```bash
dotnet run -- --input=/path/to/vetted-members.csv --environment=Staging
```

**Production Import**:
```bash
dotnet run -- --input=/path/to/vetted-members.csv --environment=Production
```

## Command Line Arguments

### --input <file>
**Required**. Path to CSV file exported from Google Sheet.

**Example**:
```bash
dotnet run -- --input=/path/to/vetted-members.csv
```

### --status <status>
**Optional**. Controls the vetting status of imported users.

**Values**: `approved` (default) or `interview-approved`

**Import Modes**:
- `approved`: Fully vetted members (VettingStatus=3, WorkflowStatus=3, Role="VettedMember")
- `interview-approved`: Approved for interview (VettingStatus=1, WorkflowStatus=1, Role="Member")

**Default**: `approved` (maintains backward compatibility)

**Examples**:
```bash
# Import fully vetted members (default)
dotnet run -- --input=vetted.csv --environment=Production

# Import interview-approved members
dotnet run -- --input=pre-vetted.csv --status=interview-approved --environment=Production
```

**Status Meanings**:
- **Approved**: Users have completed full vetting process and have immediate access to vetted-member features
- **Interview-Approved**: Users passed initial review but need interview before final approval; limited access

### --dry-run
**Optional**. Test import without writing to database.

**Example**:
```bash
dotnet run -- --input=/path/to/vetted-members.csv --dry-run
```

### --environment <env>
**Optional**. Environment name (Development, Staging, Production).

**Default**: Development

**Example**:
```bash
dotnet run -- --input=/path/to/vetted-members.csv --environment=Staging
```

### --help or -h
Show help message.

## Import Status Modes

| Mode | --status Value | VettingStatus | WorkflowStatus | Role | Use Case |
|------|---------------|---------------|----------------|------|----------|
| Fully Vetted | `approved` (default) | 3 | 3 | VettedMember | Completed vetting process |
| Interview Approved | `interview-approved` | 1 | 1 | Member | Awaiting interview |

## Import Process

### Data Mapping

**CSV → ApplicationUser**:
- Email → Email, UserName, NormalizedEmail
- Nickname → SceneName
- Pronouns → Pronouns
- FL Handles → FetLifeName
- EmailVerified = false (requires password reset)
- VettingStatus = **3 (Approved)** OR **1 (InterviewApproved)** based on `--status` parameter
- Role = **"VettedMember"** OR **"Member"** based on `--status` parameter
- Random password hash (user must reset via email verification)

**CSV → VettingApplication**:
- Nickname → SceneName
- Email → Email
- Pronouns → Pronouns
- FL Handles → FetLifeHandle
- Description/Motivation → ExperienceDescription, WhyJoinCommunity
- App Submitted → SubmittedAt
- WorkflowStatus = **3 (Approved)** OR **1 (InterviewApproved)** based on `--status` parameter
- DecisionMadeAt inferred from notes or +30 days from submission

**CSV → VettingAuditLog**:
- Relevant Notes → Parsed into audit log entries
- Extracts dates, actions, and performer names from note text
- Creates one or more audit log entries per application

### Duplicate Detection

The tool skips records with duplicate:
- Email address (case-insensitive)
- Scene name (case-insensitive)

Duplicates are logged as warnings and not imported.

### Date Parsing

Handles various date formats from Google Sheet:
- `7/11` → July 11, 2022 (default year)
- `07/11` → July 11, 2022
- `7/11/22` → July 11, 2022
- `07/11/2022` → July 11, 2022

## Output

### Success Example
```
=== Vetted Member Import Tool ===

Environment: Production
Input File: /path/to/pre-vetted.csv
Import Status: interview-approved
  VettingStatus: 1 (InterviewApproved)
  WorkflowStatus: 1 (InterviewApproved)
Dry Run: False
Connection String: Host=production-db.digitalocean.com;Port=25060;Database=***

Database connection successful
Read 77 rows from CSV file

=== IMPORT SUMMARY ===
Total Records: 77
Successful: 76
Skipped (Duplicates): 0
Errors: 1 (missing email)

=== WARNINGS ===
Row 45: Missing email address (Skipped)

IMPORT COMPLETE
```

### Error Example
```
=== IMPORT SUMMARY ===
Total Records: 77
Successful: 74
Skipped (Duplicates): 2
Errors: 1

=== ERRORS ===
Row 45: Missing email address
```

## Testing Workflow

1. **Export Google Sheet** to CSV
2. **Run dry-run locally**:
   ```bash
   dotnet run -- --input=vetted-members.csv --dry-run
   ```
3. **Review output** - Check for errors and warnings
4. **Reset local database** if needed (use `database-reset-dev` skill)
5. **Run actual import locally**:
   ```bash
   dotnet run -- --input=vetted-members.csv
   ```
6. **Verify data** in local database
7. **Import to staging**:
   ```bash
   dotnet run -- --input=vetted-members.csv --environment=Staging
   ```
8. **Test staging** - Verify users can reset passwords
9. **Import to production** (final step):
   ```bash
   dotnet run -- --input=vetted-members.csv --environment=Production
   ```

## Post-Import Actions

### For Fully Vetted Members (approved)
After successful import, users will need to:
1. Receive "NewWebsiteUser" email with password reset link (separate email feature)
2. Click password reset link
3. Set their password
4. Email verification automatically set to true upon password reset
5. Full access to vetted-member features

### For Interview-Approved Members (interview-approved)
After successful import, admins must:
1. Review imported users in vetting dashboard
2. Send Calendly interview invitation links to users
3. Monitor interview scheduling
4. Complete interviews
5. Update status to fully approved after interview completion

Users will need to:
1. Receive "NewWebsiteUser" email with password reset link
2. Set their password
3. Log in with limited access
4. Wait for Calendly invitation email
5. Schedule and complete interview
6. Wait for final approval decision

## Troubleshooting

**Cannot connect to database**:
- Verify connection string in appsettings file
- Test database connectivity: `psql -h HOST -p PORT -U USERNAME -d DATABASE`
- Check firewall rules for Staging/Production

**CSV file not found**:
- Verify file path is absolute or relative to tool directory
- Check file permissions

**Duplicate errors**:
- Expected for users who already exist in database
- Review skipped records to ensure they're intentional duplicates

**Date parsing failures**:
- Check "App Submitted" column format
- Tool handles most common formats but may log warnings

**Wrong status imported**:
- Verify `--status` parameter value (approved vs interview-approved)
- Check console output for status confirmation
- Review VettingStatus in database after import

## Security Notes

- **Passwords**: Random hashes generated - users MUST reset via email
- **Email Verification**: Set to false - users must verify before full access
- **Vetting Status**: Set based on `--status` parameter (default: Approved)
- **Connection Strings**: Never commit staging/production credentials to git

## Project Structure

```
VettedMemberImport/
├── Program.cs              # Entry point and CLI handling
├── Data/
│   └── ApplicationDbContext.cs   # Database context
├── Entities/               # Entity models
│   ├── ApplicationUser.cs
│   ├── VettingApplication.cs
│   └── VettingAuditLog.cs
├── Services/               # Business logic
│   ├── CsvReader.cs       # CSV parsing
│   ├── DateParser.cs      # Date format handling
│   └── UserImporter.cs    # Import orchestration
├── Models/                 # DTOs
│   ├── CsvRow.cs
│   └── ImportSummary.cs
├── appsettings.json        # Local configuration
├── appsettings.Staging.json
└── appsettings.Production.json
```

## Next Steps

After running this import tool:
1. Configure email system to send "NewWebsiteUser" template
2. Use email admin UI to send password reset links to imported users
3. For interview-approved imports: Send Calendly invitation emails
4. Monitor user activation and password resets
5. Archive this tool after all imports complete

## Support

For issues or questions:
- Check import summary for specific error messages
- Review logs for detailed error information
- Validate CSV file format matches expected columns
- Test with dry-run before actual import
- Verify `--status` parameter matches intended import mode
