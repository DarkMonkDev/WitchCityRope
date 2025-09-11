# Event Session Matrix - Database Design

**Date**: 2025-01-20  
**Phase**: Phase 2 - Event Session Matrix Backend  
**Status**: Ready for Implementation  
**Database**: PostgreSQL 15+  
**ORM**: Entity Framework Core 9

## Overview

This document defines the database schema for the Event Session Matrix system, enabling events with multiple sessions and flexible ticket types that include access to specific sessions.

## Requirements Analysis

### Functional Requirements
- An event can have multiple sessions (e.g., Session 1 at 7pm, Session 2 at 8pm)  
- Ticket types can include access to specific sessions (e.g., "Full Pass" includes S1+S2+S3, "Single Session" includes only S1)
- Each session has its own capacity and timing
- Ticket types support sliding scale pricing (min/max price)
- Track quantity available per ticket type
- Support both "Single" and "Couples" ticket types

### Technical Requirements
- Maintain compatibility with existing Event, Registration, and RSVP entities
- Follow PostgreSQL best practices and EF Core patterns
- UTC timestamps for all date/time fields
- Proper indexing for query performance
- Data integrity through constraints

## Entity Relationship Diagram

```
Events (Existing)
├── EventSessions (NEW)
│   ├── One-to-Many: Event → EventSessions
│   └── Many-to-Many via TicketTypeSessionInclusions: EventSessions ↔ EventTicketTypes
├── EventTicketTypes (NEW)
│   ├── One-to-Many: Event → EventTicketTypes
│   └── Many-to-Many via TicketTypeSessionInclusions: EventTicketTypes ↔ EventSessions
└── TicketTypeSessionInclusions (NEW - Junction Table)
    ├── Many-to-One: → EventTicketTypes
    └── Many-to-One: → EventSessions

Registrations (Existing) - Enhanced
├── EventTicketTypeId (NEW FK)
└── Maintains existing Event relationship
```

## Schema Design

### 1. EventSessions Table

```sql
CREATE TABLE "EventSessions" (
    "Id" UUID NOT NULL DEFAULT gen_random_uuid(),
    "EventId" UUID NOT NULL,
    "SessionIdentifier" VARCHAR(10) NOT NULL, -- S1, S2, S3, etc.
    "Name" VARCHAR(200) NOT NULL,
    "StartDateTime" TIMESTAMPTZ NOT NULL,
    "EndDateTime" TIMESTAMPTZ NOT NULL,
    "Capacity" INTEGER NOT NULL CHECK ("Capacity" > 0),
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "PK_EventSessions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EventSessions_Events" FOREIGN KEY ("EventId") 
        REFERENCES "Events"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_EventSessions_EventId_SessionIdentifier" 
        UNIQUE ("EventId", "SessionIdentifier"),
    CONSTRAINT "CHK_EventSessions_DateRange" 
        CHECK ("StartDateTime" < "EndDateTime")
);

-- Indexes for performance
CREATE INDEX "IX_EventSessions_EventId" ON "EventSessions"("EventId");
CREATE INDEX "IX_EventSessions_StartDateTime" ON "EventSessions"("StartDateTime");
CREATE INDEX "IX_EventSessions_EventId_IsActive" ON "EventSessions"("EventId", "IsActive");
```

### 2. EventTicketTypes Table

```sql
CREATE TABLE "EventTicketTypes" (
    "Id" UUID NOT NULL DEFAULT gen_random_uuid(),
    "EventId" UUID NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "TicketType" VARCHAR(20) NOT NULL CHECK ("TicketType" IN ('Single', 'Couples')),
    "MinPrice" DECIMAL(10,2) NOT NULL CHECK ("MinPrice" >= 0),
    "MaxPrice" DECIMAL(10,2) NOT NULL CHECK ("MaxPrice" >= "MinPrice"),
    "QuantityAvailable" INTEGER NULL CHECK ("QuantityAvailable" IS NULL OR "QuantityAvailable" > 0),
    "SalesEndDateTime" TIMESTAMPTZ NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "PK_EventTicketTypes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EventTicketTypes_Events" FOREIGN KEY ("EventId") 
        REFERENCES "Events"("Id") ON DELETE CASCADE
);

-- Indexes for performance
CREATE INDEX "IX_EventTicketTypes_EventId" ON "EventTicketTypes"("EventId");
CREATE INDEX "IX_EventTicketTypes_EventId_IsActive" ON "EventTicketTypes"("EventId", "IsActive");
CREATE INDEX "IX_EventTicketTypes_SalesEndDateTime" ON "EventTicketTypes"("SalesEndDateTime") 
    WHERE "SalesEndDateTime" IS NOT NULL;
```

