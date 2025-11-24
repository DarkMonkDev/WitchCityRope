# Technical Analysis: Approved for Interview CSV Import Process

**Date**: 2025-11-24
**Author**: backend-developer agent
**Purpose**: Analyze existing vetted user import process to identify modifications needed for "approved for interview" imports

---

## Executive Summary

The existing vetted member import tool at `/home/chad/repos/witchcityrope/tools/VettedMemberImport/` is a fully functional C# console application that imports vetted members from CSV to production database. The tool creates user accounts with `VettingStatus = 3` (Approved) and corresponding vetting applications with `WorkflowStatus = 3` (Approved).

**Key Modification Required**: Change two status values from "Approved" to "InterviewApproved" to import users awaiting interviews instead of fully vetted users.

---

## Import Tool Location and Structure

### Tool Location
**Full Path**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/`

### Project Structure
```
VettedMemberImport/
├── VettedMemberImport/                 # Main console application
│   ├── Program.cs                      # CLI entry point and argument parsing
│   ├── Data/
│   │   └── ApplicationDbContext.cs     # EF Core database context
│   ├── Entities/                       # Entity models (subset of production models)
│   │   ├── ApplicationUser.cs          # User entity for import
│   │   ├── VettingApplication.cs       # Vetting application entity
│   │   └── VettingAuditLog.cs         # Audit log entity
│   ├── Services/                       # Business logic
│   │   ├── CsvReader.cs               # CSV parsing service
│   │   ├── DateParser.cs              # Date format handling
│   │   └── UserImporter.cs            # 🔑 MAIN IMPORT LOGIC (LINE 128, 184)
│   ├── Models/                         # DTOs and data models
│   │   ├── CsvRow.cs                  # CSV row structure
│   │   └── ImportSummary.cs           # Import result summary
│   ├── appsettings.json                # Local development config
│   ├── appsettings.Staging.json        # Staging environment config
│   └── appsettings.Production.json     # Production environment config
└── VettedMemberImport.Tests/           # Unit tests for import tool
```

---

## Current Import Data Flow

### 1. CSV Structure
**Sample CSV File**: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR Vetting Database - New - Accepted.csv`

**CSV Columns** (from `CsvRow.cs`):
```csharp
public class CsvRow
{
    public string ApplicationDate { get; set; }     // "App Submitted"
    public string SceneName { get; set; }            // "Vettee's nickname"
    public string FetLifeHandle { get; set; }        // "FL handles"
    public string Pronouns { get; set; }             // "Vettee's pronouns"
    public string Email { get; set; }                // "Vettee's email"
    public string SponsorInfo { get; set; }          // "Sponsor's nickname and email"
    public string Vettor { get; set; }               // "Assigned Vettor"
    public string Notes { get; set; }                // "Relevant notes"
    public string MotivationDescription { get; set; } // "Description of the aplicant and motivation to join"
    public string InstagramHandle { get; set; }      // "IG handles"
    public string OtherHandles { get; set; }         // "Other handles"
    public string RelationshipWithSponsor { get; set; } // "Relationship with Sponsor..."
    public string FitForDarkAlchemy { get; set; }    // "Fit for Dark Alchemy"
    public string References { get; set; }           // "References"
}
```

### 2. Database Tables Affected

#### Table: `Users` (ApplicationUser)
**Location**: `/home/chad/repos/witchcityrope/apps/api/Models/ApplicationUser.cs`

