# Database Designer Handoff - Granular Event Timing Controls Implementation Complete
<!-- Date: 2025-11-18 -->
<!-- From: Database Designer Agent -->
<!-- To: Backend Developer Agent -->
<!-- Feature: Granular Event Timing Controls -->
<!-- Phase: Database Schema Complete → Backend Implementation Ready -->

## 🎯 IMPLEMENTATION SUMMARY

### ✅ COMPLETED WORK

**1. Event Entity Updated** (`/home/chad/repos/witchcityrope/apps/api/Models/Event.cs`)
   - Added 6 new nullable decimal properties for granular timing controls
   - All properties properly documented with XML comments
   - Follows existing entity patterns
   - Properties grouped logically with clear section headers

**2. EF Core Migration Created** (`/home/chad/repos/witchcityrope/apps/api/Migrations/20251118000000_AddEventTimingControls.cs`)
   - Adds 6 nullable `numeric(7,1)` columns to Events table
   - Implements 6 check constraints enforcing `-24` minimum value
   - Adds descriptive PostgreSQL column comments explaining business logic
   - Includes proper `Down()` method for migration rollback
   - Ready for automatic application via DatabaseInitializationService

**3. EventSeeder Updated** (`/home/chad/repos/witchcityrope/apps/api/Services/Seeding/EventSeeder.cs`)
   - ALL 12 seeded events now include realistic timing values
   - NO NULL values in seed data (demonstrates system capability)
   - Varied timing strategies by event type:
     - **Social events**: Lenient (includes post-event timing: -2, -1 hours)
     - **Workshops**: Strict (early commitment: 48-168 hours before)
     - **Advanced classes**: Very strict (safety-critical: 120-336 hours before)
   - Two helper methods updated to accept timing parameters
   - Comprehensive inline comments explaining timing strategy for each event

## 📊 TIMING VALUES IMPLEMENTED

### Social Events (Lenient)
- **Community Rope Jam**: Register until -2 hours after, cancel until -1 hour after (demonstrates post-event)
- **Rope Social & Discussion**: Register until 6 hours before, cancel until 12 hours before (moderate)
- **New Members Meetup**: Register until 2 hours before, cancel until 6 hours before (welcoming)

### Class Events (Strict to Very Strict)
- **Introduction to Rope Safety**: Register by 48 hours before, cancel by 72 hours before (strict)
- **Suspension Basics**: Register by 5 days before, cancel by 7 days before (very strict, safety)
- **Advanced Floor Work**: Register by 24 hours before, cancel by 48 hours before (balanced)

### Historical Events
- All 4 historical events include appropriate timing values
- Demonstrates varied patterns for testing

## 🔍 VALIDATION COMPLETED

### ✅ Migration Validation
- [x] 6 new columns added to Events table
- [x] All columns use `DECIMAL(7,1)` type (supports 0.5 hour increments)
- [x] All columns are nullable (backward compatible)
- [x] All columns have check constraints (`>= -24`)
- [x] All columns have descriptive PostgreSQL comments
- [x] Migration includes proper rollback (Down method)

### ✅ Entity Model Validation
- [x] 6 new properties added to Event.cs
- [x] All properties properly typed (decimal?)
- [x] All properties have XML documentation
- [x] Properties logically grouped and organized
- [x] Follows existing Event entity patterns

### ✅ Seed Data Validation
- [x] All 12 events include timing values (NO NULLs)
- [x] Values are realistic and varied
- [x] Social events demonstrate post-event timing (-2, -1, -0.5 hours)
- [x] Class events demonstrate strict timing (48-336 hours before)
- [x] Timing values align with event type (social=lenient, workshop=strict)
- [x] All values pass check constraint validation (>= -24)

## 🎯 DATABASE SCHEMA DESIGN

### New Columns Added to Events Table

| Column Name | Type | Nullable | Check Constraint | Purpose |
|-------------|------|----------|------------------|---------|
| `RegistrationOpenHours` | `DECIMAL(7,1)` | YES | `>= -24` | When RSVP/Ticket registration opens |
| `RegistrationCloseHours` | `DECIMAL(7,1)` | YES | `>= -24` | When RSVP/Ticket registration closes |
| `CancellationOpenHours` | `DECIMAL(7,1)` | YES | `>= -24` | When RSVP/Ticket cancellation opens |
| `CancellationCloseHours` | `DECIMAL(7,1)` | YES | `>= -24` | When RSVP/Ticket cancellation closes |
| `VolunteerRegistrationCloseHours` | `DECIMAL(7,1)` | YES | `>= -24` | When volunteer signup closes |
| `VolunteerCancellationCloseHours` | `DECIMAL(7,1)` | YES | `>= -24` | When volunteer cancellation closes |

### Business Logic Rules

**Timing Calculation**:
- **Positive hours** = before event start (e.g., 168 = 7 days before)
- **Negative hours** = after event start (e.g., -24 = 24 hours after)
- **NULL** = no restriction for that timing control
- **-24 minimum** = users cannot register/cancel more than 24 hours after event starts

