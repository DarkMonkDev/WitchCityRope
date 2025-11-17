# Business Requirements: Password Reset and Email Verification
<!-- Last Updated: 2025-11-17 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Draft - Awaiting Stakeholder Review -->

## Executive Summary

WitchCityRope requires **Password Reset** and **Email Verification** features to provide a complete, production-ready authentication system. These features are critical for launch readiness and user trust, enabling users to recover forgotten passwords and verifying new account email addresses before granting login access.

**Recommended Implementation**: ASP.NET Core Identity built-in token providers + SendGrid email delivery (95% confidence, 3-5 days implementation).

**Business Value**: Eliminates authentication gaps blocking production launch, reduces support burden for password recovery, prevents fake account registrations through email verification, establishes foundation for future notification features.

---

## Business Context

### Problem Statement

**Current State**: WitchCityRope's authentication system allows user registration and login but lacks two critical features required for production:

1. **No Password Reset Capability**: Users who forget their passwords cannot recover their accounts without administrator intervention, creating support burden and user frustration.

2. **No Email Verification**: New accounts can register with any email address (even invalid ones) and immediately access the system, creating security risks, spam potential, and data quality issues.

**Impact**:
- **Support Burden**: Administrators must manually reset passwords via database operations
- **User Frustration**: Users locked out of accounts with no self-service recovery
- **Security Risk**: Unverified email accounts can access platform features
- **Data Quality**: Invalid email addresses in user database prevent reliable communication
- **Production Blocker**: Missing industry-standard authentication features expected by users

### Business Value

**Password Reset Implementation**:
- **Reduced Support Time**: Eliminate 100% of manual password reset requests (estimated 2-5 requests/week)
- **Improved User Experience**: Self-service recovery available 24/7
- **Security Enhancement**: Time-limited tokens (2 hours) ensure secure password changes
- **Administrator Efficiency**: Free staff time for higher-value community support

**Email Verification Implementation**:
- **Account Quality**: Ensure all users have valid, accessible email addresses
- **Spam Prevention**: Reduce fake account registrations
- **Communication Reliability**: Foundation for event notifications, safety alerts, system announcements
- **Data Integrity**: Clean user database with verified contact information
- **Security Improvement**: Confirm user owns the email address they registered with

**Combined Business Impact**:
- **Production Readiness**: Complete authentication system meeting industry standards
- **User Trust**: Professional authentication experience comparable to established platforms
- **Foundation for Growth**: Email infrastructure enables future notification features
- **Cost Savings**: SendGrid free tier (100 emails/day) sufficient for initial launch, $19.95/month for growth

### Success Metrics

**Password Reset**:
- **Self-Service Rate**: 100% of password resets completed without administrator intervention
- **Recovery Time**: Users regain account access within 5 minutes of reset request
- **Support Reduction**: Zero manual password reset tickets after implementation
- **Security Compliance**: 100% of reset tokens expire after 2 hours

**Email Verification**:
- **Verification Rate**: >90% of new users verify email within 24 hours
- **Invalid Email Reduction**: <5% of registrations use invalid email addresses
- **Login Block Effectiveness**: 100% of unverified accounts blocked from login
- **Resend Success**: Users can request new verification emails without support intervention

**System Performance**:
- **Email Delivery**: 95% of emails delivered within 5 minutes
- **Token Generation**: <5ms per token generation operation
- **API Response Time**: <200ms for all authentication endpoints
- **Uptime**: 99.9% availability for password reset and email verification features

---

## User Stories

### Password Reset User Stories

#### PWR-US-001: Request Password Reset (Primary Flow)
**As a** user who forgot my password
**I want to** request a password reset email
**So that** I can regain access to my account without administrator help

**Acceptance Criteria**:
- **Given** I am on the login page
- **When** I click "Forgot Password?" link
- **Then** I am directed to a password reset request page
- **And** I can enter my email address
- **And** I see clear instructions about what will happen next

**Business Rules**:
- Email field required and validated for proper format
- Generic success message shown regardless of whether email exists (prevent account enumeration)
- User receives email within 5 minutes if account exists
- No error shown if email not found (security best practice)

---

#### PWR-US-002: Receive Password Reset Email
**As a** user who requested a password reset
**I want to** receive a secure reset link via email
**So that** I can verify my identity and change my password

**Acceptance Criteria**:
- **Given** I submitted a password reset request for a valid account
- **When** the system processes my request
- **Then** I receive an email within 5 minutes
- **And** the email contains a secure reset link
- **And** the email explains the link expires in 2 hours
- **And** the email includes my scene name for confirmation
- **And** the email provides support contact if I didn't request this

**Business Rules**:
- Email sent only if account exists
- Reset link contains cryptographically secure token
- Token valid for 2 hours from generation
- Email includes security warning about phishing
- No password reset email sent if account doesn't exist (silent failure)

---

#### PWR-US-003: Reset Password with Valid Token
**As a** user who received a password reset email
**I want to** click the reset link and set a new password
**So that** I can access my account with my new credentials

**Acceptance Criteria**:
- **Given** I received a password reset email with valid token
- **When** I click the reset link within 2 hours
- **Then** I am directed to a password reset form
- **And** I can enter a new password
- **And** I can confirm the new password (must match)
- **And** I see password strength requirements
- **And** I receive immediate validation feedback

**Business Rules**:
- Password must meet security requirements (8+ characters, uppercase, lowercase, number, special character)
- Password and confirmation must match
- Token validated before displaying form
- Password cannot be same as previous password
- User automatically logged out of all sessions after password change
- Success message confirms password updated
- User redirected to login page after successful reset

---

#### PWR-US-004: Handle Expired Reset Token
**As a** user who clicks an expired password reset link
**I want to** see a clear error message and option to request new link
**So that** I understand what happened and can recover my account

**Acceptance Criteria**:
- **Given** I received a password reset email more than 2 hours ago
- **When** I click the reset link
- **Then** I see a clear error message explaining the link expired
- **And** I see a "Request New Reset Link" button
- **And** clicking the button returns me to password reset request page

**Business Rules**:
- Token expires exactly 2 hours after generation
- Expired tokens cannot be used even if user tries multiple times
- Clear messaging: "This password reset link has expired. Reset links are valid for 2 hours."
- User must request new token to reset password

---

#### PWR-US-005: Handle Invalid Reset Token
**As a** user who clicks an invalid password reset link
**I want to** see a clear error message
**So that** I know the link is not valid and can request a new one

**Acceptance Criteria**:
- **Given** I received a malformed or already-used password reset link
- **When** I click the reset link
- **Then** I see a clear error message explaining the link is invalid
- **And** I see a "Request New Reset Link" button

**Business Rules**:
- Tokens are single-use (cannot be reused after password changed)
- Tampered tokens rejected
- Clear messaging: "This password reset link is invalid or has already been used."

---

#### PWR-US-006: Rate Limiting Password Reset Requests
**As a** system administrator
**I want to** limit password reset requests per account
**So that** the system is protected from abuse

**Acceptance Criteria**:
- **Given** a user has requested password reset 3 times in 1 hour
- **When** they attempt a 4th request
- **Then** they see an error message explaining the limit
- **And** they are told to wait 1 hour before requesting again

