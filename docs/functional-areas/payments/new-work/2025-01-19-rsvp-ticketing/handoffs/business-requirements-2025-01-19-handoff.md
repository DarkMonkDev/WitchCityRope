# AGENT HANDOFF DOCUMENT

## Phase: Business Requirements Analysis
## Date: 2025-09-19 (Final Stakeholder Approvals)
## Feature: RSVP and Ticketing System
## Version: 3.0 - APPROVED - Ready for Phase 2

## 🚨 CRITICAL BUSINESS RULES (MUST IMPLEMENT)

### 1. **RSVP SIMPLIFICATION**: One RSVP per user per event (only for themselves)
   - ✅ Correct: Single RSVP per user per event, self-only
   - ❌ Wrong: Multi-person RSVPs or assigning spots to others
   - **FUTURE SCOPE**: Multi-person RSVPs may be added later

### 2. **Social Events Clarification**: NOT necessarily free - often have suggested donation
   - ✅ Correct: Social events often have optional ticket purchases (suggested donation)
   - ❌ Wrong: Treating social events as completely free
   - **Implementation**: RSVP required (free), ticket purchase optional (paid)

### 3. **Ticket Terminology**: Call them "tickets" not "door entry tickets"
   - ✅ Correct: "Purchase Ticket" for both classes and social events
   - ❌ Wrong: "Door entry tickets" or similar confusing terminology
   - **Consistency**: Same terminology across all event types

### 4. **Automatic RSVP**: Purchasing a ticket automatically adds RSVP
   - ✅ Correct: Ticket purchase includes RSVP automatically
   - ❌ Wrong: Requiring separate RSVP step before ticket purchase
   - **Implementation**: Streamlined flow for ticket purchasers

### 5. **Refund Policy Consistency**: Same policy for classes and social event tickets
   - ✅ Correct: Unified refund policy across all paid participation
   - ❌ Wrong: Different policies for different event types
   - **Implementation**: Single refund system handles all ticket types

### 6. **Canceled/Refunded Tracking**: Never delete, only mark as canceled
   - ✅ Correct: RSVPs/tickets stay in system with status: canceled
   - ❌ Wrong: Deleting participation records when canceled
   - **Admin View**: Can see all canceled RSVPs and refunded tickets for liability tracking

### 7. **Role System Confirmation**: Roles DO stack (User can be Vetted + Teacher + Admin)
   - ✅ Correct: Multiple roles can be assigned to single user
   - ❌ Wrong: Single role per user
   - **Access Pattern**: Higher roles inherit lower role permissions

### 8. **Banned Status**: Cannot RSVP or purchase anything
   - ✅ Correct: Banned users blocked from all participation
   - ❌ Wrong: Allowing any form of event participation when banned
   - **Implementation**: Hard block at API level for banned users

### 9. **SendGrid Clarification**: Enhanced explanatory notes
   - **Sandbox Mode**: Validates API calls but doesn't send emails (dev testing)
   - **@sink.sendgrid.net**: Accepts then deletes emails (integration testing)
   - **Production**: Real email delivery with domain authentication

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| **APPROVED Business Requirements** | `/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/requirements/business-requirements.md` | Version 3.0 - Final stakeholder approved |
| PayPal Integration Docs | `/docs/functional-areas/payment-paypal-venmo/` | Webhook implementation details |
| DTO Alignment Strategy | `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md` | NSwag type generation requirements |
| Authentication Documentation | `/docs/functional-areas/authentication/` | BFF pattern with HTTP-only cookies |

## 🚨 SCOPE REMOVALS (Updated)

### Items Explicitly Removed from Scope:
- ❌ **Multi-person RSVPs**: Future scope only
- ❌ **Instructor compensation**: Not in scope for this feature
- ❌ **Insurance requirements**: Not in scope for this feature
- ❌ **Anonymous attendance tracking**: Changed to "actual attendance tracking"

## 🚨 SENDGRID INTEGRATION REQUIREMENTS (Enhanced)