**Key Fields Set During Import** (`UserImporter.cs`, lines 106-144):
```csharp
var user = new ApplicationUser
{
    Id = Guid.NewGuid(),
    Email = email,
    NormalizedEmail = normalizedEmail,
    UserName = email,
    NormalizedUserName = normalizedEmail,
    SceneName = row.SceneName.Trim(),
    Pronouns = row.Pronouns?.Trim() ?? "",
    FetLifeName = row.FetLifeHandle?.Trim(),
    PasswordHash = BCrypt.Net.BCrypt.HashPassword(GenerateRandomPassword()),
    SecurityStamp = Guid.NewGuid().ToString(),

    // 🔑 CRITICAL FIELD #1 - Currently set to Approved (3)
    VettingStatus = 3, // Approved (LINE 128)

    EmailConfirmed = false,  // Requires password reset
    EmailVerificationToken = GenerateVerificationToken(),
    HasVettingApplication = true,
    CreatedAt = submittedAt,
    UpdatedAt = DateTime.UtcNow,
    IsActive = true,
    TermsOfServiceAccepted = false,
    Role = "",  // Member is default
};
```

#### Table: `VettingApplications`
**Location**: `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Entities/VettingApplication.cs`

**Key Fields Set During Import** (`UserImporter.cs`, lines 164-196):
```csharp
var application = new VettingApplication
{
    Id = Guid.NewGuid(),
    UserId = user.Id,
    ApplicationNumber = GenerateApplicationNumber(submittedAt),
    StatusToken = Guid.NewGuid().ToString("N"),
    SceneName = row.SceneName.Trim(),
    Email = email,
    Pronouns = row.Pronouns?.Trim(),
    FetLifeHandle = row.FetLifeHandle?.Trim(),
    OtherNames = combinedOtherNames,
    ExperienceDescription = row.MotivationDescription?.Trim(),
    WhyJoinCommunity = row.RelationshipWithSponsor?.Trim(),
    HowDidYouHearAboutUs = row.FitForDarkAlchemy?.Trim(),
    ExperienceLevel = 2, // Intermediate
    YearsExperience = 1,

    // 🔑 CRITICAL FIELD #2 - Currently set to Approved (3)
    WorkflowStatus = 3, // Approved (LINE 184)

    SubmittedAt = submittedAt,
    DecisionMadeAt = decisionDate,
    AgreesToGuidelines = true,
    AgreesToTerms = true,
    ConsentToContact = true,
    CreatedAt = submittedAt,
    UpdatedAt = DateTime.UtcNow
};
```

#### Table: `VettingAuditLogs`
**Auto-generated from Notes field** (lines 200-202):
- Parses "Relevant notes" column into structured audit log entries
- Extracts dates, actions, and performer names from note text
- Creates audit trail for vetting process history

---

## Vetting Status Values Reference

### User.VettingStatus (int)
**Source**: `/home/chad/repos/witchcityrope/apps/api/Models/ApplicationUser.cs`, line 110

**Purpose**: Source of truth for user permissions and access control

**Current Import Value**: `3` (Approved - fully vetted member)

**Required New Value**: `1` (InterviewApproved - approved for interview, not yet vetted)

### VettingApplication.WorkflowStatus (enum VettingStatus)
**Source**: `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Entities/VettingApplication.cs`, lines 96-105

**Enum Definition**:
```csharp
public enum VettingStatus
{
    UnderReview = 0,        // Application submitted and under initial review
    InterviewApproved = 1,  // Approved for interview - Calendly link sent
    FinalReview = 2,        // Post-interview final review before decision
    Approved = 3,           // Final decision: Approved (FULLY VETTED)
    Denied = 4,             // Final decision: Denied
    OnHold = 5,             // Final decision: On hold
    Withdrawn = 6           // Applicant withdrew their application
}
```

**Current Import Value**: `VettingStatus.Approved` (3) or cast as `(int)3`

**Required New Value**: `VettingStatus.InterviewApproved` (1) or cast as `(int)1`

---

## Key Differences: Vetted vs Approved for Interview

### Current Import (Fully Vetted Members)
```csharp
// UserImporter.cs, Line 128
VettingStatus = 3,  // Approved - FULLY VETTED

// UserImporter.cs, Line 184
WorkflowStatus = 3,  // Approved - FULLY VETTED
```

