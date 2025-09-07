# CRITICAL: System Architecture vs Business Requirements Mismatch

## Date: 2025-09-07
## Severity: CRITICAL - Fundamental domain model doesn't match business rules
## Discovery: Phase 4 implementation revealed the mismatch

## The Core Problem

The backend domain model was built with the assumption that ALL events require payment/registration, while the business requirements clearly state:

**Business Requirements (Line 196-200 of business-requirements.md):**
- **Classes**: Ticket-based registration with payment required
- **Social Events**: RSVP system (free) with optional ticket purchases
- Social events show BOTH RSVP table AND tickets sold table

**What's Actually Implemented:**
- ALL events use a `Registration` entity that REQUIRES payment
- The `Registration.Confirm()` method won't work without a Payment object
- There is NO RSVP entity or concept in the backend
- Frontend was trying to paper over this fundamental mismatch

## Evidence of the Mismatch

### 1. Backend Domain Model
```csharp
// Registration.cs - ALWAYS requires payment
public void Confirm(Payment payment)
{
    if (payment == null)
        throw new ArgumentNullException(nameof(payment));
    
    if (payment.Status != PaymentStatus.Completed)
        throw new DomainException("Payment must be completed");
    
    Status = RegistrationStatus.Confirmed;
    // No way to confirm without payment!
}
```

### 2. Old Blazor Workaround
```csharp
// EventDetail.razor - Tried to fake RSVP
if (eventDetail.Price == 0)
    return "Free with RSVP"; // Just UI text, not real RSVP
```

### 3. Backend Has EventType But Doesn't Use It
```csharp
public enum EventType
{
    Workshop,  // Should require payment
    Social,    // Should allow free RSVP
    // But Registration entity treats them the same!
}
```

## Why This Happened

### Sub-Agent Communication Failure

1. **Early Research Phase**: Business requirements agent documented Classes vs Social Events distinction
2. **Implementation Phase**: React developer didn't receive/review this research
3. **Backend Review**: Backend developer found EventType exists but isn't used properly
4. **Result**: Frontend built on top of broken domain model

### The Real Issue

The agents ARE finding the information, but later agents aren't seeing earlier agents' work:
- Business requirements agent found the distinction
- Backend developer found the EventType enum
- React developer implemented without seeing either
- Each agent worked in isolation

## Impact Analysis

### What We've Been Doing Wrong

1. **Creating UI-only fixes**: Making frontend show "RSVP" while backend still requires payment
2. **Duplicating logic**: Frontend trying to handle business rules that should be in domain
3. **Building on broken foundation**: All our Phase 1-4 work assumes a working backend

### What Actually Needs to Happen

#### Option 1: Fix the Domain Model (Correct Solution)
```csharp
// New RSVP entity for Social Events
public class RSVP
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public DateTime RSVPDate { get; set; }
    public bool HasPurchasedTicket { get; set; }
    // NO payment required for basic RSVP
}

// Modified Registration for paid events
public class Registration
{
    // Existing payment-required flow for Classes
    // Optional ticket purchase for Social Events
}
```

#### Option 2: Extend Registration (Compromise)
```csharp
public void ConfirmWithoutPayment() // For free RSVPs
{
    if (Event.EventType != EventType.Social)
        throw new DomainException("Only social events allow free RSVP");
    
    Status = RegistrationStatus.Confirmed;
    // No payment needed
}
```

## The Systemic Problem

### Agent Information Sharing Is Broken

Evidence:
1. Business requirements agent documented RSVP vs Tickets
2. Implementation agents didn't see this documentation
3. We're creating duplicate/conflicting implementations

### Possible Causes

1. **Context limits**: Earlier research exceeds context window
2. **No handoff mechanism**: Agents don't pass critical findings
3. **No central knowledge base**: Each agent starts fresh
4. **No validation step**: Nobody checks implementation against requirements

## Recommendations

### Immediate Actions

1. **STOP all frontend work** until backend supports proper business rules
2. **Backend team must implement** RSVP entity or extend Registration
3. **Create explicit handoff documents** between agent phases
4. **Validate implementation** against requirements before proceeding

### Process Improvements

1. **Mandatory Requirements Review**: Every implementation agent MUST read requirements first
2. **Knowledge Transfer Doc**: Create explicit handoff between phases
3. **Validation Checkpoints**: Compare implementation to requirements at each phase
4. **Central Truth Document**: One place for critical business rules

### Technical Fix Priority

1. **Backend Domain Model**: Fix Registration/RSVP distinction (2-3 days)
2. **API Endpoints**: Separate endpoints for RSVP vs Ticket Purchase (1 day)
3. **Database Migration**: Add RSVP table if needed (1 day)
4. **Frontend Update**: Use correct endpoints based on EventType (1 day)

## Conclusion

We've been building a house on a foundation that doesn't match the blueprints. The frontend is trying to implement business rules that the backend doesn't support. This isn't just a terminology issue - it's a fundamental domain model mismatch.

The agents found all the right information but failed to share it effectively. This is a process failure as much as a technical one.

**We must fix the backend domain model before any more frontend work.**