### Development Environment Safety (CRITICAL)
1. **Sandbox Mode Required**: Enable `sandbox_mode: { enable: true }` in development
   - **Purpose**: Validates request structure without sending real emails
   - **Behavior**: Uses SendGrid credits but prevents delivery
   - **Critical**: Essential for preventing accidental emails to test accounts

2. **Test Email Strategy**: Use @sink.sendgrid.net domain for development
   - **Purpose**: Accepts emails then immediately deletes them
   - **Example**: test-emails@sink.sendgrid.net
   - **Behavior**: No inbox delivery, safe for automated testing

3. **Environment Separation**: Different SendGrid API keys per environment
   - Development: Free SendGrid account with sandbox mode
   - Staging: Separate paid account for UAT
   - Production: Dedicated account with domain authentication

### Production Configuration Requirements
1. **Domain Authentication**: Configure SPF, DKIM, and DMARC records
2. **Dedicated IP**: Consider for sender reputation management
3. **Template Management**: Create reusable email templates
4. **Monitoring**: Implement delivery rate and bounce tracking

## 🚨 KNOWN PITFALLS (Updated with New Requirements)

### 1. **RSVP Multi-person Violation**: Implementing 2-spot RSVPs
   - **Why it happens**: Previous requirements mentioned this feature
   - **How to avoid**: Implement single RSVP per user only
   - **Test**: User can only RSVP for themselves

### 2. **Social Event Oversimplification**: Treating as free-only
   - **Why it happens**: Assuming social events have no payment component
   - **How to avoid**: Implement optional ticket purchase for suggested donations
   - **Test**: Social events allow optional ticket purchase after RSVP

### 3. **Data Deletion on Cancel**: Removing participation records
   - **Why it happens**: Common pattern to clean up canceled records
   - **How to avoid**: Change status to "canceled", never delete
   - **Test**: Admin can view all canceled RSVPs and refunded tickets

### 4. **Role Exclusivity Error**: Single role per user implementation
   - **Why it happens**: Simplifying role management
   - **How to avoid**: Allow role stacking (Vetted + Teacher + Admin)
   - **Test**: User can have multiple roles simultaneously

### 5. **Banned User Bypass**: Allowing any participation when banned
   - **Why it happens**: Not checking banned status comprehensively
   - **How to avoid**: Hard block at API level for all participation
   - **Test**: Banned user cannot RSVP or purchase tickets

## ✅ VALIDATION CHECKLIST (Updated for Final Version)

Before proceeding to next phase, verify:

### Core Functionality Updates
- [ ] Implementation allows ONE RSVP per user per event (only for themselves)
- [ ] Social events support optional ticket purchases (suggested donations)
- [ ] Ticket terminology consistent: "Purchase Ticket" (not "door entry")
- [ ] Purchasing ticket automatically creates RSVP if needed
- [ ] Same refund policy for all ticket types
- [ ] Canceled RSVPs/tickets marked as "canceled" (never deleted)

### Role System Updates
- [ ] Roles can stack: User can be Vetted + Teacher + Admin
- [ ] Banned status blocks ALL participation (RSVP and ticket purchase)
- [ ] General members blocked from social events but can buy class tickets

### Technical Implementation
- [ ] HTTP-only cookie authentication (not JWT in localStorage)
- [ ] PayPal integration leverages existing webhook infrastructure
- [ ] SendGrid sandbox mode enabled in development environment
- [ ] NSwag-generated types used (no manual DTO interfaces)
- [ ] Admin can view canceled RSVPs and refunded tickets

## 🔄 DISCOVERED CONSTRAINTS (Updated)

### 1. **Simplified RSVP Model**: Single user participation only
   - **Impact**: Eliminates complex multi-user RSVP management
   - **Required Changes**: Single RSVP entity per user per event

### 2. **Social Event Revenue Model**: Optional ticket sales
   - **Impact**: Hybrid model - free RSVP + optional paid tickets
   - **Required Changes**: Two-phase participation tracking

### 3. **Audit Trail Requirements**: Historical tracking of canceled participation
   - **Impact**: Cannot delete participation records
   - **Required Changes**: Status-based cancellation, admin historical view

### 4. **Role Flexibility**: Multiple roles per user
   - **Impact**: More complex permission checking
   - **Required Changes**: Role aggregation logic for access control

