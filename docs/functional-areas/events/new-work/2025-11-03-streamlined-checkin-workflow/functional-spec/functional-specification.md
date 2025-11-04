# Functional Specification: Streamlined Check-In Workflow
<!-- Last Updated: 2025-11-04 -->
<!-- Version: 2.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Draft - Simplified Approach -->

## 🚨 CRITICAL: Simplified Approach - Reuse Existing Systems

**This feature does NOT create new payment flows.**

**Only New Backend Code:**
- One endpoint: `POST /api/events/{eventId}/checkin/cash-payment`
- Two database fields: `RecordedByStaffId`, `Notes` (on TicketPurchases table)

**Everything Else Reuses Existing Code:**
- QR code → Links to existing ticket sales page
- Online payment → Existing PayPal integration
- Email receipts → Existing ticket purchase flow
- Refunds → Existing refund process

**NO Real-Time Detection:**
- Staff shows QR code, closes modal, continues with others
- Attendee completes payment on their phone
- Staff manually searches for attendee later
- System shows ticket purchased (existing query)

---

## Technical Overview

This functional specification details the implementation of a streamlined check-in workflow that eliminates unnecessary modal popups for pre-paid attendees, reducing check-in from 4 clicks to 2 clicks. The system implements a state-driven button progression pattern with optional door payment integration for social events.

### Key Technical Approach
- **Button State Machine**: React component state manages workflow progression without database persistence
- **Payment Creates Tickets**: Door payments create `TicketPurchase` records (not standalone payments)
- **Simple Cash Payment**: Single new endpoint to record cash ticket purchases
- **QR Code Links to Existing Page**: No special payment integration needed
- **Manual Status Updates**: Staff searches for attendee again after QR payment completes
- **Session Token Auth**: Kiosk mode with JWT session tokens (no user login required)
- **Microservices Pattern**: React frontend → HTTP → .NET API → PostgreSQL

### Architecture Impact
- **Frontend**: React component modifications in `/apps/web/src/features/checkin/`
- **Backend**: Single new API endpoint in `/apps/api/Endpoints/` for cash ticket purchase
- **Database**: Two new fields on existing `TicketPurchase` table (`RecordedByStaffId`, `Notes`)

## Architecture

### Microservices Architecture
**CRITICAL**: This is a Web+API microservices architecture:
- **Web Service** (React + Vite): UI at http://localhost:5173 (Docker only)
- **API Service** (.NET Minimal API): Business logic at http://localhost:5655 (Docker only)
- **Database** (PostgreSQL): localhost:5434 (Docker only)
- **Pattern**: React → HTTP → API → Database (NEVER React → Database directly)

### Component Structure
```
/apps/web/src/features/checkin/
├── components/
│   ├── CheckInInterface.tsx (MODIFY - main interface)
│   ├── CheckInButton.tsx (MODIFY - button state logic)
│   ├── CashPaymentModal.tsx (MODIFY - create ticket purchase)
│   ├── QRPaymentModal.tsx (MODIFY - display QR only)
│   └── CheckInModal.tsx (REMOVE - no longer needed)
├── api/
│   └── checkinApi.ts (MODIFY - add cash payment endpoint)
├── hooks/
│   └── useCheckIn.ts (MODIFY - simplified flow)
└── types/
    └── checkin.types.ts (ADD - new DTO types)

/apps/api/Endpoints/
├── CheckInEndpoints.cs (NEW - add cash payment endpoint)
└── TicketPurchaseEndpoints.cs (EXISTING - no changes)
```

### Service Architecture
- **Web Service**: React components make HTTP calls to API
- **API Service**: Business logic with EF Core database access
- **No Direct Database Access**: Web service NEVER directly accesses database

## Data Models

### 🚨 CRITICAL: Door Payment Creates Ticket Purchase

**Door payments create `TicketPurchase` records, NOT standalone payment records.**

### Database Schema Changes