**Result**:
- Users can access all vetted member features immediately
- `ApplicationUser.IsVetted` computed property returns `true` (VettingStatus == 3)
- No interview required

### Required Change (Approved for Interview)
```csharp
// UserImporter.cs, Line 128
VettingStatus = 1,  // InterviewApproved - AWAITING INTERVIEW

// UserImporter.cs, Line 184
WorkflowStatus = 1,  // InterviewApproved - AWAITING INTERVIEW (or VettingStatus.InterviewApproved)
```

**Result**:
- Users are in the system but NOT fully vetted
- `ApplicationUser.IsVetted` computed property returns `false` (VettingStatus != 3)
- Interview must be completed and approved before full vetting
- Admin can send Calendly interview link
- After interview completion, admin manually moves to `FinalReview` → `Approved`

---

## Implementation Requirements for New Import Variant

### Option 1: Command Line Flag (Recommended)
Add `--status` parameter to tool:

```bash
# Current behavior (fully vetted)
dotnet run --input=vetted.csv --environment=Production

# New behavior (approved for interview)
dotnet run --input=approved-for-interview.csv --status=interview-approved --environment=Production
```

**Changes Required**:
1. Add `--status` parameter to `Program.cs` argument parser (lines 154-194)
2. Pass status value to `UserImporter` constructor or `ImportUsersAsync()` method
3. Use status value to set `VettingStatus` and `WorkflowStatus` fields
4. Default to `Approved` (3) for backward compatibility

### Option 2: Separate Tool (Alternative)
Create `/home/chad/repos/witchcityrope/tools/InterviewApprovedImport/` as copy of existing tool with hardcoded status values.

**Pros**: No risk of accidentally importing wrong status to production
**Cons**: Code duplication, maintenance burden

### Option 3: Configuration File (Alternative)
Add status configuration to `appsettings.*.json`:

```json
{
  "ImportSettings": {
    "VettingStatus": 1,      // InterviewApproved
    "WorkflowStatus": 1      // InterviewApproved
  }
}
```

**Pros**: Environment-specific defaults
**Cons**: Less flexible than CLI flag, harder to switch between import types

---

## Security and Validation Considerations

### Unchanged Security Features (Keep As-Is)
1. **Random Password Hash**: Users MUST reset password via email verification
2. **EmailConfirmed = false**: Requires email verification before access
3. **Email Verification Token**: Generated for password reset flow
4. **Duplicate Detection**: Skips existing users by email or scene name

### Additional Validation for Interview-Approved Imports
1. **Status Transition Validation**: Ensure imported users can only be moved forward in workflow (InterviewApproved → FinalReview → Approved)
2. **Audit Log Notes**: Import should note "Imported as Interview-Approved from CSV" for transparency
3. **Admin Notification**: Consider notifying vetting team that interview-approved users were added

---

## Testing Requirements

