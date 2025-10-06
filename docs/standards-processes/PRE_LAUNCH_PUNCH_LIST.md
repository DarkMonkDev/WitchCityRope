# Pre-Launch Functionality Punch List
<!-- Last Updated: 2025-10-06 -->
<!-- Version: 1.1 -->
<!-- Owner: Project Team -->
<!-- Status: Active -->

## Purpose
This document tracks all remaining functionality needed before WitchCityRope's first production launch. Items are categorized by feature area with priority ratings, effort estimates, and current status.

## Using This Document

### Adding New Items
Use the orchestrator command when testing reveals missing functionality:
```bash
/orchestrate Add [feature] to pre-launch punch list
```

### Item Template
```markdown
- [ ] **Feature Name** (Priority: High/Medium/Low)
  - **Description**: Brief description of what needs to be built
  - **Business Value**: Why this matters for launch
  - **Effort**: Small (hours) / Medium (1-2 days) / Large (3+ days)
  - **Dependencies**: What must be done first
  - **Status**: Not Started / In Progress / Blocked / Complete
  - **Notes**: Additional context, blockers, or decisions
```

## Launch Readiness Dashboard

| Category | Total Items | Complete | In Progress | Blocked | Not Started | % Complete |
|----------|-------------|----------|-------------|---------|-------------|------------|
| **Core Authentication & Authorization** | 2 | 1 | 1 | 0 | 0 | 50% |
| **Event Management & RSVP** | 8 | 3 | 2 | 0 | 3 | 38% |
| **Vetting System** | 6 | 3 | 2 | 0 | 1 | 50% |
| **Payment Processing** | 3 | 1 | 1 | 0 | 1 | 33% |
| **Admin Tools** | 4 | 1 | 1 | 0 | 2 | 25% |
| **User Dashboard** | 3 | 2 | 0 | 0 | 1 | 67% |
| **Public Pages** | 2 | 1 | 0 | 0 | 1 | 50% |
| **Testing & Quality** | 4 | 0 | 1 | 0 | 3 | 0% |
| **Documentation** | 2 | 1 | 0 | 0 | 1 | 50% |
| **TOTAL** | 34 | 13 | 8 | 0 | 13 | 38% |

---

## Core Authentication & Authorization

- [x] **BFF Authentication with httpOnly Cookies** (Priority: High) ✅ COMPLETE
  - **Description**: Secure authentication using Backend-for-Frontend pattern with httpOnly cookies
  - **Business Value**: Essential security - prevents XSS attacks, token theft
  - **Effort**: Large (3+ days) - COMPLETED
  - **Dependencies**: None
  - **Status**: Complete (2025-09-12)
  - **Notes**: Migration from localStorage JWT complete. Zero authentication timeouts. Silent token refresh working.

- [ ] **Role-Based Access Control (RBAC) Enforcement** (Priority: High)
  - **Description**: Systematic enforcement of role permissions across all endpoints and UI
  - **Business Value**: Security compliance - prevent unauthorized access to admin/teacher features
  - **Effort**: Medium (1-2 days)
  - **Dependencies**: Authentication complete
  - **Status**: In Progress
  - **Notes**: Vetting system access control has 12 failing integration tests - CRITICAL BLOCKER

---

## Event Management & RSVP

- [x] **Event Creation UI** (Priority: High) ✅ COMPLETE
  - **Description**: Admin interface for creating events with sessions, tickets, volunteer positions
  - **Business Value**: Core functionality - cannot run events without this
  - **Effort**: Large (3+ days) - COMPLETED
  - **Dependencies**: None
  - **Status**: Complete (2025-10-05)
  - **Notes**: Full event creation workflow operational. React migration complete.

- [x] **Event Listing (Public)** (Priority: High) ✅ COMPLETE
  - **Description**: Public-facing event discovery and browsing
  - **Business Value**: Marketing - how new members find events
  - **Effort**: Medium (1-2 days) - COMPLETED
  - **Dependencies**: None
  - **Status**: Complete (2025-09-20)
  - **Notes**: Public events display working. Responsive design validated.