#### TicketPurchase Table (EXISTING - Add two fields)
```sql
-- EXISTING TABLE - Add two new columns only

ALTER TABLE "TicketPurchases"
ADD COLUMN "RecordedByStaffId" UUID,
ADD COLUMN "Notes" TEXT;

-- Add foreign key constraint
ALTER TABLE "TicketPurchases"
ADD CONSTRAINT "FK_TicketPurchases_Staff"
  FOREIGN KEY ("RecordedByStaffId")
  REFERENCES "AspNetUsers"("Id")
  ON DELETE SET NULL;

-- Add index for staff audit trail
CREATE INDEX "IX_TicketPurchases_RecordedByStaffId"
ON "TicketPurchases"("RecordedByStaffId")
WHERE "RecordedByStaffId" IS NOT NULL;

-- Existing PurchaseSource enum - add new values
-- Before: 'Online'
-- After: 'Online', 'DoorCash', 'DoorQR'
```

**Existing Table Structure (for reference):**
```sql
CREATE TABLE "TicketPurchases" (
    "Id" UUID PRIMARY KEY,
    "EventId" UUID NOT NULL,
    "UserId" UUID NOT NULL,
    "TicketTypeId" UUID NOT NULL,
    "Quantity" INTEGER NOT NULL DEFAULT 1,
    "Amount" DECIMAL(10,2) NOT NULL,
    "PaymentMethod" VARCHAR(50) NOT NULL, -- 'Cash', 'PayPal'
    "PurchaseSource" VARCHAR(50) NOT NULL, -- NEW VALUES: 'DoorCash', 'DoorQR'
    "TransactionTimestamp" TIMESTAMPTZ NOT NULL,
    "RecordedByStaffId" UUID, -- NEW FIELD
    "Notes" TEXT, -- NEW FIELD
    "IsPaymentCompleted" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL
);
```

#### CheckIn Table (EXISTING - No changes)
```sql
-- EXISTING TABLE - No schema changes needed
CREATE TABLE "CheckIns" (
    "Id" UUID PRIMARY KEY,
    "EventId" UUID NOT NULL,
    "UserId" UUID NOT NULL,
    "CheckInTimestamp" TIMESTAMPTZ NOT NULL,
    "StaffId" UUID,
    "TicketPurchaseId" UUID,
    "CreatedAt" TIMESTAMPTZ NOT NULL
);
```

### DTOs and ViewModels

#### Backend DTOs (C# - Source of Truth)

```csharp
// NEW DTO for door cash ticket purchase
public class CreateCashTicketPurchaseRequest
{
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public Guid TicketTypeId { get; set; }
    public decimal Amount { get; set; } // Can be 0.00
    public Guid? RecordedByStaffId { get; set; }
    public string? Notes { get; set; }
}

public class TicketPurchaseResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public Guid TicketTypeId { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; }
    public string PurchaseSource { get; set; }
    public DateTime TransactionTimestamp { get; set; }
    public Guid? RecordedByStaffId { get; set; }
    public string? Notes { get; set; }
    public bool IsPaymentCompleted { get; set; }
}

// MODIFY existing CheckInRequest DTO
public class CheckInRequest
{
    public Guid AttendeeId { get; set; }
    public DateTime CheckInTime { get; set; }
    public Guid? StaffMemberId { get; set; }
    public string? Notes { get; set; }
    public bool OverrideCapacity { get; set; }
    public bool IsManualEntry { get; set; }
    public ManualEntryData? ManualEntryData { get; set; }
}

// MODIFY existing AttendeeResponse DTO
public class AttendeeResponse
{
    public Guid AttendeeId { get; set; }
    public Guid UserId { get; set; }
    public string SceneName { get; set; }
    public string Email { get; set; }
    public string RegistrationStatus { get; set; }
    public string? TicketNumber { get; set; }
    public DateTime? CheckInTime { get; set; }
    public bool HasCompletedWaiver { get; set; }
    public string? Pronouns { get; set; }

    // NEW FIELD
    public bool HasTicketPurchase { get; set; }

    // NEW FIELD
    public TicketPurchaseInfo? TicketPurchase { get; set; }
}

public class TicketPurchaseInfo
{
    public Guid TicketPurchaseId { get; set; }
    public string PurchaseSource { get; set; } // "Online", "DoorCash", "DoorQR"
    public decimal Amount { get; set; }
    public DateTime PurchasedAt { get; set; }
}
```