### Unit Tests (Existing Coverage)
**Location**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport.Tests/`

**Existing Tests**:
- `CsvReaderTests.cs` - CSV parsing validation
- `DateParserTests.cs` - Date format handling
- `UserImporterTests.cs` - Import logic validation

**New Tests Required**:
- Import with `--status=interview-approved` flag
- Verify `VettingStatus = 1` set correctly
- Verify `WorkflowStatus = 1` set correctly
- Verify audit logs indicate interview-approved import

### Integration Testing Workflow
1. **Local Development**: Test import to local database (port 5434)
2. **Dry Run Validation**: Test with `--dry-run` flag before actual import
3. **Staging Validation**: Import to staging environment first
4. **Production Import**: Final import after staging verification

---

## Modification Summary

### Files to Modify
1. **`/home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport/Program.cs`**
   - Add `--status` command line argument
   - Parse status value (default: "approved")
   - Pass to UserImporter

2. **`/home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport/Services/UserImporter.cs`**
   - Line 128: Change `VettingStatus = 3` to use parameter value
   - Line 184: Change `WorkflowStatus = 3` to use parameter value
   - Add constructor parameter or method parameter for status
   - Add validation to ensure status is valid enum value

3. **`/home/chad/repos/witchcityrope/tools/VettedMemberImport/README.md`**
   - Document new `--status` parameter
   - Add usage examples for interview-approved imports
   - Update command reference section

### No Changes Required
- Database schema (already supports all status values)
- CSV file format
- Duplicate detection logic
- Audit log parsing
- Email verification workflow
- Connection string configuration

---

## Recommended Implementation Approach

### Phase 1: Add Status Parameter
1. Modify `Program.cs` to accept `--status` parameter
2. Add validation for allowed values: `approved`, `interview-approved`
3. Default to `approved` for backward compatibility
4. Pass status to `UserImporter` service

### Phase 2: Update Import Logic
1. Convert status string to enum values:
   - `approved` → `VettingStatus = 3`, `WorkflowStatus = 3`
   - `interview-approved` → `VettingStatus = 1`, `WorkflowStatus = 1`
2. Update lines 128 and 184 in `UserImporter.cs`
3. Add logging to show which status is being used

### Phase 3: Testing
1. Add unit tests for new status parameter
2. Test dry-run with both status values locally
3. Verify database records have correct status values
4. Test on staging environment

### Phase 4: Documentation
1. Update README.md with new parameter
2. Update command examples
3. Document status value meanings

---

## Risk Assessment

### Low Risk Modifications
- ✅ Adding command line parameter (backward compatible)
- ✅ Changing status values (valid enum values)
- ✅ Testing on local/staging before production

### Medium Risk Considerations
- ⚠️ Accidentally importing with wrong status to production
- ⚠️ Forgetting to send interview links after import
- ⚠️ Users expecting full access but only interview-approved

### Mitigation Strategies
1. **Confirmation Prompt**: Add confirmation for production imports showing status
2. **Dry Run First**: Always require dry-run validation before production
3. **Status Logging**: Log status value prominently in import output
4. **Post-Import Checklist**: Document steps after import (send interview links, notify team)

---

## Post-Import Workflow

### For Interview-Approved Imports
1. **Import CSV** with `--status=interview-approved`
2. **Verify Import**: Check database for correct status values
3. **Send Interview Links**: Use vetting admin UI to send Calendly links to imported users
4. **Track Interviews**: Monitor interview completion in vetting dashboard
5. **Final Approval**: After successful interviews, move users to `Approved` status

### Admin UI Integration Points
- Vetting dashboard should show imported users in "Interview Approved" state
- Calendly link sending feature should work for imported users
- Status transition workflow should allow InterviewApproved → FinalReview → Approved

---

## Conclusion

The existing vetted member import tool is well-structured and requires minimal modifications to support "approved for interview" imports. The key changes are:

1. **Two status values** (lines 128 and 184 in `UserImporter.cs`)
2. **Command line parameter** for status selection
3. **Documentation updates** for new parameter

All database schema, security measures, and validation logic remain unchanged. The modification maintains backward compatibility while adding new functionality for a different use case.

**Estimated Effort**: 2-4 hours for implementation and testing

---

## Appendix: Full File Paths

### Import Tool Files
- **Main Logic**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport/Services/UserImporter.cs`
- **CLI Entry Point**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport/Program.cs`
- **README**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/README.md`
- **Sample CSV**: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/WCR Vetting Database - New - Accepted.csv`

### Production API Files
- **User Entity**: `/home/chad/repos/witchcityrope/apps/api/Models/ApplicationUser.cs`
- **Vetting Application Entity**: `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Entities/VettingApplication.cs`
- **Vetting Status Enum**: `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Entities/VettingApplication.cs` (lines 96-105)

### Documentation Files
- **Import Handoff**: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/handoffs/backend-developer-import-2025-11-18-handoff.md`
- **Test Summary**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/TEST_SUMMARY.md`
