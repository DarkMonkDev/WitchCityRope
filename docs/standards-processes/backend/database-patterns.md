# Database Patterns

**Purpose**: Entity Framework Core patterns, query optimization, and database conventions for WitchCityRope.
**When to Read**: When implementing database queries, migrations, or data access logic.
**Related**: [Service Layer Patterns](./service-layer-patterns.md), [Performance Standards](./performance-standards.md)

## Primary Reference

**Comprehensive Guide**: [Entity Framework Patterns](/docs/standards-processes/development-standards/entity-framework-patterns.md)

This document provides a quick reference. For detailed patterns, see the comprehensive EF Core guide above.

## Quick Reference

### DbContext Pattern
```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<Event> Events { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Registration> Registrations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure entities
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
```

### Query Patterns
```csharp
// ✅ CORRECT: AsNoTracking for read-only queries
public async Task<List<EventDto>> GetEventsAsync()
{
    return await _db.Events
        .AsNoTracking()
        .Select(e => new EventDto
        {
            Id = e.Id,
            Name = e.Name,
            // ... map properties
        })
        .ToListAsync();
}

// ✅ CORRECT: Include for navigation properties
public async Task<Event?> GetEventWithSessionsAsync(int id)
{
    return await _db.Events
        .Include(e => e.Sessions)
        .FirstOrDefaultAsync(e => e.Id == id);
}
```

### Migration Patterns
```csharp
// Create migration
dotnet ef migrations add AddEventSessions

// Apply migration
dotnet ef database update

// Rollback migration
dotnet ef database update PreviousMigrationName
```

### Performance Best Practices
- Use `AsNoTracking()` for read-only queries
- Project to DTOs in queries (avoid loading full entities)
- Use `Include()` for eager loading navigation properties
- Avoid N+1 queries with `.Select()` projections
- Use pagination for large result sets
- Add indexes for frequently queried columns

## Database Migrations Guide

**Comprehensive Guide**: [Database Migrations Guide](/docs/standards-processes/backend/database-migrations-guide.md)

For migration procedures, seed data, and deployment workflows, see the comprehensive migrations guide.

## Standards Maintenance

For detailed EF Core patterns and query optimization techniques:
- Read [Entity Framework Patterns](/docs/standards-processes/development-standards/entity-framework-patterns.md)
- Read [Database Migrations Guide](/docs/standards-processes/backend/database-migrations-guide.md)

---

*This document is maintained by the Backend Developer and Database Designer agents.*