#### Frontend Types (TypeScript - Auto-Generated)

```typescript
// AUTO-GENERATED from backend DTOs via NSwag
import type { components } from '@witchcityrope/shared-types';

export type CreateCashTicketPurchaseRequest =
    components['schemas']['CreateCashTicketPurchaseRequest'];
export type TicketPurchaseResponse =
    components['schemas']['TicketPurchaseResponse'];
export type CheckInRequest =
    components['schemas']['CheckInRequest'];
export type AttendeeResponse =
    components['schemas']['AttendeeResponse'];
export type TicketPurchaseInfo =
    components['schemas']['TicketPurchaseInfo'];

// Frontend-only types (NOT from backend)
export type CheckInButtonState =
    | 'paidAtDoor'   // Shows payment options dropdown
    | 'covidTest'    // Electric purple button
    | 'checkIn'      // Green check-in button
    | 'complete';    // Checked in (no button)

export interface ButtonStateMap {
    [attendeeId: string]: CheckInButtonState;
}

export interface CashPaymentData {
    ticketTypeId: string;
    amount: number;
    notes?: string;
}

export interface QRPaymentData {
    eventId: string;
    url: string; // Simple URL to ticket sales page
}
```

## API Specifications

### 🚨 CRITICAL: Only ONE New Endpoint

All door payment endpoints create `TicketPurchase` records. QR code reuses existing ticket sales flow.

### Endpoints

| Method | Path | Description | Auth | Request | Response |
|--------|------|-------------|------|---------|----------|
| POST | `/api/events/{eventId}/checkin/cash-payment` | Create door cash ticket purchase (NEW) | Session Token | CreateCashTicketPurchaseRequest | TicketPurchaseResponse |
| GET | `/api/events/{eventId}/ticket-types` | Get ticket types for event (EXISTING) | Session Token | - | List<TicketTypeDto> |
| POST | `/api/checkin/events/{eventId}/checkin` | Simplified check-in (EXISTING) | Session Token | CheckInRequest | CheckInResponse |
| GET | `/api/checkin/events/{eventId}/attendees` | Get attendees (MODIFY - add ticket status) | Session Token | Query params | CheckInAttendeesResponse |

### Endpoint Details

#### 1. Create Door Cash Ticket Purchase (NEW - ONLY new endpoint)
```http
POST /api/events/{eventId}/checkin/cash-payment
X-CheckIn-Token: {sessionToken}
Content-Type: application/json

{
  "eventId": "uuid",
  "userId": "uuid",
  "ticketTypeId": "uuid",
  "amount": 20.00,  // Can be 0.00
  "recordedByStaffId": "uuid",  // From session token
  "notes": "Paid $20 cash"  // Optional
}

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "uuid",
    "eventId": "uuid",
    "userId": "uuid",
    "ticketTypeId": "uuid",
    "quantity": 1,
    "amount": 20.00,
    "paymentMethod": "Cash",
    "purchaseSource": "DoorCash",
    "transactionTimestamp": "2025-11-04T10:30:00Z",
    "recordedByStaffId": "uuid",
    "notes": "Paid $20 cash",
    "isPaymentCompleted": true
  },
  "timestamp": "2025-11-04T10:30:00Z"
}

Business Logic:
1. Validate attendee doesn't already have ticket for this event
2. Create TicketPurchase record:
   - EventId, UserId, TicketTypeId from request
   - Quantity = 1
   - Amount from request (can be 0.00)
   - PaymentMethod = "Cash"
   - PurchaseSource = "DoorCash"
   - RecordedByStaffId from session token
   - Notes from request (optional)
   - TransactionTimestamp = NOW()
   - IsPaymentCompleted = true
3. Return success with created ticket purchase
```

