# WitchCityRope Database Seed Data Guide

## Overview

This guide documents the comprehensive seed data system for the WitchCityRope application using ASP.NET Core Identity. The seed data provides a complete testing environment with realistic data for all major entities in the system.

## Running the Seed Data

### Using the Shell Script (Recommended)
```bash
cd /path/to/WitchCityRope
./scripts/seed-database-identity.sh
```

### Using .NET CLI Directly
```bash
cd /path/to/WitchCityRope/tools/DatabaseSeeder
dotnet run
```

### From Visual Studio
1. Set `DatabaseSeeder` as the startup project
2. Press F5 or click "Start"

## Seed Data Contents

### 1. Identity Roles
The following ASP.NET Core Identity roles are created:
- **Administrator** - Full system access
- **Moderator** - Event management and moderation capabilities
- **Member** - Regular member access
- **Attendee** - Basic attendee access

### 2. Test Users

All test users have the password: **Test123!**

| Email | Scene Name | Role | Vetted | Description |
|-------|------------|------|--------|-------------|
| admin@witchcityrope.com | RopeAdmin | Administrator | Yes | System administrator with full access |
| teacher@witchcityrope.com | RopeTeacher | Moderator | Yes | Workshop instructor and event organizer |
| vetted@witchcityrope.com | VettedMember | Member | Yes | Vetted community member |
| member@witchcityrope.com | RegularMember | Member | No | Regular member awaiting vetting |
| guest@witchcityrope.com | CuriousGuest | Attendee | No | Guest user exploring the community |

**Note:** Users can log in using either their email address or scene name.

### 3. Events

The seed data creates 10 events spanning various types:

#### Past Events (2)
- **Rope Safety Fundamentals** - Past workshop for testing historical data
- **February Rope Social** - Past social event

#### Upcoming Events (8)
- **Introduction to Rope Bondage** - Beginner-friendly workshop
- **Monthly Rope Jam** - Regular practice space for vetted members
- **Suspension Intensive Workshop** - Advanced workshop with prerequisites
- **Rope and Sensation Play** - Specialized workshop
- **Virtual Rope Workshop: Self-Tying** - Online workshop
- **Rope Play Party** - Members-only play party
- **Rope Performance Showcase** - Public performance event
- **New England Rope Conference** - Multi-day conference

Each event includes:
- Multiple pricing tiers (sliding scale)
- Appropriate capacity limits
- Primary and secondary organizers
- Detailed descriptions
- Proper categorization (Workshop, Social, Virtual, etc.)

### 4. Event Registrations

Sample registrations are created for:
- Past events (marked as checked in)
- Upcoming events (various states: confirmed, pending)
- Different payment methods (card, cash)
- Different pricing tiers selected by users

### 5. Payment Records

Automatic payment records are created for all confirmed registrations, including:
- Transaction IDs
- Payment status (completed for confirmed registrations)
- Payment method details
- Proper associations with registrations

### 6. Vetting Applications

Three vetting applications are seeded:
1. **Pending Application** from RegularMember - Intermediate experience level
2. **Pending Application** from CuriousGuest - Beginner level
3. **Approved Application** from VettedMember - Historical record showing completed vetting

Each application includes:
- Detailed experience descriptions
- Safety knowledge demonstrations
- Consent understanding explanations
- References
- Proper status tracking

### 7. Incident Reports

One sample incident report is created:
- Low severity safety violation
- Complete with witness information
- Includes review by administrator
- Shows action taken (verbal warning)
- Demonstrates the full incident workflow

## Database Schema Considerations

The seed data properly handles:
- ASP.NET Core Identity integration
- Encrypted legal names for privacy
- Scene names as value objects
- Proper foreign key relationships
- Timestamp tracking (CreatedAt, UpdatedAt)
- Status enumerations
- Money value objects with currency

## Development Workflow

1. **Fresh Database**: Run migrations first, then seed data
2. **Existing Database**: The seeder checks for existing data and skips if found
3. **Reset Database**: Drop database, recreate, run migrations, then seed

### Reset Commands
```bash
# Drop and recreate database
dotnet ef database drop -f
dotnet ef database update

# Run seeder
dotnet run --project tools/DatabaseSeeder
```

## Testing Scenarios

The seed data supports testing of:

### User Flows
- Login with email or scene name
- Role-based access control
- Profile management
- Vetting status checks

### Event Management
- Creating and editing events
- Publishing/unpublishing workflows
- Multi-tier pricing
- Capacity management
- Organizer assignments

### Registration Process
- Event registration
- Payment processing
- Check-in procedures
- Waitlist management

### Community Safety
- Incident reporting
- Review processes
- Action tracking
- Vetting applications

## Troubleshooting

### Common Issues

1. **"Database already contains data"**
   - The seeder detects existing data and skips seeding
   - Drop and recreate the database if you need fresh data

2. **Connection String Issues**
   - Check `appsettings.json` in the DatabaseSeeder project
   - Ensure the connection string matches your environment

3. **Migration Errors**
   - Ensure all migrations are applied before seeding
   - Run `dotnet ef database update` first

4. **Identity Configuration Errors**
   - The seeder configures Identity with test-friendly settings
   - Password requirements are relaxed for test data

## Security Notes

⚠️ **WARNING**: This seed data is for development only!
- All passwords are the same (Test123!)
- Legal names use simple base64 encoding (not secure)
- Email addresses are not real
- This data should NEVER be used in production

## Extending the Seed Data

To add more seed data:

1. Edit `/src/WitchCityRope.Infrastructure/Data/DbInitializer.cs`
2. Add new entities in the appropriate seed method
3. Maintain referential integrity
4. Update this documentation
5. Test the complete seeding process

## Related Documentation

- [Identity Migration Guide](./IDENTITY_MIGRATION_SUMMARY.md)
- [Database Schema](./database/postgresql-migration.md)
- [Testing Guide](../TESTING_GUIDE.md)