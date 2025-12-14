# Member Import Functional Area
<!-- Last Updated: 2025-12-14 -->
<!-- Version: 1.1 -->
<!-- Owner: Backend Team -->
<!-- Status: Active -->

## Overview

This functional area contains documentation and resources for importing vetted members from historical Google Sheets data into the WitchCityRope database.

**Purpose**: Enable bulk import of vetted member data while maintaining data integrity and proper vetting workflow status.

## 📧 POST-IMPORT EMAIL WORKFLOW ✨ NEW

**After importing members, send welcome emails with password reset links:**

**Complete Guide**: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/post-import-email-workflow-guide.md`

**Quick Steps**:
1. Import members via VettedMemberImport tool
2. Navigate to Email Templates Admin (`/admin/email-templates`) → Ad-Hoc tab
3. Select **"NewImportedUsers"** segment (shows users with `EmailConfirmed = false`)
4. Choose "NewWebsiteUser" template (contains `{{reset_url}}` variable)
5. Send emails (each user gets unique password reset link)

**Key Features**:
- ✅ **NewImportedUsers segment**: Automatically targets imported users needing activation
- ✅ **Per-user variables**: `{{user_name}}`, `{{reset_url}}`, `{{verification_url}}`
- ✅ **Unique tokens**: Each user gets individual password reset link
- ✅ **8 user segments available**: AllVettedMembers, AllTeachers, EmailNotVerified, etc.
- ✅ **Fully functional**: Emails actually sent via SendGrid (or logged in dev mode)

---

## Import Tool

**Tool Location**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/`

**Documentation**:
- **Tool README**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/README.md`
- **Complete Usage Guide**: `/home/chad/repos/witchcityrope/docs/functional-areas/user-management/guides/vetted-member-import-usage-guide.md`

**Capabilities**:
- Import fully vetted members (VettingStatus=3, Role="VettedMember")
- Import interview-approved members (VettingStatus=1, Role="Member")
- Duplicate detection and validation
- Multi-environment support (Local/Staging/Production)
- Dry-run mode for testing
- Comprehensive error handling

## CSV Files

**Location**: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/Import-CSV-files/`

### Available CSV Files

1. **WCR Vetting Database - New - Accepted.csv**
   - Fully vetted members who completed vetting process
   - 596 members
   - Import with: `--status=approved` (default)

2. **WCR Vetting Database - New - Pre-Vetted.csv**
   - Members approved for interview but not yet fully vetted
   - Import with: `--status=interview-approved`
   - Post-import: Admin must send Calendly interview links

## Import Modes

### Mode 1: Fully Vetted Members

**Use When**: Importing members who completed full vetting process

**Command**:
```bash
dotnet run -- --input=accepted.csv --environment=Production
```

**Results**:
- VettingStatus: 3 (Approved)
- Role: VettedMember
- Access: Full vetted-member features

### Mode 2: Interview-Approved Members

**Use When**: Importing members approved for interview

**Command**:
```bash
dotnet run -- --input=pre-vetted.csv --status=interview-approved --environment=Production
```

**Results**:
- VettingStatus: 1 (InterviewApproved)
- Role: Member
- Access: Limited, awaiting interview

**Post-Import Required Actions**:
- Admin sends Calendly interview invitation links
- Users schedule interviews
- Vetting team conducts interviews
- Admin updates status to Approved after successful interview

## Import Workflow

### Standard Import Process

1. **Preparation**
   - Export Google Sheet to CSV
   - Review CSV for data quality
   - Build import tool: `dotnet build`

2. **Validation**
   - Run dry-run locally
   - Review output for errors/warnings
   - Fix any issues found

3. **Local Testing**
   - Import to local database
   - Verify data in database
   - Test password reset flow

4. **Staging Deployment**
   - Run dry-run on staging
   - Import to staging
   - Test full user workflow
   - Verify vetting dashboard

5. **Production Deployment**
   - Get stakeholder approval
   - Run MANDATORY dry-run
   - Review output carefully
   - Import to production
   - Verify database
   - Notify vetting team

