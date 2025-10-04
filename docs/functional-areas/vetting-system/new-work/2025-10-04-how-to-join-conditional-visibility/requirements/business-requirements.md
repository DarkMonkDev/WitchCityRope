# Business Requirements: Conditional "How to Join" Menu Visibility
<!-- Last Updated: 2025-10-04 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Draft -->

## Executive Summary

This feature enhances the user experience by intelligently showing or hiding the "How to Join" menu item based on the user's vetting application status, and displays contextual "next steps" information for users with pending applications. This prevents confusion for users who have already been vetted or whose applications are in specific states where reapplication is not appropriate.

## Business Context

### Problem Statement

Currently, all users see the "How to Join" menu item regardless of their vetting status, creating several user experience issues:

- **Vetted members** (already approved) see a menu item they no longer need, creating confusion
- **Banned users** or **users on hold** can attempt to submit new applications when they shouldn't
- **Users with pending applications** don't have clear visibility into their next steps
- **First-time visitors** need to easily find how to join the community

The lack of conditional logic creates unnecessary navigation clutter and doesn't provide helpful status information to applicants.

### Business Value

- **Improved User Experience**: Only show relevant navigation options based on user state
- **Reduced Confusion**: Vetted members don't see irrelevant "join" prompts
- **Better Communication**: Clear next steps for applicants in process
- **Process Transparency**: Users understand where they are in the vetting workflow
- **Reduced Support Burden**: Self-service status information reduces admin questions

### Success Metrics

- **Menu Relevance**: 100% of users see contextually appropriate navigation
- **User Satisfaction**: 90%+ of users understand their vetting status without contacting admin
- **Support Reduction**: 30% reduction in "what's my application status?" inquiries
- **Application Completion**: No increase in abandoned applications due to status confusion
- **Error Prevention**: 0 duplicate applications from users with existing pending applications

## Current State Analysis

### Existing Vetting System (as of 2025-10-04)

**✅ Backend API Complete**:
- `VettingStatus` enum with 10 states (Draft, Submitted, UnderReview, InterviewApproved, PendingInterview, InterviewScheduled, OnHold, Approved, Denied, Withdrawn)
- `GET /api/vetting/status` endpoint returns current user's application status
- `MyApplicationStatusResponse` with detailed status information
- `ApplicationUser` model with `IsVetted` boolean and `VettingStatus` integer fields
- Complete status description and next steps logic in `VettingService`

**✅ Frontend Components**:
- `VettingApplicationForm` component (React + Mantine)
- Authentication state management via Zustand
- React Router navigation structure

**❌ Missing Features**:
- Conditional menu visibility logic
- Status display on "How to Join" page for users with applications
- "Next steps" contextual information box
- Integration between vetting status API and navigation component

### Verified Vetting Statuses

Based on `VettingApplication.cs` entity (lines 86-97):

| Status ID | Status Name | Description |
|-----------|-------------|-------------|
| 0 | Draft | Application started but not submitted |
| 1 | Submitted | Application submitted, awaiting review |
| 2 | UnderReview | Currently being reviewed by admin |
| 3 | InterviewApproved | Approved for interview phase |
| 4 | PendingInterview | Waiting for interview to be scheduled |
| 5 | InterviewScheduled | Interview date/time confirmed |
| 6 | OnHold | Additional information needed or temporary pause |
| 7 | Approved | Vetting approved - user is vetted member |
| 8 | Denied | Application denied |
| 9 | Withdrawn | User withdrew application |

**Additional User State**: `ApplicationUser.IsVetted` boolean field (line 51) indicates vetted member status.

### User States & Menu Visibility Logic

