# Multi-Ticket Type Analysis for Complex Event Management
<!-- Last Updated: 2025-08-24 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Analysis Complete -->

## Executive Summary
Analysis of three architectural approaches to handle complex ticketing scenarios with multiple ticket types, overlapping capacities, and intricate availability calculations for WitchCityRope events. Each solution addresses the core challenge: maintaining accurate capacity management while providing flexible ticket options for multi-day workshops, tiered pricing, and series passes.

## Business Context

### Problem Statement
WitchCityRope needs sophisticated ticketing to support:
- Multi-day workshop series with both full-series and individual day passes
- Overlapping capacity constraints (venue limits vs ticket type limits)
- Complex pricing structures (member/non-member, early bird, group discounts)
- Real-time availability calculations across interdependent ticket types

### Example Scenario
**3-Day Intensive Rope Workshop**
- Venue capacity: 20 people per day
- Full Series Pass (Days 1-3): $150, max 15 available
- Individual Day 1: $60, max 10 available  
- Individual Day 2: $60, max 10 available
- Individual Day 3: $60, max 10 available

**Complexity**: If 5 buy full series, Day 1 shows 15 spots remaining (20 venue - 5 series), but individual Day 1 tickets remain capped at 10.

### Success Metrics
- Accurate real-time availability display
- Zero oversold events
- Organizer can create complex ticket structures in <5 minutes
- Attendee purchase experience <2 minutes from selection to confirmation
- Support 95% of common WitchCityRope event patterns

## Proposed Solutions

## Solution 1: Nested Ticket Hierarchy (Simplest)

### Data Model Approach
```csharp
public class EventTicketType
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int MaxQuantity { get; set; }
    public DateTime SaleStartDate { get; set; }
    public DateTime SaleEndDate { get; set; }
    public List<Guid> IncludedEventIds { get; set; } // For multi-day series
    public bool IsParentTicket { get; set; } // Series passes are parents
    public Guid? ParentTicketTypeId { get; set; } // Individual days reference parent
}

public class EventCapacityRule  
{
    public Guid EventId { get; set; }
    public int VenueCapacity { get; set; }
    public Dictionary<DateTime, int> DailyCapacities { get; set; }
}
```

**Capacity Calculation Logic:**
1. Calculate total sold for each event day
2. Subtract parent ticket sales from venue capacity
3. Remaining capacity available for individual tickets
4. Individual tickets cannot exceed their own MaxQuantity

### User Interface Design

**Organizer Setup:**
1. Create base event with venue capacity
2. Add "Series Pass" ticket type (mark as parent)
3. Add individual day tickets, link to series pass as parent
4. System auto-calculates interdependencies

**Attendee Experience:**
- Ticket options grouped: "Full Series" section, "Individual Days" section
- Real-time availability: "15 spots remaining" or "5 series passes + 8 individual Day 1 available"
- Clear pricing comparison displayed

### Business Logic
```typescript
function calculateAvailableTickets(eventId: string, date?: Date): TicketAvailability {
    const venueCapacity = getVenueCapacity(eventId, date);
    const seriesPassesSold = getTicketsSold(parentTicketTypes);
    const individualTicketsSold = getTicketsSold(individualTicketTypes, date);
    
    const remainingVenueCapacity = venueCapacity - seriesPassesSold - individualTicketsSold;
    const remainingIndividualQuota = individualTicketType.maxQuantity - individualTicketsSold;
    
    return Math.min(remainingVenueCapacity, remainingIndividualQuota);
}
```

### Pros
- **Simple mental model**: Clear parent-child relationship
- **Fast development**: Straightforward data structure
- **Easy organizer setup**: Intuitive hierarchy
- **Good for 80% of use cases**: Handles most WitchCityRope scenarios

### Cons
- **Limited flexibility**: Hard to handle complex interdependencies
- **Scaling challenges**: Becomes unwieldy with >3 ticket types
- **Edge case handling**: Difficult for prerequisites or complex group discounts
- **Business rule constraints**: Hard to implement "must buy Day 1 to get Day 2"

---

## Solution 2: Inventory Pool System (Most Flexible)

