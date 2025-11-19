# Vetted Member Import Tool

One-time console application for importing 140+ approved vetted members from Google Sheet into the WitchCityRope database.

## Overview

This tool imports historical vetted member data from a Google Sheet export (CSV) into the database, creating:
- User accounts with EmailVerified=false (requires password reset)
- VettingApplication records with historical data
- VettingAuditLog entries parsed from notes

## Prerequisites

- .NET 9.0 SDK
- PostgreSQL database (Local, Staging, or Production)
- CSV export from Google Sheet (Spreadsheet ID: 1HYa3wGFn3My0ehh7IQdF2as0eqe4lQ4Fpjhr7F8svCo, Sheet: "Accepted")

## Exporting Google Sheet to CSV

1. Open the Google Sheet
2. Go to "File" → "Download" → "Comma-separated values (.csv)"
3. Save as `vetted-members.csv`

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

**1. Dry Run (Local)**
Test import without writing to database:
```bash
dotnet run -- --input=/path/to/vetted-members.csv --dry-run
```

**2. Actual Import (Local)**
Import to local development database:
```bash
dotnet run -- --input=/path/to/vetted-members.csv
```

**3. Import to Staging**
Import to staging environment:
```bash
dotnet run -- --input=/path/to/vetted-members.csv --environment=Staging
```

**4. Import to Production**
Import to production environment:
```bash
dotnet run -- --input=/path/to/vetted-members.csv --environment=Production
```

## Command Line Arguments

- `--input <file>` (Required): Path to CSV file exported from Google Sheet
- `--dry-run` (Optional): Test import without writing to database
- `--environment <env>` (Optional): Environment name (Development, Staging, Production). Default: Development
- `--help` or `-h`: Show help message

## Import Process

### Data Mapping

**CSV → ApplicationUser**:
- Email → Email, UserName, NormalizedEmail
- Nickname → SceneName
- Pronouns → Pronouns
- FL Handles → FetLifeName
- EmailVerified = false (requires password reset)
- VettingStatus = 3 (Approved)
- Role = "VettedMember"
- Random password hash (user must reset via email verification)

**CSV → VettingApplication**:
- Nickname → SceneName
- Email → Email
- Pronouns → Pronouns
- FL Handles → FetLifeHandle
- Description/Motivation → ExperienceDescription, WhyJoinCommunity
- App Submitted → SubmittedAt
- WorkflowStatus = 3 (Approved)
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

Environment: Development
Input File: /path/to/vetted-members.csv
Dry Run: False
Connection String: Host=localhost;Port=5434;Database=witchcityrope_dev;Username=postgres;Password=***

Database connection successful
Read 142 rows from CSV file

=== IMPORT SUMMARY ===
Total Records: 142
Successful: 140
Skipped (Duplicates): 2
Errors: 0

=== WARNINGS ===
Row 23: Duplicate - Email: existing@example.com, SceneName: ExistingUser (Skipped)
Row 87: Duplicate - Email: another@example.com, SceneName: AnotherUser (Skipped)

IMPORT COMPLETE
```

### Error Example
```
=== IMPORT SUMMARY ===
Total Records: 142
Successful: 138
Skipped (Duplicates): 2
Errors: 2

=== ERRORS ===
Row 45: Missing email address
Row 92: Missing scene name/nickname
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

After successful import, users will need to:
1. Receive "NewWebsiteUser" email with password reset link (separate email feature)
2. Click password reset link
3. Set their password
4. Email verification automatically set to true upon password reset

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

## Security Notes

- **Passwords**: Random hashes generated - users MUST reset via email
- **Email Verification**: Set to false - users must verify before full access
- **Vetting Status**: Set to Approved (3) - historical import assumes approval
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
3. Monitor user activation and password resets
4. Archive this tool (one-time use)

## Support

For issues or questions:
- Check import summary for specific error messages
- Review logs for detailed error information
- Validate CSV file format matches expected columns
- Test with dry-run before actual import