**Check Constraint Rationale**:
- Enforces maximum post-event timing of 24 hours
- Prevents unreasonable values (e.g., -100 hours = 4 days after event)
- Database-level validation ensures data integrity

**Nullable Column Rationale**:
- NULL = backward compatible (existing events unaffected)
- NULL = "no restriction" for that timing aspect
- Allows gradual adoption of granular timing controls

## 📁 FILES MODIFIED

### 1. Event Entity Class
**Path**: `/home/chad/repos/witchcityrope/apps/api/Models/Event.cs`
**Changes**:
- Added 6 new decimal? properties (lines 96-146)
- Comprehensive XML documentation for each property
- Organized with section headers for clarity
- Follows existing entity patterns

### 2. EF Core Migration
**Path**: `/home/chad/repos/witchcityrope/apps/api/Migrations/20251118000000_AddEventTimingControls.cs`
**Changes**:
- Up() method: Adds 6 columns + 6 check constraints + column comments
- Down() method: Removes check constraints and columns (proper rollback)
- Uses explicit PostgreSQL schema (`public`)
- Follows WitchCityRope migration standards

### 3. Event Seeder
**Path**: `/home/chad/repos/witchcityrope/apps/api/Services/Seeding/EventSeeder.cs`
**Changes**:
- Updated `CreateSeedEvent()` helper to accept 6 timing parameters
- Updated `CreateHistoricalEvent()` helper to accept 6 timing parameters
- Added timing values to all 12 event creations
- Inline comments explain timing strategy for each event type
- Demonstrates full range of timing flexibility (strict to lenient, including post-event)

## 🚨 CRITICAL NOTES FOR BACKEND DEVELOPER

### 1. Migration Application
**The migration will be applied automatically** by `DatabaseInitializationService` on next API container startup.

**Verification Steps**:
```bash
# After container restart, verify columns exist:
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope_dev
\d "Events"
# Should show 6 new columns: RegistrationOpenHours, RegistrationCloseHours, etc.

# Verify check constraints exist:
SELECT constraint_name, check_clause
FROM information_schema.check_constraints
WHERE constraint_name LIKE 'CK_Events_%Hours';
# Should show 6 constraints
```

### 2. NULL Semantics Are Critical
**NULL = No Restriction** for that timing aspect.

Backend logic MUST handle NULL correctly:
```csharp
// ✅ CORRECT: NULL means no restriction
if (event.RegistrationCloseHours.HasValue)
{
    // Apply timing restriction
    var closeTime = event.StartDate.AddHours((double)event.RegistrationCloseHours.Value);
    if (DateTime.UtcNow > closeTime) return false;
}
// If NULL, registration is always open (until event starts)

// ❌ WRONG: Treating NULL as 0
var closeTime = event.StartDate.AddHours((double)(event.RegistrationCloseHours ?? 0));
```

### 3. Positive vs Negative Values
**Positive** = hours BEFORE event start
**Negative** = hours AFTER event start

```csharp
// ✅ CORRECT: Handle both positive and negative
var offsetHours = event.RegistrationCloseHours ?? 0;
var closeTime = event.StartDate.AddHours((double)offsetHours);

// Examples:
// event.RegistrationCloseHours = 24  → closeTime = StartDate - 24 hours (1 day before)
// event.RegistrationCloseHours = -2  → closeTime = StartDate + 2 hours (2 hours after)
```

### 4. Check Constraints Are Enforced
Database will **reject** values less than -24:
```csharp
// ❌ This will throw PostgreSQL constraint violation:
event.RegistrationCloseHours = -25m;
await context.SaveChangesAsync(); // ERROR: Check constraint violation

// ✅ This is allowed:
event.RegistrationCloseHours = -24m; // Exactly -24 is OK
event.RegistrationCloseHours = -1m;  // Any value >= -24 is OK
event.RegistrationCloseHours = 168m; // Positive values unlimited
```

### 5. Seed Data Demonstrates Full Range
Use seed data for testing backend logic:
- **Community Rope Jam**: Post-event timing (-2, -1 hours) - test edge case
- **Suspension Basics**: Very strict (120-336 hours before) - test early deadlines
- **Rope Social & Discussion**: Moderate (6-120 hours) - test balanced approach

## 🔗 NEXT STEPS FOR BACKEND DEVELOPER

### Phase 2: Backend API Implementation

**Primary Task**: Update TimeZoneService to use per-event timing controls

**Key Methods to Update**:
1. `IsRegistrationOpenAsync(Guid eventId)` - Check RegistrationOpenHours and RegistrationCloseHours
2. `IsCancellationAllowedAsync(Guid eventId)` - Check CancellationOpenHours and CancellationCloseHours
3. `IsVolunteerSignupOpenAsync(Guid eventId)` - Check VolunteerRegistrationCloseHours
4. `IsVolunteerCancellationAllowedAsync(Guid eventId)` - Check VolunteerCancellationCloseHours

**Backwards Compatibility**:
- If all timing fields are NULL → Use existing global PreStartBufferMinutes setting
- If any timing field is set → Use per-event value, ignore global setting
- This allows gradual migration of existing events