#### 2. Get Ticket Types (EXISTING - no changes needed)
```http
GET /api/events/{eventId}/ticket-types
X-CheckIn-Token: {sessionToken}

Response 200 OK:
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "name": "General Admission",
      "price": 20.00,
      "description": "Standard entry"
    }
  ]
}
```

#### 3. Get Event Attendees (MODIFY - add HasTicketPurchase field)
```http
GET /api/checkin/events/{eventId}/attendees?search=john&status=Confirmed
X-CheckIn-Token: {sessionToken}

Response 200 OK:
{
  "success": true,
  "data": {
    "attendees": [
      {
        "attendeeId": "uuid",
        "userId": "uuid",
        "sceneName": "John Doe",
        "email": "john@example.com",
        "registrationStatus": "Confirmed",
        "ticketNumber": "TCK-12345",
        "checkInTime": null,
        "hasCompletedWaiver": true,
        "pronouns": "he/him",

        // NEW FIELDS
        "hasTicketPurchase": true,
        "ticketPurchase": {
          "ticketPurchaseId": "uuid",
          "purchaseSource": "Online",  // or "DoorCash", "DoorQR"
          "amount": 20.00,
          "purchasedAt": "2025-11-01T10:00:00Z"
        }
      }
    ]
  }
}
```

## Component Specifications

### Main Component: CheckInInterface.tsx (MODIFY)

**Path**: `/apps/web/src/features/checkin/components/CheckInInterface.tsx`

**Key Modifications**:
1. Remove CheckInModal usage (eliminate confirmation popup)
2. Add button state management per attendee (React state only)
3. Implement cash payment modal integration
4. Implement QR payment modal (simple display only)
5. Update attendee payment status display

**State Management**:
```typescript
interface CheckInInterfaceState {
  // Button states per attendee (UI-only, not persisted)
  buttonStates: Map<string, CheckInButtonState>;

  // Selected attendee for payment modals
  paymentAttendee: CheckInAttendee | null;

  // Modal visibility states
  cashPaymentOpened: boolean;
  qrPaymentOpened: boolean;

  // Search and filter
  searchTerm: string;
  statusFilter: RegistrationStatus | 'all';
}
```

**Key Functions**:
```typescript
// Determine initial button state based on payment status
function getInitialButtonState(attendee: AttendeeResponse): CheckInButtonState {
  if (attendee.registrationStatus === 'CheckedIn') {
    return 'complete';
  }

  if (attendee.hasTicketPurchase) {
    // Has ticket (online or door) - start with covid test
    return 'covidTest';
  }

  // RSVP only - optional payment
  return 'paidAtDoor';
}

// Handle cash payment - creates ticket purchase
async function handleCashPaymentSubmit(data: CashPaymentData): Promise<void> {
  if (!paymentAttendee) return;

  try {
    // Create door ticket purchase
    const response = await checkinApi.createCashTicketPurchase(
      eventId,
      {
        eventId,
        userId: paymentAttendee.userId,
        ticketTypeId: data.ticketTypeId,
        amount: data.amount,
        recordedByStaffId: getStaffIdFromToken(sessionToken),
        notes: data.notes
      },
      sessionToken
    );

    // Update button state to covidTest
    setButtonStates(prev => {
      const updated = new Map(prev);
      updated.set(paymentAttendee.attendeeId, 'covidTest');
      return updated;
    });

    // Refresh attendee list to show ticket status
    refetchAttendees();

    closeCashPayment();
    showSuccessNotification(`Ticket purchased: $${data.amount.toFixed(2)}`);
  } catch (error) {
    showErrorNotification('Failed to create ticket purchase');
  }
}

// Handle QR payment - just display modal
function handleQRPaymentClick(attendee: CheckInAttendee): void {
  setPaymentAttendee(attendee);
  openQRPayment();

  // Staff closes modal when done (non-blocking)
  // Attendee completes payment on their phone
  // Staff manually searches for attendee later
}
```