### 5. **Banned User Enforcement**: Complete participation blocking
   - **Impact**: Must check banned status for all event interactions
   - **Required Changes**: Comprehensive banned user guards

## 📊 DATA MODEL DECISIONS (Updated for Final Requirements)

```
Entity: EventParticipation (Simplified Model)
- UserId: Guid (Required) - Reference to authenticated user
- EventId: Guid (Required) - Reference to event
- ParticipationType: Enum (Required) - RSVP, TicketPurchase
- PaymentAmount: decimal? (Optional) - Null for RSVPs, amount for tickets
- PaymentStatus: Enum (Optional) - For paid participations only
- Status: Enum (Required) - Active, Canceled (NEVER delete)
- CreatedAt: DateTime (Required) - Participation timestamp
- CancelledAt: DateTime? (Optional) - If cancelled/refunded

Entity: Event (Extension Required)
- EventType: Enum (Required) - SocialEvent, Class, Workshop
- AllowRSVP: bool (Required) - True for social events
- RequirePayment: bool (Required) - True for classes
- AllowOptionalTickets: bool (Required) - True for social events
- OptionalTicketPrice: decimal? (Optional) - For social events
- PriceMin: decimal? (Optional) - For sliding scale
- PriceMax: decimal? (Optional) - For sliding scale
- VettedMembersOnly: bool (Required) - True for social events

Business Logic Updates:
- ONE participation record per user per event (simplified)
- Social events: AllowRSVP=true, RequirePayment=false, AllowOptionalTickets=true
- Classes: AllowRSVP=false, RequirePayment=true, AllowOptionalTickets=false
- Ticket purchase automatically creates RSVP if not exists
- Role validation: Check all roles (can stack), block if banned
- Historical tracking: Status changes only, never delete
```

## 🎯 SUCCESS CRITERIA (Updated)

### 1. **Test Case: Simplified RSVP Flow**
   - **Input**: Vetted member clicks "RSVP Now" on social event
   - **Expected Output**: Single RSVP created, shows in "Upcoming Events", option for optional ticket

### 2. **Test Case: Automatic RSVP on Ticket Purchase**
   - **Input**: User purchases ticket for social event without existing RSVP
   - **Expected Output**: Ticket created AND RSVP automatically added

### 3. **Test Case: Role Stacking**
   - **Input**: User with Vetted + Teacher + Admin roles accesses event
   - **Expected Output**: All permissions available, proper access granted

### 4. **Test Case: Banned User Block**
   - **Input**: Banned user attempts RSVP or ticket purchase
   - **Expected Output**: Complete blocking, clear error message

### 5. **Test Case: Canceled Participation Tracking**
   - **Input**: User cancels RSVP, admin views event participation
   - **Expected Output**: RSVP marked as "canceled", visible in admin historical view

### 6. **Test Case: Social Event Optional Tickets**
   - **Input**: User RSVPs to social event, later purchases optional ticket
   - **Expected Output**: Both RSVP and ticket tracked separately, unified experience

## ⚠️ DO NOT IMPLEMENT (Updated)

- ❌ DO NOT implement multi-person RSVPs (future scope only)
- ❌ DO NOT delete participation records when canceled (use status)
- ❌ DO NOT treat social events as free-only (optional tickets allowed)
- ❌ DO NOT implement single-role-per-user (roles can stack)
- ❌ DO NOT allow banned users any participation (hard block)
- ❌ DO NOT use "door entry tickets" terminology (just "tickets")
- ❌ DO NOT require separate RSVP when purchasing tickets (auto-create)
- ❌ DO NOT implement instructor compensation (explicitly out of scope)
- ❌ DO NOT implement insurance requirements (explicitly out of scope)

## 📝 TERMINOLOGY DICTIONARY (Updated)

