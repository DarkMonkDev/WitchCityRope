# EF Core Entity Discovery Error - Troubleshooting Guide

## Error Message
```
The entity type 'EmailAddress' requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'.
```

## Quick Solution
Remove ALL navigation properties to ignored entities. Use only foreign key IDs.

## Root Cause
EF Core discovers entities through navigation properties, even if you explicitly ignore them in DbContext configuration. This is a common issue when migrating authentication systems or refactoring entity relationships.

## Detailed Explanation

### How Entity Discovery Works
1. EF Core scans your DbContext for DbSet properties
2. It then follows ALL navigation properties from discovered entities
3. Any class referenced as a navigation property is assumed to be an entity
4. Value objects without proper configuration are treated as entities

### The Problem Pattern
```csharp
// In DbContext - This is NOT enough!
modelBuilder.Ignore<Core.User>();

// In VolunteerAssignment entity - This causes the problem!
public class VolunteerAssignment
{
    public Guid UserId { get; set; }
    public User User { get; set; }  // ❌ EF Core discovers User through this!
}

// In User entity
public class User
{
    public EmailAddress Email { get; set; }  // ❌ Now EmailAddress is discovered!
}
```

## Solutions

### 1. Remove Navigation Properties
```csharp
// ✅ CORRECT - Use only foreign key
public class VolunteerAssignment
{
    public Guid UserId { get; set; }
    // No User navigation property!
}
```

### 2. Update Your Services
```csharp
// ❌ OLD - This will fail after removing navigation
var assignment = await _context.VolunteerAssignments
    .Include(a => a.User)
    .FirstOrDefaultAsync(a => a.Id == id);

// ✅ NEW - Fetch user separately
var assignment = await _context.VolunteerAssignments
    .FirstOrDefaultAsync(a => a.Id == id);
var user = await _userManager.FindByIdAsync(assignment.UserId.ToString());
```

### 3. Configure Value Objects Properly
```csharp
// Option 1: Configure as owned type
modelBuilder.Entity<User>()
    .OwnsOne(u => u.Email);

// Option 2: Store as primitive (our solution)
public class WitchCityRopeUser
{
    public string Email { get; set; }  // Store as string
    private EmailAddress _emailAddress;
    public EmailAddress EmailAddress => _emailAddress ??= new EmailAddress(Email);
}
```

## Prevention Checklist

Before generating migrations:
- [ ] Run `dotnet build` - fix ALL compilation errors first
- [ ] Search for `.Include()` statements referencing removed entities
- [ ] Check all navigation properties point to valid entities
- [ ] Verify value objects are configured as owned types or stored as primitives
- [ ] Review entity configurations for removed/replaced entities

## Common Scenarios

### Authentication Migration
When migrating from custom auth to ASP.NET Core Identity:
1. Old User entity often has navigation properties in other entities
2. New Identity user is in different namespace/assembly
3. Must update ALL references throughout the codebase

### Value Object Issues
Common value objects that cause problems:
- EmailAddress
- Money
- Address
- PhoneNumber
- Name

Always configure these as owned types or store as primitives.

## Debugging Steps

1. **Check which entities EF Core sees:**
```csharp
var model = context.Model;
foreach (var entityType in model.GetEntityTypes())
{
    Console.WriteLine($"Entity: {entityType.ClrType.Name}");
}
```

2. **Find navigation paths:**
```csharp
var emailAddressEntity = model.FindEntityType(typeof(EmailAddress));
if (emailAddressEntity != null)
{
    var navigations = model.GetEntityTypes()
        .SelectMany(e => e.GetNavigations())
        .Where(n => n.TargetEntityType == emailAddressEntity);
    
    foreach (var nav in navigations)
    {
        Console.WriteLine($"Found via: {nav.DeclaringEntityType.Name}.{nav.Name}");
    }
}
```

3. **Use migration scripts:**
```bash
# Our standardized migration generation
./scripts/generate-migration.sh TestMigration

# Check what EF Core is trying to create
dotnet ef migrations script
```

## Related Issues
- Navigation property to ignored entity
- Value object without configuration
- Authentication system migration
- Domain model refactoring
- Clean Architecture implementation

## References
- [EF Core Entity Type Discovery](https://docs.microsoft.com/en-us/ef/core/modeling/entity-types)
- [Value Objects in EF Core](https://docs.microsoft.com/en-us/ef/core/modeling/owned-entities)
- [Navigation Properties](https://docs.microsoft.com/en-us/ef/core/modeling/relationships)