6. **Post-Import Email Workflow** ✨ **NEW**
   - **Navigate** to Email Templates Admin → Ad-Hoc tab
   - **Select** "NewImportedUsers" segment
   - **Choose** "NewWebsiteUser" template
   - **Send** emails with password reset links
   - **Monitor** user activation
   - **Complete Guide**: See `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/post-import-email-workflow-guide.md`

## Database Schema

### Users Table
- Email, SceneName, Pronouns
- VettingStatus (1=InterviewApproved, 3=Approved)
- EmailConfirmed (false until password reset)
- Random password hash (user must reset)

### VettingApplications Table
- Application details
- WorkflowStatus (1=InterviewApproved, 3=Approved)
- SubmittedAt, DecisionMadeAt timestamps

### VettingAuditLog Table
- Parsed from "Relevant notes" column
- Action history and performer names

### UserRoles Table
- Role assignment (Member or VettedMember)

## Admin Responsibilities

### After Fully Vetted Import ✨ **UPDATED**

**NEW - Email Templates Admin Workflow**:
1. Navigate to `/admin/email-templates` → Ad-Hoc tab
2. Select "NewImportedUsers" segment (shows imported users with `EmailConfirmed = false`)
3. Choose "NewWebsiteUser" template (contains `{{reset_url}}` variable)
4. Send emails (each user gets unique password reset link)
5. Monitor user activation via segment count decreasing

**Legacy Manual Process** (NO LONGER NEEDED):
~~1. Verify import success in vetting dashboard~~
~~2. Manually send "NewWebsiteUser" emails with password reset links~~

**Benefits of New Workflow**:
- ✅ Automated segment targeting (no manual user selection)
- ✅ Unique password reset links per user
- ✅ Real-time recipient count preview
- ✅ Audit trail of sent emails
- ✅ Resend capability for unactivated users

### After Interview-Approved Import

1. Review imported users in vetting dashboard
2. **Send Calendly interview invitation emails** (CRITICAL)
3. Monitor interview scheduling rate
4. Coordinate vetting team availability
5. Conduct interviews
6. Update user status after interview decisions:
   - Approved → VettingStatus=3, add VettedMember role
   - Denied → VettingStatus=4, send notification
7. Send approval/denial notifications

## Email Templates ✨ **NEW SECTION**

### Available User Segments for Ad-Hoc Emails

| Segment | Filter | Use Case |
|---------|--------|----------|
| **NewImportedUsers** | `VettingStatus == Approved AND EmailConfirmed == false AND IsActive == true` | **Send welcome emails to imported members** |
| AllVettedMembers | `VettingStatus == Approved` | Announcements to all vetted members |
| AllPreVettedMembers | `VettingStatus NOT IN (Denied, OnHold) AND IsActive == true` | Updates to members in vetting |
| AllTeachers | Role contains "Teacher" | Teacher communications |
| AllDMs | Role contains "DungeonMonitor" | DM communications |
| AllSafetyTeam | Role contains "SafetyTeam" | Safety team communications |
| AllAdmins | Role contains "Administrator" | Admin communications |
| EmailNotVerified | `EmailConfirmed == false` | Resend verification emails |
| VettingPending | `VettingStatus == UnderReview` | Applicant status updates |

### NewWebsiteUser Template

**Category**: Admin
**Subject**: "Welcome to WitchCityRope - Set Your Password"
**Purpose**: Welcome message with password reset link for imported users

**Available Variables**:
- `{{user_name}}` - User's scene name (falls back to email, then "Member")
- `{{reset_url}}` - Unique password reset link with token (unique per user)
- `{{verification_url}}` - Unique email verification link (unique per user)
- `{{system_url}}` - Frontend base URL (hardcoded: `https://witchcityrope.com`)

**Example Body**:
```html
<p>Hi {{user_name}},</p>
<p>Welcome to WitchCityRope! Your vetted member account has been created.</p>
<p>To complete your account setup, please set your password:</p>
<p><a href="{{reset_url}}">Set Your Password</a></p>
<p>This link is valid for 24 hours.</p>
```

