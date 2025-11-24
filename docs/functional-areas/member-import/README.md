# Member Import Functional Area
<!-- Last Updated: 2025-11-24 -->
<!-- Version: 1.0 -->
<!-- Owner: Backend Team -->
<!-- Status: Active -->

## Overview

This functional area contains documentation and resources for importing vetted members from historical Google Sheets data into the WitchCityRope database.

**Purpose**: Enable bulk import of vetted member data while maintaining data integrity and proper vetting workflow status.

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

6. **Post-Import**
   - Send password reset emails
   - For interview-approved: Send Calendly links
   - Monitor user activation
   - Track interview scheduling

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

### After Fully Vetted Import
1. Verify import success in vetting dashboard
2. Send "NewWebsiteUser" emails with password reset links
3. Monitor user activation and password resets
4. Assist users with login issues

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

## Email Templates

### NewWebsiteUser Template
- Sent to all imported users
- Contains password reset link
- Instructions for account setup

### Interview Invitation Template
- Sent to interview-approved users only
- Contains Calendly scheduling link
- Explains interview process

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

**Detailed Troubleshooting**: See usage guide linked above

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

## Related Documentation

**User Management**:
- Functional Area: `/home/chad/repos/witchcityrope/docs/functional-areas/user-management/`
- Admin Screens: `/home/chad/repos/witchcityrope/docs/functional-areas/user-management/new-work/2025-08-12-admin-screen/`

**Implementation Details**:
- Implementation Plan: `/home/chad/repos/witchcityrope/docs/functional-areas/user-management/new-work/2025-11-24-approved-for-interview-import/implementation-plan.md`

**Vetting System**:
- Functional Area: `/home/chad/repos/witchcityrope/docs/functional-areas/vetting-system/`

## History

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
3. Test with dry-run mode first
4. Contact backend team for assistance

---

**Document Metadata**:
- **Version**: 1.0
- **Last Updated**: 2025-11-24
- **Owner**: Backend Team
- **Status**: Active