**Business Rules**:
- Maximum 3 password reset requests per email address per hour
- Rate limit tracked by email address (even if account doesn't exist)
- Generic error message to prevent account enumeration
- Counter resets after 1 hour
- Rate limiting does not block login attempts

---

### Email Verification User Stories

#### EV-US-001: Send Verification Email on Registration
**As a** new user who just registered
**I want to** receive an email verification link
**So that** I can verify my email address and access my account

**Acceptance Criteria**:
- **Given** I successfully completed the registration form
- **When** my account is created
- **Then** I see a message explaining I must verify my email before logging in
- **And** I receive a verification email within 5 minutes
- **And** the email contains a verification link
- **And** the email explains the link expires in 3 days
- **And** I am redirected to a "Check Your Email" page

**Business Rules**:
- Verification email sent immediately after registration
- Token valid for 3 days (72 hours)
- Email includes user's scene name for personalization
- "Check Your Email" page provides clear next steps
- User cannot login until email verified

---

#### EV-US-002: Verify Email with Valid Token
**As a** new user who received a verification email
**I want to** click the verification link
**So that** my email is verified and I can log in

**Acceptance Criteria**:
- **Given** I received an email verification link
- **When** I click the link within 3 days
- **Then** I see a success message confirming my email is verified
- **And** I see a "Login Now" button
- **And** clicking the button directs me to the login page
- **And** I can successfully log in with my credentials

**Business Rules**:
- Token verified cryptographically
- Email verification timestamp saved to database
- User's `EmailConfirmed` field set to `true`
- Verification tokens are single-use
- Success message: "Your email has been verified! You can now log in to WitchCityRope."

---

#### EV-US-003: Block Login for Unverified Email
**As a** system administrator
**I want to** prevent unverified users from logging in
**So that** all active accounts have verified email addresses

**Acceptance Criteria**:
- **Given** I registered an account but did not verify my email
- **When** I attempt to log in
- **Then** I see an error message explaining my email must be verified
- **And** I see a "Resend Verification Email" button
- **And** clicking the button sends a new verification email

**Business Rules**:
- Login blocked if `EmailConfirmed = false`
- Clear error message: "Please verify your email address before logging in. Check your inbox for the verification link."
- Password validation still occurs (don't reveal if email/password wrong)
- Resend verification email button always visible on login error

---

#### EV-US-004: Resend Verification Email
**As a** new user who did not receive the verification email
**I want to** request a new verification email
**So that** I can verify my email and access my account

**Acceptance Criteria**:
- **Given** I am on the login page or verification instructions page
- **When** I click "Resend Verification Email"
- **Then** I am prompted to enter my email address
- **And** I see a success message after submitting
- **And** I receive a new verification email within 5 minutes
- **And** the new email contains a fresh token (3 day expiration)

**Business Rules**:
- Maximum 5 resend requests per email address per day
- Generic success message shown regardless of whether email exists
- Previous verification tokens still valid (not invalidated)
- Rate limiting prevents abuse

---

#### EV-US-005: Handle Expired Verification Token
**As a** new user who clicks an expired verification link
**I want to** see a clear error message and option to resend
**So that** I can complete email verification

**Acceptance Criteria**:
- **Given** I received a verification email more than 3 days ago
- **When** I click the verification link
- **Then** I see an error message explaining the link expired
- **And** I see a "Resend Verification Email" button
- **And** clicking the button allows me to request a new verification email

**Business Rules**:
- Tokens expire exactly 3 days (72 hours) after generation
- Clear messaging: "This verification link has expired. Verification links are valid for 3 days."
- User must request new token to verify email

---

#### EV-US-006: Handle Already Verified Email
**As a** user who already verified my email
**I want to** see a confirmation message if I click the verification link again
**So that** I know my email is already verified

**Acceptance Criteria**:
- **Given** my email is already verified
- **When** I click a verification link (old or new)
- **Then** I see a message confirming my email is already verified
- **And** I see a "Go to Login" button

**Business Rules**:
- No error shown for already-verified accounts
- Friendly message: "Your email address has already been verified. You can log in below."
- Redirect to login page

---

### Admin User Stories

#### ADMIN-US-001: View Email Verification Status
**As an** administrator
**I want to** see which users have verified their email addresses
**So that** I can identify accounts with potential data quality issues

**Acceptance Criteria**:
- **Given** I am viewing the admin members list
- **When** I look at user details
- **Then** I see email verification status (Verified / Unverified)
- **And** I see the date email was verified (if applicable)

**Business Rules**:
- Email verification status visible in user management interface
- Sortable and filterable by verification status
- Timestamp displayed for verified emails

---

#### ADMIN-US-002: Manually Verify User Email (Future Enhancement)
**As an** administrator
**I want to** manually mark a user's email as verified
**So that** I can resolve edge cases or support issues

**Acceptance Criteria**:
- **Given** I am viewing a user's account details
- **When** I click "Manually Verify Email"
- **Then** I see a confirmation dialog
- **And** after confirming, the user's email is marked as verified
- **And** an audit log entry is created

**Business Rules**:
- Admin action logged with admin user ID and timestamp
- User notified via email that their account was verified
- Manual verification should be rare exception (not normal process)

**Status**: Out of Scope for V1 - Documented for Future Enhancement

---

## Functional Requirements

### FR-1: SendGrid Email Integration (Foundation)

**Priority**: Must Have
**Dependencies**: None
**Complexity**: Low

#### FR-1.1: SendGrid Configuration
- System MUST support SendGrid API key configuration via .NET User Secrets (development) and environment variables (production)
- Configuration MUST include API key, sender email address, sender name
- Default sender: `noreply@witchcityrope.com` with display name "WitchCityRope"

#### FR-1.2: Email Service Implementation
- System MUST implement `IEmailSender` interface from ASP.NET Core Identity
- Service MUST use SendGrid .NET SDK for email delivery
- Service MUST support both plain text and HTML email formats
- Service MUST log all email send attempts and results
- Service MUST handle SendGrid API errors gracefully

#### FR-1.3: Email Template System
- System MUST use existing email template database structure
- Templates MUST support variable substitution (user name, links, expiration times)
- System MUST create templates for:
  - Password Reset Request
  - Password Reset Confirmation
  - Email Verification
  - Email Verification Resend
  - Email Verified Confirmation

#### FR-1.4: Local Development Testing
- System MUST support SendGrid Docker mock for local development
- Docker mock MUST run on port 7000
- Development environment MUST log email content to console
- Developers MUST be able to test email flows without SendGrid account

**Acceptance Criteria**:
- [ ] SendGrid API key configurable via User Secrets
- [ ] IEmailSender implementation sends emails successfully
- [ ] Email templates seeded in database
- [ ] Docker mock configured in docker-compose.dev.yml
- [ ] Local development logs email content
- [ ] No hardcoded SendGrid API keys in code

---

### FR-2: Password Reset Flow

**Priority**: Must Have
**Dependencies**: FR-1 (SendGrid Integration)
**Complexity**: Medium

#### FR-2.1: Password Reset Request Endpoint
- API MUST provide `POST /api/auth/forgot-password` endpoint
- Endpoint MUST accept email address in request body
- Endpoint MUST return generic success message (prevent account enumeration)
- Endpoint MUST generate reset token using ASP.NET Core Identity `UserManager.GeneratePasswordResetTokenAsync()`
- Token MUST be cryptographically secure and time-limited (2 hours)

#### FR-2.2: Password Reset Email Delivery
- System MUST send password reset email if account exists
- Email MUST contain secure reset link: `{frontendUrl}/reset-password?token={urlEncodedToken}&email={urlEncodedEmail}`
- Email MUST explain token expires in 2 hours
- Email MUST include user's scene name for personalization
- Email MUST include security warning about not sharing link

#### FR-2.3: Reset Password Endpoint
- API MUST provide `POST /api/auth/reset-password` endpoint
- Endpoint MUST accept email, token, and new password in request body
- Endpoint MUST validate token using `UserManager.ResetPasswordAsync()`
- Endpoint MUST enforce password complexity requirements (ASP.NET Core Identity defaults)
- Endpoint MUST return clear error messages for:
  - Invalid token
  - Expired token
  - Weak password
  - Password same as previous

#### FR-2.4: Password Reset Frontend Pages
- Frontend MUST provide `/forgot-password` page
- Page MUST include email input field with validation
- Page MUST show generic success message after submission
- Frontend MUST provide `/reset-password` page
- Page MUST extract token and email from URL query parameters
- Page MUST include new password and confirm password fields
- Page MUST show password strength indicator
- Page MUST validate passwords match before submission
- Page MUST show appropriate error messages for expired/invalid tokens

#### FR-2.5: Rate Limiting
- System MUST limit password reset requests to 3 per email address per hour
- Rate limiting MUST apply even if account doesn't exist (prevent enumeration)
- System MUST track rate limits in memory cache (not database)
- Generic error message shown when rate limit exceeded

**Acceptance Criteria**:
- [ ] POST /api/auth/forgot-password endpoint operational
- [ ] POST /api/auth/reset-password endpoint operational
- [ ] Password reset email sent within 5 minutes
- [ ] Reset tokens expire after 2 hours
- [ ] Frontend forgot password page functional
- [ ] Frontend reset password page functional
- [ ] Rate limiting prevents abuse (3 requests/hour)
- [ ] Generic messages prevent account enumeration
- [ ] All error cases handled gracefully

---

### FR-3: Email Verification Flow

**Priority**: Must Have
**Dependencies**: FR-1 (SendGrid Integration)
**Complexity**: Medium

#### FR-3.1: Email Verification on Registration
- Registration endpoint MUST generate email verification token using `UserManager.GenerateEmailConfirmationTokenAsync()`
- System MUST send verification email immediately after account creation
- User record MUST have `EmailConfirmed = false` initially
- Token MUST expire after 3 days (72 hours)

#### FR-3.2: Email Verification Email Delivery
- System MUST send email verification email to new users
- Email MUST contain verification link: `{frontendUrl}/verify-email?token={urlEncodedToken}&email={urlEncodedEmail}`
- Email MUST explain token expires in 3 days
- Email MUST include user's scene name for personalization
- Email MUST provide instructions for requesting new verification email

#### FR-3.3: Verify Email Endpoint
- API MUST provide `POST /api/auth/verify-email` endpoint
- Endpoint MUST accept email and token in request body
- Endpoint MUST validate token using `UserManager.ConfirmEmailAsync()`
- Endpoint MUST set `EmailConfirmed = true` on success
- Endpoint MUST return clear error messages for:
  - Invalid token
  - Expired token
  - Already verified email

#### FR-3.4: Resend Verification Email Endpoint
- API MUST provide `POST /api/auth/resend-verification-email` endpoint
- Endpoint MUST accept email address in request body
- Endpoint MUST return generic success message (prevent enumeration)
- Endpoint MUST generate new verification token
- System MUST limit resend requests to 5 per email address per day

#### FR-3.5: Login Block for Unverified Email
- Login endpoint MUST check `EmailConfirmed` field before authentication
- Login MUST fail with clear error if email not verified
- Error message MUST include "Resend Verification Email" option
- Password validation MUST still occur (don't reveal if credentials wrong)

#### FR-3.6: Email Verification Frontend Pages
- Frontend MUST provide `/verify-email` page
- Page MUST extract token and email from URL query parameters
- Page MUST call verification endpoint automatically on page load
- Page MUST show success message and "Login Now" button on success
- Page MUST show appropriate error messages for expired/invalid tokens
- Page MUST provide "Resend Verification Email" button on errors
- Login page MUST show "Resend Verification Email" link on email verification error

**Acceptance Criteria**:
- [ ] Email verification token generated on registration
- [ ] Verification email sent within 5 minutes
- [ ] POST /api/auth/verify-email endpoint operational
- [ ] POST /api/auth/resend-verification-email endpoint operational
- [ ] Tokens expire after 3 days
- [ ] Login blocked for unverified emails
- [ ] Frontend verify-email page functional
- [ ] Resend verification email functional
- [ ] Rate limiting prevents abuse (5 resends/day)
- [ ] Generic messages prevent account enumeration

---

## Non-Functional Requirements

### NFR-1: Security

**Priority**: Critical

#### NFR-1.1: OWASP Compliance
- Password reset MUST follow OWASP Forgot Password Cheat Sheet guidelines
- System MUST prevent account enumeration through generic error messages
- Tokens MUST be cryptographically secure (ASP.NET Core Identity default)
- Tokens MUST be single-use (invalidated after password reset or email verification)
- All authentication endpoints MUST use HTTPS only

#### NFR-1.2: Token Security
- Password reset tokens MUST expire after 2 hours
- Email verification tokens MUST expire after 3 days
- Tokens MUST be URL-safe Base64 encoded
- Tokens MUST be validated server-side (no client-side validation)
- Token generation MUST use `UserManager` built-in cryptographic providers

#### NFR-1.3: Rate Limiting
- Password reset requests: Maximum 3 per email per hour
- Email verification resend: Maximum 5 per email per day
- Rate limits MUST prevent abuse and enumeration attacks
- Rate limiting MUST be implemented at application layer (not firewall)

#### NFR-1.4: Email Security
- Password reset links MUST expire after 2 hours
- Email verification links MUST expire after 3 days
- Emails MUST include security warnings
- Reset/verification links MUST use HTTPS protocol
- SendGrid API keys MUST be stored securely (User Secrets, environment variables)

**Acceptance Criteria**:
- [ ] OWASP Forgot Password guidelines implemented
- [ ] Generic error messages prevent enumeration
- [ ] Tokens cryptographically secure
- [ ] Token expiration enforced
- [ ] Rate limiting operational
- [ ] HTTPS required for all links
- [ ] No hardcoded API keys

---

### NFR-2: Performance

**Priority**: High

#### NFR-2.1: Email Delivery Performance
- 95% of emails MUST be delivered within 5 minutes
- SendGrid API calls MUST not block user-facing API responses
- Email sending MUST be asynchronous (background task)
- System MUST handle SendGrid API rate limits gracefully

#### NFR-2.2: Token Generation Performance
- Token generation MUST complete in <5ms
- Token validation MUST complete in <10ms
- Database queries for token validation MUST use indexes
- No N+1 query problems in token operations

#### NFR-2.3: API Response Time
- Password reset request endpoint: <200ms
- Password reset endpoint: <300ms (includes password hashing)
- Email verification endpoint: <200ms
- Resend verification endpoint: <200ms

**Acceptance Criteria**:
- [ ] Email delivery within 5 minutes (95% of sends)
- [ ] Token operations <5ms generation, <10ms validation
- [ ] API endpoints <300ms response time
- [ ] Asynchronous email sending implemented
- [ ] Database indexes on token-related queries

---

### NFR-3: Usability

**Priority**: High

#### NFR-3.1: Clear Error Messages
- Users MUST receive actionable error messages
- Expired token error MUST explain expiration time and provide resend option
- Invalid token error MUST explain what happened and provide recovery path
- Login blocked error MUST explain email verification required and provide resend option

#### NFR-3.2: Mobile-Friendly Forms
- All password reset and email verification pages MUST be mobile-responsive
- Forms MUST use mobile-friendly input fields (type="email", type="password")
- Buttons MUST meet minimum touch target size (44x44px)
- Typography MUST scale fluidly (clamp() CSS)
- Forms MUST work on iOS Safari, Android Chrome, Firefox Mobile

#### NFR-3.3: User Guidance
- "Check Your Email" page MUST provide clear next steps after registration
- Password reset form MUST show password strength requirements
- Password fields MUST show/hide password toggle
- All pages MUST include support contact information

**Acceptance Criteria**:
- [ ] Error messages clear and actionable
- [ ] Mobile responsive at 375px, 768px, 1440px viewports
- [ ] Touch targets ≥44x44px
- [ ] Password strength indicator shown
- [ ] Show/hide password toggle functional
- [ ] Support contact visible on all pages

---

### NFR-4: Testability

**Priority**: High

#### NFR-4.1: Local Development Testing
- SendGrid Docker mock MUST be available for local testing
- Mock MUST log email content to console
- Developers MUST be able to test complete flows without SendGrid account
- Mock MUST run on port 7000 (docker-compose.dev.yml)

#### NFR-4.2: Unit Test Coverage
- Token generation services: >90% code coverage
- Email sending services: >90% code coverage
- Authentication endpoints: >85% code coverage
- Service layer validation logic: >95% code coverage

#### NFR-4.3: Integration Test Coverage
- Password reset flow: End-to-end integration test
- Email verification flow: End-to-end integration test
- Rate limiting: Integration tests for limits
- Token expiration: Integration tests for expiry

#### NFR-4.4: E2E Test Coverage
- Password reset flow: Happy path E2E test
- Email verification flow: Happy path E2E test
- Expired token handling: E2E test
- Invalid token handling: E2E test

**Acceptance Criteria**:
- [ ] SendGrid Docker mock operational
- [ ] Unit tests >85% coverage
- [ ] Integration tests for all flows
- [ ] E2E tests for critical paths
- [ ] Mock logs email content locally

---

## Technical Constraints

### TC-1: Technology Stack

- **Backend Framework**: ASP.NET Core 9.0 Minimal API
- **Authentication**: ASP.NET Core Identity with built-in token providers
- **Email Service**: SendGrid .NET SDK (v9.x)
- **Email Template Storage**: PostgreSQL database (existing `email_templates` table)
- **Frontend**: React 18 + TypeScript + React Router v7
- **Form Library**: Mantine v7 forms with validation
- **HTTP Client**: TanStack Query v5 for API calls

### TC-2: Architecture Patterns

- **Backend Architecture**: Vertical slice architecture (no MediatR, no CQRS)
- **API Pattern**: Direct `Results.Ok(dto)` or `Results.Problem()` (RFC 7807)
- **Authentication**: httpOnly cookies + JWT (existing BFF pattern)
- **Type Generation**: NSwag auto-generated TypeScript types from C# DTOs
- **Email Queue**: Asynchronous background email sending (IEmailSender interface)

### TC-3: Security Requirements

- **Token Providers**: ASP.NET Core Identity default token providers (REQUIRED - already configured via `.AddDefaultTokenProviders()`)
- **Password Hashing**: ASP.NET Core Identity default (PBKDF2)
- **HTTPS**: Required for all authentication endpoints
- **CORS**: Configured for React frontend origin
- **Secrets Management**: .NET User Secrets (dev), Environment Variables (production)

### TC-4: Database Requirements

- **User Table**: `AspNetUsers` table already exists
- **Email Confirmed Field**: `EmailConfirmed` boolean field already exists
- **Email Verification Created Field**: `EmailVerificationTokenCreatedAt` datetime field already exists
- **No Migrations Required**: Existing schema supports email verification
- **Database Changes**: NONE - all required fields already present

### TC-5: Deployment Requirements

- **Development**: Docker Compose with SendGrid mock on port 7000
- **Staging**: DigitalOcean with real SendGrid API (sandbox mode)
- **Production**: DigitalOcean with real SendGrid API (production mode)
- **Environment Variables**: `SENDGRID_API_KEY`, `SENDGRID_SENDER_EMAIL`, `SENDGRID_SENDER_NAME`

---

## Data Requirements

### DR-1: User Table (Existing)

**Table**: `AspNetUsers` (ASP.NET Core Identity default)

**Required Fields** (already exist):
- `Id` (UUID, primary key)
- `Email` (varchar 256, unique, indexed)
- `EmailConfirmed` (boolean, default: false) - **ALREADY EXISTS**
- `PasswordHash` (text)
- `SecurityStamp` (varchar 256) - Used for token validation

**Additional Fields** (already exist in WitchCityRope schema):
- `SceneName` (varchar 100) - User's display name for email personalization
- `EmailVerificationTokenCreatedAt` (timestamp with time zone) - **ALREADY EXISTS**

**No Database Migration Required**: All necessary fields already present.

### DR-2: Email Templates (Existing)

**Table**: `email_templates` (existing WitchCityRope structure)

**Required Templates** (to be seeded):

1. **PasswordResetRequest**
   - **Category**: Admin
   - **Name**: Password Reset Request
   - **Subject**: Reset Your WitchCityRope Password
   - **Variables**: `{{SceneName}}`, `{{ResetLink}}`, `{{ExpirationHours}}`

2. **PasswordResetConfirmation**
   - **Category**: Admin
   - **Name**: Password Reset Confirmation
   - **Subject**: Your WitchCityRope Password Has Been Changed
   - **Variables**: `{{SceneName}}`, `{{ChangeDateTime}}`

3. **EmailVerification**
   - **Category**: Admin
   - **Name**: Email Verification
   - **Subject**: Verify Your WitchCityRope Email Address
   - **Variables**: `{{SceneName}}`, `{{VerificationLink}}`, `{{ExpirationDays}}`

4. **EmailVerificationResend**
   - **Category**: Admin
   - **Name**: Email Verification Resend
   - **Subject**: New Email Verification Link for WitchCityRope
   - **Variables**: `{{SceneName}}`, `{{VerificationLink}}`, `{{ExpirationDays}}`

5. **EmailVerifiedConfirmation**
   - **Category**: Admin
   - **Name**: Email Verified Confirmation
   - **Subject**: Your WitchCityRope Email is Verified
   - **Variables**: `{{SceneName}}`, `{{LoginLink}}`

**Template Seeding**: Templates seeded via database migration or seed data script.

### DR-3: Configuration Data

**User Secrets** (Development):
```json
{
  "SendGrid": {
    "ApiKey": "SG.test_key_for_local_development",
    "SenderEmail": "noreply@witchcityrope.com",
    "SenderName": "WitchCityRope"
  }
}
```

**Environment Variables** (Production):
- `SENDGRID_API_KEY`: SendGrid API key (secret)
- `SENDGRID_SENDER_EMAIL`: noreply@witchcityrope.com
- `SENDGRID_SENDER_NAME`: WitchCityRope
- `FRONTEND_URL`: https://witchcityrope.com (for email links)

### DR-4: Rate Limiting Storage

**In-Memory Cache** (not database):
- Password reset requests: Track count per email per hour
- Email verification resend: Track count per email per day
- Cache expiration: 1 hour for password reset, 24 hours for verification
- Implementation: `IMemoryCache` interface (ASP.NET Core)

---

## Integration Points

### INT-1: ASP.NET Core Identity

**Components**:
- `UserManager<ApplicationUser>`: User account operations
- `SignInManager<ApplicationUser>`: Authentication operations
- `IEmailSender`: Email sending interface (implemented by SendGrid service)

**Methods Used**:
- `UserManager.GeneratePasswordResetTokenAsync(user)`: Generate password reset token
- `UserManager.ResetPasswordAsync(user, token, newPassword)`: Reset password with token
- `UserManager.GenerateEmailConfirmationTokenAsync(user)`: Generate email verification token
- `UserManager.ConfirmEmailAsync(user, token)`: Verify email with token
- `SignInManager.PasswordSignInAsync(email, password, isPersistent, lockoutOnFailure)`: Login with email check

**Token Provider Configuration** (already configured):
```csharp
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedEmail = true; // ENABLE THIS
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultProvider;
})
.AddDefaultTokenProviders(); // ALREADY CONFIGURED
```

### INT-2: SendGrid API

**SDK**: SendGrid .NET SDK (v9.x)

**API Integration**:
- **Authentication**: API key in header (`Authorization: Bearer {apiKey}`)
- **Endpoint**: `POST https://api.sendgrid.com/v3/mail/send`
- **Request Format**: JSON payload with sender, recipient, subject, content
- **Response**: HTTP 202 Accepted on success

**SendGridEmailSender Implementation**:
```csharp
public class SendGridEmailSender : IEmailSender
{
    private readonly SendGridClient _client;

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var msg = new SendGridMessage
        {
            From = new EmailAddress("noreply@witchcityrope.com", "WitchCityRope"),
            Subject = subject,
            HtmlContent = htmlMessage
        };
        msg.AddTo(new EmailAddress(email));

        await _client.SendEmailAsync(msg);
    }
}
```

### INT-3: Email Template System

**Database Query**:
```csharp
var template = await _dbContext.EmailTemplates
    .FirstOrDefaultAsync(t => t.Name == "PasswordResetRequest");
```

**Variable Substitution**:
```csharp
var content = template.Content
    .Replace("{{SceneName}}", user.SceneName)
    .Replace("{{ResetLink}}", resetLink)
    .Replace("{{ExpirationHours}}", "2");
```

**Integration**: Backend services query email templates, substitute variables, pass to `IEmailSender`.

### INT-4: React Frontend Integration

**API Calls** (TanStack Query):
```typescript
// Password reset request
const forgotPasswordMutation = useMutation({
  mutationFn: (email: string) =>
    apiClient.post('/api/auth/forgot-password', { email }),
});

// Reset password
const resetPasswordMutation = useMutation({
  mutationFn: (data: ResetPasswordRequest) =>
    apiClient.post('/api/auth/reset-password', data),
});

// Verify email
const verifyEmailMutation = useMutation({
  mutationFn: (data: VerifyEmailRequest) =>
    apiClient.post('/api/auth/verify-email', data),
});

// Resend verification email
const resendVerificationMutation = useMutation({
  mutationFn: (email: string) =>
    apiClient.post('/api/auth/resend-verification-email', { email }),
});
```

**Type Safety**: All DTOs auto-generated from C# using NSwag (no manual interfaces).

### INT-5: Docker Mock SendGrid (Development)

**Docker Compose Configuration**:
```yaml
sendgrid-mock:
  image: ghashange/sendgrid-mock:latest
  ports:
    - "7000:7000"
  environment:
    - MOCK_MODE=true
```

**Backend Configuration** (Development):
```json
{
  "SendGrid": {
    "ApiKey": "mock-api-key",
    "ApiUrl": "http://localhost:7000", // Override for mock
    "SenderEmail": "noreply@witchcityrope.com"
  }
}
```

**Email Logging**: Mock logs all emails to console for verification during testing.

---

## Security Requirements

### SEC-1: OWASP Password Reset Security

**OWASP Cheat Sheet Compliance**:

1. **Generate Password Reset Token**
   - Use cryptographically secure random token (ASP.NET Core Identity default)
   - Sufficient entropy (256-bit minimum)
   - URL-safe encoding

2. **Token Expiration**
   - Reset tokens expire after 2 hours
   - Email verification tokens expire after 3 days
   - Expired tokens cannot be used

3. **Token Single-Use**
   - Tokens invalidated after successful password reset
   - Email verification tokens invalidated after verification
   - Same token cannot be reused

4. **Prevent Account Enumeration**
   - Generic success messages for reset requests
   - No error if email doesn't exist
   - Same response time regardless of account existence
   - Rate limiting applies to all emails (existing and non-existing)

5. **Side-Channel Protection**
   - Timing attacks mitigated by consistent processing time
   - No conditional logic revealing account existence
   - Database queries execute regardless of email validity

**Implementation**:
- ASP.NET Core Identity built-in token providers (OWASP-compliant by design)
- `UserManager.GeneratePasswordResetTokenAsync()` uses cryptographic tokens
- Token validation via `UserManager.ResetPasswordAsync()`

### SEC-2: Rate Limiting

**Password Reset Rate Limits**:
- **Limit**: 3 requests per email address per hour
- **Tracking**: In-memory cache (`IMemoryCache`)
- **Scope**: Per email address (not per IP)
- **Enforcement**: Before token generation
- **Error Message**: Generic (does not reveal if email exists)

**Email Verification Rate Limits**:
- **Limit**: 5 resend requests per email address per day
- **Tracking**: In-memory cache (`IMemoryCache`)
- **Scope**: Per email address
- **Enforcement**: Before token generation

**Implementation**:
```csharp
// Check rate limit
var cacheKey = $"password-reset:{email}";
var attempts = _cache.Get<int>(cacheKey);
if (attempts >= 3) {
    return Results.Problem("Too many requests. Please try again later.", statusCode: 429);
}

// Increment counter
_cache.Set(cacheKey, attempts + 1, TimeSpan.FromHours(1));
```

### SEC-3: Token Security

**Token Generation**:
- ASP.NET Core Identity `DataProtectorTokenProvider`
- 256-bit cryptographically secure tokens
- URL-safe Base64 encoding
- Embedded expiration timestamp

**Token Storage**:
- **No database storage required** (cryptographic validation)
- Token contains: user ID, purpose, expiration timestamp
- Encrypted using ASP.NET Core Data Protection API
- Validation confirms token not tampered

**Token Transmission**:
- Tokens sent only via email (not exposed in UI)
- HTTPS required for all reset/verification links
- URL encoding for safe transmission

### SEC-4: Email Security

**Email Content**:
- Security warning in all emails: "If you did not request this, please ignore this email or contact support."
- Clear expiration time shown (2 hours for reset, 3 days for verification)
- Link format: `https://witchcityrope.com/reset-password?token={token}&email={email}`
- No sensitive information in email body

**Email Delivery**:
- SendGrid provides DKIM, SPF, DMARC authentication
- TLS encryption for email transmission
- Unsubscribe link not required (transactional emails)

**Phishing Protection**:
- Consistent sender email: `noreply@witchcityrope.com`
- Consistent sender name: "WitchCityRope"
- Clear instructions about legitimate email appearance

### SEC-5: Login Security with Email Verification

**Login Flow**:
1. User submits email and password
2. System validates password (even if email unverified)
3. If password valid but email unverified, return specific error
4. Error message: "Please verify your email address. Check your inbox or request a new verification link."

**Rationale**:
- Password validation prevents timing attacks
- Error reveals email verification status only after valid password
- Users with correct credentials can resend verification email

**Implementation**:
```csharp
var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);

if (result.IsNotAllowed) // Email not confirmed
{
    return Results.Problem(
        "Please verify your email address before logging in.",
        statusCode: 403
    );
}
```

---

## Compliance Requirements

### COMP-1: Data Protection

**GDPR Compliance**:
- Email addresses collected with user consent (registration form)
- Users can request account deletion (existing feature)
- Email verification tokens not personally identifiable (encrypted)
- Password reset emails transactional (GDPR exempt from marketing consent)

**Data Retention**:
- Tokens stored in memory only (not database)
- Tokens automatically expire and purge
- Email logs retained for 30 days (SendGrid default)
- No long-term storage of tokens

### COMP-2: Accessibility

**WCAG 2.1 AA Compliance**:
- All forms keyboard navigable
- Clear focus indicators on form fields
- Error messages associated with form fields (ARIA)
- Color contrast ratios >4.5:1 for text
- Touch targets ≥44x44px

**Form Accessibility**:
- `<label>` elements for all inputs
- ARIA attributes for error messages
- Screen reader announcements for validation errors
- Password show/hide toggle accessible via keyboard

### COMP-3: Security Standards

**OWASP Top 10 Compliance**:
- A01: Broken Access Control - Rate limiting prevents abuse
- A02: Cryptographic Failures - HTTPS required, tokens encrypted
- A03: Injection - Parameterized queries prevent SQL injection
- A07: Authentication Failures - Secure token providers, email verification required

**CWE Mitigation**:
- CWE-640: Weak Password Recovery - ASP.NET Core Identity secure tokens
- CWE-287: Improper Authentication - Email verification required for login
- CWE-307: Improper Password Restriction - Rate limiting on reset requests

---

## User Impact Analysis

| User Type | Password Reset Impact | Email Verification Impact | Priority |
|-----------|------------------------|---------------------------|----------|
| **Admin** | Can reset own password without database access; no longer needs to manually reset user passwords | Can view email verification status in member management; reduced support tickets | High |
| **Teacher** | Self-service password recovery available 24/7; no dependency on admin | Must verify email on first registration; improves account security | High |
| **Vetted Member** | Self-service password recovery; immediate account access restoration | Must verify email on first registration; ensures reliable communication for events | High |
| **General Member** | Self-service password recovery; reduced friction | Must verify email on first registration; prevents spam accounts | High |
| **Guest/Attendee** | Self-service password recovery; improved onboarding experience | Must verify email on first registration; one-time 3-day window | Medium |
| **New User** | N/A (no existing account) | Email verification required before login; extra step but industry-standard | High |

### Positive Impacts

**For All Users**:
- **Self-Service Recovery**: 24/7 password reset without waiting for admin
- **Account Security**: Email verification ensures account ownership
- **Professional Experience**: Industry-standard authentication comparable to established platforms
- **Immediate Access**: Password reset completed within minutes

**For Administrators**:
- **Reduced Support Burden**: Zero manual password resets required
- **Better Data Quality**: All users have verified email addresses
- **Improved Communication**: Foundation for reliable event notifications
- **Audit Trail**: Email verification status visible in member management

**For Community**:
- **Trust**: Email verification reduces spam and fake accounts
- **Safety**: Verified contact information for safety reporting
- **Reliability**: Event notifications reach verified emails

### Negative Impacts (Mitigations)

**For New Users**:
- **Extra Step**: Email verification required before login
  - **Mitigation**: Clear messaging during registration, 3-day token expiration window, easy resend process
- **Email Delivery Delays**: Verification email may take up to 5 minutes
  - **Mitigation**: Clear expectations set ("Check your email in the next few minutes"), resend option available immediately
- **Lost Verification Email**: Users may delete or not receive email
  - **Mitigation**: Unlimited resend requests (with rate limiting), clear instructions on login error

**For Existing Users** (if retroactive email verification required):
- **Login Blocked**: Existing users with unverified emails cannot login
  - **Mitigation**: Grandfather existing users (not required for V1), prompt for verification on next login (optional)

---

## Examples/Scenarios

### Scenario 1: New User Registration with Email Verification (Happy Path)

**User**: Sarah (new member)

1. **Registration**
   - Sarah visits witchcityrope.com and clicks "Register"
   - Fills out registration form: email `sarah@example.com`, scene name "Rope Enthusiast Sarah"
   - Submits form successfully

2. **Email Verification Email**
   - Sarah sees message: "Please check your email to verify your account. You must verify your email before you can log in."
   - Within 2 minutes, receives email:
     ```
     Subject: Verify Your WitchCityRope Email Address

     Hi Rope Enthusiast Sarah,

     Welcome to WitchCityRope! Please verify your email address by clicking the link below:

     [Verify Email Address]

     This link will expire in 3 days. If you did not create this account, please ignore this email.

     - WitchCityRope Team
     ```
   - Clicks "Verify Email Address" link

3. **Email Verification Success**
   - Redirected to `witchcityrope.com/verify-email?token=...&email=sarah@example.com`
   - Sees success message: "Your email has been verified! You can now log in to WitchCityRope."
   - Clicks "Login Now" button

4. **Login Success**
   - Enters email and password
   - Successfully logs in and sees dashboard

**Business Value**: Seamless onboarding with verified email address, no support intervention needed.

---

### Scenario 2: Forgot Password (Happy Path)

**User**: Marcus (existing member, forgot password)

1. **Password Reset Request**
   - Marcus visits login page, clicks "Forgot Password?"
   - Enters email: `marcus@example.com`
   - Sees message: "If an account exists with this email, you will receive password reset instructions shortly."

2. **Password Reset Email**
   - Within 3 minutes, receives email:
     ```
     Subject: Reset Your WitchCityRope Password

     Hi Marcus the Rigger,

     You requested to reset your WitchCityRope password. Click the link below to set a new password:

     [Reset Password]

     This link will expire in 2 hours. If you did not request this, please contact support immediately.

     - WitchCityRope Team
     ```
   - Clicks "Reset Password" link

3. **Password Reset Form**
   - Redirected to `witchcityrope.com/reset-password?token=...&email=marcus@example.com`
   - Enters new password (sees strength indicator: "Strong")
   - Confirms password (matches)
   - Clicks "Reset Password"

4. **Password Reset Success**
   - Sees success message: "Your password has been updated successfully. You can now log in."
   - Redirected to login page
   - Logs in with new password

**Business Value**: User regains account access in <5 minutes without administrator intervention.

---

### Scenario 3: Expired Password Reset Token (Error Handling)

**User**: Lisa (requested reset, waited 3 hours before clicking link)

1. **Initial Request**
   - Lisa requests password reset at 2:00 PM
   - Receives email with reset link at 2:02 PM
   - Forgets to check email

2. **Expired Token**
   - At 5:30 PM (3.5 hours later), clicks reset link
   - Redirected to `witchcityrope.com/reset-password?token=...`
   - Sees error message:
     ```
     This password reset link has expired.

     Reset links are valid for 2 hours for security reasons.

     [Request New Reset Link]
     ```

3. **Request New Link**
   - Clicks "Request New Reset Link" button
   - Redirected to forgot password page
   - Enters email again
   - Receives new reset email within 2 minutes

4. **Successful Reset**
   - Clicks new link immediately
   - Completes password reset successfully

**Business Value**: Clear error handling with recovery path, no user confusion or support tickets.

---

### Scenario 4: Unverified Email Login Attempt (Email Verification Block)

**User**: Alex (new user, did not verify email)

1. **Registration**
   - Alex registers account at 10:00 AM
   - Receives verification email but ignores it

2. **Login Attempt**
   - At 10:15 AM, tries to log in
   - Enters correct email and password
   - Sees error message:
     ```
     Please verify your email address before logging in.

     Check your inbox for the verification link, or request a new one below.

     [Resend Verification Email]
     ```

3. **Resend Verification Email**
   - Clicks "Resend Verification Email" button
   - Sees confirmation: "Verification email sent! Check your inbox."
   - Receives new verification email within 2 minutes

4. **Email Verification**
   - Clicks verification link
   - Sees success message
   - Logs in successfully

**Business Value**: Email verification enforced, user guided to complete verification, no support needed.

---

### Scenario 5: Rate Limiting Protection (Security)

**User**: Malicious actor attempting account enumeration

1. **First 3 Attempts** (within 1 hour)
   - Submits password reset requests for: `admin@witchcityrope.com`, `test@witchcityrope.com`, `user@witchcityrope.com`
   - Receives generic success messages for all 3

2. **4th Attempt** (rate limit triggered)
   - Submits request for `another@witchcityrope.com`
   - Receives error:
     ```
     Too many password reset requests. Please wait 1 hour before trying again.
     ```

3. **Result**
   - Attacker cannot determine which emails exist
   - System protected from enumeration attack
   - Rate limit resets after 1 hour

**Business Value**: Account enumeration prevented, system abuse blocked, security maintained.

---

## Questions for Product Manager

### Implementation Priorities

- [ ] **Q1**: Is email verification required for existing users, or only new registrations?
  - **Option A**: Grandfather existing users (no retroactive verification)
  - **Option B**: Prompt existing users to verify on next login
  - **Recommendation**: Option A (avoid disruption for existing community)

- [ ] **Q2**: Should password reset emails be sent for non-existent accounts?
  - **Current Recommendation**: No (prevent enumeration)
  - **Alternative**: Send generic "if account exists" email to prevent confusion
  - **OWASP Recommendation**: Current approach (no email for non-existent accounts)

### Token Expiration Times

- [ ] **Q3**: Are the proposed token expiration times acceptable?
  - **Password Reset**: 2 hours (industry standard)
  - **Email Verification**: 3 days (72 hours, accommodates delayed onboarding)
  - **Alternative**: Different timeframes based on user feedback

### Rate Limiting

- [ ] **Q4**: Are the proposed rate limits appropriate for community size?
  - **Password Reset**: 3 requests per hour per email
  - **Email Verification Resend**: 5 requests per day per email
  - **Consideration**: Salem community size, expected usage patterns

### SendGrid Plan

- [ ] **Q5**: Which SendGrid plan should be used?
  - **Free Tier**: 100 emails/day (sufficient for initial launch)
  - **Essentials Plan**: $19.95/month, 40,000 emails/month (recommended for production)
  - **Current Estimate**: ~50-100 authentication emails/week initially

### Admin Features

- [ ] **Q6**: Should administrators be able to manually verify user emails?
  - **Use Case**: Support edge cases (email delivery issues, special circumstances)
  - **Security Risk**: Potential for abuse if not audited
  - **Recommendation**: Implement in V2 with comprehensive audit logging

### User Communication

- [ ] **Q7**: Should users receive confirmation emails after password changes?
  - **Security Benefit**: Alerts user to unauthorized password changes
  - **Implementation**: Simple email template, 1 line of code
  - **Recommendation**: Yes (security best practice)

### Testing Strategy

- [ ] **Q8**: Should SendGrid sandbox mode be used in staging?
  - **Option A**: Real SendGrid API with sandbox mode (emails not delivered)
  - **Option B**: Real SendGrid API with limited test emails
  - **Recommendation**: Option A for staging, Option B for final pre-production testing

---

## Quality Gate Checklist (95% Required)

### Requirements Completeness (15 items)

- [x] All user roles addressed (Admin, Teacher, Vetted Member, General Member, Guest)
- [x] Clear acceptance criteria for each user story
- [x] Business value clearly defined
- [x] Edge cases considered (expired tokens, invalid tokens, rate limiting)
- [x] Security requirements documented (OWASP compliance, rate limiting, token security)
- [x] Compliance requirements checked (GDPR, WCAG 2.1 AA, OWASP Top 10)
- [x] Performance expectations set (<5 minutes email delivery, <200ms API response)
- [x] Mobile experience considered (responsive forms, touch targets, mobile email clients)
- [x] Examples provided (5 detailed scenarios)
- [x] Success metrics defined (self-service rate, verification rate, email delivery)
- [x] User impact analysis completed (all user types, positive/negative impacts)
- [x] Integration points documented (Identity, SendGrid, Email Templates, React Frontend, Docker Mock)
- [x] Technical constraints identified (ASP.NET Core Identity, SendGrid, React, Mantine, NSwag)
- [x] Data requirements specified (user table, email templates, configuration)
- [x] Out of scope items documented (V2 features: SMS verification, MFA, social login, manual admin verification)

**Quality Gate**: 15/15 items complete (100%) ✅ PASSED

### Stakeholder Communication (8 items)

- [x] Questions for Product Manager documented (8 critical decision points)
- [x] Implementation recommendations provided with confidence levels (95% confidence)
- [x] Effort estimates included (3-5 days implementation)
- [x] Safety implications highlighted (verified emails for safety reporting)
- [x] Privacy implications noted (GDPR compliance, data retention)
- [x] Impact on existing features analyzed (authentication system, user management)
- [x] Alternative approaches evaluated (database tokens, Auth0/Firebase vs ASP.NET Core Identity)
- [x] Risk assessment completed (email delivery, token expiration, SendGrid quota, account enumeration, abuse)

**Quality Gate**: 8/8 items complete (100%) ✅ PASSED

---

## Implementation Phases

### Phase 1: SendGrid Integration (Foundation)
**Timeline**: 1 day
**Priority**: Critical (blocks other phases)

**Deliverables**:
- SendGrid .NET SDK installed and configured
- `IEmailSender` implementation with SendGrid
- Email template database seeding (5 templates)
- SendGrid Docker mock configured in `docker-compose.dev.yml`
- User Secrets configuration for SendGrid API key
- Environment variables documented for production

**Acceptance Criteria**:
- [ ] SendGrid API key configured via User Secrets
- [ ] Email templates seeded in database
- [ ] Docker mock sends emails to console in development
- [ ] SendGrid service successfully sends test email in staging
- [ ] No hardcoded API keys in codebase

**Estimated Effort**: 6-8 hours

---

### Phase 2: Email Verification (Higher Priority)
**Timeline**: 1-2 days
**Priority**: High (blocks production launch)
**Dependencies**: Phase 1 (SendGrid Integration)

**Backend Deliverables**:
- `POST /api/auth/verify-email` endpoint
- `POST /api/auth/resend-verification-email` endpoint
- Update registration endpoint to generate verification token
- Update login endpoint to check `EmailConfirmed` field
- Email verification token generation and validation
- Rate limiting for verification resend (5/day)

**Frontend Deliverables**:
- `/verify-email` page (auto-verification on load)
- "Check Your Email" page (post-registration)
- "Resend Verification Email" functionality on login error
- Email verification error handling (expired/invalid tokens)
- TypeScript types auto-generated from backend DTOs

**Testing Deliverables**:
- Unit tests for token generation/validation
- Integration tests for verification endpoints
- E2E test for complete verification flow
- Rate limiting tests

**Acceptance Criteria**:
- [ ] New users receive verification email within 5 minutes
- [ ] Verification link successfully verifies email
- [ ] Login blocked for unverified emails with clear error
- [ ] Resend verification email functional
- [ ] Tokens expire after 3 days
- [ ] Rate limiting prevents abuse (5 resends/day)
- [ ] All tests passing (unit, integration, E2E)

**Estimated Effort**: 10-12 hours (backend 6hrs, frontend 4hrs, testing 2hrs)

---

### Phase 3: Password Reset (Standard Priority)
**Timeline**: 1-2 days
**Priority**: High (production readiness)
**Dependencies**: Phase 1 (SendGrid Integration)

**Backend Deliverables**:
- `POST /api/auth/forgot-password` endpoint
- `POST /api/auth/reset-password` endpoint
- Password reset token generation and validation
- Rate limiting for password reset requests (3/hour)
- Password reset confirmation email (optional)

**Frontend Deliverables**:
- `/forgot-password` page
- `/reset-password` page
- Password strength indicator
- Password show/hide toggle
- Error handling (expired/invalid tokens)
- TypeScript types auto-generated from backend DTOs

**Testing Deliverables**:
- Unit tests for token generation/validation
- Integration tests for password reset endpoints
- E2E test for complete password reset flow
- Rate limiting tests
- Security tests (account enumeration prevention)

**Acceptance Criteria**:
- [ ] Users can request password reset email
- [ ] Password reset email sent within 5 minutes
- [ ] Reset link successfully resets password
- [ ] Tokens expire after 2 hours
- [ ] Rate limiting prevents abuse (3 requests/hour)
- [ ] Generic messages prevent account enumeration
- [ ] All tests passing (unit, integration, E2E, security)

**Estimated Effort**: 10-12 hours (backend 6hrs, frontend 4hrs, testing 2hrs)

---

## Testing Requirements

### Unit Tests

**Backend Unit Tests** (>90% coverage target):

**Token Generation Tests**:
```csharp
[Fact]
public async Task GeneratePasswordResetToken_ShouldReturnValidToken()
{
    // Arrange
    var user = CreateTestUser();

    // Act
    var token = await _userManager.GeneratePasswordResetTokenAsync(user);

    // Assert
    Assert.NotNull(token);
    Assert.NotEmpty(token);
}

[Fact]
public async Task GenerateEmailConfirmationToken_ShouldReturnValidToken()
{
    // Similar pattern
}
```

**Token Validation Tests**:
```csharp
[Fact]
public async Task ResetPassword_WithValidToken_ShouldSucceed()
{
    // Arrange
    var user = CreateTestUser();
    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
    var newPassword = "NewSecureP@ssw0rd";

    // Act
    var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

    // Assert
    Assert.True(result.Succeeded);
}

[Fact]
public async Task ResetPassword_WithExpiredToken_ShouldFail()
{
    // Test token expiration
}
```

**Rate Limiting Tests**:
```csharp
[Fact]
public async Task ForgotPassword_ExceedsRateLimit_ShouldReturn429()
{
    // Test 4th request within 1 hour returns 429
}
```

**Frontend Unit Tests** (>85% coverage target):

**Form Validation Tests**:
```typescript
describe('ForgotPasswordForm', () => {
  it('validates email format', () => {
    // Test email validation
  });

  it('shows generic success message', () => {
    // Test no account enumeration
  });
});

describe('ResetPasswordForm', () => {
  it('validates passwords match', () => {
    // Test password confirmation
  });

  it('shows password strength indicator', () => {
    // Test strength indicator
  });
});
```

---

### Integration Tests

**Password Reset Flow**:
```csharp
[Fact]
public async Task PasswordResetFlow_EndToEnd_ShouldSucceed()
{
    // 1. Request password reset
    var response1 = await _client.PostAsync("/api/auth/forgot-password",
        new { email = "test@example.com" });
    Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

    // 2. Extract token from mock email service
    var token = _mockEmailService.GetLastSentToken();

    // 3. Reset password with token
    var response2 = await _client.PostAsync("/api/auth/reset-password",
        new { email = "test@example.com", token, newPassword = "NewP@ssw0rd123" });
    Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

    // 4. Verify login works with new password
    var loginResponse = await _client.PostAsync("/api/auth/login",
        new { email = "test@example.com", password = "NewP@ssw0rd123" });
    Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
}
```

**Email Verification Flow**:
```csharp
[Fact]
public async Task EmailVerificationFlow_EndToEnd_ShouldSucceed()
{
    // 1. Register new user
    var registerResponse = await _client.PostAsync("/api/auth/register", newUserData);

    // 2. Extract verification token from mock email
    var token = _mockEmailService.GetLastSentToken();

    // 3. Verify email
    var verifyResponse = await _client.PostAsync("/api/auth/verify-email",
        new { email, token });
    Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

    // 4. Verify login now works
    var loginResponse = await _client.PostAsync("/api/auth/login", loginData);
    Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
}
```

---

### E2E Tests (Playwright)

**Password Reset E2E Test**:
```typescript
test('user can reset forgotten password', async ({ page }) => {
  // 1. Navigate to login page
  await page.goto('/login');

  // 2. Click "Forgot Password?"
  await page.click('text=Forgot Password?');

  // 3. Enter email
  await page.fill('input[name="email"]', 'test@example.com');
  await page.click('button:has-text("Send Reset Link")');

  // 4. Verify success message
  await expect(page.locator('text=Check your email')).toBeVisible();

  // 5. Extract reset link from mock email
  const resetLink = await getResetLinkFromMockEmail();

  // 6. Navigate to reset page
  await page.goto(resetLink);

  // 7. Enter new password
  await page.fill('input[name="password"]', 'NewSecureP@ssw0rd');
  await page.fill('input[name="confirmPassword"]', 'NewSecureP@ssw0rd');
  await page.click('button:has-text("Reset Password")');

  // 8. Verify success and login works
  await expect(page.locator('text=Password updated')).toBeVisible();
  await page.fill('input[name="email"]', 'test@example.com');
  await page.fill('input[name="password"]', 'NewSecureP@ssw0rd');
  await page.click('button:has-text("Login")');
  await expect(page).toHaveURL('/dashboard');
});
```

**Email Verification E2E Test**:
```typescript
test('new user must verify email before login', async ({ page }) => {
  // 1. Register new user
  await page.goto('/register');
  await page.fill('input[name="email"]', 'newuser@example.com');
  await page.fill('input[name="sceneName"]', 'Test User');
  await page.fill('input[name="password"]', 'SecureP@ssw0rd');
  await page.click('button:has-text("Register")');

  // 2. Verify "Check Email" message
  await expect(page.locator('text=Check your email')).toBeVisible();

  // 3. Attempt login without verification
  await page.goto('/login');
  await page.fill('input[name="email"]', 'newuser@example.com');
  await page.fill('input[name="password"]', 'SecureP@ssw0rd');
  await page.click('button:has-text("Login")');

  // 4. Verify login blocked
  await expect(page.locator('text=verify your email')).toBeVisible();

  // 5. Extract verification link from mock email
  const verifyLink = await getVerificationLinkFromMockEmail();

  // 6. Click verification link
  await page.goto(verifyLink);
  await expect(page.locator('text=Email verified')).toBeVisible();

  // 7. Login successfully
  await page.click('text=Login Now');
  await page.fill('input[name="email"]', 'newuser@example.com');
  await page.fill('input[name="password"]', 'SecureP@ssw0rd');
  await page.click('button:has-text("Login")');
  await expect(page).toHaveURL('/dashboard');
});
```

---

### Security Tests

**Account Enumeration Prevention**:
```csharp
[Fact]
public async Task ForgotPassword_NonExistentEmail_ShouldReturnGenericMessage()
{
    // Arrange
    var email = "nonexistent@example.com";

    // Act
    var response = await _client.PostAsync("/api/auth/forgot-password", new { email });
    var content = await response.Content.ReadAsStringAsync();

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Contains("Check your email", content); // Generic message
    Assert.DoesNotContain("not found", content); // No enumeration hint
}
```

**Rate Limiting Tests**:
```csharp
[Fact]
public async Task ForgotPassword_RateLimit_ShouldBlock4thRequest()
{
    // Make 3 requests (should succeed)
    for (int i = 0; i < 3; i++)
    {
        var response = await _client.PostAsync("/api/auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // 4th request should be rate limited
    var blockedResponse = await _client.PostAsync("/api/auth/forgot-password", new { email });
    Assert.Equal(HttpStatusCode.TooManyRequests, blockedResponse.StatusCode);
}
```

**Token Expiration Tests**:
```csharp
[Fact]
public async Task ResetPassword_ExpiredToken_ShouldReturnError()
{
    // Generate token and simulate 3 hour wait
    var token = await GenerateTokenAndWait(TimeSpan.FromHours(3));

    // Attempt to use expired token
    var response = await _client.PostAsync("/api/auth/reset-password",
        new { email, token, newPassword });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    var content = await response.Content.ReadAsStringAsync();
    Assert.Contains("expired", content);
}
```

---

## Documentation Requirements

### API Documentation (OpenAPI/Swagger)

**Endpoints to Document**:

1. **POST /api/auth/forgot-password**
   - Request body: `{ email: string }`
   - Response: `200 OK` with generic message
   - Error responses: `429 Too Many Requests`
   - OpenAPI annotations with examples

2. **POST /api/auth/reset-password**
   - Request body: `{ email: string, token: string, newPassword: string }`
   - Response: `200 OK` with success message
   - Error responses: `400 Bad Request` (invalid/expired token, weak password)
   - OpenAPI annotations with validation rules

3. **POST /api/auth/verify-email**
   - Request body: `{ email: string, token: string }`
   - Response: `200 OK` with success message
   - Error responses: `400 Bad Request` (invalid/expired token)
   - OpenAPI annotations

4. **POST /api/auth/resend-verification-email**
   - Request body: `{ email: string }`
   - Response: `200 OK` with generic message
   - Error responses: `429 Too Many Requests`
   - OpenAPI annotations

**NSwag Type Generation**:
- All DTOs auto-generated to TypeScript
- No manual interfaces created
- OpenAPI schema exported for frontend consumption

---

### User Documentation

**Help Center Articles** (to be created):

1. **"How to Reset Your Password"**
   - Step-by-step instructions with screenshots
   - Troubleshooting: "Didn't receive email", "Link expired"
   - Expected timeframes (email within 5 minutes, link valid 2 hours)

2. **"Email Verification for New Accounts"**
   - Why email verification is required
   - How to verify email (step-by-step)
   - How to resend verification email
   - Troubleshooting: "Didn't receive email", "Link expired"

3. **"Account Security Best Practices"**
   - Password requirements explanation
   - Recognizing legitimate emails from WitchCityRope
   - What to do if you receive unexpected password reset email
   - Phishing awareness

**In-App Messaging**:
- Clear instructions on all authentication pages
- Contextual help links to relevant articles
- Support contact prominently displayed

---

### Developer Documentation

**Setup Guide** (to be created):

1. **"SendGrid Configuration"**
   - How to obtain SendGrid API key
   - User Secrets setup for development
   - Environment variables for production
   - Docker mock configuration

2. **"Email Template Management"**
   - How to add new email templates
   - Variable substitution syntax
   - Testing email templates locally
   - SendGrid template vs database template approach

3. **"Testing Email Flows Locally"**
   - SendGrid Docker mock usage
   - Viewing email content in console logs
   - Extracting tokens from mock emails for E2E tests
   - Switching between mock and real SendGrid

**Architecture Documentation** (to be created):

1. **"Password Reset Architecture"**
   - Flow diagrams (request → email → reset → confirmation)
   - Token lifecycle and security
   - Rate limiting implementation
   - Integration with ASP.NET Core Identity

2. **"Email Verification Architecture"**
   - Flow diagrams (register → email → verify → login)
   - Token lifecycle and expiration
   - Login blocking mechanism
   - Integration with authentication system

---

## Out of Scope (V2 Features)

The following features are explicitly **out of scope** for the initial implementation. They are documented here for future reference and potential V2 enhancements.

### V2-1: SMS Verification (6-digit codes)

**Description**: Alternative verification method using SMS instead of email.

**Use Case**: Users without reliable email access or who prefer SMS.

**Implementation Considerations**:
- Twilio or similar SMS provider integration
- 6-digit numeric codes (instead of URLs)
- Code expiration (10 minutes typical)
- SMS delivery costs ($0.01-0.02 per message)
- Phone number collection and storage
- International phone number support

**Effort Estimate**: 3-5 days
**Cost Impact**: ~$50-100/month for moderate SMS volume
**Priority**: Low (email verification sufficient for rope bondage community)

---

### V2-2: Multi-Factor Authentication (MFA)

**Description**: Optional second factor (TOTP apps like Google Authenticator, Authy).

**Use Case**: Enhanced security for admin accounts and security-conscious users.

**Implementation Considerations**:
- TOTP library (OtpNet or similar)
- QR code generation for authenticator setup
- Backup codes generation and storage
- Recovery mechanism if TOTP device lost
- Optional vs required MFA (user choice)

**Effort Estimate**: 5-7 days
**Cost Impact**: None (TOTP is free)
**Priority**: Medium (valuable for admin accounts)

---

### V2-3: Social Login Integration

**Description**: Login with Google, Facebook, or FetLife accounts.

**Use Case**: Simplified registration, reduced password management.

**Implementation Considerations**:
- OAuth 2.0 provider integration
- Account linking (social account + local account)
- Email verification from social provider
- Privacy implications (data sharing with social platforms)
- Community concerns (FetLife integration most relevant)

**Effort Estimate**: 7-10 days
**Cost Impact**: None (OAuth is free)
**Priority**: Low (community may prefer independence from social platforms)

---

### V2-4: Account Recovery Without Email Access

**Description**: Alternative recovery methods if user loses email access.

**Use Case**: User's email account deleted, spam filtered, or inaccessible.

**Implementation Considerations**:
- Security questions (weak security, not recommended)
- SMS-based recovery (requires phone number)
- Support ticket verification (manual admin process)
- Identity verification challenge (community-specific questions)

**Effort Estimate**: 5-7 days
**Cost Impact**: None (unless SMS used)
**Priority**: Low (email verification ensures email access during account lifecycle)

---

### V2-5: Password Change Confirmation Email

**Description**: Email notification when password is changed (security alert).

**Status**: **INCLUDED in implementation** (simple addition)

**Implementation**: Single email template, 1 line of code after password reset.

**Recommendation**: Implement in Phase 3 (no additional effort).

---

### V2-6: Admin Manual Email Verification

**Description**: Administrators can manually mark user emails as verified.

**Use Case**: Support edge cases (email delivery failures, special circumstances).

**Implementation Considerations**:
- Admin permission check
- Audit log entry (who verified, when, why)
- User notification email (account verified)
- Rare exception process (not normal flow)

**Effort Estimate**: 1-2 days
**Cost Impact**: None
**Priority**: Low (normal flow should handle 99%+ of cases)

**Status**: Documented in ADMIN-US-002 user story for future reference.

---

## Appendix A: Email Template Examples

### Template 1: Password Reset Request

**Subject**: Reset Your WitchCityRope Password

**Content** (HTML):
```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .button { display: inline-block; padding: 12px 24px; background: #880124; color: white; text-decoration: none; border-radius: 4px; }
        .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; }
    </style>
</head>
<body>
    <div class="container">
        <h2>Password Reset Request</h2>

        <p>Hi {{SceneName}},</p>

        <p>You requested to reset your WitchCityRope password. Click the button below to set a new password:</p>

        <p><a href="{{ResetLink}}" class="button">Reset Password</a></p>

        <p>This link will expire in {{ExpirationHours}} hours for security reasons.</p>

        <p><strong>If you did not request this password reset, please ignore this email or contact support immediately at support@witchcityrope.com.</strong></p>

        <div class="footer">
            <p>This is an automated message from WitchCityRope. Please do not reply to this email.</p>
            <p>WitchCityRope • Salem, MA • witchcityrope.com</p>
        </div>
    </div>
</body>
</html>
```

**Variables**:
- `{{SceneName}}`: User's scene name (e.g., "Rope Enthusiast Sarah")
- `{{ResetLink}}`: Password reset URL with token
- `{{ExpirationHours}}`: "2" (hours until token expires)

---

### Template 2: Email Verification

**Subject**: Verify Your WitchCityRope Email Address

**Content** (HTML):
```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .button { display: inline-block; padding: 12px 24px; background: #614B79; color: white; text-decoration: none; border-radius: 4px; }
        .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; }
    </style>
</head>
<body>
    <div class="container">
        <h2>Welcome to WitchCityRope!</h2>

        <p>Hi {{SceneName}},</p>

        <p>Thank you for joining the WitchCityRope community. Please verify your email address by clicking the button below:</p>

        <p><a href="{{VerificationLink}}" class="button">Verify Email Address</a></p>

        <p>This link will expire in {{ExpirationDays}} days. You must verify your email before you can log in.</p>

        <p>If you did not create this account, please ignore this email.</p>

        <p>Need help? Contact us at support@witchcityrope.com</p>

        <div class="footer">
            <p>This is an automated message from WitchCityRope. Please do not reply to this email.</p>
            <p>WitchCityRope • Salem, MA • witchcityrope.com</p>
        </div>
    </div>
</body>
</html>
```

**Variables**:
- `{{SceneName}}`: User's scene name
- `{{VerificationLink}}`: Email verification URL with token
- `{{ExpirationDays}}`: "3" (days until token expires)

---

### Template 3: Password Reset Confirmation

**Subject**: Your WitchCityRope Password Has Been Changed

**Content** (HTML):
```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .alert { padding: 12px; background: #FFF8F0; border-left: 4px solid #880124; }
        .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; }
    </style>
</head>
<body>
    <div class="container">
        <h2>Password Changed Successfully</h2>

        <p>Hi {{SceneName}},</p>

        <p>This email confirms that your WitchCityRope password was successfully changed on {{ChangeDateTime}}.</p>

        <div class="alert">
            <p><strong>Did you make this change?</strong></p>
            <p>If you did not change your password, please contact support immediately at support@witchcityrope.com or call our safety hotline.</p>
        </div>

        <p>For your security, you have been logged out of all devices. Please log in again with your new password.</p>

        <div class="footer">
            <p>This is an automated message from WitchCityRope. Please do not reply to this email.</p>
            <p>WitchCityRope • Salem, MA • witchcityrope.com</p>
        </div>
    </div>
</body>
</html>
```

**Variables**:
- `{{SceneName}}`: User's scene name
- `{{ChangeDateTime}}`: Timestamp of password change (e.g., "November 17, 2025 at 3:42 PM EST")

---

## Appendix B: API Request/Response Examples

### Password Reset Request

**Request**:
```http
POST /api/auth/forgot-password HTTP/1.1
Host: api.witchcityrope.com
Content-Type: application/json

{
  "email": "user@example.com"
}
```

**Response** (success):
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "message": "If an account exists with this email, you will receive password reset instructions shortly."
}
```

**Response** (rate limited):
```http
HTTP/1.1 429 Too Many Requests
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.29",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Too many password reset requests. Please wait 1 hour before trying again."
}
```

---

### Reset Password

**Request**:
```http
POST /api/auth/reset-password HTTP/1.1
Host: api.witchcityrope.com
Content-Type: application/json

{
  "email": "user@example.com",
  "token": "CfDJ8N5...[long token]...XyZ==",
  "newPassword": "NewSecureP@ssw0rd123"
}
```

**Response** (success):
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "message": "Your password has been reset successfully. You can now log in with your new password."
}
```

**Response** (invalid token):
```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Invalid Token",
  "status": 400,
  "detail": "This password reset link is invalid or has already been used. Please request a new reset link."
}
```

**Response** (expired token):
```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Token Expired",
  "status": 400,
  "detail": "This password reset link has expired. Reset links are valid for 2 hours. Please request a new reset link."
}
```

---

### Verify Email

**Request**:
```http
POST /api/auth/verify-email HTTP/1.1
Host: api.witchcityrope.com
Content-Type: application/json

{
  "email": "newuser@example.com",
  "token": "CfDJ8N5...[long token]...XyZ=="
}
```

**Response** (success):
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "message": "Your email has been verified successfully. You can now log in."
}
```

**Response** (already verified):
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "message": "Your email address has already been verified. You can log in below."
}
```

---

### Resend Verification Email

**Request**:
```http
POST /api/auth/resend-verification-email HTTP/1.1
Host: api.witchcityrope.com
Content-Type: application/json

{
  "email": "newuser@example.com"
}
```

**Response** (success):
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "message": "If an account exists with this email and is not yet verified, a new verification email has been sent."
}
```

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-11-17 | Business Requirements Agent | Initial requirements document created |

---

**End of Business Requirements Document**

**Next Steps**:
1. **Stakeholder Review**: Product Manager reviews and answers questions (8 critical decisions)
2. **Approval**: Stakeholder approves implementation approach and priorities
3. **Handoff to Design**: UI Designer creates wireframes for password reset and email verification pages
4. **Handoff to Backend**: Backend Developer implements SendGrid integration and authentication endpoints
5. **Handoff to Frontend**: React Developer implements forms and pages
6. **Handoff to Testing**: Test Developer creates comprehensive test suite

**Contact**: For questions or clarifications about these requirements, contact the Business Requirements Agent or Product Manager.
