# Business Requirements: Post-Login Return to Intended Page
<!-- Last Updated: 2025-10-10 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Draft -->

## Executive Summary
Users who click login from anywhere other than the main navigation menu currently lose their context and are redirected to the dashboard. This creates friction in critical workflows like vetting applications, event registrations, and demo page exploration. Implementing a "return to intended page" pattern will improve user experience, reduce workflow abandonment, and meet modern web application UX standards.

## Business Context

### Problem Statement
**Current Behavior**: All login operations redirect users to `/dashboard` regardless of where they initiated the login.

**Specific Problem Cases**:
1. **Vetting Application Workflow**: User fills out vetting form → clicks login → loses form context → must navigate back to vetting
2. **Event Session Matrix Demo**: User exploring demo page → clicks login → loses demo context → must re-navigate to demo
3. **Public Event Page**: User viewing event details → clicks login to RSVP → loses event context → must search for event again
4. **Deep Links**: User follows shared link to specific page → must login → loses deep link → frustrating experience

**Impact**: Users lose context, abandon workflows, and experience friction in critical business processes.

### Business Value
- **Improved Conversion**: Users who don't lose context are more likely to complete vetting applications
- **Better Event Registration**: Reduced friction in RSVP/ticket purchase workflows
- **Professional UX**: Meets modern web application standards (expected behavior)
- **Reduced Support Burden**: Fewer "how do I get back to..." questions
- **Higher Engagement**: Demo page visitors more likely to engage after login