**Global Setting Deprecation**:
- **DO NOT delete PreStartBufferMinutes yet** (Phase 5 cleanup only)
- Backend must check per-event first, fall back to global

### Required Backend Updates

**1. Update EventDto** (`/apps/api/Features/Events/DTOs/EventDto.cs`):
```csharp
public decimal? RegistrationOpenHours { get; set; }
public decimal? RegistrationCloseHours { get; set; }
public decimal? CancellationOpenHours { get; set; }
public decimal? CancellationCloseHours { get; set; }
public decimal? VolunteerRegistrationCloseHours { get; set; }
public decimal? VolunteerCancellationCloseHours { get; set; }
```

**2. Update TimeZoneService** (`/apps/api/Services/TimeZoneService.cs`):
- Refactor to read per-event timing fields
- Maintain backward compatibility with global setting
- Handle NULL as "no restriction"
- Support negative values (post-event timing)

**3. Update Event CRUD Endpoints**:
- Include timing fields in create/update operations
- Validate values client-side (optional)
- Database check constraints provide server-side validation

**4. Add Unit Tests** (`/tests/WitchCityRope.Core.Tests/`):
- Test NULL handling (no restriction)
- Test positive values (before event)
- Test negative values (after event)
- Test -24 boundary (exactly -24 allowed)
- Test check constraint enforcement (< -24 rejected)

### Testing Strategy

**Use Existing Seed Data**:
1. Start API container (migration applies automatically)
2. Verify 12 events created with timing values
3. Test TimeZoneService against varied event types:
   - Social events with post-event timing
   - Workshops with strict early deadlines
   - Historical events with realistic patterns

**Integration Tests**:
- Test registration/cancellation timing logic
- Test volunteer signup/cancellation timing logic
- Test backward compatibility (NULL values)
- Test edge cases (exactly -24, exactly 0, etc.)

## 📋 VALIDATION CHECKLIST FOR BACKEND DEVELOPER

Before starting Phase 2 implementation:
- [ ] Migration applied successfully (verify with `\d "Events"`)
- [ ] 6 new columns exist in Events table
- [ ] Check constraints exist and enforce >= -24
- [ ] 12 seeded events all have timing values (no NULLs)
- [ ] Event.cs entity has 6 new properties
- [ ] EventDto includes 6 new properties
- [ ] TimeZoneService refactored to use per-event timing
- [ ] Unit tests created for timing validation
- [ ] Integration tests pass with new timing logic
- [ ] Backward compatibility verified (NULL handling)

## 🎯 SUCCESS CRITERIA

### Database Schema
- ✅ 6 new nullable decimal columns added to Events table
- ✅ Check constraints enforce -24 minimum
- ✅ PostgreSQL comments explain business logic
- ✅ Migration is reversible (Down method implemented)

### Entity Model
- ✅ Event.cs updated with 6 new properties
- ✅ Properties properly typed and documented
- ✅ Follows existing entity conventions

### Seed Data
- ✅ All 12 events include realistic timing values
- ✅ NO NULL values in seed data
- ✅ Varied timing strategies demonstrated (strict to lenient)
- ✅ Post-event timing demonstrated (-2, -1 hours)

### Quality Assurance
- ✅ Migration compiles successfully
- ✅ Entity model compiles successfully
- ✅ Seed data follows proper patterns
- ✅ All check constraints validated
- ✅ Backward compatibility maintained (nullable fields)

## 🔧 TROUBLESHOOTING

### Issue: Migration Doesn't Apply
**Symptom**: New columns don't appear after container restart
**Solution**:
```bash
# Check migration status
cd /apps/api
dotnet ef migrations list

# Manually apply if needed
dotnet ef database update
```

### Issue: Check Constraint Violation
**Symptom**: Error when saving event with timing value < -24
**Solution**: This is correct behavior. Values must be >= -24 per business rules.

### Issue: Seed Data Fails
**Symptom**: Event creation fails during seeding
**Solution**: Check timing values are all >= -24. Review EventSeeder.cs for typos.

## 📝 DOCUMENTATION REFERENCES

**Standards Applied**:
- [Entity Framework Patterns](/docs/standards-processes/development-standards/entity-framework-patterns.md)
- [Database Migrations Guide](/docs/standards-processes/backend/database-migrations-guide.md)
- [Database Designer Lessons Learned](/docs/lessons-learned/database-designer-lessons-learned.md)

**Related Documents**:
- [Implementation Plan](/docs/functional-areas/events/admin/granular-timing-controls/implementation-plan.md)
- [Business Requirements Handoff](/docs/functional-areas/events/admin/granular-timing-controls/handoffs/database-designer-handoff.md)

## 🤝 HANDOFF CONFIRMATION

**Phase Complete**: Database Schema Design (Phase 1)
**Date Completed**: 2025-11-18
**Work Quality**: All requirements met, comprehensive seed data, full validation

**Next Agent**: Backend Developer Agent
**Next Phase**: Backend API Implementation (Phase 2)
**Estimated Effort**: 2-3 days for TimeZoneService refactoring and testing

**Blockers**: None. All schema changes complete and ready for backend implementation.

---

**Database schema is production-ready. Backend implementation can begin immediately.**