| User State | Has Application | Vetting Status | IsVetted | Show "How to Join"? | Reason |
|------------|----------------|----------------|----------|---------------------|--------|
| **Guest/New User** | No | N/A | false | ✅ YES | Need to see how to apply |
| **Member (No App)** | No | N/A | false | ✅ YES | Need to see how to apply |
| **Submitted** | Yes | Submitted (1) | false | ✅ YES (with status) | Show status, can view application |
| **Under Review** | Yes | UnderReview (2) | false | ✅ YES (with status) | Show progress, next steps |
| **Interview Process** | Yes | InterviewApproved (3-5) | false | ✅ YES (with status) | Show interview status |
| **On Hold** | Yes | OnHold (6) | false | ❌ NO | Cannot reapply while on hold |
| **Approved/Vetted** | Yes | Approved (7) | true | ❌ NO | Already vetted, no longer relevant |
| **Denied** | Yes | Denied (8) | false | ❌ NO | Cannot immediately reapply |
| **Withdrawn** | Yes | Withdrawn (9) | false | ✅ YES | Can submit new application |
| **Draft** | Yes | Draft (0) | false | ✅ YES | Can complete draft application |

**Business Rule Discovery**: Users with status OnHold, Approved (Vetted), or Denied should NOT see "How to Join" menu item.

## User Stories

### Story 1: Guest/New User Sees "How to Join"
**As a** guest or new user without a vetting application
**I want to** see the "How to Join" menu item
**So that** I can learn about the vetting process and submit an application

**Acceptance Criteria:**
- Given I am not logged in (guest)
- When I view the main navigation menu
- Then I see the "How to Join" menu item
- And clicking it takes me to the vetting application page
- And I see information about how to become a vetted member

**Business Rules:**
- Menu item always visible for unauthenticated users
- Menu links to `/vetting/apply` or equivalent route
- Page shows vetting process overview and application form

### Story 2: Authenticated User Without Application Sees "How to Join"
**As a** logged-in member without a vetting application
**I want to** see the "How to Join" menu item
**So that** I can submit my vetting application

**Acceptance Criteria:**
- Given I am logged in as a member
- And I have never submitted a vetting application
- When I view the main navigation menu
- Then I see the "How to Join" menu item
- And clicking it shows the vetting application form
- And the form is pre-populated with my account information (email, scene name)

**Business Rules:**
- Check `GET /api/vetting/status` endpoint to determine if user has application
- If `HasApplication: false`, show menu item
- Form should use authenticated user's data for pre-population

### Story 3: User with Pending Application Sees Status and Next Steps
**As a** member with a submitted vetting application
**I want to** see my application status and next steps on the "How to Join" page
**So that** I understand where I am in the process and what happens next

**Acceptance Criteria:**
- Given I am logged in as a member
- And I have a vetting application with status: Submitted, UnderReview, or Interview-related (statuses 1-5)
- When I navigate to the "How to Join" page
- Then I see a prominent status box at the top of the page
- And the box shows my current application status in user-friendly language
- And the box shows my "next steps" specific to my current status
- And I can see my application number and submission date
- And I cannot submit a new application (form is hidden or disabled)
- And the "How to Join" menu item is still visible (so I can check my status)

**Status Box Examples:**

**Submitted Status:**
```
┌─────────────────────────────────────────────────────────┐
│ 📋 Application Status: Submitted                        │
│                                                          │
│ Application #: VET-20251004-ABC123                      │
│ Submitted: October 4, 2025                              │
│                                                          │
│ Your application is in queue for review. Our vetting    │
│ committee will begin reviewing applications within      │
│ 3-5 business days.                                      │
│                                                          │
│ Next Steps: No action needed - we'll contact you via    │
│ email when your application enters review.              │
│                                                          │
│ Estimated review time: 14 days                          │
└─────────────────────────────────────────────────────────┘
```

**Under Review Status:**
```
┌─────────────────────────────────────────────────────────┐
│ 🔍 Application Status: Under Review                     │
│                                                          │
│ Application #: VET-20251004-ABC123                      │
│ Last Updated: October 6, 2025                           │
│                                                          │
│ Your application is currently being reviewed by our     │
│ vetting committee. References may be contacted soon.    │
│                                                          │
│ Next Steps: Watch your email for reference contact      │
│ notifications or additional information requests.       │
│                                                          │
│ Estimated time remaining: 7-10 days                     │
└─────────────────────────────────────────────────────────┘
```