- [ ] **Event Detail View/Modal** (Priority: High)
  - **Description**: Detailed event information page/modal when clicking event card
  - **Business Value**: User experience - users need full event details before RSVP/purchase
  - **Effort**: Medium (1-2 days)
  - **Dependencies**: Event listing complete
  - **Status**: Not Started
  - **Notes**: E2E tests failing - component structure doesn't exist. 6 test failures related to this.

- [ ] **Event Card Click Interaction** (Priority: High)
  - **Description**: Make event cards clickable to open detail view
  - **Business Value**: Navigation - users cannot access event details without this
  - **Effort**: Small (4-6 hours)
  - **Dependencies**: Event detail view complete
  - **Status**: Not Started
  - **Notes**: Currently timing out in E2E tests. Missing `data-testid` attributes.

- [x] **RSVP System for Social Events** (Priority: High) ✅ COMPLETE
  - **Description**: Free RSVP functionality for social events (no payment required)
  - **Business Value**: Event participation - essential for social events
  - **Effort**: Medium (1-2 days) - COMPLETED
  - **Dependencies**: Event system, authentication
  - **Status**: Complete (2025-09-20)
  - **Notes**: 90% functional. Access control integration has issues (vetting system blocker).

- [ ] **Ticket Purchase Flow** (Priority: High)
  - **Description**: Complete PayPal integration for class/workshop ticket purchases
  - **Business Value**: Revenue - cannot charge for classes without this
  - **Effort**: Large (3+ days)
  - **Dependencies**: PayPal webhook integration (complete), event system
  - **Status**: In Progress
  - **Notes**: PayPal webhook integration complete (2025-09-14). Frontend purchase flow partially implemented.

- [ ] **Public Events Anonymous Access** (Priority: High)
  - **Description**: Allow browsing events without authentication (currently returns 401)
  - **Business Value**: Marketing - new visitors must see events before creating account
  - **Effort**: Small (4-6 hours)
  - **Dependencies**: None
  - **Status**: Not Started
  - **Notes**: E2E test failing - public endpoint returning 401 Unauthorized. Backend configuration issue.

- [ ] **Admin Event Editing** (Priority: Medium)
  - **Description**: Admin interface to update existing events
  - **Business Value**: Event management - admins need to correct mistakes or update details
  - **Effort**: Medium (1-2 days)
  - **Dependencies**: Event creation complete
  - **Status**: In Progress
  - **Notes**: E2E test Step 2 failing - admin cannot find/edit events in admin panel. UI incomplete or broken.

---

## Vetting System

- [x] **Vetting Application Submission** (Priority: High) ✅ COMPLETE
  - **Description**: Members can submit vetting applications
  - **Business Value**: Membership growth - required for member advancement
  - **Effort**: Large (3+ days) - COMPLETED
  - **Dependencies**: Authentication
  - **Status**: Complete (2025-10-04)
  - **Notes**: Simplified 7-stage vetting application working. Form validation fixed.

- [x] **Conditional "How to Join" Menu Visibility** (Priority: Medium) ✅ COMPLETE
  - **Description**: Show/hide "How to Join" menu based on vetting status
  - **Business Value**: UX improvement - reduce confusion for vetted members
  - **Effort**: Medium (1-2 days) - COMPLETED
  - **Dependencies**: Vetting application system
  - **Status**: Complete (2025-10-04)
  - **Notes**: 46 tests passing. Full TypeScript coverage. Production-ready.

- [x] **Vetting Status Display** (Priority: Medium) ✅ COMPLETE
  - **Description**: Show current vetting application status to users
  - **Business Value**: Transparency - users need to know where they are in process
  - **Effort**: Small (4-6 hours) - COMPLETED
  - **Dependencies**: Vetting application system
  - **Status**: Complete (2025-10-04)
  - **Notes**: VettingStatusBox component with 10 status variants complete.

- [ ] **Vetting Workflow Backend** (Priority: High) ⚠️ CRITICAL BLOCKER
  - **Description**: Status transitions, audit logging, email notifications, access control
  - **Business Value**: Core functionality - vetting process cannot work without this
  - **Effort**: Medium (2-3 days)
  - **Dependencies**: Vetting application system
  - **Status**: In Progress
  - **Notes**: **12 INTEGRATION TESTS FAILING** - Status transitions broken, audit logs not created, email notifications failing, 403 access control checks broken. Blocks RSVP access control.

