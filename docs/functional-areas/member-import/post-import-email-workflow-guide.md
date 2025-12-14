# Post-Import Email Workflow Guide
<!-- Last Updated: 2025-12-14 -->
<!-- Version: 1.0 -->
<!-- Owner: Backend Team -->
<!-- Status: Active -->

## Overview

This guide documents the complete workflow for sending welcome emails to newly imported vetted members using the Email Templates Admin UI with user segmentation.

**Purpose**: After importing vetted members via the VettedMemberImport tool, admins can send personalized welcome emails with password reset links using the new **NewImportedUsers** segment.

---

## Prerequisites

Before sending welcome emails, ensure:

1. ✅ **Import Tool Execution Complete**
   - VettedMemberImport tool has successfully imported users
   - Users have `VettingStatus = Approved` (3)
   - Users have `EmailConfirmed = false`
   - Users have `IsActive = true`

2. ✅ **Email Templates Admin Access**
   - Admin logged in with Administrator role
   - Access to Email Templates admin page at `/admin/email-templates`

3. ✅ **SendGrid Configuration**
   - SendGrid API key configured (production/staging only)
   - Development mode: Emails logged to console instead

---

## Available User Segments

The email system supports **8 user segments** for targeted email sending:

| Segment Name | Filter Criteria | Typical Use Case |
|--------------|----------------|------------------|
| **NewImportedUsers** | `VettingStatus == Approved AND EmailConfirmed == false AND IsActive == true` | Send welcome emails to imported vetted members who need password reset links |
| **AllVettedMembers** | `VettingStatus == Approved` (3) | Send announcements to all vetted members |
| **AllPreVettedMembers** | `VettingStatus NOT IN (Denied, OnHold) AND IsActive == true` | Send updates to members in vetting process |
| **AllTeachers** | Role contains "Teacher" | Send teacher-specific communications |
| **AllDMs** | Role contains "DungeonMonitor" | Send DM-specific communications |
| **AllSafetyTeam** | Role contains "SafetyTeam" | Send safety team communications |
| **AllAdmins** | Role contains "Administrator" | Send admin team communications |
| **EmailNotVerified** | `EmailConfirmed == false` | Resend verification emails |
| **VettingPending** | `VettingStatus == UnderReview` (0) | Send status updates to applicants |

---

## NewImportedUsers Segment Details

### Who is Included?

**Filter**: Users who are:
- ✅ **Approved**: `VettingStatus == 3` (Approved)
- ✅ **Email Not Confirmed**: `EmailConfirmed == false`
- ✅ **Active**: `IsActive == true`

**Typical Users**:
- Members imported via VettedMemberImport tool
- Members who haven't yet set their password
- Members who haven't completed email verification

### When to Use This Segment

Use **NewImportedUsers** when:
- ✅ After running VettedMemberImport tool (fully vetted members)
- ✅ After importing interview-approved members (if sending welcome messages)
- ✅ Sending password reset links to bulk imported users
- ✅ Re-sending welcome emails to users who didn't activate

---

## Available Email Variables

### Per-User Variables (Unique Tokens)

These variables generate **unique values for each recipient**:

| Variable | Description | Example Output |
|----------|-------------|----------------|
| `{{user_name}}` | User's scene name | "RopeArtist42" |
| `{{reset_url}}` | Unique password reset link | `https://witchcityrope.com/reset-password?token=abc123...` |
| `{{verification_url}}` | Unique email verification link | `https://witchcityrope.com/verify-email?token=xyz789...` |

**CRITICAL**: These variables require **per-user email sending** (not bulk). The system automatically sends individual emails when these variables are used.

### System-Wide Variables

| Variable | Description | Value |
|----------|-------------|-------|
| `{{system_url}}` | Frontend base URL | `https://witchcityrope.com` (hardcoded) |

**Note**: Contact emails (`support@witchcityrope.com`, `info@witchcityrope.com`, `events@witchcityrope.com`) are hardcoded in templates as of 2025-11-17.

---

## Step-by-Step Workflow

### Step 1: Import Vetted Members

**Tool**: VettedMemberImport console application

**Command**:
```bash
cd /home/chad/repos/witchcityrope/tools/VettedMemberImport/VettedMemberImport

# Dry-run first (ALWAYS)
dotnet run -- --input=accepted.csv --dry-run --environment=Production

# Actual import
dotnet run -- --input=accepted.csv --environment=Production
```

**Result**:
- Users imported with `EmailConfirmed = false`
- Users need password reset to activate accounts

**Next**: Send welcome emails with password reset links

---

### Step 2: Navigate to Email Templates Admin

1. **Login** as Administrator
2. **Navigate** to `/admin/email-templates`
3. **Click** "Ad-Hoc" tab in the email templates interface