**Sending Behavior**:
- Emails sent **individually** (one per user) when using `{{reset_url}}` or `{{verification_url}}`
- Each user receives unique password reset token
- Tokens valid for 24 hours

### Post-Import Email Workflow

**Complete Documentation**: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/post-import-email-workflow-guide.md`

**Topics Covered**:
- Step-by-step sending instructions
- Variable replacement details
- Security considerations
- Troubleshooting common issues
- Performance considerations
- FAQ

## Security & Privacy

**CSV File Handling**:
- Contains PII (personally identifiable information)
- Store securely
- Delete after successful import
- Never commit to version control

**Database Credentials**:
- Never commit staging/production credentials to git
- Store in secure password manager
- Rotate passwords regularly
- Use SSL/TLS for remote connections

**Password Security**:
- Random hashes generated during import
- Users MUST reset via email verification
- No plain-text passwords stored
- Email verification required before access

**Email Token Security** ✨ **NEW**:
- Cryptographically secure random generation
- Unique per user, per request
- 24-hour expiration
- Single-use (consumed on password set)
- HTTPS required in production

## Testing

**Unit Tests**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport.Tests/`

**Test Coverage**:
- 52 tests passing (100% pass rate)
- Both import modes tested
- Duplicate detection validated
- Date parsing verified
- Error handling confirmed

**Run Tests**:
```bash
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport
dotnet test
```

## Troubleshooting

**Common Issues**:
- Cannot connect to database → Verify connection string
- CSV file not found → Use absolute path
- Duplicate records → Expected for production, review skipped users
- Wrong status imported → Verify `--status` parameter
- Date parsing failures → Check CSV date format
- "value too long for type character varying(X)" → Fields are auto-truncated (see below)

**Email Issues** ✨ **NEW**:
- **Segment shows 0 recipients** → Check `VettingStatus`, `EmailConfirmed`, `IsActive` values
- **Users not receiving emails** → Check SendGrid dashboard or Docker logs (dev mode)
- **Reset links expired** → Resend emails (generates fresh tokens, valid 24 hours)

**Detailed Troubleshooting**: See usage guide and post-import email workflow guide

## Critical Implementation Notes (2025-11-30)

### Field Truncation
The import tool automatically truncates fields to fit database column constraints:
- **User.SceneName**: max 50 chars
- **User.Pronouns**: max 50 chars
- **User.FetLifeName**: max 100 chars
- **VettingApplication.SceneName**: max 100 chars
- **VettingApplication.Pronouns**: max 50 chars
- **VettingApplication.FetLifeHandle**: max 100 chars
- **VettingApplication.OtherNames**: max 1000 chars

### Field Mapping Differences Between CSVs
The Accepted and Pre-Vetted CSVs have different data semantics for the same columns. The import tool uses **conditional field mapping** based on the `--status` parameter:

**For Accepted (--status=approved)**:
| CSV Column | Maps To |
|------------|---------|
| RelationshipWithSponsor ("How did they learn about DA") | WhyJoinCommunity |
| FitForDarkAlchemy ("Fit for Dark Alchemy") | HowDidYouHearAboutUs |
| MotivationDescription ("Description of applicant") | ExperienceDescription |

**For Pre-Vetted (--status=interview-approved)**:
| CSV Column | Maps To |
|------------|---------|
| MotivationDescription ("Description of applicant") | WhyJoinCommunity |
| RelationshipWithSponsor ("How did they learn about DA") | HowDidYouHearAboutUs |
| FitForDarkAlchemy ("Fit for Dark Alchemy") | ExperienceDescription |

### Date Parsing
- Dates without year (e.g., "7/11") default to **2024**, not current year
- Dates with 2-digit year (e.g., "7/11/22") are converted to 2000s (2022)
- Dates are stored as UTC