| Term | Definition | Example |
|------|------------|---------|
| **RSVP** | Free reservation for social events | User clicks "RSVP Now" → Free participation, shows in "Upcoming Events" |
| **Purchase Ticket** | Paid participation for any event | User clicks "Purchase Ticket" → PayPal payment, shows in "My Tickets" |
| **Optional Ticket** | Suggested donation for social events | After RSVP, user can buy ticket for suggested donation |
| **Canceled Status** | Inactive participation (never deleted) | RSVP/ticket marked as canceled, visible to admin for history |
| **Role Stacking** | Multiple roles per user | User can be Vetted + Teacher + Admin simultaneously |
| **Banned Status** | Complete participation blocking | Cannot RSVP or purchase anything, hard API block |

## 📧 SENDGRID IMPLEMENTATION STRATEGY (Enhanced)

### Phase 1: Development Setup (Enhanced Safety)
1. **Environment Configuration**
   ```bash
   SENDGRID_API_KEY_DEV=dev_key_here
   SENDGRID_SANDBOX_MODE=true
   SENDGRID_FROM_EMAIL=dev-test@sink.sendgrid.net
   ```

2. **Safety Measures (Critical)**
   - **Sandbox mode**: Validates but never sends emails
   - **Sink domain**: @sink.sendgrid.net accepts then deletes
   - **Environment isolation**: Separate keys for each environment
   - **Test prevention**: No real email addresses in development

### Phase 2: Template Creation
1. **RSVP Confirmation Template**
   - Event details, optional ticket purchase link
   - Calendar invite attachment
   - Cancellation policy information

2. **Ticket Purchase Confirmation Template**
   - Ticket details, payment confirmation
   - Unified experience for all ticket types
   - Refund policy (same for all events)

3. **Cancellation Template**
   - Confirmation of cancellation
   - Historical record maintenance notice

## 🔗 NEXT AGENT INSTRUCTIONS

### MANDATORY READING ORDER:
1. **FIRST**: Read APPROVED business requirements document completely (Version 3.0)
2. **SECOND**: Understand simplified RSVP model (one per user, self-only)
3. **THIRD**: Review role stacking requirements (multiple roles per user)
4. **FOURTH**: Check canceled participation tracking (status-based, never delete)
5. **FIFTH**: Understand social event optional ticket model
6. **THEN**: Begin functional design ensuring:
   - Single RSVP per user implementation
   - Role stacking access control
   - Canceled participation historical tracking
   - Optional ticket purchase flow for social events
   - Banned user complete blocking

### CRITICAL VALIDATION POINTS:
- Verify single RSVP per user per event (no multi-person)
- Confirm social events support optional ticket purchases
- Validate role stacking (Vetted + Teacher + Admin possible)
- Test banned user complete blocking
- Ensure canceled records retained for admin view
- Check automatic RSVP creation on ticket purchase

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: Business Requirements Agent (Final Approval)
**Previous Phase Completed**: 2025-09-19 (Stakeholder Requirements APPROVED)
**Status**: ✅ **APPROVED - Ready for Phase 2**

**Next Agent Should Be**: UI Designer for wireframe creation, then Functional Spec Agent
**Next Phase**: Functional Design and UI/UX Design with approved requirements
**Estimated Effort**: 3-4 days for complete functional specification

**Critical Success Factors**:
- Single RSVP per user implementation
- Role stacking access control
- Canceled participation tracking
- Social event optional ticket flow
- Banned user enforcement

---

## 🚀 INTEGRATION NOTES (Updated)

**Existing Systems to Leverage:**
- PayPal webhook processing (operational since 2025-09-14)
- HTTP-only cookie authentication (BFF pattern complete)
- Event management system (needs extension for optional tickets)
- Dashboard framework (needs "Upcoming Events" and "My Tickets" sections)

**New Components Required:**
- Simplified RSVP flow (single user only)
- Optional ticket purchase for social events
- Role stacking access control
- Canceled participation tracking (status-based)
- Banned user enforcement
- SendGrid email integration with enhanced safety
- Admin historical participation viewing

**Data Model Simplifications:**
- Single participation record per user per event
- Status-based cancellation (never delete)
- Role aggregation for access control
- Optional ticket tracking for social events

This handoff ensures the next phase implements the final approved requirements with critical stakeholder corrections, including RSVP simplification, social event optional tickets, role stacking, canceled participation tracking, and comprehensive banned user enforcement.