### Data Model Approach
```csharp
public class EventInventoryPool
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public DateTime EventDate { get; set; }
    public string PoolName { get; set; } // "Day1Attendance", "Day2Attendance"
    public int TotalCapacity { get; set; }
    public int ReservedCapacity { get; set; }
    public int AvailableCapacity => TotalCapacity - ReservedCapacity;
}

public class EventTicketType  
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int MaxQuantity { get; set; }
    public List<InventoryPoolAllocation> PoolAllocations { get; set; }
    public List<TicketTypeRule> BusinessRules { get; set; }
}

public class InventoryPoolAllocation
{
    public Guid InventoryPoolId { get; set; }
    public int QuantityRequired { get; set; } // How many pool slots this ticket consumes
}

public class TicketTypeRule
{
    public string RuleType { get; set; } // "PREREQUISITE", "MEMBER_ONLY", "EARLY_BIRD"
    public string RuleConfiguration { get; set; } // JSON for rule parameters
}
```

**Example Configuration:**
- **Day 1 Pool**: 20 capacity
- **Day 2 Pool**: 20 capacity  
- **Day 3 Pool**: 20 capacity
- **Series Pass**: Allocates 1 from each pool (3 total)
- **Individual Day 1**: Allocates 1 from Day 1 pool only
- **Couples Pass**: Allocates 2 from target pool(s)

### User Interface Design

**Organizer Setup:**
1. Define inventory pools (usually by date/session)
2. Create ticket types with pool allocations
3. Set business rules via configuration UI
4. Preview capacity scenarios before publishing

**Attendee Experience:**
- Smart availability display: "3 series passes available (limited by Day 2)"
- Dynamic pricing based on pool availability
- Clear bottleneck communication: "Day 1 filling fast - only 2 individual spots left"

### Business Logic
```typescript
function calculateTicketAvailability(ticketTypeId: string): TicketAvailability {
    const ticketType = getTicketType(ticketTypeId);
    const maxByQuota = ticketType.maxQuantity - getTicketsSold(ticketTypeId);
    
    let maxByPools = Infinity;
    for (const allocation of ticketType.poolAllocations) {
        const pool = getInventoryPool(allocation.inventoryPoolId);
        const canSellFromThisPool = Math.floor(pool.availableCapacity / allocation.quantityRequired);
        maxByPools = Math.min(maxByPools, canSellFromThisPool);
    }
    
    return {
        available: Math.min(maxByQuota, maxByPools),
        limitedBy: maxByQuota < maxByPools ? 'quota' : 'capacity',
        bottleneckPool: getBottleneckPool(ticketType.poolAllocations)
    };
}
```

### Pros
- **Maximum flexibility**: Handles any conceivable scenario
- **Scalable architecture**: Easy to add new complexity
- **Clear capacity attribution**: Know exactly what's limiting availability
- **Powerful rule engine**: Complex business logic possible
- **Future-proof**: Can grow with community needs

### Cons  
- **High complexity**: Significant development time
- **Organizer learning curve**: Setup requires understanding of pool concept  
- **Over-engineering risk**: May be too complex for simple events
- **Debugging challenges**: Complex interdependencies hard to troubleshoot

---

## Solution 3: Event Session Matrix (Balanced)

### Data Model Approach
```csharp
public class EventSession
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string SessionName { get; set; } // "Day 1", "Day 2", "Day 3"
    public DateTime SessionDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int Capacity { get; set; }
    public bool IsRequired { get; set; } // For prerequisite handling
}

public class EventTicketType
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int MaxQuantity { get; set; }
    public List<Guid> IncludedSessionIds { get; set; }
    public PricingTier PricingTier { get; set; } // Member, Non-member, Early Bird
    public List<string> Prerequisites { get; set; } // "MUST_ATTEND_SESSION_{ID}"
}

public class SessionAttendance
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public Guid TicketTypeId { get; set; }
    public DateTime PurchaseDate { get; set; }
}
```

**Matrix Approach:**
- Events composed of discrete sessions
- Ticket types specify which sessions they include
- Capacity calculated per session, not per ticket type
- Natural support for prerequisites and dependencies

### User Interface Design

**Organizer Setup:**
1. Create event sessions (Day 1, Day 2, Day 3)
2. Set capacity per session (usually same, can differ)
3. Create ticket types selecting which sessions included
4. Configure pricing tiers and prerequisites

**Attendee Experience:**
- Session-based view: See each day's availability
- Matrix comparison: "Series Pass includes all 3 days vs individual selection"
- Smart recommendations: "Day 1 is prerequisite for Day 2"