### Audit Log Creation
Each imported application creates these audit log entries:
1. **"Application Submitted"** - NewValue="UnderReview", with descriptive note
2. **"Vettor Assigned"** - If vettor name present in CSV
3. **"Note Added"** - For each note line parsed from VettingStatus/Notes columns
4. **"Status Changed"** - OldValue="UnderReview", NewValue="Approved" or "InterviewApproved" (Notes=null per system standard)

### CSV Column Mapping
The import tool handles multiple column name variations for the same data:
- Application date: "App Submitted Date", "App Submitted", "ApplicationDate", etc.
- Vetting notes: "Vetting status \n(specify dates and sign)", "Vetting status", etc.
- See `CsvReader.cs` for full mapping definitions

## Quick Start

**Import Fully Vetted Members**:
```bash
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport

# Dry-run first (ALWAYS)
dotnet run -- --input=accepted.csv --dry-run

# Actual import
dotnet run -- --input=accepted.csv
```

**Import Interview-Approved Members**:
```bash
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport

# Dry-run first (ALWAYS)
dotnet run -- --input=pre-vetted.csv --status=interview-approved --dry-run

# Actual import
dotnet run -- --input=pre-vetted.csv --status=interview-approved
```

**Send Welcome Emails** ✨ **NEW**:
1. Login as Administrator
2. Navigate to `/admin/email-templates` → Ad-Hoc tab
3. Select "NewImportedUsers" segment
4. Choose "NewWebsiteUser" template
5. Click "Send Email"

**Complete workflow**: See `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/post-import-email-workflow-guide.md`

## Related Documentation

**User Management**:
- Functional Area: `/home/chad/repos/witchcityrope/docs/functional-areas/user-management/`
- Admin Screens: `/home/chad/repos/witchcityrope/docs/functional-areas/user-management/new-work/2025-08-12-admin-screen/`

**Implementation Details**:
- Implementation Plan: `/home/chad/repos/witchcityrope/docs/functional-areas/user-management/new-work/2025-11-24-approved-for-interview-import/implementation-plan.md`

**Email Templates** ✨ **NEW**:
- **Post-Import Email Workflow Guide**: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/post-import-email-workflow-guide.md` ⭐ **COMPREHENSIVE**
- Admin Management: `/home/chad/repos/witchcityrope/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/`
- Static Variables: `/home/chad/repos/witchcityrope/docs/functional-areas/email-templates/new-work/2025-11-17-hardcode-static-variables/`

**Member Import & Email Enhancement**:
- Orchestrator Handoff: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/handoffs/orchestrator-2025-11-18-handoff.md`
- Backend Email Handoff: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/handoffs/backend-developer-email-2025-11-18-handoff.md`

**Vetting System**:
- Functional Area: `/home/chad/repos/witchcityrope/docs/functional-areas/vetting-system/`

## History

**2025-12-14**: Post-import email workflow documentation added
- Added NewImportedUsers segment documentation
- Added per-user variable replacement details
- Added complete post-import email workflow guide
- Updated admin responsibilities with new email workflow
- Removed legacy manual email process

**2025-11-30**: Field mapping and truncation improvements
- Added automatic field truncation for database column constraints
- Fixed date parsing to use 2024 as default year instead of current year
- Added conditional field mapping for Accepted vs Pre-Vetted CSVs
- Fixed audit log creation to match system standard format
- Added inner exception extraction for better error diagnostics
- Successfully imported 598 vetted members and 75 interview-approved members to staging

**2025-11-24**: Added interview-approved import mode
- New `--status` parameter supports both import modes
- Comprehensive testing (52 tests, 100% pass rate)
- Complete documentation update

**2025-11-18**: Initial member import tool creation
- Fully vetted members import (596 members)
- CSV parsing with duplicate detection
- Multi-environment support

## Support

For questions or issues:
1. Review tool README and usage guide
2. Check troubleshooting section
3. For email issues: See post-import email workflow guide
4. Test with dry-run mode first
5. Contact backend team for assistance

---

**Document Metadata**:
- **Version**: 1.1
- **Last Updated**: 2025-12-14
- **Owner**: Backend Team + Librarian
- **Status**: Active