- [ ] **Admin Vetting Review Grid** (Priority: Medium)
  - **Description**: Admin interface for reviewing and processing vetting applications
  - **Business Value**: Operations - admins must be able to process applications
  - **Effort**: Medium (1-2 days)
  - **Dependencies**: Vetting workflow backend
  - **Status**: In Progress
  - **Notes**: Component created (2025-10-04) but backend workflow must be fixed first.

- [ ] **Bulk Vetting Operations Backend** (Priority: Low)
  - **Description**: Implement backend bulk endpoints for vetting operations (reminder emails, status changes)
  - **Business Value**: Administrative efficiency - currently using parallel individual API calls instead of true bulk operations
  - **Current Status**: Database schema complete, frontend partially implemented (bulk on-hold), backend endpoints missing
  - **Effort**: Medium (2-3 days)
  - **Dependencies**: Vetting workflow backend (must be fixed first)
  - **Status**: Not Started
  - **Notes**: Investigation complete (2025-10-06). Business requirements in Story 6. Current implementation uses Promise.all() for parallel calls - works but not optimal for large batches. Backend service methods and bulk endpoints need implementation. See `/home/chad/repos/witchcityrope/session-work/2025-10-06/bulk-validation-investigation.md` for technical details.
  - **Technical Details**:
    - Database entities ready: VettingBulkOperation, VettingBulkOperationItem, VettingBulkOperationLog
    - DTOs ready: BulkReminderRequest, BulkStatusChangeRequest, BulkOperationResult
    - Need: Backend service methods, API endpoints (/api/vetting/bulk/*), audit trail integration
    - Estimated effort: 2-3 days backend + 1 day frontend updates + 1 day testing = 4-5 days total

---

## Payment Processing

- [x] **PayPal Webhook Integration** (Priority: High) ✅ COMPLETE
  - **Description**: Receive and process PayPal payment notifications
  - **Business Value**: Revenue - cannot accept payments without webhook processing
  - **Effort**: Large (3+ days) - COMPLETED
  - **Dependencies**: None
  - **Status**: Complete (2025-09-14)
  - **Notes**: Cloudflare tunnel configured. Strongly-typed PayPal events. Mock service for CI/CD. Production-ready.

- [ ] **Payment Confirmation Emails** (Priority: Medium)
  - **Description**: Send email receipts after successful ticket purchases
  - **Business Value**: Customer service - users need payment confirmation
  - **Effort**: Small (4-6 hours)
  - **Dependencies**: PayPal webhook integration, email service
  - **Status**: Not Started
  - **Notes**: Email infrastructure exists (vetting emails working). Need payment-specific templates.

- [ ] **Payment History/Receipt Access** (Priority: Low)
  - **Description**: User dashboard view of past payments and receipts
  - **Business Value**: Customer service - users need access to past receipts
  - **Effort**: Medium (1-2 days)
  - **Dependencies**: Payment processing complete
  - **Status**: Not Started
  - **Notes**: Lower priority - can be added post-launch if needed.

---

## Admin Tools

- [x] **User Management Grid** (Priority: High) ✅ COMPLETE
  - **Description**: Admin interface to view and manage user accounts
  - **Business Value**: User support - admins need to manage member accounts
  - **Effort**: Medium (1-2 days) - COMPLETED
  - **Dependencies**: Authentication
  - **Status**: Complete (2025-08-12)
  - **Notes**: Full user management with role assignment working.

- [ ] **Event Attendance Tracking** (Priority: Medium)
  - **Description**: Mark attendees as present/absent for events
  - **Business Value**: Event management - track who actually attended vs RSVP
  - **Effort**: Medium (1-2 days)
  - **Dependencies**: RSVP system, event system
  - **Status**: Not Started
  - **Notes**: Check-in system partially designed. Needs implementation.

- [ ] **Admin Dashboard Analytics** (Priority: Low)
  - **Description**: High-level metrics (total events, RSVPs, revenue, active members)
  - **Business Value**: Operations - admins need visibility into platform health
  - **Effort**: Medium (1-2 days)
  - **Dependencies**: Event system, payment system
  - **Status**: Not Started
  - **Notes**: Nice-to-have for launch. Can be added post-launch.

- [ ] **Bulk Email to Members** (Priority: Low)
  - **Description**: Send announcements/newsletters to member segments
  - **Business Value**: Communication - platform communication beyond individual emails
  - **Effort**: Large (3+ days)
  - **Dependencies**: Email service
  - **Status**: Not Started
  - **Notes**: Not critical for launch. Use external service initially if needed.

---

## User Dashboard

- [x] **Dashboard Overview** (Priority: High) ✅ COMPLETE
  - **Description**: Member dashboard with upcoming events, vetting status, quick actions
  - **Business Value**: User engagement - primary member interface
  - **Effort**: Medium (1-2 days) - COMPLETED
  - **Dependencies**: Authentication, events, vetting
  - **Status**: Complete (2025-09-11)
  - **Notes**: Dashboard displaying correctly. Error handling has issues (40-50 unit tests failing).

- [x] **User Profile Viewing** (Priority: Medium) ✅ COMPLETE
  - **Description**: Members can view their profile information
  - **Business Value**: User experience - members need to see their info
  - **Effort**: Small (4-6 hours) - COMPLETED
  - **Dependencies**: Authentication
  - **Status**: Complete (2025-08-12)
  - **Notes**: Basic profile viewing working.

- [ ] **User Profile Editing** (Priority: Medium)
  - **Description**: Members can update name, email, profile picture, preferences
  - **Business Value**: User control - members need to maintain accurate information
  - **Effort**: Medium (1-2 days)
  - **Dependencies**: User profile viewing
  - **Status**: Not Started
  - **Notes**: Profile editing UI not yet implemented. Backend endpoints may exist.

---

## Public Pages

- [x] **Homepage/Landing Page** (Priority: High) ✅ COMPLETE
  - **Description**: Public landing page with navigation, about, call-to-action
  - **Business Value**: Marketing - first impression for new visitors
  - **Effort**: Medium (1-2 days) - COMPLETED
  - **Dependencies**: None
  - **Status**: Complete (2025-08-19)
  - **Notes**: Homepage navigation refresh complete. Design system v7 applied.

- [ ] **About/Community Information Page** (Priority: Medium)
  - **Description**: Information about WitchCityRope, community values, safety practices
  - **Business Value**: Trust building - new members need to understand community
  - **Effort**: Small (4-6 hours)
  - **Dependencies**: None
  - **Status**: Not Started
  - **Notes**: Content needs to be written. May exist from Blazor version.

---

## Testing & Quality

- [ ] **Complete Test Suite Coverage** (Priority: High) ⚠️ CRITICAL
  - **Description**: Achieve >90% pass rate across all test suites
  - **Business Value**: Quality assurance - cannot launch with failing tests
  - **Effort**: Large (see testing completion plan)
  - **Dependencies**: All feature implementations
  - **Status**: In Progress
  - **Notes**: See `/home/chad/repos/witchcityrope/session-work/2025-10-06/testing-completion-plan.md` for detailed strategy. Current: 56% React unit, 55% integration, 57-83% E2E.

- [ ] **Fix Dashboard Error Handling** (Priority: High)
  - **Description**: Network timeout handling, malformed API responses, login/logout error states
  - **Business Value**: User experience - graceful degradation when APIs fail
  - **Effort**: Medium (1-2 days)
  - **Dependencies**: Dashboard complete
  - **Status**: Not Started
  - **Notes**: 40-50 React unit tests failing. Category A legitimate bugs in error handling.

- [ ] **Performance Optimization** (Priority: Medium)
  - **Description**: Ensure all pages load <2s, API responses <200ms
  - **Business Value**: User experience - slow site drives users away
  - **Effort**: Medium (1-2 days)
  - **Dependencies**: All features implemented
  - **Status**: Not Started
  - **Notes**: Current performance acceptable (1.7s event load, 49ms API). May need optimization under load.

- [ ] **Security Audit** (Priority: High)
  - **Description**: Review authentication, authorization, data validation, XSS/CSRF protection
  - **Business Value**: Security - prevent data breaches and unauthorized access
  - **Effort**: Medium (1-2 days)
  - **Dependencies**: All features implemented
  - **Status**: Not Started
  - **Notes**: BFF authentication pattern provides good XSS/CSRF protection. Need systematic review.

---

## Documentation

- [x] **User Guide/Help Documentation** (Priority: Medium) ✅ PARTIAL
  - **Description**: How-to guides for members (submit vetting, RSVP, purchase tickets, etc.)
  - **Business Value**: User support - reduce support burden
  - **Effort**: Medium (1-2 days)
  - **Dependencies**: All features implemented
  - **Status**: Complete (partial)
  - **Notes**: Some documentation exists. Needs expansion for all features.

- [ ] **Admin Training Documentation** (Priority: Medium)
  - **Description**: Guides for admins (review vetting, create events, manage users, etc.)
  - **Business Value**: Operations - admins need to know how to use admin tools
  - **Effort**: Medium (1-2 days)
  - **Dependencies**: All admin features implemented
  - **Status**: Not Started
  - **Notes**: Critical for launch - admins must know how to operate platform.

---

## Launch Blockers Summary

### 🔴 CRITICAL (Must Fix Before Launch)

1. **Vetting Workflow Backend** - 12 integration tests failing, blocks RSVP access control
2. **Complete Test Suite** - Cannot launch with 56% React unit pass rate
3. **Dashboard Error Handling** - 40-50 tests failing, poor user experience
4. **Public Events Anonymous Access** - New visitors cannot see events (returns 401)
5. **Event Detail View** - Users cannot see full event details

### 🟠 HIGH Priority (Strongly Recommended)

1. **Event Card Click Interaction** - Navigation broken for event details
2. **Admin Event Editing** - Admins cannot update events
3. **Ticket Purchase Flow** - Revenue generation requires this
4. **Security Audit** - Risk mitigation before public launch

### 🟡 MEDIUM Priority (Nice to Have)

1. **Admin Vetting Review Grid** - Manual workaround possible short-term
2. **User Profile Editing** - Users can contact admin for changes
3. **Event Attendance Tracking** - Manual tracking possible
4. **Payment Confirmation Emails** - PayPal sends receipt, our email is bonus

### 🟢 LOW Priority (Post-Launch)

1. **Admin Dashboard Analytics** - Can use database queries initially
2. **Payment History Access** - PayPal provides this, our UI is convenience
3. **Bulk Email System** - Can use MailChimp or similar externally
4. **About Page** - Can use placeholder content
5. **Bulk Vetting Operations** - Current parallel call approach works, true bulk endpoints are optimization

---

## Estimated Time to Launch

**Critical Path (Absolute Minimum)**:
- Vetting workflow backend: 2-3 days
- Test suite completion: 4-5 days (per testing completion plan)
- Dashboard error handling: 1-2 days
- Public events access: 4-6 hours
- Event detail view: 1-2 days
- **Total**: ~10-13 days (2-3 weeks)

**Recommended Path (Reduced Risk)**:
- Critical items above: 10-13 days
- Event card interaction: 4-6 hours
- Admin event editing: 1 day
- Ticket purchase flow: 2-3 days
- Security audit: 1-2 days
- **Total**: ~16-21 days (3-4 weeks)

**Full Feature Path (Complete Product)**:
- Recommended items above: 16-21 days
- Medium priority items: 5-7 days
- Bulk vetting operations: 4-5 days
- **Total**: ~25-33 days (5-7 weeks)

---

## Notes

- **Updated**: 2025-10-06 - Added bulk vetting operations based on codebase investigation
- **Source**: `/home/chad/repos/witchcityrope/test-results/comprehensive-test-analysis-2025-10-05.md`
- **Testing Plan**: `/home/chad/repos/witchcityrope/session-work/2025-10-06/testing-completion-plan.md`
- **Bulk Ops Investigation**: `/home/chad/repos/witchcityrope/session-work/2025-10-06/bulk-validation-investigation.md`
- **Add Items**: Use `/orchestrate Add [feature] to pre-launch punch list` during testing
- **Review Frequency**: Update weekly or after major feature completions
- **Priority Changes**: Adjust priorities based on user feedback and business needs

---

## Revision History

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-10-06 | 1.0 | Initial creation with comprehensive test analysis integration | Librarian Agent |
| 2025-10-06 | 1.1 | Added bulk vetting operations work item after codebase investigation | Librarian Agent |