**Interview Approved Status:**
```
┌─────────────────────────────────────────────────────────┐
│ ✅ Application Status: Interview Approved                │
│                                                          │
│ Application #: VET-20251004-ABC123                      │
│ Last Updated: October 10, 2025                          │
│                                                          │
│ Congratulations! Your application has been approved for │
│ the interview phase. Someone will contact you soon to   │
│ schedule your interview.                                │
│                                                          │
│ Next Steps: Wait for email with interview scheduling    │
│ options. Respond promptly to schedule your interview.   │
└─────────────────────────────────────────────────────────┘
```

**Business Rules:**
- Status information comes from `GET /api/vetting/status` endpoint
- Use `StatusDescription` and `NextSteps` fields from API response
- Show `EstimatedDaysRemaining` if available
- Application number format: `VET-YYYYMMDD-XXXXX` (from API)
- Menu item remains visible so user can return to check status

### Story 4: Vetted Member Does NOT See "How to Join"
**As a** vetted member (approved status)
**I want to** NOT see the "How to Join" menu item
**So that** my navigation is clean and relevant to my current status

**Acceptance Criteria:**
- Given I am logged in as a member
- And my vetting application status is "Approved" (status 7)
- Or my `IsVetted` flag is true
- When I view the main navigation menu
- Then I do NOT see the "How to Join" menu item
- And I cannot navigate to the vetting application page via menu
- And if I manually navigate to the vetting URL, I see a message: "You are already a vetted member"

**Business Rules:**
- Check both `VettingStatus === "Approved"` AND `IsVetted === true`
- Either condition triggers menu hiding
- Direct URL access should show friendly message, not error
- Alternative navigation options (e.g., "Member Dashboard") should be promoted

### Story 5: User on Hold Does NOT See "How to Join"
**As a** user with application on hold
**I want to** NOT see the "How to Join" menu item
**So that** I don't attempt to submit a duplicate application

**Acceptance Criteria:**
- Given I am logged in as a member
- And my vetting application status is "OnHold" (status 6)
- When I view the main navigation menu
- Then I do NOT see the "How to Join" menu item
- And if I manually navigate to the vetting URL, I see a message explaining my on-hold status
- And the message provides contact information for questions

**On-Hold Status Page Message:**
```
┌─────────────────────────────────────────────────────────┐
│ ⏸ Application Status: On Hold                           │
│                                                          │
│ Application #: VET-20251004-ABC123                      │
│ Last Updated: October 8, 2025                           │
│                                                          │
│ Your application is currently on hold. This typically   │
│ means we need additional information or there's a       │
│ temporary pause in processing.                          │
│                                                          │
│ Please check your email for any requests from our       │
│ vetting committee. If you have questions, contact:      │
│ vetting@witchcityrope.com                               │
│                                                          │
│ You cannot submit a new application while your current  │
│ application is on hold.                                 │
└─────────────────────────────────────────────────────────┘
```

**Business Rules:**
- OnHold status (6) hides menu item
- Direct URL navigation shows status message, not application form
- Email contact provided for admin communication
- User cannot submit new application while on hold
- Admin must change status to allow reapplication

### Story 6: Denied User Does NOT See "How to Join"
**As a** user with denied application
**I want to** NOT see the "How to Join" menu item immediately
**So that** I understand I cannot immediately reapply

**Acceptance Criteria:**
- Given I am logged in as a member
- And my vetting application status is "Denied" (status 8)
- When I view the main navigation menu
- Then I do NOT see the "How to Join" menu item
- And if I manually navigate to the vetting URL, I see a message about the denial
- And the message explains the reapplication policy (typically 6 months)

