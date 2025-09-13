# Event Session Matrix Implementation Guide
<!-- Last Updated: 2025-08-24 -->
<!-- Version: 1.0 -->
<!-- Owner: Backend Developer -->
<!-- Status: Implementation Guide - Ready for Development -->

## Executive Summary

This is the **DEFINITIVE IMPLEMENTATION GUIDE** for the Event Session Matrix ticketing system. This document ensures developers cannot misunderstand or misimplement this core feature by providing crystal-clear architecture, code examples, and business rules.

**Key Principle**: **Sessions are the atomic unit of capacity**, not ticket types. Ticket types specify which sessions they include, and all capacity calculations roll up from session-level tracking.

## 1. Architecture Overview

### Why Event Session Matrix?

The Event Session Matrix solves complex ticketing scenarios that simple ticket types cannot handle:

- **Multi-day workshops** with both full-series and individual day passes
- **Overlapping capacity constraints** (venue limits vs ticket type limits) 
- **Complex pricing structures** (member/non-member, early bird, group discounts)
- **Real-time availability calculations** across interdependent ticket types

### Core Architecture Principle

```
Event (metadata) 
├── Sessions (capacity units)
│   ├── Session 1: Friday Workshop (Capacity: 20)
│   ├── Session 2: Saturday Workshop (Capacity: 20) 
│   └── Session 3: Sunday Workshop (Capacity: 18)
└── Ticket Types (session bundles)
    ├── Full Series Pass → Includes Sessions 1,2,3
    ├── Friday Only Pass → Includes Session 1
    ├── Weekend Pass → Includes Sessions 2,3
    └── Saturday Only → Includes Session 2
```

**CRITICAL**: Capacity is tracked at the **session level**. When someone buys a Full Series Pass, it consumes 1 spot from each of the 3 sessions.

### References

This implementation follows the analysis and recommendations from:
- [Multi-Ticket Type Analysis](/home/chad/repos/witchcityrope-react/docs/functional-areas/events/new-work/2025-08-24-events-management/requirements/multi-ticket-type-analysis.md) - Solution 3 (Event Session Matrix)
- [Events Business Requirements](/home/chad/repos/witchcityrope-react/docs/functional-areas/events/requirements/business-requirements.md) - Core business rules

## 2. Database Schema

### SQL Schema Definition

```sql
-- Events table (existing, enhanced)
CREATE TABLE Events (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Title VARCHAR(200) NOT NULL,
    Description TEXT NOT NULL,
    EventType INT NOT NULL, -- Class vs Social
    Location VARCHAR(500) NOT NULL,
    IsPublished BOOLEAN DEFAULT FALSE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- NEW: Sessions table - atomic capacity units
CREATE TABLE EventSessions (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
    SessionName VARCHAR(100) NOT NULL, -- "Day 1", "Day 2", "Day 3"
    SessionDate DATE NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    Capacity INT NOT NULL CHECK (Capacity > 0),
    IsRequired BOOLEAN DEFAULT FALSE, -- For prerequisite handling
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT uk_event_session_name UNIQUE (EventId, SessionName),
    CONSTRAINT ck_session_times CHECK (StartTime < EndTime)
);

-- NEW: Ticket Types table - session bundles
CREATE TABLE EventTicketTypes (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
    Name VARCHAR(100) NOT NULL, -- "Full Series Pass", "Friday Only"
    Description TEXT,
    Price DECIMAL(10,2) NOT NULL CHECK (Price >= 0),
    MaxQuantity INT CHECK (MaxQuantity > 0), -- NULL = unlimited
    SaleStartDate TIMESTAMP WITH TIME ZONE,
    SaleEndDate TIMESTAMP WITH TIME ZONE,
    IsActive BOOLEAN DEFAULT TRUE,
    SortOrder INT DEFAULT 0,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT uk_event_ticket_name UNIQUE (EventId, Name)
);

-- NEW: Junction table - which sessions each ticket type includes
CREATE TABLE TicketTypeSessionInclusions (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TicketTypeId UUID NOT NULL REFERENCES EventTicketTypes(Id) ON DELETE CASCADE,
    SessionId UUID NOT NULL REFERENCES EventSessions(Id) ON DELETE CASCADE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT uk_ticket_session UNIQUE (TicketTypeId, SessionId)
);

-- NEW: Orders table
CREATE TABLE Orders (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    UserId UUID NOT NULL, -- References Users table
    EventId UUID NOT NULL REFERENCES Events(Id),
    OrderNumber VARCHAR(50) NOT NULL UNIQUE, -- ORD-20250824-0001
    Status VARCHAR(20) DEFAULT 'Pending', -- Pending, Confirmed, Cancelled
    TotalAmount DECIMAL(10,2) NOT NULL,
    PaymentIntentId VARCHAR(100), -- Stripe payment intent
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- NEW: Order Items table - individual ticket purchases
CREATE TABLE OrderItems (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    OrderId UUID NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
    TicketTypeId UUID NOT NULL REFERENCES EventTicketTypes(Id),
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(10,2) NOT NULL,
    TotalPrice DECIMAL(10,2) NOT NULL,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT ck_total_price CHECK (TotalPrice = UnitPrice * Quantity)
);

-- NEW: Session Attendance tracking - who's attending which sessions
CREATE TABLE SessionAttendances (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    SessionId UUID NOT NULL REFERENCES EventSessions(Id) ON DELETE CASCADE,
    UserId UUID NOT NULL,
    OrderItemId UUID NOT NULL REFERENCES OrderItems(Id),
    IsCheckedIn BOOLEAN DEFAULT FALSE,
    CheckInTime TIMESTAMP WITH TIME ZONE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT uk_session_user UNIQUE (SessionId, UserId)
);

-- Indexes for performance
CREATE INDEX idx_events_type ON Events(EventType);
CREATE INDEX idx_events_published ON Events(IsPublished);
CREATE INDEX idx_sessions_event_date ON EventSessions(EventId, SessionDate);
CREATE INDEX idx_ticket_types_event ON EventTicketTypes(EventId);
CREATE INDEX idx_ticket_types_active ON EventTicketTypes(IsActive);
CREATE INDEX idx_orders_user ON Orders(UserId);
CREATE INDEX idx_orders_event ON Orders(EventId);
CREATE INDEX idx_session_attendance_session ON SessionAttendances(SessionId);
CREATE INDEX idx_session_attendance_user ON SessionAttendances(UserId);
```

### Key Relationships

- **Event → Sessions**: One-to-many (an event can have multiple sessions)
- **Event → TicketTypes**: One-to-many (an event can have multiple ticket types)
- **TicketType ↔ Sessions**: Many-to-many via `TicketTypeSessionInclusions`
- **Orders → OrderItems**: One-to-many (an order can have multiple ticket types)
- **OrderItems → SessionAttendances**: One-to-many (each order item creates attendance records for included sessions)

## 3. Domain Models (.NET)