---

### Step 3: Select NewImportedUsers Segment

**In the "Send Ad-Hoc Email" section**:

1. **Segment Selector**: Choose "NewImportedUsers" from dropdown
2. **Recipient Count**: Verify count matches expected import size
   - Example: "140 recipients"
3. **Preview Recipients**: View first 10 users who will receive email
   - Shows: Scene Name, Email Address

**Verification**:
- ✅ Recipient count matches import tool output
- ✅ Preview shows expected users
- ✅ All users have `EmailConfirmed = false`

---

### Step 4: Select or Create Email Template

#### Option A: Use NewWebsiteUser Template (Recommended)

**Pre-Configured Template**:
- **Template Name**: NewWebsiteUser
- **Category**: Admin
- **Subject**: "Welcome to WitchCityRope - Set Your Password"
- **Contains**: `{{user_name}}` and `{{reset_url}}` variables
- **Purpose**: Welcome message with password reset link

**To Use**:
1. Click "NewWebsiteUser" template card
2. Review subject and body
3. Verify `{{reset_url}}` variable present

#### Option B: Create Custom Template

**If customization needed**:
1. Click "Create New Template" or edit existing
2. **Subject Line**: Enter subject
3. **Body**: Use rich text editor
4. **Include Variables**:
   - `{{user_name}}` - Personalizes greeting
   - `{{reset_url}}` - Critical password reset link
5. **Save** template

**Example Template Body**:
```html
<p>Hi {{user_name}},</p>

<p>Welcome to WitchCityRope! Your vetted member account has been created.</p>

<p>To complete your account setup, please set your password by clicking the link below:</p>

<p><a href="{{reset_url}}" style="color: #880124;">Set Your Password</a></p>

<p>This link is valid for 24 hours. If you need assistance, contact us at support@witchcityrope.com.</p>

<p>Best regards,<br>The WitchCityRope Team</p>
```

---

### Step 5: Send Emails

**In the Send Ad-Hoc Email section**:

1. **Review**:
   - ✅ Segment: NewImportedUsers
   - ✅ Recipient count correct
   - ✅ Template selected
   - ✅ `{{reset_url}}` variable present

2. **Click** "Send Email" button

3. **Confirmation Dialog**:
   - Shows final recipient count
   - Warns about per-user sending (if using unique variables)
   - **Confirm**: Click "Send"

4. **Processing**:
   - System sends **one email per user** (unique tokens)
   - Each user gets unique password reset link
   - Progress shown (if available)

5. **Success Notification**:
   - "Emails sent successfully to 140 recipients"
   - Emails logged to `SentAdHocEmails` table

---

### Step 6: Verify Email Sending

#### Development Environment

**Emails logged to console** (not actually sent):

To view email logs in development, check the API container output. Look for lines containing "DEVELOPMENT MODE" or "EMAIL".

**Expected Output**:
```
[INFO] DEVELOPMENT MODE: Would send email to: user1@example.com
       Subject: Welcome to WitchCityRope - Set Your Password
       Reset URL: http://localhost:5173/reset-password?token=abc123...

[INFO] DEVELOPMENT MODE: Would send email to: user2@example.com
       Subject: Welcome to WitchCityRope - Set Your Password
       Reset URL: http://localhost:5173/reset-password?token=def456...
```

#### Staging/Production Environments

**Emails sent via SendGrid**:

1. **Check SendGrid Dashboard**:
   - Verify 140 emails queued/sent
   - Check delivery status
   - Monitor bounces/failures

2. **Database Verification**:
   ```sql
   SELECT COUNT(*)
   FROM "SentAdHocEmails"
   WHERE "SentAt" >= NOW() - INTERVAL '1 hour';
   ```

3. **User Inbox Verification**:
   - Ask test user to check inbox
   - Verify email received
   - Test password reset link

---

## Variable Replacement Details

### How Per-User Variables Work

**Backend Processing**:
1. System detects `{{reset_url}}` or `{{verification_url}}` in template
2. Switches to **per-user sending mode** (not bulk)
3. For each recipient:
   - Generates unique password reset token
   - Constructs unique reset URL with token
   - Replaces `{{user_name}}` with recipient's scene name
   - Replaces `{{reset_url}}` with unique URL
   - Sends individual email

**Token Generation**:
- ASP.NET Core Identity token generator
- Cryptographically secure random tokens
- Unique per user, per request
- 24-hour expiration (configurable)

**URL Construction**:
```
{{reset_url}} becomes:
https://witchcityrope.com/reset-password?userId=123&token=CfDJ8...
```

### Fallback Behavior