**Denied Status Page Message:**
```
┌─────────────────────────────────────────────────────────┐
│ ❌ Application Status: Not Approved                      │
│                                                          │
│ Application #: VET-20251004-ABC123                      │
│ Decision Date: October 15, 2025                         │
│                                                          │
│ Your vetting application was not approved at this time. │
│                                                          │
│ Per our community guidelines, you may reapply after     │
│ 6 months from the decision date (April 15, 2026).       │
│                                                          │
│ If you have questions about this decision, please       │
│ contact: vetting@witchcityrope.com                      │
└─────────────────────────────────────────────────────────┘
```

**Business Rules:**
- Denied status (8) hides menu item
- 6-month waiting period before reapplication (configurable)
- Calculate reapplication date from `DecisionMadeAt` timestamp
- After 6 months, menu item should reappear (requires status change to "Withdrawn" or new application support)
- Contact email provided for appeals/questions

### Story 7: User with Withdrawn Application Sees "How to Join"
**As a** user who withdrew their application
**I want to** see the "How to Join" menu item
**So that** I can submit a new application when ready

**Acceptance Criteria:**
- Given I am logged in as a member
- And my vetting application status is "Withdrawn" (status 9)
- When I view the main navigation menu
- Then I see the "How to Join" menu item
- And clicking it allows me to submit a new application
- And my previous application data is NOT pre-populated (fresh start)

**Business Rules:**
- Withdrawn status (9) allows new application
- User can start completely fresh application
- Previous application remains in history for admin reference
- No waiting period required for withdrawn applications

## Business Rules

### Menu Visibility Rules

1. **Show "How to Join" Menu Item When:**
   - User is not authenticated (guest)
   - User has no vetting application (`HasApplication: false`)
   - User has application with status: Draft (0), Submitted (1), UnderReview (2), InterviewApproved (3), PendingInterview (4), InterviewScheduled (5), or Withdrawn (9)

2. **Hide "How to Join" Menu Item When:**
   - User has application with status: OnHold (6), Approved (7), or Denied (8)
   - User's `IsVetted` field is `true`

3. **Status Display Rules:**
   - For statuses 1-5 (Submitted through InterviewScheduled): Show status box with next steps
   - For status 6 (OnHold): Show on-hold message with admin contact
   - For status 7 (Approved): Show "already vetted" message
   - For status 8 (Denied): Show denial message with reapplication date
   - For status 9 (Withdrawn): Show fresh application form

### API Integration Rules

1. **Status Check Endpoint**: `GET /api/vetting/status`
   - Returns: `MyApplicationStatusResponse`
   - Fields used: `HasApplication`, `Application.Status`, `Application.StatusDescription`, `Application.NextSteps`, `Application.EstimatedDaysRemaining`

2. **Data Caching**:
   - Cache vetting status for 5 minutes to reduce API calls
   - Invalidate cache on application submission or status change
   - Refresh on page navigation to "How to Join"

3. **Error Handling**:
   - If API call fails: Show menu item (fail-open approach)
   - Log error for monitoring
   - Retry API call on next navigation

### User Experience Rules

1. **Status Box Display**:
   - Always at top of "How to Join" page when user has pending application
   - Prominent visual styling (border, background color, icon)
   - Status-specific icons: 📋 (Submitted), 🔍 (Review), ✅ (Interview), ⏸ (Hold), ❌ (Denied)
   - Mobile-responsive design

2. **Navigation Behavior**:
   - Menu item hidden/shown updates immediately on login
   - Status changes reflected within 5 minutes (cache expiry)
   - Direct URL navigation always works (shows appropriate message/form)

3. **Application Form Display**:
   - Hidden for statuses: OnHold (6), Approved (7), Denied (8)
   - Shown for statuses: Draft (0), Withdrawn (9), or no application
   - Disabled/read-only for statuses: Submitted (1) through InterviewScheduled (5) - show view-only mode

## Constraints & Assumptions