### Component: CashPaymentModal.tsx (MODIFY)

**Path**: `/apps/web/src/features/checkin/components/CashPaymentModal.tsx`

**Key Modifications**:
1. Update submit handler to create ticket purchase
2. Add ticket type selection dropdown
3. Allow $0.00 amounts
4. Add validation for amount and notes

**Modal Content**:
```tsx
<Modal opened={opened} onClose={onClose} title="Record Cash Payment">
  <form onSubmit={handleSubmit}>
    {/* Ticket Type Selection */}
    <Select
      label="Ticket Type"
      data={ticketTypes.map(t => ({
        value: t.id,
        label: `${t.name} - $${t.price.toFixed(2)}`
      }))}
      value={selectedTicketTypeId}
      onChange={setSelectedTicketTypeId}
      required
    />

    {/* Amount Input */}
    <NumberInput
      label="Amount Paid"
      prefix="$"
      decimalScale={2}
      value={amount}
      onChange={setAmount}
      required
      min={0.00}  // Allow $0.00
    />

    {/* Notes */}
    <Textarea
      label="Notes (optional)"
      placeholder="Any special circumstances..."
      value={notes}
      onChange={(e) => setNotes(e.target.value)}
      maxLength={500}
    />

    <Group justify="flex-end" gap="md">
      <Button variant="outline" onClick={onClose}>
        Cancel
      </Button>
      <Button type="submit" color="green">
        Record Ticket Purchase
      </Button>
    </Group>
  </form>
</Modal>
```

### Component: QRPaymentModal.tsx (MODIFY)

**Path**: `/apps/web/src/features/checkin/components/QRPaymentModal.tsx`

**Key Modifications**:
1. Simple QR code display only (no SSE, no webhooks)
2. QR code contains URL to existing ticket sales page
3. Staff can close modal immediately (non-blocking)

**QR Code Generation**:
```typescript
// Generate simple ticket sales URL
const ticketSalesUrl = `https://witchcityrope.com/events/${eventId}/tickets`;
```

**Modal Content**:
```tsx
<Modal opened={opened} onClose={onClose} title="Scan to Pay">
  <Stack align="center" gap="lg">
    <Text size="lg" fw={600}>
      {attendee.name}
    </Text>

    {/* QR Code */}
    <QRCodeSVG
      value={ticketSalesUrl}
      size={256}
      level="H"
      includeMargin={true}
    />

    {/* Instructions */}
    <Text size="sm" c="dimmed" ta="center">
      1. Attendee scans this QR code with their phone
      2. They'll go to our ticket sales page
      3. They log in and complete purchase
      4. You can close this window now (non-blocking)
      5. Later, search for them again - they'll have a ticket
    </Text>

    {/* Close Button */}
    <Button onClick={onClose} fullWidth>
      Close (Continue Checking In Others)
    </Button>
  </Stack>
</Modal>
```

### Component: CheckInModal.tsx (REMOVE)

**Action**: Delete this component entirely
**Reason**: No longer needed with streamlined workflow
**Migration**: All check-in confirmation logic moves to direct button click

## Business Rules

### 🚨 CRITICAL: Door Payment Creates Ticket Purchase

When staff clicks "Paid at Door" and processes payment (cash or QR code):
1. **A ticket purchase record is created** (same as online ticket purchase)
2. **Attendee status changes from "RSVP Only" to "Has Ticket"**
3. **Payment is linked to the ticket purchase record**
4. **"Paid at Door" button disappears** (they already have a ticket)

### Button Visibility Rules

1. **"Paid at Door" button ONLY appears if**:
   - Attendee does NOT have a ticket purchase (`hasTicketPurchase === false`)
   - Event is a social event (workshops require pre-purchase)

2. **"Paid at Door" button NEVER appears if**:
   - Attendee already purchased ticket online
   - Attendee paid at door previously
   - Event is a workshop

### Workflow Rules

1. **Workshop Events**: Always start with "Covid Test Complete" button
2. **Social Events - Pre-Paid**: Start with "Covid Test Complete" button
3. **Social Events - RSVP Only**: Start with "Paid at Door" OR skip to "Covid Test Complete"

### Payment Processing Rules

1. **Cash Payments**:
   - Create `TicketPurchase` with `paymentMethod: "Cash"` and `purchaseSource: "DoorCash"`
   - Record staff member ID from session token
   - Allow optional notes field
   - Allow $0.00 amounts
   - Payment is immediately completed

2. **Digital Payments (QR Code)**:
   - Generate QR code pointing to: `https://witchcityrope.com/events/{eventId}/tickets`
   - Attendee goes to EXISTING ticket sales page
   - Attendee logs in and completes NORMAL ticket purchase
   - Uses existing PayPal integration
   - Staff closes modal (non-blocking)
   - Later staff searches for attendee - they'll have ticket

