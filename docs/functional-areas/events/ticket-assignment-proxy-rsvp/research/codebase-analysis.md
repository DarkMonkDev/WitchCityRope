# Codebase Analysis: Ticket Assignment & Proxy RSVP

<!-- Last Updated: 2026-03-18 -->
<!-- Purpose: Document current system state so future agents understand what exists before implementing changes -->

## Current System Architecture

### Ticket Purchase System

**Key Files:**
- `apps/api/Features/Participation/Services/AttendanceService.cs` (2011 lines) - Core business logic
- `apps/api/Features/Payments/Endpoints/CheckoutEndpoints.cs` - Credit card checkout (Authorize.net)
- `apps/api/Features/Payments/Endpoints/PayPalCheckoutController.cs` - PayPal checkout
- `apps/api/Features/Payments/Endpoints/KioskPaymentEndpoints.cs` - Door/cash purchases
- `apps/api/Models/TicketType.cs` - Ticket type definition
- `apps/api/Models/TicketPurchase.cs` - Purchase record
- `apps/web/src/features/payments/pages/EventPaymentPage.tsx` - Frontend checkout page

**Current Behavior:**
- Hardcoded to 1 ticket per purchase per person
- Tickets purchased by authenticated user for themselves only (`userId` from JWT claims)
- No first/last name fields for buying on behalf of others
- 4-stage atomic checkout: Validate -> Create Pending -> Charge -> Finalize (with rollback)
- Supports both Credit Card (Authorize.net Accept.js) and PayPal
- Sliding scale pricing (0-75% discount) supported
- Multi-session tickets: One TicketPurchase creates multiple EventAttendance records (one per session)
- Idempotency keys prevent duplicate processing

**Capacity Management:**
- `Event.Capacity` - Total event capacity
- `TicketType.Available` - Per-ticket-type inventory
- `TicketType.Sold` - Calculated property (not stored), counts active EventAttendance records
- Reserved count = Active + PendingPayment (prevents overselling during checkout)

### RSVP System

**Key Files:**
- `apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs` - RSVP endpoints
- `apps/api/Features/Participation/Services/AttendanceService.cs` - RSVP business logic
- `apps/api/Features/Participation/Models/CreateRSVPRequest.cs` - RSVP request DTO
- `apps/web/src/features/events/api/mutations.ts` - Frontend RSVP mutations

**Current Behavior:**
- Self-service only (no proxy RSVP)
- POST `/api/events/{eventId}/rsvp` with `CreateRSVPRequest` (eventId, notes, eventWaiverAccepted=true)
- Vetting check via `IVettingAccessControlService.CanUserRsvpAsync()`
- Timing validation via `ITimeZoneService.IsActionAllowedAsync()`
- Capacity check using reserved count
- Duplicate prevention: Queries for existing ACTIVE RSVPs
- Email confirmation sent on success

### EventAttendance Model (Shared by Tickets and RSVPs)

**File:** `apps/api/Features/Participation/Entities/EventAttendance.cs`

**Critical Architecture:**
- Both RSVPs and Tickets are `EventAttendance` records with different `AttendanceType` enum values
- `AttendanceType.RSVP = 1`, `AttendanceType.Ticket = 2`
- `AttendanceStatus`: Active(1), Cancelled(2), Refunded(3), Waitlisted(4), PendingPayment(5)
- Ticket purchase auto-creates RSVP record for social events (two EventAttendance records)
- Ticket cancellation cancels both ticket AND associated RSVP records
- Users CAN have both active Ticket and active RSVP simultaneously

**Unique Constraint:** `(UserId, EventId, AttendanceType, SessionId)` where Status=Active

**Waiver Fields (already exist):**
- `EventWaiverAccepted` (bool) - on both EventAttendance AND TicketPurchase
- `EventWaiverAcceptedAt` (DateTime?, UTC) - timestamp of acceptance

### Vetting Access Control

**File:** `apps/api/Features/Vetting/Services/VettingAccessControlService.cs`

**Current Behavior:**
- `CanUserRsvpAsync()` and `CanUserPurchaseTicketAsync()` check user's vetting status
- Blocks: OnHold(5), Denied(4), Withdrawn(6)
- Allows: All other statuses including no application
- 5-minute cache per user for performance
- `Event.VettedMembersOnly` (bool) flag exists on events

**Potential Gap:** The vetting check may not fully enforce `VettedMembersOnly` against non-vetted users (only blocks specifically denied statuses). This should be verified during implementation.

### User Profile System

**Key Files:**
- `apps/api/Models/ApplicationUser.cs` - User entity
- `apps/web/src/pages/dashboard/ProfileSettingsPage.tsx` - Profile settings (3 tabs: Personal, Password, Vetting)
- `apps/api/Features/Users/Models/MemberDetails/MemberDetailsModels.cs` - Admin member details

**Relevant Fields on ApplicationUser:**
- `VettingStatus` (int) - Source of truth for vetting state (0-6 enum)
- `IsVetted` (computed) - Returns `VettingStatus == 3`
- `TermsOfServiceAccepted` (bool) - Platform-wide ToS acceptance
- `TermsOfServiceAcceptedAt` (DateTime?, UTC)
- `SceneName` (string) - Display name used in community

### Member Search

**Current:** Admin-only member search via `GET /api/admin/users` with filters (search, role, isVetted, pagination). No user-facing member search exists.

### Email System

**Existing Templates:** RSVP confirmation, ticket purchase confirmation, cancellation emails. All sent fire-and-forget.

---

## Gaps Identified (What Needs to Be Built)

| Gap | Description | Impact |
|-----|-------------|--------|
| **No multi-ticket purchase** | Hardcoded to 1 ticket per purchase | Need quantity selector + per-ticket assignment |
| **No ticket assignment** | No concept of buying for someone else | Need PENDING_ACCEPTANCE status, assignment tracking |
| **No delegate/proxy system** | No authorization model for acting on behalf of another | Need Authorized Contacts entity + profile UI |
| **No user-facing member search** | Only admin can search members | Need scoped search for authorized contacts |
| **No PENDING_ACCEPTANCE status** | Current statuses: Active, Cancelled, Refunded, Waitlisted, PendingPayment | Need new AttendanceStatus enum value |
| **No assignment notification emails** | Only confirmation and cancellation emails exist | Need 4 new email templates (ticket + RSVP, each with assignment + reminder) |
| **No proxy RSVP** | RSVPs are self-service only | Need delegate RSVP creation flow |
| **No ticket reassignment** | Once purchased, ticket is permanent to buyer | Need reassignment for declined/unassigned tickets |
| **No configurable quantity limit** | No per-event or per-ticket quantity configuration | Need max quantity field on TicketType and/or Event |