### Technical Constraints
- Must use existing `GET /api/vetting/status` endpoint (no backend changes required)
- Must integrate with existing React navigation component structure
- Must respect existing authentication state management (Zustand)
- Must work with Mantine UI component library

### Business Constraints
- Cannot change vetting workflow or status progression
- Must maintain audit trail for all status changes
- Admin can manually override status to change menu visibility
- 6-month waiting period for denied applications (business policy)

### Assumptions
- User has stable internet connection for API status checks
- Navigation component can conditionally render menu items
- Vetting status API returns accurate, real-time data
- User roles and permissions are properly enforced at API level
- Email notifications for status changes are handled separately

## Security & Privacy Requirements

### Authorization
- Only authenticated users can check their own vetting status
- Status API must validate user identity via JWT token
- Users cannot view other users' application statuses
- Admin role required to change vetting status

### Data Privacy
- Application status visible only to applicant and admins
- Application details (references, notes) NOT exposed in status API
- Denial reasons NOT shown in public-facing status messages
- Status token for anonymous tracking NOT used in this feature

### Safety Considerations
- Banned users (Denied status) cannot easily submit duplicate applications
- OnHold status prevents reapplication during investigation
- Clear communication reduces user frustration and support burden
- Contact email provided for escalation and questions

## Compliance Requirements

### Community Guidelines
- Vetting process must maintain community safety standards
- Denial waiting period enforces fair reapplication policy
- Transparency in status communication builds trust
- Admin contact available for appeals and questions

### Platform Policies
- GDPR compliance: User can request application data deletion
- Right to explanation: User can request denial reasoning (via admin contact)
- Data retention: Application history retained for audit purposes
- Communication preferences: Email notifications respect user settings

## User Impact Analysis

| User Type | Impact | Priority | Notes |
|-----------|--------|----------|-------|
| **Guest/Unauthenticated** | Low | Low | Menu item always visible - no change |
| **New Members (No App)** | Low | Medium | Menu item visible - encourages application |
| **Pending Applicants** | High | High | Improved transparency, clear next steps |
| **Vetted Members** | Medium | High | Cleaner navigation, less confusion |
| **OnHold Users** | High | High | Clear communication, prevents duplicate apps |
| **Denied Users** | High | High | Clear policy communication, reduces support |
| **Withdrawn Users** | Low | Low | Can reapply easily when ready |
| **Admins** | Low | Medium | Fewer support questions about status |

## Examples/Scenarios

### Scenario 1: Happy Path - New User Applies
1. **Initial State**: User registers account, has no vetting application
2. **Navigation**: User sees "How to Join" in menu
3. **Page Visit**: User clicks menu, sees vetting process overview and application form
4. **Application**: User completes and submits application
5. **Post-Submission**: User redirected to confirmation page
6. **Return Visit**: User clicks "How to Join" again, sees status box (Submitted status)
7. **Next Steps Shown**: "No action needed - we'll contact you via email"
8. **Menu Remains**: "How to Join" still visible for status checking

**Expected Behavior**: Menu item visible throughout, status updates shown on return visits.

### Scenario 2: Vetted Member Login
1. **Initial State**: User has Approved vetting status, `IsVetted: true`
2. **Login**: User logs in with credentials
3. **Navigation Check**: System calls `GET /api/vetting/status`
4. **API Response**: `HasApplication: true, Status: "Approved"`
5. **Menu Render**: "How to Join" menu item is hidden
6. **Dashboard**: User sees "Member Dashboard" or other vetted-member options
7. **Direct URL**: If user navigates to `/vetting/apply`, sees "already vetted" message

**Expected Behavior**: Menu item hidden, direct access shows friendly message.

### Scenario 3: Application Placed on Hold
1. **Initial State**: User has Submitted application
2. **Admin Action**: Admin changes status to OnHold (needs more information)
3. **User Login**: User logs in next day
4. **Navigation Check**: System calls `GET /api/vetting/status`
5. **API Response**: `HasApplication: true, Status: "OnHold"`
6. **Menu Render**: "How to Join" menu item is hidden
7. **Direct URL**: User navigates to `/vetting/apply`, sees on-hold message
8. **Message Content**: Explanation of hold status, admin contact email
9. **Form Hidden**: Application form not accessible