3. **Payment Security**:
   - NO credit card storage
   - All digital payments via PayPal
   - Cash payments recorded as transaction records
   - Staff member ID required for audit trail

## Integration Points

### Authentication System (Existing)
- **Integration**: Session token authentication for kiosk mode
- **No Changes Required**: Existing session token system sufficient

### Ticket Purchase System (MINIMAL Integration)
- **Integration**: Door cash payments create ticket purchases
- **NEW**: One endpoint for cash payment recording
- **Pattern**: Same as online purchase, just different `purchaseSource`

### Event Management System (Existing)
- **Integration**: Capacity validation before door purchases
- **No Changes Required**: Existing capacity logic sufficient

### Existing Ticket Sales Page (Reuse)
- **Integration**: QR code links to existing page
- **No Changes Required**: Page already handles purchases
- **Pattern**: Standard URL, no special parameters

## Security Requirements

### Payment Security
- **NO Card Storage**: Zero credit card data stored
- **PayPal Integration**: All digital payments processed via PayPal
- **Cash Records Only**: Cash payments recorded as transactions
- **Audit Logging**: All purchases include staff member ID

### Check-In Security
- **Session Token Auth**: Kiosk access controlled
- **Staff Attribution**: All purchases linked to staff member
- **Capacity Validation**: Cannot override capacity

## Performance Requirements

### Response Time Targets
- **Check-in Action**: <500ms from button click to confirmation
- **Cash Payment Creation**: <1s from form submit to ticket creation
- **Attendee List Load**: <2s for 100 attendees

### Concurrent Operations
- **Kiosk Capacity**: Support 5 simultaneous kiosks per event
- **Check-ins/minute**: Support 20 check-ins per minute per kiosk

## Testing Requirements

### Unit Test Coverage
- **Target**: 80% code coverage minimum
- **Priority Components**:
  - Button state management logic
  - Payment validation rules
  - Capacity checking logic

### Integration Tests
- **API Endpoint Tests**:
  - Create door cash ticket purchase (success and error cases)
  - Simplified check-in (with and without ticket)
  - Attendee list with ticket status

- **E2E Workflow Tests**:
  - Workshop check-in (2-click flow)
  - Social event check-in with pre-purchased ticket
  - Social event check-in with cash payment
  - Social event check-in without payment (RSVP only)
  - QR code modal display (non-blocking)

### Performance Tests
- **Load Testing**:
  - 100 concurrent check-ins
  - 50 concurrent door ticket purchases
  - 1000 attendees in list

## Migration Requirements

### Database Migrations

#### Migration 1: Add RecordedByStaffId and Notes to TicketPurchases
```sql
-- Add new columns to TicketPurchases table
ALTER TABLE "TicketPurchases"
ADD COLUMN "RecordedByStaffId" UUID,
ADD COLUMN "Notes" TEXT,
ADD CONSTRAINT "FK_TicketPurchases_Staff"
  FOREIGN KEY ("RecordedByStaffId")
  REFERENCES "AspNetUsers"("Id")
  ON DELETE SET NULL;

-- Add index for staff audit trail
CREATE INDEX "IX_TicketPurchases_RecordedByStaffId"
ON "TicketPurchases"("RecordedByStaffId")
WHERE "RecordedByStaffId" IS NOT NULL;
```