**For `{{user_name}}`**:
1. First tries: `User.SceneName`
2. Falls back to: `User.Email`
3. Last resort: `"Member"`

**Example**:
- User with SceneName "RopeArtist42" → `{{user_name}}` = "RopeArtist42"
- User with null SceneName → `{{user_name}}` = "user@example.com"
- User with null SceneName and null Email → `{{user_name}}` = "Member"

---

## Common Use Cases

### Use Case 1: Welcome Email After Full Vetting Import

**Scenario**: Imported 140 fully vetted members from Google Sheets

**Steps**:
1. Import with VettedMemberImport tool (`--status=approved`)
2. Navigate to Email Templates → Ad-Hoc tab
3. Select "NewImportedUsers" segment (should show 140 recipients)
4. Select "NewWebsiteUser" template
5. Send emails
6. Users receive password reset links
7. Users click link, set password, `EmailConfirmed` becomes `true`
8. Users removed from NewImportedUsers segment (no longer match filter)

**Expected Timeline**:
- **Immediate**: Emails sent
- **Within 24 hours**: Most users set passwords
- **After 24 hours**: Expired tokens need resend

---

### Use Case 2: Resend to Users Who Didn't Activate

**Scenario**: Some imported users didn't set password within 24 hours

**Steps**:
1. Navigate to Email Templates → Ad-Hoc tab
2. Select "NewImportedUsers" segment
3. Check recipient count (should show only unactivated users)
4. Preview recipients (verify expected users)
5. Select "NewWebsiteUser" template
6. Send emails (generates fresh tokens)
7. Monitor activation

**Verification**:
```sql
-- Find users who haven't activated (imported > 24 hours ago)
SELECT "Email", "SceneName", "CreatedAt"
FROM "Users"
WHERE "VettingStatus" = 3
  AND "EmailConfirmed" = false
  AND "IsActive" = true
  AND "CreatedAt" < NOW() - INTERVAL '24 hours';
```

---

### Use Case 3: Custom Message to Imported Users

**Scenario**: Send additional information to recently imported users

**Steps**:
1. Create custom template in Ad-Hoc category
2. Use variables:
   - `{{user_name}}` for personalization
   - Optional: `{{reset_url}}` if needed
3. Select "NewImportedUsers" segment
4. Send custom message

**Example**: Event invitation to new members:
```html
<p>Hi {{user_name}},</p>

<p>Welcome! We're excited to invite you to our upcoming New Members Social on January 15th.</p>

<p>Details: [Event details here]</p>

<p>If you haven't set your password yet, please do so here: {{reset_url}}</p>
```

---

## Troubleshooting

### Issue: Segment Shows 0 Recipients

**Possible Causes**:
1. ❌ All imported users already activated (`EmailConfirmed = true`)
2. ❌ Import tool didn't set `IsActive = true`
3. ❌ Import tool set wrong `VettingStatus`

**Solution**:
```sql
-- Check user statuses
SELECT "VettingStatus", "EmailConfirmed", "IsActive", COUNT(*)
FROM "Users"
WHERE "VettingStatus" = 3
GROUP BY "VettingStatus", "EmailConfirmed", "IsActive";
```

**Expected for NewImportedUsers segment**:
```
VettingStatus | EmailConfirmed | IsActive | Count
3             | false          | true     | 140
```

---

### Issue: Users Not Receiving Emails

**Development Environment**:
- ✅ **Expected**: Emails logged to console, NOT actually sent
- ✅ **Check**: Docker logs for "DEVELOPMENT MODE" messages

**Staging/Production Environment**:
- ❌ SendGrid API key not configured
- ❌ SendGrid account suspended
- ❌ Email addresses bouncing
- ❌ Spam filters blocking

**Solution**:
1. Check SendGrid dashboard for delivery status
2. Verify API key in configuration
3. Check email logs in database:
   ```sql
   SELECT "RecipientEmail", "Status", "ErrorMessage"
   FROM "SentAdHocEmails"
   WHERE "SentAt" >= NOW() - INTERVAL '1 hour'
   ORDER BY "SentAt" DESC;
   ```

---

### Issue: Reset Links Expired

**Cause**: Users waited > 24 hours to click link

**Solution**:
1. Users still in "NewImportedUsers" segment (`EmailConfirmed = false`)
2. Resend welcome email (generates fresh tokens)
3. Advise users to click link within 24 hours

**Alternative**:
- Increase token expiration in configuration (if needed)
- Document expiration clearly in email template

---

### Issue: Wrong Users in Segment

**Cause**: Segment filter includes unexpected users