**Expected Behavior**: Menu hidden, clear communication, no duplicate application possible.

### Scenario 4: Application Denied, Reapplication Attempt
1. **Initial State**: User's application denied on October 1, 2025
2. **Immediate Login**: User logs in October 2, 2025
3. **Menu Check**: "How to Join" is hidden (Denied status)
4. **Direct URL**: User tries to access `/vetting/apply`
5. **Message Shown**: Denial message with reapplication date (April 1, 2026)
6. **Contact Provided**: Admin email for questions
7. **6 Months Later**: Admin manually changes status to allow reapplication OR system workflow creates new application path
8. **Menu Reappears**: User can now see "How to Join" again

**Expected Behavior**: Immediate denial period enforced, clear reapplication timeline, admin contact available.

### Scenario 5: Interview Scheduled - User Checks Status
1. **Initial State**: Application approved for interview, status = InterviewScheduled
2. **Navigation**: User clicks "How to Join" to check status
3. **Status Box Shown**: Interview scheduled status with date/time
4. **Next Steps**: "Attend your scheduled interview on [date]"
5. **Calendar Link**: Optional calendar integration for interview appointment
6. **Form Hidden**: Application form replaced with status view
7. **Menu Visible**: Can return to check status anytime

**Expected Behavior**: Clear interview information, actionable next steps, menu remains for easy access.

### Scenario 6: Edge Case - API Failure During Status Check
1. **Initial State**: User logs in, navigation renders
2. **API Call**: `GET /api/vetting/status` called
3. **API Failure**: Network timeout or server error
4. **Fallback Behavior**: Menu item IS shown (fail-open)
5. **Error Logged**: Client-side error logged for monitoring
6. **User Experience**: User sees "How to Join" and can navigate
7. **Page Load**: Vetting page attempts fresh API call
8. **Success**: If API succeeds on page load, correct status shown
9. **Continued Failure**: Graceful error message with retry option

**Expected Behavior**: Fail-open approach ensures access, errors logged for debugging.

## Implementation Considerations

### Frontend Components Required

1. **Navigation Component Updates** (`apps/web/src/components/Navigation.tsx` or equivalent):
   - Add conditional rendering logic for "How to Join" menu item
   - Integrate vetting status API call
   - Cache status result for 5 minutes
   - Handle loading and error states

2. **How to Join Page Component** (`apps/web/src/pages/HowToJoin.tsx` or equivalent):
   - Display status box at top for users with applications
   - Conditionally show application form vs status message
   - Handle all 10 vetting status states
   - Mobile-responsive status boxes

3. **Vetting Status Hook** (`apps/web/src/features/vetting/hooks/useVettingStatus.ts`):
   - Wraps `GET /api/vetting/status` API call
   - Implements caching logic (React Query)
   - Provides menu visibility logic
   - Exports status display helpers

### API Endpoints (No Changes Required)

**Existing Endpoint**: `GET /api/vetting/status`
- **Response**: `MyApplicationStatusResponse`
- **Authentication**: Required (JWT token)
- **Response Fields**:
  - `HasApplication: boolean`
  - `Application?: ApplicationStatusInfo`
    - `ApplicationId: Guid`
    - `ApplicationNumber: string`
    - `Status: string` (enum name)
    - `StatusDescription: string` (user-friendly)
    - `SubmittedAt: DateTime`
    - `LastUpdated: DateTime`
    - `NextSteps: string?`
    - `EstimatedDaysRemaining: int?`

### React Query Integration