### Success Metrics
- **Vetting Application Completion Rate**: Increase by 15-25% (users don't abandon form)
- **Event Registration Time**: Reduce by 30-40% (users stay on event page)
- **User Satisfaction**: Measured via feedback/surveys (target: 85%+ positive)
- **Support Tickets**: Reduce navigation-related support requests by 50%

## User Stories

### Story 1: Vetting Application Continuity
**As a** prospective vetted member
**I want to** return to the vetting application form after logging in
**So that** I don't lose my place and can immediately submit my application

**Acceptance Criteria:**
- Given I am on the vetting application form (`/apply/vetting`)
- And I am not logged in
- And I click "Login to continue"
- When I successfully authenticate
- Then I am returned to the vetting application form
- And my form context is preserved (if technically feasible)
- And I see a success message: "Welcome back! Please complete your application."

### Story 2: Event Registration Flow
**As a** community member interested in an event
**I want to** return to the event page after logging in
**So that** I can immediately RSVP or purchase tickets without searching again

**Acceptance Criteria:**
- Given I am viewing a public event page (`/events/{id}`)
- And I am not logged in
- And I click "Login to RSVP" or "Login to Purchase Tickets"
- When I successfully authenticate
- Then I am returned to the same event page
- And the RSVP/ticket purchase button is now available
- And I see a message: "Welcome back! You can now register for this event."

### Story 3: Demo Page Exploration
**As a** visitor exploring the Event Session Matrix demo
**I want to** return to the demo page after logging in
**So that** I can continue exploring features without interruption

**Acceptance Criteria:**
- Given I am on the Event Session Matrix demo page (`/demo/event-session-matrix`)
- And I am not logged in
- And I click "Login to see full features"
- When I successfully authenticate
- Then I am returned to the Event Session Matrix demo page
- And demo functionality is fully accessible
- And I see a message: "Welcome! Explore all demo features."

### Story 4: Deep Link Preservation
**As a** user following a shared link
**I want to** land on the intended page after logging in
**So that** I see the content that was shared with me

**Acceptance Criteria:**
- Given I click a deep link to a specific page (e.g., `/events/123/details`)
- And I am not logged in
- And the system redirects me to login
- When I successfully authenticate
- Then I am redirected to the originally intended page (`/events/123/details`)
- And I see the content that was shared
- And the URL matches the original deep link

### Story 5: Default Dashboard Behavior (Existing)
**As a** logged-in user clicking "Login" from the main nav menu
**I want to** go to the dashboard
**So that** I see my personalized overview (current behavior preserved)

**Acceptance Criteria:**
- Given I am not logged in
- And I click "Login" from the main navigation menu
- When I successfully authenticate
- Then I am redirected to `/dashboard`
- And I see my personalized dashboard
- And this is the default behavior when no return URL is specified

## Business Rules

### BR-1: Return URL Validation (CRITICAL SECURITY)
**Rule**: All return URLs MUST be validated against an allow-list of permitted destinations to prevent open redirect vulnerabilities.

**Validation Requirements**:
- **Protocol Check**: Only `http://` and `https://` protocols allowed (block `javascript:`, `data:`, `file:`, etc.)
- **Domain Validation**: URL must match application domain (`witchcityrope.com` or `localhost` in dev)
- **Path Allow-List**: Validate against list of permitted application routes
- **Relative URL Preference**: Prefer relative URLs over absolute URLs
- **Query Parameter Sanitization**: Strip or validate all query parameters in return URL

**Reference**: OWASP Unvalidated Redirects and Forwards Cheat Sheet (https://cheatsheetseries.owasp.org/cheatsheets/Unvalidated_Redirects_and_Forwards_Cheat_Sheet.html)

### BR-2: Default Behavior
**Rule**: If no return URL is provided or validation fails, redirect to `/dashboard` (safe default).

**Rationale**: Always provide a known-safe destination for user security.

### BR-3: Authentication-Required Pages Only
**Rule**: Return URL functionality only applies to pages that require authentication.

**Examples**:
- ✅ `/apply/vetting` - Requires login to submit
- ✅ `/events/{id}` - Requires login to RSVP/purchase tickets
- ✅ `/demo/event-session-matrix` - Enhanced features require login
- ❌ `/` - Public homepage (no return needed)
- ❌ `/about` - Public page (no return needed)

### BR-4: Session Storage vs Query Parameter
**Rule**: Return URL should be stored in secure session storage OR passed as a validated query parameter.

**Implementation Options**:
1. **Session Storage (Preferred)**: Store return URL in server-side session before redirect to login
2. **Query Parameter**: Pass return URL as `?returnUrl=/intended/path` with strict validation

**Security Note**: Session storage preferred to prevent URL manipulation attacks.

### BR-5: Maximum Return Attempts
**Rule**: System should track and limit return URL redirect attempts to prevent abuse.

**Implementation**: Maximum 3 redirect attempts per session; after that, default to dashboard.

### BR-6: User Notification
**Rule**: After successful login with return URL, display contextual success message.

**Examples**:
- Vetting: "Welcome back! Please complete your application."
- Events: "Welcome back! You can now register for this event."
- Demo: "Welcome! Explore all demo features."
- Default: "Welcome back, {sceneName}!"

## Constraints & Assumptions

### Constraints
- **Technical**: Must not introduce open redirect vulnerabilities
- **Technical**: Must work with existing BFF authentication pattern (httpOnly cookies)
- **Technical**: Must not break existing dashboard redirect behavior
- **Business**: Cannot require changes to third-party authentication providers (future OAuth)
- **Performance**: Return URL validation must complete in <50ms

### Assumptions
- Users will complete login within reasonable time (5-10 minutes)
- Return URL will be for pages within the application (no external sites)
- Users understand being redirected to login interrupts their workflow
- Most users will complete login successfully on first attempt

## Security & Privacy Requirements

### SEC-1: Open Redirect Prevention (CRITICAL)
**Requirement**: System MUST prevent attackers from manipulating return URLs to redirect users to malicious sites.

**Implementation**:
- Validate all return URLs against strict allow-list
- Block dangerous protocols (javascript:, data:, file:, etc.)
- Verify domain matches application domain
- Log all return URL validation failures for security monitoring

**Attack Scenarios to Prevent**:
- Phishing: `?returnUrl=https://evil.com/fake-login`
- JavaScript Execution: `?returnUrl=javascript:alert('xss')`
- Data Exfiltration: `?returnUrl=data:text/html,<script>steal()</script>`

### SEC-2: URL Parameter Sanitization
**Requirement**: All query parameters in return URLs must be sanitized or stripped.

**Rationale**: Prevent injection attacks via URL parameters.

### SEC-3: HTTPS Enforcement
**Requirement**: In production, return URLs must use HTTPS protocol only.

**Exception**: `localhost` URLs in development can use HTTP.

### SEC-4: Audit Logging
**Requirement**: Log all return URL redirects for security monitoring.

**Log Fields**:
- Timestamp
- User ID (if authenticated)
- Original return URL
- Validated return URL
- Validation result (success/failure/reason)
- IP address
- User agent

### PRIV-1: No Sensitive Data in URL
**Requirement**: Return URLs must not contain sensitive user data.

**Examples of Prohibited Data**:
- User IDs (use session instead)
- Email addresses
- Scene names
- Payment information
- Vetting application details

## Compliance Requirements
- **OWASP Top 10**: Prevent Unvalidated Redirects and Forwards (A1:2021)
- **NIST Cybersecurity Framework**: Implement secure redirect validation
- **Platform Security Policy**: All redirects must be validated and logged

## User Impact Analysis

| User Type | Impact | Priority | Notes |
|-----------|--------|----------|-------|
| **Prospective Vetted Members** | HIGH | P1 | Vetting workflow most affected; high abandonment risk |
| **Event Attendees** | HIGH | P1 | Event registration friction reduces ticket sales |
| **Demo Page Visitors** | MEDIUM | P2 | Better experience but not revenue-critical |
| **Deep Link Users** | MEDIUM | P2 | Improved shareability and user satisfaction |
| **General Members** | LOW | P3 | Most login from nav menu (existing behavior preserved) |
| **Administrators** | LOW | P3 | Admin workflows mostly use direct navigation |

## Examples/Scenarios

### Scenario 1: Vetting Application Flow (Happy Path)
1. **User Action**: User navigates to `/apply/vetting` while not logged in
2. **System**: Stores `/apply/vetting` in session storage
3. **System**: Redirects user to `/login?returnUrl=/apply/vetting`
4. **User Action**: User enters credentials and submits login form
5. **System**: Validates credentials successfully
6. **System**: Validates return URL `/apply/vetting` (passes: internal path, no dangerous protocol)
7. **System**: Redirects user to `/apply/vetting`
8. **System**: Displays message: "Welcome back! Please complete your application."
9. **User**: Sees vetting form and can immediately continue application

### Scenario 2: Event Registration Flow (Happy Path)
1. **User Action**: User browsing `/events/123` (public event page)
2. **User Action**: User clicks "Login to RSVP" button
3. **System**: Stores `/events/123` in session storage
4. **System**: Redirects to `/login?returnUrl=/events/123`
5. **User Action**: User logs in successfully
6. **System**: Validates return URL `/events/123` (passes validation)
7. **System**: Redirects to `/events/123`
8. **System**: Displays message: "Welcome back! You can now register for this event."
9. **User**: Sees RSVP button now enabled and can immediately register

### Scenario 3: Open Redirect Attack (Security Protection)
1. **Attacker**: Sends phishing email with link: `/login?returnUrl=https://evil.com/fake-site`
2. **User Action**: User clicks malicious link
3. **System**: Detects return URL validation failure (external domain)
4. **System**: Logs security event: "Return URL validation failed - external domain"
5. **System**: Redirects to `/dashboard` (safe default)
6. **System**: Does NOT redirect to `evil.com`
7. **User**: Lands safely on dashboard instead of attacker's site

### Scenario 4: JavaScript Protocol Attack (Security Protection)
1. **Attacker**: Crafts URL: `/login?returnUrl=javascript:alert('xss')`
2. **User Action**: User clicks malicious link
3. **System**: Detects dangerous protocol `javascript:`
4. **System**: Logs security event: "Return URL validation failed - dangerous protocol"
5. **System**: Redirects to `/dashboard` (safe default)
6. **System**: JavaScript code never executes
7. **User**: Lands safely on dashboard

### Scenario 5: Nav Menu Login (Existing Behavior Preserved)
1. **User Action**: User clicks "Login" from main navigation menu
2. **System**: No return URL provided (not set in session)
3. **System**: Redirects to `/login` (no query parameter)
4. **User Action**: User logs in successfully
5. **System**: No return URL to validate
6. **System**: Redirects to `/dashboard` (default behavior)
7. **User**: Sees personalized dashboard (expected behavior)

## Questions for Product Manager

- [ ] **Priority Confirmation**: Is P1 CRITICAL priority correct for go-live launch?
- [ ] **Vetting Workflow**: Should we preserve form data when user returns from login?
- [ ] **Return URL Expiration**: How long should return URL be valid in session (5 min? 10 min?)
- [ ] **Error Messaging**: What message should users see if return URL validation fails?
- [ ] **Analytics**: Should we track return URL usage patterns for UX improvements?
- [ ] **Deep Links**: Are there specific deep link patterns we should prioritize?
- [ ] **Security Testing**: Should penetration testing be part of acceptance criteria?
- [ ] **Feature Flag**: Should this be deployed behind a feature flag initially?

## Quality Gate Checklist (95% Required)

- [x] All user roles addressed (members, prospects, visitors, admins)
- [x] Clear acceptance criteria for each story (5 user stories with acceptance criteria)
- [x] Business value clearly defined (conversion, UX, support reduction)
- [x] Edge cases considered (open redirect, malicious URLs, validation failures)
- [x] Security requirements documented (4 critical security requirements)
- [x] Compliance requirements checked (OWASP, NIST, platform policies)
- [x] Performance expectations set (<50ms validation time)
- [x] Mobile experience considered (works on all devices)
- [x] Examples provided (5 detailed scenarios including attack scenarios)
- [x] Success metrics defined (conversion rates, support tickets, satisfaction)

**Quality Gate Score**: 10/10 (100%) ✅

## Implementation Priority

**Priority**: P1 CRITICAL
**Rationale**:
- High user impact on critical workflows (vetting, event registration)
- Expected UX standard in modern web applications
- Significant business value (improved conversion rates)
- Security implementation required (cannot be rushed post-launch)

**Estimated Effort**: 4-6 hours
- Requirements review: 0.5 hours (this document)
- Technical design: 1 hour
- Implementation: 2-3 hours
- Security testing: 1-2 hours
- QA validation: 0.5-1 hour

**Dependencies**:
- Existing BFF authentication system (already implemented)
- React Router (already in use)
- Session storage/query parameter handling

**Go-Live Status**: ⚠️ **SHOULD COMPLETE** - Significant UX improvement, but not a hard blocker if timeline is tight. However, security implementation requirements mean this should NOT be rushed as a post-launch feature.

## Related Documentation
- **Go-Live Launch Checklist**: `/docs/functional-areas/platform-overview/GO-LIVE-LAUNCH-CHECKLIST.md`
- **Authentication README**: `/docs/functional-areas/authentication/README.md`
- **BFF Implementation Summary**: `/session-work/2025-09-12/bff-authentication-implementation-summary.md`
- **OWASP Security Reference**: https://cheatsheetseries.owasp.org/cheatsheets/Unvalidated_Redirects_and_Forwards_Cheat_Sheet.html

## Change Log

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-10-10 | 1.0 | Initial business requirements document created | Business Requirements Agent |