**Verification**:
```sql
-- Preview NewImportedUsers segment filter
SELECT "Id", "Email", "SceneName", "VettingStatus", "EmailConfirmed", "IsActive", "CreatedAt"
FROM "Users"
WHERE "VettingStatus" = 3
  AND "EmailConfirmed" = false
  AND "IsActive" = true
ORDER BY "CreatedAt" DESC;
```

**Expected**: Only recently imported users

**If includes wrong users**:
- Check `VettingStatus` values
- Check `EmailConfirmed` flags
- Check `IsActive` flags
- Verify import tool parameters

---

## Security Considerations

### Password Reset Token Security

**Token Characteristics**:
- ✅ Cryptographically secure random generation
- ✅ Unique per user, per request
- ✅ 24-hour expiration
- ✅ Single-use (consumed on password set)

**URL Transmission**:
- ✅ HTTPS required in production
- ✅ Tokens in URL parameters (industry standard)
- ✅ No tokens stored in plain text
- ✅ Tokens hashed in database

### Email Address Privacy

**Who can see recipient emails**:
- ✅ Administrators only (Email Templates admin page)
- ✅ Preview shows first 10 recipients
- ✅ Full list not exposed to frontend
- ✅ Sent emails logged for audit

**Data Protection**:
- ✅ Emails sent individually (no CC/BCC exposure)
- ✅ No recipient list disclosure
- ✅ GDPR-compliant data handling

---

## Performance Considerations

### Bulk Email Sending

**Per-User Variables**:
- Sends **one email per recipient** (required for unique tokens)
- **Performance**: ~100-200 emails/minute (SendGrid rate limit)
- **Time Estimate**: 140 users = ~1-2 minutes

**System-Wide Variables Only**:
- Can use bulk sending (if implemented)
- Faster processing
- Shared content across recipients

### Database Load

**Queries**:
- Segment filtering: Efficient indexes on `VettingStatus`, `EmailConfirmed`, `IsActive`
- Token generation: Minimal overhead
- Email logging: Batch inserts

**Optimization**:
- Segments cached for performance
- Preview limited to 10 users
- Async email sending

---

## Related Documentation

### Import Tool Documentation

- **Import Tool README**: `/home/chad/repos/witchcityrope/tools/VettedMemberImport/README.md`
- **Usage Guide**: `/home/chad/repos/witchcityrope/docs/functional-areas/user-management/guides/vetted-member-import-usage-guide.md`
- **Functional Area**: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/README.md`

### Email Templates Documentation

- **Admin Management**: `/home/chad/repos/witchcityrope/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/`
- **Static Variables**: `/home/chad/repos/witchcityrope/docs/functional-areas/email-templates/new-work/2025-11-17-hardcode-static-variables/`

### Member Import & Email Enhancement

- **Orchestrator Handoff**: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/handoffs/orchestrator-2025-11-18-handoff.md`
- **Backend Email Handoff**: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/handoffs/backend-developer-email-2025-11-18-handoff.md`

---

## FAQ

### Q: Can I send to NewImportedUsers multiple times?

**A**: Yes! Segment updates in real-time:
- Users who set passwords (`EmailConfirmed = true`) automatically removed from segment
- Users who haven't activated remain in segment
- Safe to resend to segment multiple times

### Q: What if I need to send different messages to different import batches?

**A**: Use custom segments:
1. Tag users during import (custom field)
2. Filter by `CreatedAt` date range
3. Or manually select users in admin interface (if feature exists)

### Q: Can I schedule emails for later?

**A**: Not currently supported. Feature request:
- Store email as draft
- Schedule send time
- Cron job processes scheduled emails

### Q: How do I know if users activated their accounts?

**A**: Monitor NewImportedUsers segment count:
- Decreasing count = users activating
- Stable count after 48 hours = need follow-up

**Query**:
```sql
SELECT COUNT(*) AS "Still Unactivated"
FROM "Users"
WHERE "VettingStatus" = 3
  AND "EmailConfirmed" = false
  AND "IsActive" = true
  AND "CreatedAt" < NOW() - INTERVAL '48 hours';
```

### Q: Can I send bulk emails to other segments?

**A**: Yes! All 8 segments support ad-hoc emails:
- **AllVettedMembers**: Announcements to vetted community
- **AllTeachers**: Teacher-specific updates
- **AllSafetyTeam**: Safety team communications
- **EmailNotVerified**: Resend verification emails
- etc.

---

## Change History

**2025-12-14**: Initial documentation
- Documented NewImportedUsers segment
- Documented per-user variable replacement
- Documented complete workflow for post-import emails
- Documented all 8 available user segments
- Added troubleshooting section
- Added security and performance considerations

---

**Document Metadata**:
- **Version**: 1.0
- **Last Updated**: 2025-12-14
- **Owner**: Backend Team + Librarian
- **Status**: Active