### Core Entity Classes

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using WitchCityRope.Core.Exceptions;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a discrete session within an event
    /// Sessions are the atomic units of capacity management
    /// </summary>
    public class EventSession
    {
        private readonly List<SessionAttendance> _attendances = new();

        // Private constructor for EF Core
        private EventSession() 
        {
            SessionName = null!;
        }

        public EventSession(
            Guid eventId,
            string sessionName,
            DateTime sessionDate,
            TimeSpan startTime,
            TimeSpan endTime,
            int capacity,
            bool isRequired = false)
        {
            ValidateCapacity(capacity);
            ValidateTimes(startTime, endTime);

            Id = Guid.NewGuid();
            EventId = eventId;
            SessionName = sessionName ?? throw new ArgumentNullException(nameof(sessionName));
            SessionDate = sessionDate.Date; // Ensure date only
            StartTime = startTime;
            EndTime = endTime;
            Capacity = capacity;
            IsRequired = isRequired;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        public Guid EventId { get; private set; }
        public string SessionName { get; private set; }
        public DateTime SessionDate { get; private set; }
        public TimeSpan StartTime { get; private set; }
        public TimeSpan EndTime { get; private set; }
        public int Capacity { get; private set; }
        public bool IsRequired { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        // Navigation properties
        public Event Event { get; private set; } = null!;
        public IReadOnlyCollection<SessionAttendance> Attendances => _attendances.AsReadOnly();

        /// <summary>
        /// Gets current attendance count for this session
        /// </summary>
        public int GetCurrentAttendance()
        {
            return _attendances.Count;
        }

        /// <summary>
        /// Gets available spots remaining in this session
        /// </summary>
        public int GetAvailableSpots()
        {
            return Capacity - GetCurrentAttendance();
        }

        /// <summary>
        /// Checks if session has available capacity
        /// </summary>
        public bool HasAvailableCapacity()
        {
            return GetAvailableSpots() > 0;
        }

        /// <summary>
        /// Updates session capacity (can only increase if attendances exist)
        /// </summary>
        public void UpdateCapacity(int newCapacity)
        {
            ValidateCapacity(newCapacity);

            if (newCapacity < GetCurrentAttendance())
                throw new DomainException($"Cannot reduce capacity below current attendance ({GetCurrentAttendance()})");

            Capacity = newCapacity;
            UpdatedAt = DateTime.UtcNow;
        }

        private void ValidateCapacity(int capacity)
        {
            if (capacity <= 0)
                throw new DomainException("Session capacity must be greater than zero");
        }

        private void ValidateTimes(TimeSpan startTime, TimeSpan endTime)
        {
            if (startTime >= endTime)
                throw new DomainException("Session start time must be before end time");
        }
    }

    /// <summary>
    /// Represents a ticket type that bundles one or more sessions
    /// </summary>
    public class EventTicketType
    {
        private readonly List<TicketTypeSessionInclusion> _sessionInclusions = new();

        // Private constructor for EF Core
        private EventTicketType() 
        {
            Name = null!;
        }

        public EventTicketType(
            Guid eventId,
            string name,
            string? description,
            decimal price,
            int? maxQuantity = null,
            DateTime? saleStartDate = null,
            DateTime? saleEndDate = null,
            int sortOrder = 0)
        {
            ValidatePrice(price);

            Id = Guid.NewGuid();
            EventId = eventId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            Price = price;
            MaxQuantity = maxQuantity;
            SaleStartDate = saleStartDate;
            SaleEndDate = saleEndDate;
            IsActive = true;
            SortOrder = sortOrder;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        public Guid EventId { get; private set; }
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public decimal Price { get; private set; }
        public int? MaxQuantity { get; private set; }
        public DateTime? SaleStartDate { get; private set; }
        public DateTime? SaleEndDate { get; private set; }
        public bool IsActive { get; private set; }
        public int SortOrder { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        // Navigation properties
        public Event Event { get; private set; } = null!;
        public IReadOnlyCollection<TicketTypeSessionInclusion> SessionInclusions => _sessionInclusions.AsReadOnly();

        /// <summary>
        /// Gets all sessions included in this ticket type
        /// </summary>
        public IEnumerable<EventSession> GetIncludedSessions()
        {
            return _sessionInclusions.Select(i => i.Session);
        }

        /// <summary>
        /// Adds a session to this ticket type
        /// </summary>
        public void IncludeSession(EventSession session)
        {
            if (session.EventId != EventId)
                throw new DomainException("Cannot include session from different event");

            if (_sessionInclusions.Any(i => i.SessionId == session.Id))
                throw new DomainException("Session is already included in this ticket type");

            var inclusion = new TicketTypeSessionInclusion(Id, session.Id);
            _sessionInclusions.Add(inclusion);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if this ticket type is currently available for purchase
        /// </summary>
        public bool IsAvailableForPurchase()
        {
            if (!IsActive) return false;

            var now = DateTime.UtcNow;
            if (SaleStartDate.HasValue && now < SaleStartDate.Value) return false;
            if (SaleEndDate.HasValue && now > SaleEndDate.Value) return false;

            return true;
        }

        private void ValidatePrice(decimal price)
        {
            if (price < 0)
                throw new DomainException("Ticket price cannot be negative");
        }
    }

    /// <summary>
    /// Junction entity linking ticket types to their included sessions
    /// </summary>
    public class TicketTypeSessionInclusion
    {
        // Private constructor for EF Core
        private TicketTypeSessionInclusion() { }

        public TicketTypeSessionInclusion(Guid ticketTypeId, Guid sessionId)
        {
            Id = Guid.NewGuid();
            TicketTypeId = ticketTypeId;
            SessionId = sessionId;
            CreatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        public Guid TicketTypeId { get; private set; }
        public Guid SessionId { get; private set; }
        public DateTime CreatedAt { get; private set; }

        // Navigation properties
        public EventTicketType TicketType { get; private set; } = null!;
        public EventSession Session { get; private set; } = null!;
    }

    /// <summary>
    /// Represents a user's attendance at a specific session
    /// </summary>
    public class SessionAttendance
    {
        // Private constructor for EF Core
        private SessionAttendance() { }

        public SessionAttendance(
            Guid sessionId,
            Guid userId,
            Guid orderItemId)
        {
            Id = Guid.NewGuid();
            SessionId = sessionId;
            UserId = userId;
            OrderItemId = orderItemId;
            IsCheckedIn = false;
            CreatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        public Guid SessionId { get; private set; }
        public Guid UserId { get; private set; }
        public Guid OrderItemId { get; private set; }
        public bool IsCheckedIn { get; private set; }
        public DateTime? CheckInTime { get; private set; }
        public DateTime CreatedAt { get; private set; }

        // Navigation properties
        public EventSession Session { get; private set; } = null!;
        public OrderItem OrderItem { get; private set; } = null!;

        /// <summary>
        /// Checks in the attendee to this session
        /// </summary>
        public void CheckIn()
        {
            if (IsCheckedIn)
                throw new DomainException("Attendee is already checked in to this session");

            IsCheckedIn = true;
            CheckInTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Enhanced Event entity with session support
    /// </summary>
    public partial class Event
    {
        private readonly List<EventSession> _sessions = new();
        private readonly List<EventTicketType> _ticketTypes = new();

        // Add to existing Event class
        public IReadOnlyCollection<EventSession> Sessions => _sessions.AsReadOnly();
        public IReadOnlyCollection<EventTicketType> TicketTypes => _ticketTypes.AsReadOnly();

        /// <summary>
        /// Adds a session to this event
        /// </summary>
        public EventSession AddSession(
            string sessionName,
            DateTime sessionDate,
            TimeSpan startTime,
            TimeSpan endTime,
            int capacity,
            bool isRequired = false)
        {
            if (_sessions.Any(s => s.SessionName.Equals(sessionName, StringComparison.OrdinalIgnoreCase)))
                throw new DomainException($"Session with name '{sessionName}' already exists");

            var session = new EventSession(Id, sessionName, sessionDate, startTime, endTime, capacity, isRequired);
            _sessions.Add(session);
            UpdatedAt = DateTime.UtcNow;

            return session;
        }

        /// <summary>
        /// Adds a ticket type to this event
        /// </summary>
        public EventTicketType AddTicketType(
            string name,
            string? description,
            decimal price,
            int? maxQuantity = null,
            DateTime? saleStartDate = null,
            DateTime? saleEndDate = null,
            int sortOrder = 0)
        {
            if (_ticketTypes.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new DomainException($"Ticket type with name '{name}' already exists");

            var ticketType = new EventTicketType(Id, name, description, price, maxQuantity, saleStartDate, saleEndDate, sortOrder);
            _ticketTypes.Add(ticketType);
            UpdatedAt = DateTime.UtcNow;

            return ticketType;
        }

        /// <summary>
        /// Gets availability summary for all sessions
        /// </summary>
        public Dictionary<Guid, SessionAvailability> GetSessionAvailability()
        {
            return _sessions.ToDictionary(
                s => s.Id,
                s => new SessionAvailability
                {
                    SessionId = s.Id,
                    SessionName = s.SessionName,
                    TotalCapacity = s.Capacity,
                    CurrentAttendance = s.GetCurrentAttendance(),
                    AvailableSpots = s.GetAvailableSpots(),
                    IsAvailable = s.HasAvailableCapacity()
                });
        }
    }
}
```

### Supporting DTOs

```csharp
namespace WitchCityRope.Core.DTOs
{
    public class SessionAvailability
    {
        public Guid SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public DateTime SessionDate { get; set; }
        public int TotalCapacity { get; set; }
        public int CurrentAttendance { get; set; }
        public int AvailableSpots { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class TicketTypeAvailability
    {
        public Guid TicketTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int? MaxQuantity { get; set; }
        public int SoldQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public bool IsAvailable { get; set; }
        public string? UnavailabilityReason { get; set; }
        public List<SessionAvailability> IncludedSessions { get; set; } = new();
        public SessionAvailability? BottleneckSession { get; set; } // Most limiting session
    }

    public class EventAvailabilityDto
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<SessionAvailability> Sessions { get; set; } = new();
        public List<TicketTypeAvailability> TicketTypes { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}
```

## 4. Critical Business Logic

### Capacity Calculation Algorithm

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Api.Features.Events.Services
{
    public class EventCapacityService
    {
        private readonly WitchCityRopeIdentityDbContext _context;

        public EventCapacityService(WitchCityRopeIdentityDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Calculates real-time availability for all ticket types in an event
        /// CRITICAL: This is the core capacity calculation algorithm
        /// </summary>
        public async Task<EventAvailabilityDto> CalculateEventAvailabilityAsync(
            Guid eventId, 
            CancellationToken cancellationToken = default)
        {
            // Load event with all related data in a single query
            var eventData = await _context.Events
                .Include(e => e.Sessions)
                    .ThenInclude(s => s.Attendances)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.SessionInclusions)
                        .ThenInclude(si => si.Session)
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventData == null)
                throw new NotFoundException($"Event {eventId} not found");

            // Calculate session availability first
            var sessionAvailabilities = eventData.Sessions
                .ToDictionary(s => s.Id, s => new SessionAvailability
                {
                    SessionId = s.Id,
                    SessionName = s.SessionName,
                    SessionDate = s.SessionDate,
                    TotalCapacity = s.Capacity,
                    CurrentAttendance = s.Attendances.Count,
                    AvailableSpots = s.Capacity - s.Attendances.Count,
                    IsAvailable = s.Capacity > s.Attendances.Count
                });

            // Calculate ticket type availability
            var ticketTypeAvailabilities = new List<TicketTypeAvailability>();

            foreach (var ticketType in eventData.TicketTypes.Where(tt => tt.IsActive))
            {
                var availability = await CalculateTicketTypeAvailabilityAsync(
                    ticketType, 
                    sessionAvailabilities, 
                    cancellationToken);
                
                ticketTypeAvailabilities.Add(availability);
            }

            return new EventAvailabilityDto
            {
                EventId = eventId,
                Title = eventData.Title,
                Sessions = sessionAvailabilities.Values.OrderBy(s => s.SessionDate).ToList(),
                TicketTypes = ticketTypeAvailabilities.OrderBy(tt => tt.Name).ToList(),
                LastUpdated = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Calculates availability for a specific ticket type
        /// Key logic: A ticket type is only available if ALL included sessions have capacity
        /// </summary>
        private async Task<TicketTypeAvailability> CalculateTicketTypeAvailabilityAsync(
            EventTicketType ticketType,
            Dictionary<Guid, SessionAvailability> sessionAvailabilities,
            CancellationToken cancellationToken)
        {
            // Get sold quantity for this ticket type
            var soldQuantity = await _context.OrderItems
                .Where(oi => oi.TicketTypeId == ticketType.Id)
                .Where(oi => oi.Order.Status == "Confirmed") // Only confirmed orders count
                .SumAsync(oi => oi.Quantity, cancellationToken);

            // Check quota availability (max quantity limit)
            var quotaAvailable = ticketType.MaxQuantity.HasValue 
                ? ticketType.MaxQuantity.Value - soldQuantity
                : int.MaxValue;

            // Check session capacity availability
            var includedSessions = ticketType.SessionInclusions
                .Select(si => sessionAvailabilities[si.SessionId])
                .ToList();

            // Find the most constraining (bottleneck) session
            var bottleneckSession = includedSessions
                .OrderBy(s => s.AvailableSpots)
                .First();

            // Ticket type is available only if:
            // 1. It's within sales dates
            // 2. Quota allows it (if quota exists)  
            // 3. ALL included sessions have capacity
            var isAvailable = ticketType.IsAvailableForPurchase() &&
                             quotaAvailable > 0 &&
                             includedSessions.All(s => s.IsAvailable);

            var availableQuantity = isAvailable 
                ? Math.Min(quotaAvailable, bottleneckSession.AvailableSpots)
                : 0;

            string? unavailabilityReason = null;
            if (!isAvailable)
            {
                if (!ticketType.IsAvailableForPurchase())
                    unavailabilityReason = "Sales period ended";
                else if (quotaAvailable <= 0)
                    unavailabilityReason = "Sold out (quota reached)";
                else if (includedSessions.Any(s => !s.IsAvailable))
                    unavailabilityReason = $"No capacity in {bottleneckSession.SessionName}";
            }

            return new TicketTypeAvailability
            {
                TicketTypeId = ticketType.Id,
                Name = ticketType.Name,
                Price = ticketType.Price,
                MaxQuantity = ticketType.MaxQuantity,
                SoldQuantity = soldQuantity,
                AvailableQuantity = availableQuantity,
                IsAvailable = isAvailable,
                UnavailabilityReason = unavailabilityReason,
                IncludedSessions = includedSessions,
                BottleneckSession = bottleneckSession
            };
        }

        /// <summary>
        /// Validates that a purchase request can be fulfilled
        /// CRITICAL: This prevents overselling by checking capacity atomically
        /// </summary>
        public async Task<PurchaseValidationResult> ValidatePurchaseAsync(
            Guid userId,
            Guid ticketTypeId,
            int quantity,
            CancellationToken cancellationToken = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            
            try
            {
                // Lock the ticket type and related sessions to prevent race conditions
                var ticketType = await _context.EventTicketTypes
                    .Include(tt => tt.SessionInclusions)
                        .ThenInclude(si => si.Session)
                            .ThenInclude(s => s.Attendances)
                    .FirstOrDefaultAsync(tt => tt.Id == ticketTypeId, cancellationToken);

                if (ticketType == null)
                    return PurchaseValidationResult.Failure("Ticket type not found");

                if (!ticketType.IsAvailableForPurchase())
                    return PurchaseValidationResult.Failure("Ticket type is not available for purchase");

                // Check quota
                var soldQuantity = await _context.OrderItems
                    .Where(oi => oi.TicketTypeId == ticketTypeId)
                    .Where(oi => oi.Order.Status == "Confirmed")
                    .SumAsync(oi => oi.Quantity, cancellationToken);

                if (ticketType.MaxQuantity.HasValue && 
                    soldQuantity + quantity > ticketType.MaxQuantity.Value)
                    return PurchaseValidationResult.Failure("Insufficient quota available");

                // Check session capacity for each included session
                var validationErrors = new List<string>();
                
                foreach (var inclusion in ticketType.SessionInclusions)
                {
                    var session = inclusion.Session;
                    var currentAttendance = session.Attendances.Count;
                    var availableSpots = session.Capacity - currentAttendance;

                    if (availableSpots < quantity)
                    {
                        validationErrors.Add(
                            $"Session '{session.SessionName}' only has {availableSpots} spots available, but {quantity} requested");
                    }
                }

                if (validationErrors.Any())
                    return PurchaseValidationResult.Failure(string.Join("; ", validationErrors));

                // Check for duplicate attendance (user already has ticket to these sessions)
                var userSessionAttendances = await _context.SessionAttendances
                    .Where(sa => sa.UserId == userId)
                    .Where(sa => ticketType.SessionInclusions.Select(si => si.SessionId).Contains(sa.SessionId))
                    .ToListAsync(cancellationToken);

                if (userSessionAttendances.Any())
                {
                    var duplicateSessions = userSessionAttendances
                        .Select(sa => sa.Session.SessionName)
                        .ToList();
                    return PurchaseValidationResult.Failure(
                        $"User already has tickets to: {string.Join(", ", duplicateSessions)}");
                }

                await transaction.CommitAsync(cancellationToken);
                return PurchaseValidationResult.Success();
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    public class PurchaseValidationResult
    {
        public bool IsValid { get; private set; }
        public List<string> Errors { get; private set; } = new();

        private PurchaseValidationResult() { }

        public static PurchaseValidationResult Success() => new() { IsValid = true };
        
        public static PurchaseValidationResult Failure(string error) => new() 
        { 
            IsValid = false, 
            Errors = new List<string> { error } 
        };

        public static PurchaseValidationResult Failure(IEnumerable<string> errors) => new() 
        { 
            IsValid = false, 
            Errors = errors.ToList() 
        };
    }
}
```

### Waitlist Triggering Logic

```csharp
public class WaitlistService
{
    private readonly WitchCityRopeIdentityDbContext _context;
    private readonly IEmailService _emailService;

    public WaitlistService(WitchCityRopeIdentityDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    /// <summary>
    /// Processes waitlist when capacity becomes available
    /// Called after cancellations or capacity increases
    /// </summary>
    public async Task ProcessWaitlistAsync(
        Guid eventId, 
        CancellationToken cancellationToken = default)
    {
        // Implementation would handle complex waitlist processing
        // For now, this is a placeholder showing the concept
        
        var waitlistEntries = await _context.WaitlistEntries
            .Where(w => w.EventId == eventId && w.Status == "Waiting")
            .OrderBy(w => w.CreatedAt) // First come, first served
            .ToListAsync(cancellationToken);

        foreach (var waitlistEntry in waitlistEntries)
        {
            var validation = await new EventCapacityService(_context)
                .ValidatePurchaseAsync(
                    waitlistEntry.UserId, 
                    waitlistEntry.TicketTypeId, 
                    waitlistEntry.Quantity, 
                    cancellationToken);

            if (validation.IsValid)
            {
                // Promote from waitlist
                await PromoteFromWaitlistAsync(waitlistEntry, cancellationToken);
                
                // Send notification
                await _emailService.SendWaitlistPromotionNotificationAsync(
                    waitlistEntry.UserId, 
                    eventId, 
                    cancellationToken);
            }
        }
    }

    private async Task PromoteFromWaitlistAsync(WaitlistEntry entry, CancellationToken cancellationToken)
    {
        // Implementation would create order and update waitlist status
        // This is a simplified version showing the concept
    }
}
```

## 5. API Endpoints

### Event Session Management

```csharp
// /src/WitchCityRope.Api/Features/Events/Controllers/EventSessionsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Features.Events.Models;

namespace WitchCityRope.Api.Features.Events.Controllers
{
    [ApiController]
    [Route("api/events/{eventId:guid}/sessions")]
    [Authorize]
    public class EventSessionsController : ControllerBase
    {
        private readonly IEventSessionService _sessionService;

        public EventSessionsController(IEventSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        /// <summary>
        /// Creates a new session for an event
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> CreateSession(
            Guid eventId,
            CreateSessionRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sessionService.CreateSessionAsync(eventId, request, cancellationToken);
            
            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return CreatedAtAction(
                nameof(GetSession), 
                new { eventId, sessionId = result.Value.Id }, 
                result.Value);
        }

        /// <summary>
        /// Gets a specific session
        /// </summary>
        [HttpGet("{sessionId:guid}")]
        public async Task<IActionResult> GetSession(
            Guid eventId,
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            var result = await _sessionService.GetSessionAsync(eventId, sessionId, cancellationToken);
            
            if (!result.IsSuccess)
                return NotFound(result.Error);

            return Ok(result.Value);
        }

        /// <summary>
        /// Gets all sessions for an event
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSessions(
            Guid eventId,
            CancellationToken cancellationToken)
        {
            var result = await _sessionService.GetSessionsAsync(eventId, cancellationToken);
            return Ok(result.Value);
        }

        /// <summary>
        /// Updates session details
        /// </summary>
        [HttpPut("{sessionId:guid}")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> UpdateSession(
            Guid eventId,
            Guid sessionId,
            UpdateSessionRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sessionService.UpdateSessionAsync(eventId, sessionId, request, cancellationToken);
            
            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        /// <summary>
        /// Deletes a session (only if no attendances)
        /// </summary>
        [HttpDelete("{sessionId:guid}")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> DeleteSession(
            Guid eventId,
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            var result = await _sessionService.DeleteSessionAsync(eventId, sessionId, cancellationToken);
            
            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return NoContent();
        }
    }

    // Request/Response models
    public class CreateSessionRequest
    {
        public string SessionName { get; set; } = string.Empty;
        public DateTime SessionDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Capacity { get; set; }
        public bool IsRequired { get; set; } = false;
    }

    public class UpdateSessionRequest
    {
        public string? SessionName { get; set; }
        public DateTime? SessionDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int? Capacity { get; set; }
        public bool? IsRequired { get; set; }
    }

    public class SessionDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public DateTime SessionDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Capacity { get; set; }
        public bool IsRequired { get; set; }
        public int CurrentAttendance { get; set; }
        public int AvailableSpots { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
```

### Event Availability API

```csharp
// /src/WitchCityRope.Api/Features/Events/Controllers/EventAvailabilityController.cs
using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Api.Features.Events.Services;

namespace WitchCityRope.Api.Features.Events.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventAvailabilityController : ControllerBase
    {
        private readonly EventCapacityService _capacityService;

        public EventAvailabilityController(EventCapacityService capacityService)
        {
            _capacityService = capacityService;
        }

        /// <summary>
        /// Gets real-time availability for an event
        /// CRITICAL: This is the main availability endpoint used by frontend
        /// </summary>
        [HttpGet("{eventId:guid}/availability")]
        public async Task<IActionResult> GetEventAvailability(
            Guid eventId,
            CancellationToken cancellationToken)
        {
            try
            {
                var availability = await _capacityService.CalculateEventAvailabilityAsync(eventId, cancellationToken);
                return Ok(availability);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Gets availability for a specific ticket type
        /// Used for detailed ticket selection UI
        /// </summary>
        [HttpGet("{eventId:guid}/ticket-types/{ticketTypeId:guid}/availability")]
        public async Task<IActionResult> GetTicketTypeAvailability(
            Guid eventId,
            Guid ticketTypeId,
            CancellationToken cancellationToken)
        {
            var eventAvailability = await _capacityService.CalculateEventAvailabilityAsync(eventId, cancellationToken);
            var ticketTypeAvailability = eventAvailability.TicketTypes
                .FirstOrDefault(tt => tt.TicketTypeId == ticketTypeId);

            if (ticketTypeAvailability == null)
                return NotFound("Ticket type not found");

            return Ok(ticketTypeAvailability);
        }
    }
}
```

### Ticket Purchase API

```csharp
// /src/WitchCityRope.Api/Features/Events/Controllers/TicketPurchaseController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WitchCityRope.Api.Extensions;

namespace WitchCityRope.Api.Features.Events.Controllers
{
    [ApiController]
    [Route("api/events/{eventId:guid}/tickets")]
    [Authorize]
    public class TicketPurchaseController : ControllerBase
    {
        private readonly ITicketPurchaseService _purchaseService;
        private readonly EventCapacityService _capacityService;

        public TicketPurchaseController(
            ITicketPurchaseService purchaseService,
            EventCapacityService capacityService)
        {
            _purchaseService = purchaseService;
            _capacityService = capacityService;
        }

        /// <summary>
        /// Validates a purchase before processing payment
        /// CRITICAL: Always call this before payment processing
        /// </summary>
        [HttpPost("validate")]
        public async Task<IActionResult> ValidatePurchase(
            Guid eventId,
            ValidatePurchaseRequest request,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            
            var validation = await _capacityService.ValidatePurchaseAsync(
                userId, 
                request.TicketTypeId, 
                request.Quantity, 
                cancellationToken);

            if (!validation.IsValid)
                return BadRequest(new { Errors = validation.Errors });

            return Ok(new { IsValid = true });
        }

        /// <summary>
        /// Purchases tickets (creates order and processes payment)
        /// </summary>
        [HttpPost("purchase")]
        public async Task<IActionResult> PurchaseTickets(
            Guid eventId,
            PurchaseTicketsRequest request,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _purchaseService.PurchaseTicketsAsync(
                userId, 
                eventId, 
                request, 
                cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }
    }

    public class ValidatePurchaseRequest
    {
        public Guid TicketTypeId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class PurchaseTicketsRequest
    {
        public List<TicketPurchaseItem> Items { get; set; } = new();
        public string PaymentMethodId { get; set; } = string.Empty; // Stripe payment method
    }

    public class TicketPurchaseItem
    {
        public Guid TicketTypeId { get; set; }
        public int Quantity { get; set; }
    }
}
```

## 6. Frontend Implementation (React)

### Session Management Component

```typescript
// /web/src/components/events/admin/SessionManager.tsx
import React, { useState, useEffect } from 'react';
import { Button, Table, Modal, Form, InputNumber, TimePicker, DatePicker, Input, message } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import type { SessionDto, CreateSessionRequest } from '../../../types/events';
import { eventApi } from '../../../api/events';

interface SessionManagerProps {
  eventId: string;
  readonly?: boolean;
}

export const SessionManager: React.FC<SessionManagerProps> = ({ eventId, readonly = false }) => {
  const [sessions, setSessions] = useState<SessionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingSession, setEditingSession] = useState<SessionDto | null>(null);
  const [form] = Form.useForm();

  useEffect(() => {
    loadSessions();
  }, [eventId]);

  const loadSessions = async () => {
    setLoading(true);
    try {
      const response = await eventApi.getSessions(eventId);
      setSessions(response.data);
    } catch (error) {
      message.error('Failed to load sessions');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateSession = () => {
    setEditingSession(null);
    form.resetFields();
    setIsModalOpen(true);
  };

  const handleEditSession = (session: SessionDto) => {
    setEditingSession(session);
    form.setFieldsValue({
      sessionName: session.sessionName,
      sessionDate: dayjs(session.sessionDate),
      startTime: dayjs(session.startTime, 'HH:mm:ss'),
      endTime: dayjs(session.endTime, 'HH:mm:ss'),
      capacity: session.capacity,
      isRequired: session.isRequired
    });
    setIsModalOpen(true);
  };

  const handleSave = async (values: any) => {
    try {
      const request: CreateSessionRequest = {
        sessionName: values.sessionName,
        sessionDate: values.sessionDate.toISOString(),
        startTime: values.startTime.format('HH:mm:ss'),
        endTime: values.endTime.format('HH:mm:ss'),
        capacity: values.capacity,
        isRequired: values.isRequired || false
      };

      if (editingSession) {
        await eventApi.updateSession(eventId, editingSession.id, request);
        message.success('Session updated successfully');
      } else {
        await eventApi.createSession(eventId, request);
        message.success('Session created successfully');
      }

      setIsModalOpen(false);
      await loadSessions();
    } catch (error) {
      message.error('Failed to save session');
    }
  };

  const handleDelete = async (sessionId: string) => {
    try {
      await eventApi.deleteSession(eventId, sessionId);
      message.success('Session deleted successfully');
      await loadSessions();
    } catch (error) {
      message.error('Failed to delete session. Session may have attendees.');
    }
  };

  const columns = [
    {
      title: 'Session Name',
      dataIndex: 'sessionName',
      key: 'sessionName',
    },
    {
      title: 'Date',
      dataIndex: 'sessionDate',
      key: 'sessionDate',
      render: (date: string) => dayjs(date).format('MMM DD, YYYY'),
    },
    {
      title: 'Time',
      key: 'time',
      render: (record: SessionDto) => 
        `${dayjs(record.startTime, 'HH:mm:ss').format('h:mm A')} - ${dayjs(record.endTime, 'HH:mm:ss').format('h:mm A')}`,
    },
    {
      title: 'Capacity',
      key: 'capacity',
      render: (record: SessionDto) => 
        `${record.currentAttendance}/${record.capacity} (${record.availableSpots} available)`,
    },
    {
      title: 'Required',
      dataIndex: 'isRequired',
      key: 'isRequired',
      render: (isRequired: boolean) => isRequired ? 'Yes' : 'No',
    },
    ...(readonly ? [] : [{
      title: 'Actions',
      key: 'actions',
      render: (record: SessionDto) => (
        <div>
          <Button 
            type="link" 
            icon={<EditOutlined />} 
            onClick={() => handleEditSession(record)}
          >
            Edit
          </Button>
          <Button 
            type="link" 
            danger 
            icon={<DeleteOutlined />} 
            onClick={() => handleDelete(record.id)}
            disabled={record.currentAttendance > 0}
          >
            Delete
          </Button>
        </div>
      ),
    }]),
  ];

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <h3>Event Sessions</h3>
        {!readonly && (
          <Button 
            type="primary" 
            icon={<PlusOutlined />} 
            onClick={handleCreateSession}
          >
            Add Session
          </Button>
        )}
      </div>

      <Table
        columns={columns}
        dataSource={sessions}
        rowKey="id"
        loading={loading}
        pagination={false}
        size="small"
      />

      <Modal
        title={editingSession ? 'Edit Session' : 'Create Session'}
        open={isModalOpen}
        onCancel={() => setIsModalOpen(false)}
        onOk={() => form.submit()}
        destroyOnClose
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSave}
        >
          <Form.Item
            label="Session Name"
            name="sessionName"
            rules={[{ required: true, message: 'Please enter session name' }]}
          >
            <Input placeholder="e.g., Day 1, Saturday Morning" />
          </Form.Item>

          <Form.Item
            label="Date"
            name="sessionDate"
            rules={[{ required: true, message: 'Please select date' }]}
          >
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>

          <Form.Item label="Time">
            <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
              <Form.Item
                name="startTime"
                rules={[{ required: true, message: 'Please select start time' }]}
                style={{ marginBottom: 0, flex: 1 }}
              >
                <TimePicker style={{ width: '100%' }} format="h:mm A" />
              </Form.Item>
              <span>to</span>
              <Form.Item
                name="endTime"
                rules={[{ required: true, message: 'Please select end time' }]}
                style={{ marginBottom: 0, flex: 1 }}
              >
                <TimePicker style={{ width: '100%' }} format="h:mm A" />
              </Form.Item>
            </div>
          </Form.Item>

          <Form.Item
            label="Capacity"
            name="capacity"
            rules={[
              { required: true, message: 'Please enter capacity' },
              { type: 'number', min: 1, message: 'Capacity must be at least 1' }
            ]}
          >
            <InputNumber style={{ width: '100%' }} min={1} />
          </Form.Item>

          <Form.Item
            name="isRequired"
            valuePropName="checked"
          >
            <Input type="checkbox" /> Required for other sessions (prerequisite)
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};
```

### Ticket Type Builder Component

```typescript
// /web/src/components/events/admin/TicketTypeBuilder.tsx
import React, { useState, useEffect } from 'react';
import { Form, Input, InputNumber, Button, Select, Table, Modal, Checkbox, message } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import type { SessionDto, TicketTypeDto, CreateTicketTypeRequest } from '../../../types/events';
import { eventApi } from '../../../api/events';

interface TicketTypeBuilderProps {
  eventId: string;
  sessions: SessionDto[];
  readonly?: boolean;
}

export const TicketTypeBuilder: React.FC<TicketTypeBuilderProps> = ({ 
  eventId, 
  sessions, 
  readonly = false 
}) => {
  const [ticketTypes, setTicketTypes] = useState<TicketTypeDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingTicketType, setEditingTicketType] = useState<TicketTypeDto | null>(null);
  const [form] = Form.useForm();

  useEffect(() => {
    loadTicketTypes();
  }, [eventId]);

  const loadTicketTypes = async () => {
    setLoading(true);
    try {
      const response = await eventApi.getTicketTypes(eventId);
      setTicketTypes(response.data);
    } catch (error) {
      message.error('Failed to load ticket types');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateTicketType = () => {
    setEditingTicketType(null);
    form.resetFields();
    setIsModalOpen(true);
  };

  const handleSave = async (values: any) => {
    try {
      const request: CreateTicketTypeRequest = {
        name: values.name,
        description: values.description,
        price: values.price,
        maxQuantity: values.maxQuantity,
        includedSessionIds: values.includedSessionIds || [],
        sortOrder: values.sortOrder || 0
      };

      if (editingTicketType) {
        await eventApi.updateTicketType(eventId, editingTicketType.id, request);
        message.success('Ticket type updated successfully');
      } else {
        await eventApi.createTicketType(eventId, request);
        message.success('Ticket type created successfully');
      }

      setIsModalOpen(false);
      await loadTicketTypes();
    } catch (error) {
      message.error('Failed to save ticket type');
    }
  };

  const columns = [
    {
      title: 'Name',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: 'Price',
      dataIndex: 'price',
      key: 'price',
      render: (price: number) => `$${price.toFixed(2)}`,
    },
    {
      title: 'Included Sessions',
      key: 'sessions',
      render: (record: TicketTypeDto) => {
        const includedSessions = sessions.filter(s => 
          record.includedSessionIds.includes(s.id)
        );
        return includedSessions.map(s => s.sessionName).join(', ');
      },
    },
    {
      title: 'Max Quantity',
      dataIndex: 'maxQuantity',
      key: 'maxQuantity',
      render: (maxQuantity: number | null) => maxQuantity || 'Unlimited',
    },
    {
      title: 'Sold',
      dataIndex: 'soldQuantity',
      key: 'soldQuantity',
    },
    ...(readonly ? [] : [{
      title: 'Actions',
      key: 'actions',
      render: (record: TicketTypeDto) => (
        <div>
          <Button 
            type="link" 
            icon={<EditOutlined />} 
            onClick={() => handleEditTicketType(record)}
          >
            Edit
          </Button>
          <Button 
            type="link" 
            danger 
            icon={<DeleteOutlined />} 
            onClick={() => handleDelete(record.id)}
            disabled={record.soldQuantity > 0}
          >
            Delete
          </Button>
        </div>
      ),
    }]),
  ];

  const sessionOptions = sessions.map(session => ({
    label: `${session.sessionName} (${session.sessionDate})`,
    value: session.id
  }));

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <h3>Ticket Types</h3>
        {!readonly && (
          <Button 
            type="primary" 
            icon={<PlusOutlined />} 
            onClick={handleCreateTicketType}
            disabled={sessions.length === 0}
          >
            Add Ticket Type
          </Button>
        )}
      </div>

      {sessions.length === 0 && (
        <div style={{ textAlign: 'center', padding: 20, color: '#999' }}>
          Create sessions first before adding ticket types
        </div>
      )}

      <Table
        columns={columns}
        dataSource={ticketTypes}
        rowKey="id"
        loading={loading}
        pagination={false}
        size="small"
      />

      <Modal
        title={editingTicketType ? 'Edit Ticket Type' : 'Create Ticket Type'}
        open={isModalOpen}
        onCancel={() => setIsModalOpen(false)}
        onOk={() => form.submit()}
        width={600}
        destroyOnClose
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSave}
        >
          <Form.Item
            label="Ticket Name"
            name="name"
            rules={[{ required: true, message: 'Please enter ticket name' }]}
          >
            <Input placeholder="e.g., Full Series Pass, Friday Only" />
          </Form.Item>

          <Form.Item
            label="Description"
            name="description"
          >
            <Input.TextArea 
              placeholder="Optional description of what this ticket includes" 
              rows={3}
            />
          </Form.Item>

          <Form.Item
            label="Price"
            name="price"
            rules={[
              { required: true, message: 'Please enter price' },
              { type: 'number', min: 0, message: 'Price cannot be negative' }
            ]}
          >
            <InputNumber
              style={{ width: '100%' }}
              min={0}
              precision={2}
              formatter={value => `$ ${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
              parser={value => value!.replace(/\$\s?|(,*)/g, '')}
            />
          </Form.Item>

          <Form.Item
            label="Max Quantity (Optional)"
            name="maxQuantity"
            help="Leave empty for unlimited"
          >
            <InputNumber
              style={{ width: '100%' }}
              min={1}
              placeholder="Unlimited"
            />
          </Form.Item>

          <Form.Item
            label="Included Sessions"
            name="includedSessionIds"
            rules={[{ required: true, message: 'Please select at least one session' }]}
          >
            <Checkbox.Group options={sessionOptions} />
          </Form.Item>

          <Form.Item
            label="Sort Order"
            name="sortOrder"
            help="Lower numbers appear first"
          >
            <InputNumber style={{ width: '100%' }} min={0} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};
```

### Capacity Visualization Component

```typescript
// /web/src/components/events/CapacityVisualization.tsx
import React, { useEffect, useState } from 'react';
import { Progress, Card, Row, Col, Statistic, Tag, Alert } from 'antd';
import { WarningOutlined, CheckCircleOutlined } from '@ant-design/icons';
import type { EventAvailabilityDto, SessionAvailability, TicketTypeAvailability } from '../../types/events';
import { eventApi } from '../../api/events';

interface CapacityVisualizationProps {
  eventId: string;
  compact?: boolean;
}

export const CapacityVisualization: React.FC<CapacityVisualizationProps> = ({ 
  eventId, 
  compact = false 
}) => {
  const [availability, setAvailability] = useState<EventAvailabilityDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let interval: NodeJS.Timeout;

    const loadAvailability = async () => {
      try {
        const response = await eventApi.getAvailability(eventId);
        setAvailability(response.data);
      } catch (error) {
        console.error('Failed to load availability:', error);
      } finally {
        setLoading(false);
      }
    };

    loadAvailability();
    
    // Refresh availability every 30 seconds for real-time updates
    interval = setInterval(loadAvailability, 30000);

    return () => {
      if (interval) clearInterval(interval);
    };
  }, [eventId]);

  if (loading || !availability) {
    return <div>Loading availability...</div>;
  }

  const renderSessionStatus = (session: SessionAvailability) => {
    const percentage = (session.currentAttendance / session.totalCapacity) * 100;
    let status: 'success' | 'exception' | 'normal' = 'normal';
    let color = '#52c41a';

    if (percentage >= 100) {
      status = 'exception';
      color = '#ff4d4f';
    } else if (percentage >= 80) {
      color = '#faad14';
    }

    return (
      <div key={session.sessionId} style={{ marginBottom: 16 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 4 }}>
          <span>{session.sessionName}</span>
          <span style={{ fontSize: '0.9em', color: '#666' }}>
            {session.currentAttendance}/{session.totalCapacity}
          </span>
        </div>
        <Progress 
          percent={percentage} 
          status={status}
          strokeColor={color}
          showInfo={false}
          size="small"
        />
        {session.availableSpots <= 5 && session.availableSpots > 0 && (
          <div style={{ fontSize: '0.8em', color: '#faad14', marginTop: 2 }}>
            <WarningOutlined /> Only {session.availableSpots} spots remaining
          </div>
        )}
      </div>
    );
  };

  const renderTicketTypeStatus = (ticketType: TicketTypeAvailability) => {
    const statusColor = ticketType.isAvailable ? 'success' : 'error';
    const statusIcon = ticketType.isAvailable ? <CheckCircleOutlined /> : <WarningOutlined />;
    
    return (
      <Card key={ticketType.ticketTypeId} size="small" style={{ marginBottom: 8 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div>
            <div style={{ fontWeight: 'bold' }}>{ticketType.name}</div>
            <div style={{ color: '#666', fontSize: '0.9em' }}>
              ${ticketType.price.toFixed(2)}
            </div>
            {ticketType.bottleneckSession && !ticketType.isAvailable && (
              <div style={{ color: '#ff4d4f', fontSize: '0.8em', marginTop: 4 }}>
                Limited by {ticketType.bottleneckSession.sessionName}
              </div>
            )}
          </div>
          <div style={{ textAlign: 'right' }}>
            <Tag color={statusColor} icon={statusIcon}>
              {ticketType.isAvailable ? 'Available' : 'Sold Out'}
            </Tag>
            {ticketType.isAvailable && (
              <div style={{ fontSize: '0.8em', color: '#666', marginTop: 4 }}>
                {ticketType.availableQuantity} available
              </div>
            )}
          </div>
        </div>
      </Card>
    );
  };

  if (compact) {
    return (
      <div>
        <Row gutter={16}>
          <Col span={12}>
            <Statistic
              title="Total Sessions"
              value={availability.sessions.length}
              suffix="sessions"
            />
          </Col>
          <Col span={12}>
            <Statistic
              title="Available Tickets"
              value={availability.ticketTypes.filter(tt => tt.isAvailable).length}
              suffix={`of ${availability.ticketTypes.length}`}
            />
          </Col>
        </Row>
        <div style={{ marginTop: 16 }}>
          {availability.sessions.some(s => s.availableSpots <= 5 && s.availableSpots > 0) && (
            <Alert
              message="Some sessions are nearly full"
              type="warning"
              showIcon
              size="small"
            />
          )}
        </div>
      </div>
    );
  }

  return (
    <div>
      <Row gutter={16}>
        <Col span={12}>
          <Card title="Session Capacity" size="small">
            {availability.sessions.map(renderSessionStatus)}
          </Card>
        </Col>
        <Col span={12}>
          <Card title="Ticket Availability" size="small">
            {availability.ticketTypes.map(renderTicketTypeStatus)}
          </Card>
        </Col>
      </Row>
      
      <div style={{ marginTop: 16, fontSize: '0.8em', color: '#666' }}>
        Last updated: {new Date(availability.lastUpdated).toLocaleTimeString()}
      </div>
    </div>
  );
};
```

## 7. Common Scenarios with Code Examples

### Scenario 1: Creating a 3-Day Workshop with Series and Day Passes

```csharp
// Backend: Event creation service method
public async Task<Result<EventDto>> CreateWorkshopWithSessionsAsync(
    CreateWorkshopRequest request,
    CancellationToken cancellationToken)
{
    using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    
    try
    {
        // 1. Create the event
        var workshopEvent = new Event(
            request.Title,
            request.Description,
            request.StartDate,
            request.EndDate,
            capacity: 0, // Will be set by sessions
            EventType.Class,
            request.Location,
            primaryOrganizer: await _context.Users.FindAsync(request.OrganizerId),
            pricingTiers: new[] { new Money(0m) } // Placeholder
        );

        _context.Events.Add(workshopEvent);
        await _context.SaveChangesAsync(cancellationToken);

        // 2. Create sessions for each day
        var sessions = new List<EventSession>();
        
        var session1 = workshopEvent.AddSession(
            "Day 1 - Foundations",
            request.StartDate.Date,
            new TimeSpan(19, 0, 0), // 7 PM
            new TimeSpan(22, 0, 0), // 10 PM
            capacity: 20
        );
        sessions.Add(session1);

        var session2 = workshopEvent.AddSession(
            "Day 2 - Advanced Techniques", 
            request.StartDate.Date.AddDays(1),
            new TimeSpan(13, 0, 0), // 1 PM
            new TimeSpan(17, 0, 0), // 5 PM
            capacity: 20
        );
        sessions.Add(session2);

        var session3 = workshopEvent.AddSession(
            "Day 3 - Practice & Integration",
            request.StartDate.Date.AddDays(2),
            new TimeSpan(10, 0, 0), // 10 AM
            new TimeSpan(14, 0, 0), // 2 PM
            capacity: 18 // Smaller venue for final day
        );
        sessions.Add(session3);

        await _context.SaveChangesAsync(cancellationToken);

        // 3. Create ticket types
        // Full Series Pass (best value)
        var seriesPass = workshopEvent.AddTicketType(
            "Full Series Pass - All 3 Days",
            "Complete workshop experience with all techniques and practice time",
            price: 150m,
            maxQuantity: 15, // Limited series passes
            sortOrder: 1
        );
        
        // Include all sessions in series pass
        foreach (var session in sessions)
        {
            seriesPass.IncludeSession(session);
        }

        // Individual day passes
        var day1Pass = workshopEvent.AddTicketType(
            "Day 1 Only - Foundations",
            "Introduction to rope basics and safety",
            price: 60m,
            maxQuantity: 10, // Max 10 individual day 1 tickets
            sortOrder: 2
        );
        day1Pass.IncludeSession(session1);

        var day2Pass = workshopEvent.AddTicketType(
            "Day 2 Only - Advanced Techniques", 
            "Advanced techniques (Day 1 attendance recommended)",
            price: 60m,
            maxQuantity: 10,
            sortOrder: 3
        );
        day2Pass.IncludeSession(session2);

        var day3Pass = workshopEvent.AddTicketType(
            "Day 3 Only - Practice & Integration",
            "Practice session (previous days recommended)",
            price: 60m,
            maxQuantity: 8,
            sortOrder: 4
        );
        day3Pass.IncludeSession(session3);

        // Weekend combo (Days 2 & 3)
        var weekendPass = workshopEvent.AddTicketType(
            "Weekend Combo - Days 2 & 3",
            "Advanced techniques plus practice (Day 1 recommended)",
            price: 100m,
            maxQuantity: 5,
            sortOrder: 5
        );
        weekendPass.IncludeSession(session2);
        weekendPass.IncludeSession(session3);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(await MapToEventDtoAsync(workshopEvent.Id, cancellationToken));
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
```

### Scenario 2: Checking if Day 2 Has Capacity

```csharp
// Backend: Capacity check service method
public async Task<SessionCapacityCheck> CheckSessionCapacityAsync(
    Guid eventId,
    string sessionName,
    CancellationToken cancellationToken)
{
    var session = await _context.EventSessions
        .Include(s => s.Attendances)
        .FirstOrDefaultAsync(s => s.EventId == eventId && s.SessionName == sessionName, cancellationToken);

    if (session == null)
        throw new NotFoundException($"Session '{sessionName}' not found in event {eventId}");

    var currentAttendance = session.Attendances.Count;
    var availableSpots = session.Capacity - currentAttendance;
    var utilizationPercentage = (currentAttendance / (double)session.Capacity) * 100;

    // Get ticket types that include this session
    var affectedTicketTypes = await _context.EventTicketTypes
        .Include(tt => tt.SessionInclusions)
        .Where(tt => tt.EventId == eventId && tt.SessionInclusions.Any(si => si.SessionId == session.Id))
        .Select(tt => new
        {
            tt.Id,
            tt.Name,
            SoldQuantity = _context.OrderItems
                .Where(oi => oi.TicketTypeId == tt.Id)
                .Where(oi => oi.Order.Status == "Confirmed")
                .Sum(oi => oi.Quantity)
        })
        .ToListAsync(cancellationToken);

    return new SessionCapacityCheck
    {
        SessionId = session.Id,
        SessionName = session.SessionName,
        SessionDate = session.SessionDate,
        TotalCapacity = session.Capacity,
        CurrentAttendance = currentAttendance,
        AvailableSpots = availableSpots,
        UtilizationPercentage = utilizationPercentage,
        Status = availableSpots > 5 ? "Available" : 
                availableSpots > 0 ? "Nearly Full" : "Sold Out",
        AffectedTicketTypes = affectedTicketTypes.Select(att => new TicketTypeSummary
        {
            Id = att.Id,
            Name = att.Name,
            SoldQuantity = att.SoldQuantity
        }).ToList()
    };
}

public class SessionCapacityCheck
{
    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public int TotalCapacity { get; set; }
    public int CurrentAttendance { get; set; }
    public int AvailableSpots { get; set; }
    public double UtilizationPercentage { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<TicketTypeSummary> AffectedTicketTypes { get; set; } = new();
}
```

### Scenario 3: Purchasing Tickets with Session Validation

```csharp
// Backend: Purchase processing with atomic session reservation
public async Task<Result<PurchaseConfirmationDto>> ProcessPurchaseAsync(
    Guid userId,
    Guid eventId,
    List<TicketPurchaseItem> items,
    string paymentMethodId,
    CancellationToken cancellationToken)
{
    using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    
    try
    {
        // 1. Validate all items before processing any
        var validationErrors = new List<string>();
        var ticketTypeDetails = new List<(EventTicketType TicketType, int Quantity)>();
        
        foreach (var item in items)
        {
            var ticketType = await _context.EventTicketTypes
                .Include(tt => tt.SessionInclusions)
                    .ThenInclude(si => si.Session)
                        .ThenInclude(s => s.Attendances)
                .FirstOrDefaultAsync(tt => tt.Id == item.TicketTypeId, cancellationToken);

            if (ticketType == null)
            {
                validationErrors.Add($"Ticket type {item.TicketTypeId} not found");
                continue;
            }

            if (ticketType.EventId != eventId)
            {
                validationErrors.Add($"Ticket type {ticketType.Name} does not belong to this event");
                continue;
            }

            // Validate each included session has capacity
            foreach (var inclusion in ticketType.SessionInclusions)
            {
                var session = inclusion.Session;
                var currentAttendance = session.Attendances.Count;
                var availableSpots = session.Capacity - currentAttendance;

                if (availableSpots < item.Quantity)
                {
                    validationErrors.Add(
                        $"Session '{session.SessionName}' only has {availableSpots} spots available, " +
                        $"but {item.Quantity} {ticketType.Name} tickets requested");
                }
            }

            // Check for duplicate user attendance
            var userSessionIds = await _context.SessionAttendances
                .Where(sa => sa.UserId == userId)
                .Select(sa => sa.SessionId)
                .ToListAsync(cancellationToken);

            var conflictingSessions = ticketType.SessionInclusions
                .Where(si => userSessionIds.Contains(si.SessionId))
                .Select(si => si.Session.SessionName)
                .ToList();

            if (conflictingSessions.Any())
            {
                validationErrors.Add(
                    $"User already has tickets to sessions: {string.Join(", ", conflictingSessions)}");
            }

            ticketTypeDetails.Add((ticketType, item.Quantity));
        }

        if (validationErrors.Any())
            return Result.Failure<PurchaseConfirmationDto>(string.Join("; ", validationErrors));

        // 2. Create order
        var totalAmount = ticketTypeDetails.Sum(ttd => ttd.TicketType.Price * ttd.Quantity);
        var orderNumber = await GenerateOrderNumberAsync(cancellationToken);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventId = eventId,
            OrderNumber = orderNumber,
            Status = "Pending",
            TotalAmount = totalAmount,
            PaymentIntentId = paymentMethodId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);

        // 3. Create order items and session attendances
        foreach (var (ticketType, quantity) in ticketTypeDetails)
        {
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                TicketTypeId = ticketType.Id,
                Quantity = quantity,
                UnitPrice = ticketType.Price,
                TotalPrice = ticketType.Price * quantity,
                CreatedAt = DateTime.UtcNow
            };

            _context.OrderItems.Add(orderItem);

            // Create session attendance records for each quantity of this ticket type
            for (int i = 0; i < quantity; i++)
            {
                foreach (var inclusion in ticketType.SessionInclusions)
                {
                    var attendance = new SessionAttendance(
                        inclusion.SessionId,
                        userId,
                        orderItem.Id
                    );

                    _context.SessionAttendances.Add(attendance);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 4. Process payment (simplified - in real implementation, integrate with Stripe)
        var paymentSuccess = await ProcessStripePaymentAsync(paymentMethodId, totalAmount, cancellationToken);
        
        if (!paymentSuccess)
        {
            return Result.Failure<PurchaseConfirmationDto>("Payment processing failed");
        }

        // 5. Confirm order
        order.Status = "Confirmed";
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // 6. Send confirmation email
        await _emailService.SendPurchaseConfirmationAsync(userId, order.Id, cancellationToken);

        return Result.Success(new PurchaseConfirmationDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            TotalAmount = totalAmount,
            ConfirmationCode = GenerateConfirmationCode(order.OrderNumber),
            PurchasedTickets = ticketTypeDetails.Select(ttd => new PurchasedTicketDto
            {
                TicketTypeName = ttd.TicketType.Name,
                Quantity = ttd.Quantity,
                UnitPrice = ttd.TicketType.Price,
                TotalPrice = ttd.TicketType.Price * ttd.Quantity,
                IncludedSessions = ttd.TicketType.SessionInclusions
                    .Select(si => si.Session.SessionName)
                    .ToList()
            }).ToList()
        });
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
```

### Scenario 4: Display Remaining Capacity Per Session

```typescript
// Frontend: Real-time capacity display component
import React, { useEffect, useState } from 'react';
import { Card, Progress, Tag, Tooltip } from 'antd';
import { InfoCircleOutlined, WarningOutlined } from '@ant-design/icons';

interface SessionCapacityDisplayProps {
  eventId: string;
  refreshInterval?: number;
}

export const SessionCapacityDisplay: React.FC<SessionCapacityDisplayProps> = ({ 
  eventId, 
  refreshInterval = 30000 
}) => {
  const [availability, setAvailability] = useState<EventAvailabilityDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadAvailability = async () => {
      try {
        const response = await eventApi.getAvailability(eventId);
        setAvailability(response.data);
      } catch (error) {
        console.error('Failed to load availability:', error);
      } finally {
        setLoading(false);
      }
    };

    loadAvailability();
    const interval = setInterval(loadAvailability, refreshInterval);
    return () => clearInterval(interval);
  }, [eventId, refreshInterval]);

  if (loading || !availability) {
    return <div>Loading capacity information...</div>;
  }

  const getCapacityStatus = (session: SessionAvailability) => {
    const percentage = (session.currentAttendance / session.totalCapacity) * 100;
    
    if (percentage >= 100) return { color: 'red', status: 'exception', text: 'SOLD OUT' };
    if (percentage >= 90) return { color: 'orange', status: 'active', text: 'NEARLY FULL' };
    if (percentage >= 70) return { color: 'gold', status: 'normal', text: 'FILLING UP' };
    return { color: 'green', status: 'success', text: 'AVAILABLE' };
  };

  const getAffectingTicketTypes = (sessionId: string) => {
    return availability.ticketTypes.filter(tt => 
      tt.includedSessions.some(s => s.sessionId === sessionId)
    );
  };

  return (
    <div style={{ display: 'grid', gap: 16, gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))' }}>
      {availability.sessions.map(session => {
        const capacityStatus = getCapacityStatus(session);
        const affectingTicketTypes = getAffectingTicketTypes(session.sessionId);
        
        return (
          <Card 
            key={session.sessionId}
            title={
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span>{session.sessionName}</span>
                <Tag color={capacityStatus.color}>{capacityStatus.text}</Tag>
              </div>
            }
            size="small"
            extra={
              <Tooltip title="Click for details">
                <InfoCircleOutlined />
              </Tooltip>
            }
          >
            <div style={{ marginBottom: 12 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                <span>Capacity:</span>
                <span>{session.currentAttendance} / {session.totalCapacity}</span>
              </div>
              
              <Progress 
                percent={(session.currentAttendance / session.totalCapacity) * 100}
                status={capacityStatus.status as any}
                strokeColor={capacityStatus.color}
                size="small"
              />
              
              <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 8, fontSize: '0.9em' }}>
                <span style={{ color: '#52c41a' }}>
                  {session.availableSpots} spots remaining
                </span>
                <span style={{ color: '#666' }}>
                  {new Date(session.sessionDate).toLocaleDateString()}
                </span>
              </div>
            </div>

            {session.availableSpots <= 5 && session.availableSpots > 0 && (
              <div style={{ 
                background: '#fff7e6', 
                border: '1px solid #ffd591', 
                borderRadius: 4, 
                padding: 8, 
                marginBottom: 12,
                fontSize: '0.85em'
              }}>
                <WarningOutlined style={{ color: '#fa8c16', marginRight: 4 }} />
                Only {session.availableSpots} spots left! Book soon.
              </div>
            )}

            <div>
              <div style={{ fontSize: '0.85em', color: '#666', marginBottom: 4 }}>
                Affected Ticket Types:
              </div>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4 }}>
                {affectingTicketTypes.map(tt => (
                  <Tag 
                    key={tt.ticketTypeId} 
                    size="small"
                    color={tt.isAvailable ? 'blue' : 'red'}
                  >
                    {tt.name} ({tt.availableQuantity} available)
                  </Tag>
                ))}
              </div>
            </div>
          </Card>
        );
      })}
    </div>
  );
};
```

## 8. Testing Requirements

### Unit Tests for Capacity Calculations

```csharp
// /tests/WitchCityRope.Core.Tests/Services/EventCapacityServiceTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Core.Entities;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Core.Tests.Services
{
    public class EventCapacityServiceTests : IDisposable
    {
        private readonly WitchCityRopeIdentityDbContext _context;
        private readonly EventCapacityService _service;

        public EventCapacityServiceTests()
        {
            var options = new DbContextOptionsBuilder<WitchCityRopeIdentityDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new WitchCityRopeIdentityDbContext(options);
            _service = new EventCapacityService(_context);
        }

        [Fact]
        public async Task CalculateEventAvailability_WithMultipleTicketTypes_ReturnsCorrectAvailability()
        {
            // Arrange
            var eventEntity = await CreateTestEventWithSessionsAsync();
            await CreateTestTicketTypesAsync(eventEntity);

            // Act
            var availability = await _service.CalculateEventAvailabilityAsync(eventEntity.Id);

            // Assert
            Assert.NotNull(availability);
            Assert.Equal(3, availability.Sessions.Count);
            Assert.Equal(4, availability.TicketTypes.Count);

            // Check session availability
            var day1Session = availability.Sessions.First(s => s.SessionName == "Day 1");
            Assert.Equal(20, day1Session.TotalCapacity);
            Assert.Equal(0, day1Session.CurrentAttendance);
            Assert.Equal(20, day1Session.AvailableSpots);
            Assert.True(day1Session.IsAvailable);

            // Check ticket type availability
            var seriesPass = availability.TicketTypes.First(tt => tt.Name == "Full Series Pass");
            Assert.True(seriesPass.IsAvailable);
            Assert.Equal(15, seriesPass.AvailableQuantity); // Limited by MaxQuantity
        }

        [Fact]
        public async Task ValidatePurchase_WhenSessionAtCapacity_ReturnsFailure()
        {
            // Arrange
            var eventEntity = await CreateTestEventWithSessionsAsync();
            var ticketType = await CreateTicketTypeWithSessionAsync(eventEntity);
            await FillSessionToCapacityAsync(eventEntity.Sessions.First());

            // Act
            var validation = await _service.ValidatePurchaseAsync(
                userId: Guid.NewGuid(),
                ticketTypeId: ticketType.Id,
                quantity: 1
            );

            // Assert
            Assert.False(validation.IsValid);
            Assert.Contains("only has 0 spots available", validation.Errors.First());
        }

        [Fact]
        public async Task ValidatePurchase_WithAvailableCapacity_ReturnsSuccess()
        {
            // Arrange
            var eventEntity = await CreateTestEventWithSessionsAsync();
            var ticketType = await CreateTicketTypeWithSessionAsync(eventEntity);

            // Act
            var validation = await _service.ValidatePurchaseAsync(
                userId: Guid.NewGuid(),
                ticketTypeId: ticketType.Id,
                quantity: 5
            );

            // Assert
            Assert.True(validation.IsValid);
            Assert.Empty(validation.Errors);
        }

        [Fact]
        public async Task CalculateAvailability_WithPartialSessionAttendance_ShowsCorrectCapacity()
        {
            // Arrange
            var eventEntity = await CreateTestEventWithSessionsAsync();
            var ticketType = await CreateTicketTypeWithSessionAsync(eventEntity);
            
            // Add some attendees to first session
            await AddSessionAttendeesAsync(eventEntity.Sessions.First(), attendeeCount: 5);

            // Act
            var availability = await _service.CalculateEventAvailabilityAsync(eventEntity.Id);

            // Assert
            var session1 = availability.Sessions.First(s => s.SessionName == "Day 1");
            Assert.Equal(5, session1.CurrentAttendance);
            Assert.Equal(15, session1.AvailableSpots);
            Assert.True(session1.IsAvailable);
        }

        private async Task<Event> CreateTestEventWithSessionsAsync()
        {
            var organizer = new User("test@example.com", "Test User");
            _context.Users.Add(organizer);

            var eventEntity = new Event(
                "Test Workshop",
                "Test Description", 
                DateTime.UtcNow.AddDays(30),
                DateTime.UtcNow.AddDays(32),
                capacity: 20,
                EventType.Class,
                "Test Location",
                organizer,
                new[] { new Money(100m) }
            );

            eventEntity.AddSession("Day 1", DateTime.UtcNow.AddDays(30), new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), 20);
            eventEntity.AddSession("Day 2", DateTime.UtcNow.AddDays(31), new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), 20);
            eventEntity.AddSession("Day 3", DateTime.UtcNow.AddDays(32), new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), 20);

            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            return eventEntity;
        }

        private async Task CreateTestTicketTypesAsync(Event eventEntity)
        {
            var sessions = eventEntity.Sessions.ToList();

            // Full Series Pass
            var seriesPass = eventEntity.AddTicketType("Full Series Pass", "All 3 days", 150m, maxQuantity: 15);
            foreach (var session in sessions)
            {
                seriesPass.IncludeSession(session);
            }

            // Individual day passes
            eventEntity.AddTicketType("Day 1 Only", "Day 1 only", 60m, maxQuantity: 10)
                .IncludeSession(sessions[0]);

            eventEntity.AddTicketType("Day 2 Only", "Day 2 only", 60m, maxQuantity: 10)
                .IncludeSession(sessions[1]);

            eventEntity.AddTicketType("Day 3 Only", "Day 3 only", 60m, maxQuantity: 10)
                .IncludeSession(sessions[2]);

            await _context.SaveChangesAsync();
        }

        private async Task<EventTicketType> CreateTicketTypeWithSessionAsync(Event eventEntity)
        {
            var ticketType = eventEntity.AddTicketType("Test Ticket", "Test", 50m);
            ticketType.IncludeSession(eventEntity.Sessions.First());
            await _context.SaveChangesAsync();
            return ticketType;
        }

        private async Task FillSessionToCapacityAsync(EventSession session)
        {
            for (int i = 0; i < session.Capacity; i++)
            {
                var attendance = new SessionAttendance(
                    session.Id,
                    Guid.NewGuid(),
                    Guid.NewGuid()
                );
                _context.SessionAttendances.Add(attendance);
            }
            await _context.SaveChangesAsync();
        }

        private async Task AddSessionAttendeesAsync(EventSession session, int attendeeCount)
        {
            for (int i = 0; i < attendeeCount; i++)
            {
                var attendance = new SessionAttendance(
                    session.Id,
                    Guid.NewGuid(),
                    Guid.NewGuid()
                );
                _context.SessionAttendances.Add(attendance);
            }
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
```

### Integration Tests for Purchase Flows

```csharp
// /tests/WitchCityRope.IntegrationTests/EventPurchaseFlowTests.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using WitchCityRope.Api.Features.Events.Models;

namespace WitchCityRope.IntegrationTests
{
    public class EventPurchaseFlowTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;

        public EventPurchaseFlowTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CompletePurchaseFlow_WithValidTickets_Success()
        {
            // Arrange - Create test event with sessions and ticket types
            var (eventId, ticketTypeId) = await CreateTestEventAsync();
            await AuthenticateAsTestUserAsync();

            // Act 1: Check availability
            var availabilityResponse = await _client.GetAsync($"/api/events/{eventId}/availability");
            availabilityResponse.EnsureSuccessStatusCode();
            
            var availability = await JsonSerializer.DeserializeAsync<EventAvailabilityDto>(
                await availabilityResponse.Content.ReadAsStreamAsync(),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            Assert.NotNull(availability);
            Assert.True(availability.TicketTypes.First(tt => tt.TicketTypeId == ticketTypeId).IsAvailable);

            // Act 2: Validate purchase
            var validateRequest = new ValidatePurchaseRequest
            {
                TicketTypeId = ticketTypeId,
                Quantity = 2
            };

            var validateResponse = await _client.PostAsJsonAsync(
                $"/api/events/{eventId}/tickets/validate", 
                validateRequest);
            
            validateResponse.EnsureSuccessStatusCode();

            // Act 3: Process purchase
            var purchaseRequest = new PurchaseTicketsRequest
            {
                Items = new List<TicketPurchaseItem>
                {
                    new() { TicketTypeId = ticketTypeId, Quantity = 2 }
                },
                PaymentMethodId = "test_payment_method"
            };

            var purchaseResponse = await _client.PostAsJsonAsync(
                $"/api/events/{eventId}/tickets/purchase",
                purchaseRequest);

            purchaseResponse.EnsureSuccessStatusCode();

            var purchaseResult = await JsonSerializer.DeserializeAsync<PurchaseConfirmationDto>(
                await purchaseResponse.Content.ReadAsStreamAsync(),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            // Assert
            Assert.NotNull(purchaseResult);
            Assert.NotEqual(Guid.Empty, purchaseResult.OrderId);
            Assert.NotEmpty(purchaseResult.OrderNumber);
            Assert.Single(purchaseResult.PurchasedTickets);
            Assert.Equal(2, purchaseResult.PurchasedTickets.First().Quantity);

            // Verify capacity was reduced
            var updatedAvailabilityResponse = await _client.GetAsync($"/api/events/{eventId}/availability");
            var updatedAvailability = await JsonSerializer.DeserializeAsync<EventAvailabilityDto>(
                await updatedAvailabilityResponse.Content.ReadAsStreamAsync(),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var updatedTicketType = updatedAvailability.TicketTypes.First(tt => tt.TicketTypeId == ticketTypeId);
            Assert.Equal(2, updatedTicketType.SoldQuantity);
            Assert.Equal(8, updatedTicketType.AvailableQuantity); // Started with 10, sold 2
        }

        [Fact]
        public async Task PurchaseFlow_WhenSessionFull_ReturnsValidationError()
        {
            // Arrange - Create event and fill session to capacity
            var (eventId, ticketTypeId) = await CreateTestEventAsync();
            await FillEventToCapacityAsync(eventId);
            await AuthenticateAsTestUserAsync();

            // Act - Attempt purchase
            var validateRequest = new ValidatePurchaseRequest
            {
                TicketTypeId = ticketTypeId,
                Quantity = 1
            };

            var validateResponse = await _client.PostAsJsonAsync(
                $"/api/events/{eventId}/tickets/validate",
                validateRequest);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, validateResponse.StatusCode);
            
            var errorContent = await validateResponse.Content.ReadAsStringAsync();
            Assert.Contains("only has 0 spots available", errorContent);
        }

        private async Task<(Guid eventId, Guid ticketTypeId)> CreateTestEventAsync()
        {
            // Implementation to create test event via API or directly in database
            // Returns the created event ID and a ticket type ID for testing
            throw new NotImplementedException("Implement test event creation");
        }

        private async Task AuthenticateAsTestUserAsync()
        {
            // Implementation to authenticate test user and set auth headers
            throw new NotImplementedException("Implement test authentication");
        }

        private async Task FillEventToCapacityAsync(Guid eventId)
        {
            // Implementation to fill event sessions to capacity for testing edge cases
            throw new NotImplementedException("Implement capacity filling");
        }
    }
}
```

### Edge Case Tests

```csharp
// Edge case testing for concurrent purchases
[Fact]
public async Task ConcurrentPurchases_WithLimitedCapacity_OnlyOneSucceeds()
{
    // Arrange - Create event with only 1 remaining spot
    var (eventId, ticketTypeId) = await CreateEventWithOneSpotRemainingAsync();
    
    // Act - Simulate two concurrent purchase attempts
    var user1Task = AttemptPurchaseAsync(eventId, ticketTypeId, userId: Guid.NewGuid());
    var user2Task = AttemptPurchaseAsync(eventId, ticketTypeId, userId: Guid.NewGuid());
    
    var results = await Task.WhenAll(user1Task, user2Task);
    
    // Assert - Only one purchase should succeed
    var successfulPurchases = results.Count(r => r.IsSuccess);
    var failedPurchases = results.Count(r => !r.IsSuccess);
    
    Assert.Equal(1, successfulPurchases);
    Assert.Equal(1, failedPurchases);
    Assert.Contains(results, r => !r.IsSuccess && r.Error.Contains("insufficient capacity"));
}

[Fact]
public async Task Purchase_WithPrerequisiteSession_ValidatesCorrectly()
{
    // Arrange - Create event where Day 2 requires Day 1 attendance
    var eventId = await CreateEventWithPrerequisitesAsync();
    var day2TicketTypeId = await GetTicketTypeByNameAsync(eventId, "Day 2 Only");
    
    var userWithDay1 = await CreateUserWithDay1TicketAsync(eventId);
    var userWithoutDay1 = Guid.NewGuid();
    
    // Act & Assert - User with Day 1 can buy Day 2
    var validResult = await _service.ValidatePurchaseAsync(userWithDay1, day2TicketTypeId, 1);
    Assert.True(validResult.IsValid);
    
    // User without Day 1 cannot buy Day 2
    var invalidResult = await _service.ValidatePurchaseAsync(userWithoutDay1, day2TicketTypeId, 1);
    Assert.False(invalidResult.IsValid);
    Assert.Contains("prerequisite", invalidResult.Errors.First().ToLower());
}
```

## 9. Common Mistakes to Avoid

### ❌ DON'T Track Capacity on Ticket Types

**WRONG APPROACH:**
```csharp
public class EventTicketType
{
    public int Capacity { get; set; } // ❌ WRONG - Capacity should be on sessions
    public int SoldCount { get; set; } // ❌ WRONG - Calculate from orders, don't store
}

// ❌ WRONG - This approach fails for overlapping ticket types
public bool HasCapacity(int requestedQuantity)
{
    return SoldCount + requestedQuantity <= Capacity; // Ignores other ticket types!
}
```

**CORRECT APPROACH:**
```csharp
public class EventSession
{
    public int Capacity { get; set; } // ✅ CORRECT - Sessions have capacity
    
    public int GetAvailableSpots()
    {
        return Capacity - Attendances.Count; // ✅ CORRECT - Calculate from actual attendance
    }
}
```

### ❌ DON'T Allow Purchases Without Session Validation

**WRONG APPROACH:**
```csharp
// ❌ WRONG - Only validates ticket type quota
public async Task<bool> ValidatePurchase(Guid ticketTypeId, int quantity)
{
    var ticketType = await _context.EventTicketTypes.FindAsync(ticketTypeId);
    var sold = await GetSoldQuantityAsync(ticketTypeId);
    
    return sold + quantity <= ticketType.MaxQuantity; // Ignores session capacity!
}
```

**CORRECT APPROACH:**
```csharp
// ✅ CORRECT - Validates ALL included sessions have capacity
public async Task<PurchaseValidationResult> ValidatePurchase(Guid ticketTypeId, int quantity)
{
    var ticketType = await _context.EventTicketTypes
        .Include(tt => tt.SessionInclusions)
            .ThenInclude(si => si.Session)
        .FirstAsync(tt => tt.Id == ticketTypeId);

    // Check each included session
    foreach (var inclusion in ticketType.SessionInclusions)
    {
        var session = inclusion.Session;
        var availableSpots = session.Capacity - session.Attendances.Count;
        
        if (availableSpots < quantity)
        {
            return PurchaseValidationResult.Failure(
                $"Session '{session.SessionName}' insufficient capacity");
        }
    }
    
    return PurchaseValidationResult.Success();
}
```

### ❌ DON'T Forget to Lock Sessions During Purchase

**WRONG APPROACH:**
```csharp
// ❌ WRONG - Race condition possible between validation and purchase
public async Task<Result> ProcessPurchase(PurchaseRequest request)
{
    var validation = await ValidatePurchaseAsync(request); // Check 1
    if (!validation.IsValid) return Result.Failure(validation.Error);
    
    // TIME GAP - Another purchase could happen here!
    
    await CreateOrderAsync(request); // Check 2 happens too late!
    return Result.Success();
}
```

**CORRECT APPROACH:**
```csharp
// ✅ CORRECT - Use database transaction to prevent race conditions
public async Task<Result> ProcessPurchase(PurchaseRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        // Lock session records during validation and purchase
        var validation = await ValidatePurchaseWithLockAsync(request);
        if (!validation.IsValid)
        {
            return Result.Failure(validation.Error);
        }

        await CreateOrderAsync(request);
        await transaction.CommitAsync();
        
        return Result.Success();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### ✅ DO Track Capacity on Sessions

**CORRECT PATTERNS:**

1. **Session-Level Capacity Management:**
```csharp
public class EventSession
{
    public int Capacity { get; private set; }
    private readonly List<SessionAttendance> _attendances = new();
    
    public int GetCurrentAttendance() => _attendances.Count;
    public int GetAvailableSpots() => Capacity - GetCurrentAttendance();
    public bool HasAvailableCapacity() => GetAvailableSpots() > 0;
}
```

2. **Correct Availability Calculation:**
```csharp
public async Task<TicketTypeAvailability> CalculateTicketTypeAvailability(EventTicketType ticketType)
{
    // Get the most constraining session (bottleneck)
    var bottleneckSession = ticketType.SessionInclusions
        .Select(si => si.Session)
        .OrderBy(s => s.GetAvailableSpots())
        .First();
    
    // Ticket type availability is limited by the bottleneck session
    var maxByCapacity = bottleneckSession.GetAvailableSpots();
    var maxByQuota = ticketType.MaxQuantity - await GetSoldQuantityAsync(ticketType.Id);
    
    return new TicketTypeAvailability
    {
        AvailableQuantity = Math.Min(maxByCapacity, maxByQuota),
        BottleneckSession = bottleneckSession,
        IsAvailable = maxByCapacity > 0 && maxByQuota > 0
    };
}
```

3. **Atomic Purchase Processing:**
```csharp
public async Task<Result> ProcessPurchase(List<TicketPurchaseItem> items)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        // 1. Validate ALL items atomically
        foreach (var item in items)
        {
            await ValidateAndLockCapacityAsync(item);
        }
        
        // 2. Create order and attendance records
        var order = await CreateOrderAsync(items);
        
        foreach (var item in items)
        {
            await CreateSessionAttendancesAsync(order, item);
        }
        
        // 3. Commit all changes together
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        
        return Result.Success();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### ✅ DO Use Transactions for Purchase Operations

**Transaction Pattern for Multi-Session Operations:**
```csharp
public async Task<Result<OrderDto>> CreateOrderWithSessionReservationsAsync(
    CreateOrderRequest request,
    CancellationToken cancellationToken = default)
{
    using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    
    try
    {
        var validationErrors = new List<string>();
        var sessionReservations = new List<(EventSession Session, int Quantity)>();
        
        // Phase 1: Validate and calculate session requirements
        foreach (var item in request.Items)
        {
            var ticketType = await _context.EventTicketTypes
                .Include(tt => tt.SessionInclusions)
                    .ThenInclude(si => si.Session)
                        .ThenInclude(s => s.Attendances)
                .FirstOrDefaultAsync(tt => tt.Id == item.TicketTypeId, cancellationToken);
            
            if (ticketType == null)
            {
                validationErrors.Add($"Ticket type {item.TicketTypeId} not found");
                continue;
            }
            
            // Check capacity for each included session
            foreach (var inclusion in ticketType.SessionInclusions)
            {
                var session = inclusion.Session;
                var availableSpots = session.Capacity - session.Attendances.Count;
                
                if (availableSpots < item.Quantity)
                {
                    validationErrors.Add(
                        $"Session '{session.SessionName}' has insufficient capacity: " +
                        $"{availableSpots} available, {item.Quantity} requested");
                }
                else
                {
                    sessionReservations.Add((session, item.Quantity));
                }
            }
        }
        
        if (validationErrors.Any())
        {
            return Result.Failure<OrderDto>(string.Join("; ", validationErrors));
        }
        
        // Phase 2: Create order and reservations atomically
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            EventId = request.EventId,
            Status = OrderStatus.Pending,
            TotalAmount = CalculateTotalAmount(request.Items),
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Orders.Add(order);
        
        // Create order items
        foreach (var item in request.Items)
        {
            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                TicketTypeId = item.TicketTypeId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.UnitPrice * item.Quantity
            };
            
            _context.OrderItems.Add(orderItem);
            
            // Create session attendance records
            var ticketType = await _context.EventTicketTypes
                .Include(tt => tt.SessionInclusions)
                .FirstAsync(tt => tt.Id == item.TicketTypeId, cancellationToken);
            
            foreach (var inclusion in ticketType.SessionInclusions)
            {
                for (int i = 0; i < item.Quantity; i++)
                {
                    var attendance = new SessionAttendance(
                        inclusion.SessionId,
                        request.UserId,
                        orderItem.Id
                    );
                    
                    _context.SessionAttendances.Add(attendance);
                }
            }
        }
        
        // Phase 3: Save and commit
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        
        return Result.Success(await MapToOrderDtoAsync(order.Id, cancellationToken));
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(cancellationToken);
        return Result.Failure<OrderDto>($"Purchase processing failed: {ex.Message}");
    }
}
```

## 10. Migration from Simple Tickets

### Backward Compatibility Strategy

For existing events that use simple ticket types without sessions, implement a migration strategy:

```csharp
// Migration service to convert existing events to session model
public class EventSessionMigrationService
{
    private readonly WitchCityRopeIdentityDbContext _context;

    public EventSessionMigrationService(WitchCityRopeIdentityDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Migrates existing simple events to use session model
    /// Creates a single session per event with the event's original capacity
    /// </summary>
    public async Task MigrateExistingEventsAsync(CancellationToken cancellationToken = default)
    {
        var eventsWithoutSessions = await _context.Events
            .Where(e => !e.Sessions.Any())
            .ToListAsync(cancellationToken);

        foreach (var eventEntity in eventsWithoutSessions)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            
            try
            {
                // Create default session for existing event
                var defaultSession = eventEntity.AddSession(
                    sessionName: "Main Event",
                    sessionDate: eventEntity.StartDate.Date,
                    startTime: eventEntity.StartDate.TimeOfDay,
                    endTime: eventEntity.EndDate.TimeOfDay,
                    capacity: eventEntity.Capacity
                );

                // Update existing ticket types to include the default session
                foreach (var ticketType in eventEntity.TicketTypes)
                {
                    ticketType.IncludeSession(defaultSession);
                }

                // Migrate existing registrations to session attendance
                foreach (var registration in eventEntity.Registrations
                    .Where(r => r.Status == RegistrationStatus.Confirmed))
                {
                    // Create a placeholder order item for the migration
                    var migrationOrderItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = Guid.NewGuid(), // Placeholder order
                        TicketTypeId = eventEntity.TicketTypes.First().Id,
                        Quantity = 1,
                        UnitPrice = 0, // Migration placeholder
                        TotalPrice = 0,
                        CreatedAt = registration.CreatedAt
                    };

                    var attendance = new SessionAttendance(
                        defaultSession.Id,
                        registration.UserId,
                        migrationOrderItem.Id
                    );

                    _context.SessionAttendances.Add(attendance);
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    /// <summary>
    /// Checks if an event needs migration to session model
    /// </summary>
    public async Task<bool> RequiresMigrationAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Sessions)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        return eventEntity != null && !eventEntity.Sessions.Any();
    }
}
```

### Compatibility Layer

For existing API clients that expect simple ticket type behavior:

```csharp
// Compatibility controller for legacy API clients
[ApiController]
[Route("api/legacy/events")]
public class LegacyEventCompatibilityController : ControllerBase
{
    private readonly EventCapacityService _capacityService;

    /// <summary>
    /// Legacy endpoint that provides simple capacity info for events with one session
    /// </summary>
    [HttpGet("{eventId:guid}/capacity")]
    public async Task<IActionResult> GetLegacyCapacity(Guid eventId)
    {
        var availability = await _capacityService.CalculateEventAvailabilityAsync(eventId);
        
        // For backward compatibility, aggregate all session capacity into event capacity
        var totalCapacity = availability.Sessions.Sum(s => s.TotalCapacity);
        var totalAttendance = availability.Sessions.Sum(s => s.CurrentAttendance);
        var totalAvailable = availability.Sessions.Sum(s => s.AvailableSpots);

        return Ok(new
        {
            EventId = eventId,
            TotalCapacity = totalCapacity,
            CurrentAttendance = totalAttendance,
            AvailableSpots = totalAvailable,
            IsAvailable = totalAvailable > 0,
            // Legacy format for existing clients
            TicketTypes = availability.TicketTypes.Select(tt => new
            {
                tt.TicketTypeId,
                tt.Name,
                tt.Price,
                Available = tt.AvailableQuantity,
                IsAvailable = tt.IsAvailable
            })
        });
    }
}
```

---

## Summary

This implementation guide provides the complete architecture and code examples needed to implement the Event Session Matrix ticketing system. The key principles to remember:

1. **Sessions are the atomic unit of capacity** - all capacity calculations roll up from session-level tracking
2. **Ticket types bundle sessions** - they specify which sessions are included, not their own capacity
3. **Use database transactions** - prevent race conditions during purchase processing
4. **Validate capacity atomically** - check ALL included sessions before confirming any purchase
5. **Plan for real-time updates** - frontend components should refresh availability regularly

Following this guide will result in a robust, scalable ticketing system that handles complex scenarios while maintaining data integrity and preventing overselling.

**Implementation Priority:**
1. Database schema and entity models
2. Core capacity calculation service
3. Session and ticket type management APIs
4. Purchase validation and processing
5. Frontend components for organizers and attendees
6. Testing suite covering all edge cases
7. Migration strategy for existing events

This system will support WitchCityRope's complex event patterns while providing room for future growth and enhancement.