### 3. TicketTypeSessionInclusions Junction Table

```sql
CREATE TABLE "TicketTypeSessionInclusions" (
    "Id" UUID NOT NULL DEFAULT gen_random_uuid(),
    "EventTicketTypeId" UUID NOT NULL,
    "EventSessionId" UUID NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "PK_TicketTypeSessionInclusions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TicketTypeSessionInclusions_EventTicketTypes" 
        FOREIGN KEY ("EventTicketTypeId") 
        REFERENCES "EventTicketTypes"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TicketTypeSessionInclusions_EventSessions" 
        FOREIGN KEY ("EventSessionId") 
        REFERENCES "EventSessions"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_TicketTypeSessionInclusions_Unique" 
        UNIQUE ("EventTicketTypeId", "EventSessionId")
);

-- Indexes for performance
CREATE INDEX "IX_TicketTypeSessionInclusions_EventTicketTypeId" 
    ON "TicketTypeSessionInclusions"("EventTicketTypeId");
CREATE INDEX "IX_TicketTypeSessionInclusions_EventSessionId" 
    ON "TicketTypeSessionInclusions"("EventSessionId");
```

### 4. Enhanced Registrations Table

```sql
-- Add new column to existing Registrations table
ALTER TABLE "Registrations" 
ADD COLUMN "EventTicketTypeId" UUID NULL;

-- Add foreign key constraint
ALTER TABLE "Registrations"
ADD CONSTRAINT "FK_Registrations_EventTicketTypes" 
    FOREIGN KEY ("EventTicketTypeId") 
    REFERENCES "EventTicketTypes"("Id") ON DELETE SET NULL;

-- Add index for performance
CREATE INDEX "IX_Registrations_EventTicketTypeId" 
    ON "Registrations"("EventTicketTypeId");
```

## Entity Framework Core Configuration

### 1. EventSession Entity

```csharp
using System;
using System.Collections.Generic;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Core.Entities
{
    public class EventSession
    {
        private readonly List<TicketTypeSessionInclusion> _ticketTypeInclusions = new();
        
        // Private constructor for EF Core
        private EventSession() 
        { 
            SessionIdentifier = null!;
            Name = null!;
        }

        public EventSession(
            Event @event,
            string sessionIdentifier,
            string name,
            DateTime startDateTime,
            DateTime endDateTime,
            int capacity)
        {
            Id = Guid.NewGuid();
            Event = @event ?? throw new ArgumentNullException(nameof(@event));
            EventId = @event.Id;
            SessionIdentifier = sessionIdentifier ?? throw new ArgumentNullException(nameof(sessionIdentifier));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            StartDateTime = startDateTime.Kind == DateTimeKind.Utc ? startDateTime : DateTime.SpecifyKind(startDateTime, DateTimeKind.Utc);
            EndDateTime = endDateTime.Kind == DateTimeKind.Utc ? endDateTime : DateTime.SpecifyKind(endDateTime, DateTimeKind.Utc);
            Capacity = capacity > 0 ? capacity : throw new ArgumentException("Capacity must be greater than zero");
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            ValidateDateRange();
        }

        public Guid Id { get; private set; }
        public Guid EventId { get; private set; }
        public Event Event { get; private set; }
        public string SessionIdentifier { get; private set; }
        public string Name { get; private set; }
        public DateTime StartDateTime { get; private set; }
        public DateTime EndDateTime { get; private set; }
        public int Capacity { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        
        public IReadOnlyCollection<TicketTypeSessionInclusion> TicketTypeInclusions => _ticketTypeInclusions.AsReadOnly();

        private void ValidateDateRange()
        {
            if (StartDateTime >= EndDateTime)
                throw new DomainException("Start date time must be before end date time");
        }
    }
}
```