### Business Logic
```typescript
function calculateSessionAvailability(sessionId: string): SessionAvailability {
    const session = getEventSession(sessionId);
    const totalAttendees = getSessionAttendance(sessionId).length;
    const availableSpots = session.capacity - totalAttendees;
    
    return {
        sessionId,
        sessionName: session.sessionName,
        totalCapacity: session.capacity,
        currentAttendance: totalAttendees,
        availableSpots,
        isAvailable: availableSpots > 0
    };
}

function validateTicketPurchase(ticketTypeId: string, userId: string): PurchaseValidation {
    const ticketType = getTicketType(ticketTypeId);
    const errors = [];
    
    // Check session availability
    for (const sessionId of ticketType.includedSessionIds) {
        const sessionAvailable = calculateSessionAvailability(sessionId);
        if (!sessionAvailable.isAvailable) {
            errors.push(`${sessionAvailable.sessionName} is sold out`);
        }
    }
    
    // Check prerequisites
    for (const prerequisite of ticketType.prerequisites) {
        if (!userMeetsPrerequisite(userId, prerequisite)) {
            errors.push(`Must satisfy: ${prerequisite}`);
        }
    }
    
    return { isValid: errors.length === 0, errors };
}
```

### Pros
- **Intuitive model**: Sessions are natural mental units
- **Balanced complexity**: More flexible than hierarchy, simpler than pools
- **Great UX**: Users understand "days" and "sessions" naturally  
- **Prerequisite support**: Natural way to handle "Day 1 required for Day 2"
- **Reporting advantages**: Easy to generate attendance by session

### Cons
- **Session-only limitations**: Harder to model non-temporal groupings
- **Complex cross-session rules**: Challenging for intricate dependencies
- **Capacity edge cases**: May not handle all WitchCityRope scenarios optimally

---

## Recommendation: Solution 3 (Event Session Matrix)

### Primary Justification
**Best fit for WitchCityRope community needs:**
- Natural alignment with how community thinks about workshops (days/sessions)
- Handles 90% of use cases with reasonable complexity
- Room to grow without architectural changes
- Intuitive for both organizers and attendees

### Implementation Priority
1. **Phase 1**: Basic session matrix with simple ticket types
2. **Phase 2**: Add prerequisite handling and member pricing
3. **Phase 3**: Enhanced business rules and group discounts
4. **Phase 4**: Consider hybrid approach if complex edge cases emerge

### Specific WitchCityRope Applications

#### Multi-Day Workshop Series
```
Workshop: "Rope Foundations - 3 Day Intensive"
- Session 1: Friday 7-10pm (Capacity: 20)
- Session 2: Saturday 1-5pm (Capacity: 20)  
- Session 3: Sunday 10-2pm (Capacity: 18) // Smaller venue

Ticket Types:
- Full Series Pass: $150, includes all 3 sessions
- Friday Only: $60, includes session 1
- Weekend Only: $100, includes sessions 2+3, prerequisite: Friday attendance
```

#### Tiered Pricing Events
```
Event: "Advanced Suspension Techniques"
- Session 1: Saturday Workshop (Capacity: 12)

Ticket Types:
- Vetted Member: $40
- General Member: $60  
- Non-Member: $80
```

#### Group Events
```
Event: "Couples Rope Communication"
- Session 1: Sunday 2-5pm (Capacity: 16) // 8 couples

Ticket Types:
- Couple Pass: $120, reserves 2 spots
- Individual: $70, single spot
```

### Next Steps
1. **Architecture Discovery Phase**: Validate session model against all known WitchCityRope use cases
2. **API Design**: Create DTOs following NSwag auto-generation strategy  
3. **Database Schema**: Design session and ticket type tables
4. **UI Mockups**: Design organizer setup and attendee purchase flows
5. **Human Review**: Present complete solution before implementation

## Risk Mitigation

### Technical Risks
- **Performance**: Session availability calculations may be expensive - implement caching
- **Race conditions**: Multiple simultaneous purchases - use optimistic locking
- **Data consistency**: Session attendance tracking - implement audit trails

### Business Risks
- **Organizer adoption**: Training materials and templates for common scenarios
- **Attendee confusion**: Clear UI/UX with examples and help text
- **Edge cases**: Plan migration path to Solution 2 if complexity outgrows Solution 3

### Quality Gates
- [ ] All existing WitchCityRope event patterns validated against session model
- [ ] Performance testing with realistic load scenarios
- [ ] User acceptance testing with actual organizers and attendees
- [ ] Clear escalation path for unsupported scenarios

---

**Analysis Complete**: Solution 3 (Event Session Matrix) recommended as optimal balance of functionality, complexity, and community fit. Ready for human review and architecture approval.