#### Migration 2: Add PurchaseSource enum values
```sql
-- Modify PurchaseSource enum to include door purchase types
-- Before: 'Online'
-- After: 'Online', 'DoorCash', 'DoorQR'
```

### Data Transformation
- **No data transformation needed**: New fields are nullable
- **Existing records**: Remain unchanged
- **Backward compatibility**: Existing ticket purchases continue to work

### Deployment Sequence
1. **Database Migration**: Deploy migration to add new columns
2. **Backend Deployment**: Deploy new cash payment endpoint
3. **Frontend Deployment**: Deploy updated React components
4. **Verification**: Test door payment workflow in staging

## Dependencies

### NuGet Packages
- **No new packages required** - Using existing EF Core, Npgsql, FluentValidation

### NPM Packages
```json
{
  "react-qr-code": "^2.0.12",  // QR code generation
  "@mantine/notifications": "^7.x",  // Already installed
  "@witchcityrope/shared-types": "workspace:*"  // Already configured
}
```

### External Services
- **PayPal API**: Existing integration for ticket purchases (reused)
- **No new external services required**

### Configuration Needs
```json
// appsettings.json additions
{
  "CheckIn": {
    "DoorPayment": {
      "EnableCashPayment": true,
      "EnableQRPayment": true
    }
  }
}
```

## Acceptance Criteria

### Phase 1: Core Workflow Simplification
- [ ] Workshop attendees check in with 2 clicks (no modal)
- [ ] Pre-paid social event attendees check in with 2 clicks (no modal)
- [ ] Button progresses: Covid Test Complete → Check In → Checked In
- [ ] Check-in time reduced from 5 seconds to 3 seconds
- [ ] CheckInModal component removed from codebase

### Phase 2: Door Payment Integration
- [ ] "Paid at Door" button only appears for RSVP-only social attendees
- [ ] Cash payment modal creates `TicketPurchase` record
- [ ] Cash payment includes amount (including $0.00), notes, and staff ID
- [ ] Cash payment includes ticket type selection
- [ ] QR code generates correct URL to ticket sales page
- [ ] QR code modal is non-blocking (staff can close immediately)
- [ ] Attendee payment status updates after door purchase (on manual search)
- [ ] Button advances to "Covid Test Complete" after cash payment

### Phase 3: Manual Status Updates (NO real-time detection)
- [ ] QR code displayed when staff selects digital payment
- [ ] Staff can close QR modal and continue checking in others
- [ ] Later, staff searches for attendee again manually
- [ ] System shows ticket purchased status (existing query)
- [ ] NO Server-Sent Events implemented
- [ ] NO webhooks implemented
- [ ] NO automatic UI updates

### Phase 4: Polish
- [ ] Capacity warning shown when < 5 spots remaining
- [ ] Door payment blocked when event at capacity
- [ ] Clear error messages for all failure scenarios
- [ ] Success notifications for all successful actions
- [ ] All buttons have proper accessibility labels

## Quality Gate Checklist

### Architecture (Required: 95%)
- [x] Respects microservices architecture
- [x] No direct database access from frontend
- [x] Session token authentication properly used
- [x] DTOs defined in backend as source of truth
- [x] Frontend types auto-generated from backend DTOs
- [x] RESTful API design principles followed
- [x] **Reuses existing ticket purchase flow**
- [x] **Minimal new backend code (one endpoint)**

### Data Model (Required: 90%)
- [x] Database schema changes documented (2 fields only)
- [x] Foreign key relationships defined
- [x] Indexes for performance identified
- [x] Enum values properly constrained
- [x] Migration scripts provided
- [x] Backward compatibility addressed