### 2. EventTicketType Entity

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Core.Entities
{
    public class EventTicketType
    {
        private readonly List<TicketTypeSessionInclusion> _sessionInclusions = new();
        
        // Private constructor for EF Core
        private EventTicketType() 
        { 
            Name = null!;
        }

        public EventTicketType(
            Event @event,
            string name,
            TicketTypeEnum ticketType,
            decimal minPrice,
            decimal maxPrice,
            int? quantityAvailable = null,
            DateTime? salesEndDateTime = null)
        {
            Id = Guid.NewGuid();
            Event = @event ?? throw new ArgumentNullException(nameof(@event));
            EventId = @event.Id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            TicketType = ticketType;
            MinPrice = minPrice >= 0 ? minPrice : throw new ArgumentException("Min price cannot be negative");
            MaxPrice = maxPrice >= minPrice ? maxPrice : throw new ArgumentException("Max price must be >= min price");
            QuantityAvailable = quantityAvailable > 0 ? quantityAvailable : quantityAvailable == null ? null : throw new ArgumentException("Quantity must be positive or null");
            SalesEndDateTime = salesEndDateTime?.Kind == DateTimeKind.Utc ? salesEndDateTime : salesEndDateTime == null ? null : DateTime.SpecifyKind(salesEndDateTime.Value, DateTimeKind.Utc);
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        public Guid EventId { get; private set; }
        public Event Event { get; private set; }
        public string Name { get; private set; }
        public TicketTypeEnum TicketType { get; private set; }
        public decimal MinPrice { get; private set; }
        public decimal MaxPrice { get; private set; }
        public int? QuantityAvailable { get; private set; }
        public DateTime? SalesEndDateTime { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        
        public IReadOnlyCollection<TicketTypeSessionInclusion> SessionInclusions => _sessionInclusions.AsReadOnly();
        
        public IReadOnlyCollection<EventSession> IncludedSessions => 
            _sessionInclusions.Select(i => i.EventSession).ToList().AsReadOnly();
    }
}
```

### 3. TicketTypeSessionInclusion Junction Entity

```csharp
using System;

namespace WitchCityRope.Core.Entities
{
    public class TicketTypeSessionInclusion
    {
        // Private constructor for EF Core
        private TicketTypeSessionInclusion() { }

        public TicketTypeSessionInclusion(EventTicketType ticketType, EventSession eventSession)
        {
            Id = Guid.NewGuid();
            EventTicketType = ticketType ?? throw new ArgumentNullException(nameof(ticketType));
            EventTicketTypeId = ticketType.Id;
            EventSession = eventSession ?? throw new ArgumentNullException(nameof(eventSession));
            EventSessionId = eventSession.Id;
            CreatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        public Guid EventTicketTypeId { get; private set; }
        public EventTicketType EventTicketType { get; private set; }
        public Guid EventSessionId { get; private set; }
        public EventSession EventSession { get; private set; }
        public DateTime CreatedAt { get; private set; }
    }
}
```

### 4. TicketTypeEnum

```csharp
namespace WitchCityRope.Core.Enums
{
    public enum TicketTypeEnum
    {
        Single,
        Couples
    }
}
```

### 5. EF Core Configurations

#### EventSessionConfiguration.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class EventSessionConfiguration : IEntityTypeConfiguration<EventSession>
    {
        public void Configure(EntityTypeBuilder<EventSession> builder)
        {
            builder.ToTable("EventSessions");

            builder.HasKey(es => es.Id);
            builder.Property(es => es.Id).ValueGeneratedNever();

            builder.Property(es => es.SessionIdentifier)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(es => es.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(es => es.StartDateTime)
                .IsRequired()
                .HasColumnType("timestamptz");

            builder.Property(es => es.EndDateTime)
                .IsRequired()
                .HasColumnType("timestamptz");

            builder.Property(es => es.Capacity)
                .IsRequired();

            builder.Property(es => es.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(es => es.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamptz");

            builder.Property(es => es.UpdatedAt)
                .IsRequired()
                .HasColumnType("timestamptz");

            // Relationships
            builder.HasOne(es => es.Event)
                .WithMany()
                .HasForeignKey(es => es.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Constraints
            builder.HasIndex(es => es.EventId);
            builder.HasIndex(es => es.StartDateTime);
            builder.HasIndex(es => new { es.EventId, es.IsActive });
            builder.HasIndex(es => new { es.EventId, es.SessionIdentifier })
                .IsUnique();

            // Check constraints
            builder.ToTable(t => t.HasCheckConstraint("CHK_EventSessions_Capacity", "\"Capacity\" > 0"));
            builder.ToTable(t => t.HasCheckConstraint("CHK_EventSessions_DateRange", "\"StartDateTime\" < \"EndDateTime\""));
        }
    }
}
```

#### EventTicketTypeConfiguration.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class EventTicketTypeConfiguration : IEntityTypeConfiguration<EventTicketType>
    {
        public void Configure(EntityTypeBuilder<EventTicketType> builder)
        {
            builder.ToTable("EventTicketTypes");

            builder.HasKey(tt => tt.Id);
            builder.Property(tt => tt.Id).ValueGeneratedNever();

            builder.Property(tt => tt.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(tt => tt.TicketType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(tt => tt.MinPrice)
                .IsRequired()
                .HasColumnType("decimal(10,2)");

            builder.Property(tt => tt.MaxPrice)
                .IsRequired()
                .HasColumnType("decimal(10,2)");

            builder.Property(tt => tt.QuantityAvailable)
                .IsRequired(false);

            builder.Property(tt => tt.SalesEndDateTime)
                .IsRequired(false)
                .HasColumnType("timestamptz");

            builder.Property(tt => tt.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(tt => tt.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamptz");

            builder.Property(tt => tt.UpdatedAt)
                .IsRequired()
                .HasColumnType("timestamptz");

            // Relationships
            builder.HasOne(tt => tt.Event)
                .WithMany()
                .HasForeignKey(tt => tt.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(tt => tt.EventId);
            builder.HasIndex(tt => new { tt.EventId, tt.IsActive });
            builder.HasIndex(tt => tt.SalesEndDateTime)
                .HasFilter("\"SalesEndDateTime\" IS NOT NULL");

            // Check constraints
            builder.ToTable(t => t.HasCheckConstraint("CHK_EventTicketTypes_MinPrice", "\"MinPrice\" >= 0"));
            builder.ToTable(t => t.HasCheckConstraint("CHK_EventTicketTypes_MaxPrice", "\"MaxPrice\" >= \"MinPrice\""));
            builder.ToTable(t => t.HasCheckConstraint("CHK_EventTicketTypes_Quantity", "\"QuantityAvailable\" IS NULL OR \"QuantityAvailable\" > 0"));
        }
    }
}
```

#### TicketTypeSessionInclusionConfiguration.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class TicketTypeSessionInclusionConfiguration : IEntityTypeConfiguration<TicketTypeSessionInclusion>
    {
        public void Configure(EntityTypeBuilder<TicketTypeSessionInclusion> builder)
        {
            builder.ToTable("TicketTypeSessionInclusions");

            builder.HasKey(tsi => tsi.Id);
            builder.Property(tsi => tsi.Id).ValueGeneratedNever();

            builder.Property(tsi => tsi.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamptz");

            // Relationships
            builder.HasOne(tsi => tsi.EventTicketType)
                .WithMany(tt => tt.SessionInclusions)
                .HasForeignKey(tsi => tsi.EventTicketTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(tsi => tsi.EventSession)
                .WithMany(es => es.TicketTypeInclusions)
                .HasForeignKey(tsi => tsi.EventSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes and constraints
            builder.HasIndex(tsi => tsi.EventTicketTypeId);
            builder.HasIndex(tsi => tsi.EventSessionId);
            builder.HasIndex(tsi => new { tsi.EventTicketTypeId, tsi.EventSessionId })
                .IsUnique();
        }
    }
}
```

## Migration Strategy

### Step 1: Generate Migration

```bash
# From project root
./scripts/generate-migration.sh AddEventSessionMatrix
```

### Step 2: Migration File Structure

The migration should create tables in this order to handle dependencies:
1. `EventSessions` table
2. `EventTicketTypes` table  
3. `TicketTypeSessionInclusions` junction table
4. Add `EventTicketTypeId` column to `Registrations` table
5. Create all indexes and constraints

### Step 3: Update DbContext

Add the new DbSets to `WitchCityRopeDbContext`:

```csharp
public DbSet<EventSession> EventSessions { get; set; }
public DbSet<EventTicketType> EventTicketTypes { get; set; }
public DbSet<TicketTypeSessionInclusion> TicketTypeSessionInclusions { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Existing configurations...
    modelBuilder.ApplyConfiguration(new EventSessionConfiguration());
    modelBuilder.ApplyConfiguration(new EventTicketTypeConfiguration());
    modelBuilder.ApplyConfiguration(new TicketTypeSessionInclusionConfiguration());
}
```

### Step 4: Update Enhanced Registration Configuration

Modify `RegistrationConfiguration.cs` to include the new foreign key:

```csharp
builder.HasOne<EventTicketType>()
    .WithMany()
    .HasForeignKey(r => r.EventTicketTypeId)
    .OnDelete(DeleteBehavior.SetNull);

builder.HasIndex(r => r.EventTicketTypeId);
```

## Performance Considerations

### Indexing Strategy

1. **EventSessions**:
   - Primary key on `Id` (automatic)
   - `EventId` for event lookups
   - `StartDateTime` for time-based queries
   - Composite `EventId + IsActive` for active session lookups
   - Unique `EventId + SessionIdentifier` for session identification

2. **EventTicketTypes**:
   - Primary key on `Id` (automatic)  
   - `EventId` for event lookups
   - Composite `EventId + IsActive` for active ticket types
   - `SalesEndDateTime` with partial index for sales cutoff queries

3. **TicketTypeSessionInclusions**:
   - Primary key on `Id` (automatic)
   - `EventTicketTypeId` for ticket type lookups
   - `EventSessionId` for session lookups  
   - Unique composite `EventTicketTypeId + EventSessionId` for integrity

### Query Optimization

1. **Session Capacity Queries**: Use aggregate queries to calculate registered count per session
2. **Available Tickets**: Join with registrations to get real-time availability
3. **Session Inclusions**: Use efficient JOINs for ticket type session lookups

### Expected Query Patterns

```sql
-- Get all sessions for an event with registration counts
SELECT es.*, COUNT(r.Id) as RegisteredCount
FROM "EventSessions" es
LEFT JOIN "TicketTypeSessionInclusions" tsi ON es."Id" = tsi."EventSessionId"
LEFT JOIN "Registrations" r ON tsi."EventTicketTypeId" = r."EventTicketTypeId"
WHERE es."EventId" = $1 AND es."IsActive" = true
GROUP BY es."Id"
ORDER BY es."SessionIdentifier";

-- Get all ticket types for an event with included sessions
SELECT tt.*, STRING_AGG(es."SessionIdentifier", ', ') as SessionIdentifiers
FROM "EventTicketTypes" tt
LEFT JOIN "TicketTypeSessionInclusions" tsi ON tt."Id" = tsi."EventTicketTypeId"
LEFT JOIN "EventSessions" es ON tsi."EventSessionId" = es."Id"
WHERE tt."EventId" = $1 AND tt."IsActive" = true
GROUP BY tt."Id"
ORDER BY tt."Name";
```

## Data Integrity & Constraints

### Database-Level Constraints

1. **Check Constraints**:
   - Session capacity > 0
   - Session start < end time
   - Min price >= 0
   - Max price >= min price
   - Quantity available > 0 or NULL

2. **Foreign Key Constraints**:
   - Cascade delete for event relationships
   - Set NULL for optional registration ticket type

3. **Unique Constraints**:
   - Session identifier per event
   - Ticket type + session inclusion combination

### Business Rule Enforcement

1. **Session Scheduling**: Validate session times don't conflict
2. **Ticket Type Sessions**: Ensure all referenced sessions belong to same event
3. **Registration Validation**: Check ticket type is active and event matches
4. **Capacity Management**: Aggregate capacity checks across sessions

## Security Considerations

### Data Protection

1. **Soft Deletes**: Use `IsActive` flags instead of hard deletes
2. **Audit Trail**: CreatedAt/UpdatedAt timestamps on all entities
3. **Access Control**: Service-layer authorization for modifications

### Migration Safety

1. **Non-breaking Changes**: New columns with defaults/nullable
2. **Data Validation**: Check existing data compatibility
3. **Rollback Plan**: Document reverse migration steps

## Monitoring & Observability

### Key Metrics to Track

1. **Session Utilization**: Registered count vs capacity per session
2. **Ticket Type Performance**: Sales by ticket type and timeframe  
3. **Query Performance**: Monitor slow queries on new tables
4. **Data Growth**: Table size growth and index effectiveness

### Health Checks

1. **Referential Integrity**: Regular FK constraint validation
2. **Capacity Consistency**: Session registrations don't exceed capacity
3. **Price Validation**: Min/max price ranges are logical

## Testing Strategy

### Unit Tests

1. **Entity Business Rules**: Test all validation logic
2. **Value Object Creation**: Money objects, date validations
3. **Relationship Management**: Junction table operations

### Integration Tests  

1. **Database Constraints**: Test all constraint violations
2. **Query Performance**: Benchmark common query patterns
3. **Migration Verification**: Ensure clean up/down migrations

### Data Migration Tests

1. **Schema Creation**: Test on empty database
2. **Existing Data**: Test migration with sample data
3. **Rollback Scenario**: Test migration reversal

## Related Documentation

- [Entity Framework Patterns](/home/chad/repos/witchcityrope-react/docs/standards-processes/development-standards/entity-framework-patterns.md)
- [PostgreSQL Best Practices](/home/chad/repos/witchcityrope-react/docs/lessons-learned/database-developers.md)
- [Event Session Matrix Frontend Components](/home/chad/repos/witchcityrope-react/apps/web/src/components/events/)

---

**Next Steps**: This design is ready for backend-developer implementation. See handoff document for critical implementation notes and development priorities.