```typescript
// Example usage - NOT implementation code
const { data: vettingStatus, isLoading } = useQuery({
  queryKey: ['vetting-status'],
  queryFn: () => vettingApi.getMyStatus(),
  staleTime: 5 * 60 * 1000, // 5 minutes
  enabled: isAuthenticated
});

const showHowToJoinMenu = useMemo(() => {
  if (!isAuthenticated) return true; // Always show for guests
  if (isLoading) return true; // Show while loading
  if (!vettingStatus) return true; // Fail-open if no data

  // Hide for: OnHold (6), Approved (7), Denied (8)
  const hideStatuses = ['OnHold', 'Approved', 'Denied'];

  if (!vettingStatus.HasApplication) return true;
  if (hideStatuses.includes(vettingStatus.Application.Status)) return false;

  return true;
}, [isAuthenticated, isLoading, vettingStatus]);
```

### Status Box Components

```typescript
// Status box component structure - NOT implementation code
interface StatusBoxProps {
  status: VettingStatus;
  applicationNumber: string;
  submittedAt: Date;
  lastUpdated: Date;
  statusDescription: string;
  nextSteps?: string;
  estimatedDaysRemaining?: number;
}

// Different status boxes for each state
<SubmittedStatusBox {...props} />
<UnderReviewStatusBox {...props} />
<InterviewApprovedStatusBox {...props} />
<OnHoldStatusBox {...props} />
<ApprovedStatusBox {...props} />
<DeniedStatusBox {...props} />
```

## Questions for Product Manager

- [ ] **Denial Reapplication Policy**: Confirm 6-month waiting period is correct policy. Is this configurable or hard-coded?
- [ ] **OnHold Escalation**: Should on-hold status have automatic timeout (e.g., 30 days) or remain until admin action?
- [ ] **Withdrawn Status Behavior**: Can users withdraw their own applications, or admin-only action?
- [ ] **Status Change Notifications**: Should users receive email when status changes affect menu visibility (e.g., moved to OnHold)?
- [ ] **Mobile Navigation**: How should status information be displayed in mobile hamburger menu?
- [ ] **Analytics Tracking**: Should we track how often users check their application status?
- [ ] **Admin Override**: Can admins bypass the denial waiting period for special cases?
- [ ] **Calendar Integration**: For InterviewScheduled status, should we provide calendar (.ics) download link?
- [ ] **Multiple Applications**: Future consideration - should users ever be allowed to have multiple applications (e.g., after withdrawal)?

## Quality Gate Checklist (95% Required)

- [x] All user roles addressed (Guest, Member, Vetted, OnHold, Denied)
- [x] Clear acceptance criteria for each user story
- [x] Business value clearly defined (reduced confusion, better communication)
- [x] Edge cases considered (API failure, withdrawn apps, direct URL navigation)
- [x] Security requirements documented (authorization, privacy)
- [x] Compliance requirements checked (GDPR, community guidelines)
- [x] Performance expectations set (5-minute cache, fail-open)
- [x] Mobile experience considered (responsive status boxes)
- [x] Examples provided (6 detailed scenarios)
- [x] Success metrics defined (100% contextual accuracy, 30% support reduction)
- [x] Existing vetting system analyzed (10 statuses verified)
- [x] API integration points documented (GET /api/vetting/status)
- [x] User impact analysis completed (8 user types)
- [x] Business rules clearly defined (visibility rules, API integration)
- [x] Status descriptions documented for all 10 states
- [x] Error handling strategy defined (fail-open, retry logic)
- [x] Data privacy considerations addressed
- [x] Implementation considerations outlined (components, hooks, API)
- [x] Questions for PM documented (9 clarification points)

## Next Steps

1. **Product Manager Review**: Address questions above and approve requirements
2. **UI Design**: Create mockups for status boxes and denied/on-hold messages
3. **Technical Design**: Define component architecture and state management approach
4. **Implementation**: Develop navigation logic, status boxes, and API integration
5. **Testing**: Verify all 10 status states render correctly, menu visibility accurate
6. **Documentation**: Update user guide with status checking instructions