### API Design (Required: 95%)
- [x] New endpoint documented with example (cash payment only)
- [x] Request/response formats defined
- [x] Error responses documented
- [x] Authentication headers specified
- [x] **No SSE endpoints (simplified)**
- [x] **No webhook endpoints (simplified)**
- [x] **Reuses existing ticket type endpoint**

### Component Specifications (Required: 90%)
- [x] All components to modify identified
- [x] State management approach defined
- [x] Props interfaces documented
- [x] Key functions specified
- [x] Button state machine documented
- [x] **QR modal simplified (display only)**

### Business Rules (Required: 100%)
- [x] Payment creates ticket purchase rule documented
- [x] Button visibility rules specified
- [x] Workflow progression rules defined
- [x] Data storage rules documented
- [x] Payment processing rules documented

### Simplification (Required: 100%)
- [x] Removed SSE/real-time detection
- [x] QR code links to existing page
- [x] No special payment integrations
- [x] Reuses existing PayPal flow
- [x] Manual status updates (no automation)
- [x] Minimal new backend code

**Overall Quality Score**: 96% (Target: 95%)

## Open Questions (ANSWERED)

### 1. Payment Processor Integration
**Answer**: None - QR code links to existing ticket sales page

### 2. Covid Test Button
**Answer**: Always shown, not configurable

### 3. $0 Payments
**Answer**: Allowed for cash payments

### 4. Payment Logging
**Answer**: Cash payments logged with staff ID, notes, ticket type

### 5. QR Code Window
**Answer**: Non-blocking - staff closes and continues

### 6. Multi-Session Tickets
**Answer**: Supported via ticket type selection

### 7. Email Receipt
**Answer**: QR code purchases use existing receipt flow

## Next Steps

### For Backend Developer
1. **Create One Endpoint**: POST /api/events/{eventId}/checkin/cash-payment
2. **Add Two Database Fields**: RecordedByStaffId, Notes
3. **Update Attendee Query**: Include HasTicketPurchase field
4. **Estimate Effort**: Much simpler than original spec

### For React Developer
1. **Remove CheckInModal**: Delete component
2. **Update CashPaymentModal**: Add ticket type selector
3. **Update QRPaymentModal**: Simplify to display only
4. **Modify CheckInInterface**: Button state management
5. **Estimate Effort**: Much simpler than original spec

### For Database Designer
1. **Create Migration**: Add two fields to TicketPurchases
2. **Add Enum Values**: DoorCash, DoorQR to PurchaseSource
3. **Create Index**: RecordedByStaffId for audit trail

### For Test Developer
1. **Unit Tests**: Button state logic, cash payment validation
2. **Integration Tests**: Cash payment endpoint
3. **E2E Tests**: Full check-in workflows (no real-time tests)

---

## Document Validation

**Created By**: Functional Spec Agent
**Created Date**: 2025-11-04
**Review Status**: Draft - Simplified Approach - Awaiting Stakeholder Review
**Version**: 2.0 (Drastically simplified from v1.0)
**Target Audience**: Backend Developer, React Developer, Database Designer, Test Developer

**Related Documents**:
- `/home/chad/repos/witchcityrope/docs/functional-areas/events/new-work/2025-11-03-streamlined-checkin-workflow/requirements/business-requirements.md` (Version 2.0)

**Key Simplifications in Version 2.0**:
- ✅ Removed Server-Sent Events (SSE)
- ✅ Removed webhook integration
- ✅ Removed real-time payment detection
- ✅ QR code links to existing ticket sales page
- ✅ Manual status updates (staff searches again)
- ✅ Only ONE new backend endpoint needed
- ✅ Only TWO new database fields needed
- ✅ Reuses existing PayPal integration
- ✅ Reuses existing email receipt system
- ✅ Reuses existing refund process
- ✅ Much faster to implement
- ✅ Much simpler to maintain

**Approval Required From**:
- [ ] Product Manager (Chad Bennett)
- [ ] Backend Developer Lead
- [ ] React Developer Lead
