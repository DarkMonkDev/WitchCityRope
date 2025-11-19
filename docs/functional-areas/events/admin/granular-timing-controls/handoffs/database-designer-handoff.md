# Database Designer Handoff - Granular Event Timing Controls
<!-- Date: 2025-11-18 -->
<!-- From: Business Requirements Analysis -->
<!-- To: Database Designer Agent -->
<!-- Feature: Granular Event Timing Controls -->

## 🎯 CRITICAL DATABASE REQUIREMENTS (MUST IMPLEMENT)

### 1. Add 6 New Nullable Decimal Columns to Events Table
**Rule**: All timing control fields must be nullable decimals with precision for half-hour increments.
- ✅ Correct: `DECIMAL(7,1)` - supports values like 168.5 (1 week + 30 min)
- ❌ Wrong: `INTEGER` - cannot support 0.5 hour increments
- ❌ Wrong: `NOT NULL` - breaks backward compatibility

### 2. Post-Event Maximum Constraint
**Rule**: All timing fields must enforce -24 hour minimum (no more than 24 hours AFTER event).
- ✅ Correct: `CHECK (FieldName IS NULL OR FieldName >= -24)`
- ❌ Wrong: `CHECK (FieldName >= 0)` - prevents post-event timing

### 3. NULL Means No Restriction
**Rule**: NULL values indicate no timing restriction (backward compatible default).
- ✅ Correct: New columns default to NULL, existing events unaffected
- ❌ Wrong: Default value 0 or any number - changes existing event behavior

### 4. Deprecate Global PreStartBufferMinutes Setting
**Rule**: Remove obsolete global setting from Settings table AFTER backend implementation complete.
- ✅ Correct: Migration removes setting only in Phase 5 (cleanup)
- ❌ Wrong: Remove in initial migration - breaks existing code

### 5. Database Comments Required
**Rule**: All new columns must have descriptive comments explaining business logic.
- ✅ Correct: Comments explain positive=before, negative=after, NULL=no restriction
- ❌ Wrong: No comments - future developers confused

### 6. 🚨 CRITICAL: Update EventsSeeder.cs with Realistic Timing Values
**Rule**: ALL seeded events MUST include realistic values for all 6 timing fields demonstrating system flexibility.

**Requirement Details**:
- **NO NULL values in seed data** - All 6 timing fields must have concrete values
- **Varied and realistic values** - Show different timing patterns for different event types
- **Social Events**: More lenient timing (e.g., register until -2 hours before, cancel until -1 hour before)
- **Workshops**: Stricter timing (e.g., register until 24 hours before, cancel until 48 hours before)
- **Volunteer Timing**: Separate patterns for volunteer-specific controls

**Example Seed Data Values**:
```csharp
// Social Event - Flexible (allow late registration/cancellation)
new Event {
    RegistrationOpenHours = 168m,      // 1 week before
    RegistrationCloseHours = -2m,      // 2 hours AFTER start
    CancellationOpenHours = 24m,       // 1 day before
    CancellationCloseHours = -1m,      // 1 hour AFTER start
    VolunteerRegistrationCloseHours = 48m,   // 2 days before
    VolunteerCancellationCloseHours = 24m,   // 1 day before
}

// Workshop - Strict (early commitment required)
new Event {
    RegistrationOpenHours = 240m,      // 10 days before
    RegistrationCloseHours = 24m,      // 24 hours BEFORE start
    CancellationOpenHours = 120m,      // 5 days before
    CancellationCloseHours = 48m,      // 48 hours BEFORE start
    VolunteerRegistrationCloseHours = 72m,   // 3 days before
    VolunteerCancellationCloseHours = 72m,   // 3 days before
}

// Performance - Medium (balanced approach)
new Event {
    RegistrationOpenHours = 336m,      // 2 weeks before
    RegistrationCloseHours = 6m,       // 6 hours BEFORE start
    CancellationOpenHours = 72m,       // 3 days before
    CancellationCloseHours = 24m,      // 24 hours BEFORE start
    VolunteerRegistrationCloseHours = 96m,   // 4 days before
    VolunteerCancellationCloseHours = 48m,   // 2 days before
}
```

**Why This Matters**:
- Demonstrates system flexibility to admins and users
- Tests all positive, negative, and varied timing scenarios
- Shows realistic business logic for different event types
- Validates UI displays different timing rules correctly
- Enables testing of edge cases (post-event timing with -2, -1 hours)

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| Implementation Plan | `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/granular-timing-controls/implementation-plan.md` | Database Schema Changes section |
| Events Entity | `/apps/api/Features/Events/Models/Event.cs` | Existing Event properties |
| Events Seeder | `/apps/api/Infrastructure/Data/Seeders/EventsSeeder.cs` | Current seeded event data |

## 🚨 KNOWN PITFALLS

### Pitfall 1: Using INTEGER Instead of DECIMAL
**Why it happens**: Hours sound like whole numbers
**How to avoid**: User requirement specifies 0.5 hour increments (30 minutes) - must use DECIMAL

### Pitfall 2: Making Fields NOT NULL
**Why it happens**: Assumption that all events need timing controls
**How to avoid**: NULL = no restriction (backward compatible), existing events must remain unrestricted

### Pitfall 3: Removing PreStartBufferMinutes Too Early
**Why it happens**: Want to clean up obsolete code
**How to avoid**: Backend still uses this setting until Phase 2 complete - remove ONLY in Phase 5

### Pitfall 4: Forgetting Check Constraints
**Why it happens**: Validation seems like application logic
**How to avoid**: Database MUST enforce -24 minimum - prevents data corruption from bad API calls

### Pitfall 5: Using NULL in Seed Data
**Why it happens**: Assumption that seeded events should follow pre-migration defaults
**How to avoid**: Seed data MUST demonstrate system capability - use concrete realistic values for ALL 6 fields

## ✅ VALIDATION CHECKLIST

Before proceeding to backend implementation, verify:

- [ ] Migration adds exactly 6 new columns to Events table
- [ ] All columns use `DECIMAL(7,1)` type
- [ ] All columns are nullable (NULL allowed)
- [ ] All columns have check constraints enforcing >= -24
- [ ] All columns have descriptive comments
- [ ] PreStartBufferMinutes setting NOT removed yet (Phase 5 only)
- [ ] Migration tested on local dev database
- [ ] Migration tested on staging database
- [ ] Existing events remain unaffected (all NULL values)
- [ ] Migration is reversible (rollback script created)
- [ ] EventsSeeder.cs updated with realistic timing values for ALL events
- [ ] Seed data includes varied examples (social, workshop, performance events)
- [ ] NO NULL values in seeded event timing fields

## 🔄 DISCOVERED CONSTRAINTS

### Existing Event Entity
**Location**: `/apps/api/Features/Events/Models/Event.cs`
**Current Fields**: Event entity already has 30+ properties
**Impact**: New timing fields fit existing pattern (nullable decimals common)
**Required Changes**: Add 6 properties to Event.cs class after migration

### Current Global Setting
**Location**: Settings table, Key = "PreStartBufferMinutes"
**Current Usage**: TimeZoneService.IsRegistrationOpenAsync reads this setting
**Impact**: Must remain until backend Phase 2 complete
**Required Changes**: Delete this setting in Phase 5 cleanup migration

### TimeZone-Aware Events
**Constraint**: Events use StartDateTime which is already timezone-aware
**Impact**: Timing calculations must use TimeZoneService for correct timezone conversion
**Required Changes**: None in database layer - handled by service layer

## 📊 DATA MODEL SPECIFICATION

### Events Table - New Columns

```sql
-- Phase 1 Migration: Add Timing Control Columns
ALTER TABLE Events
  ADD COLUMN RegistrationOpenHours DECIMAL(7,1) NULL,
  ADD COLUMN RegistrationCloseHours DECIMAL(7,1) NULL,
  ADD COLUMN CancellationOpenHours DECIMAL(7,1) NULL,
  ADD COLUMN CancellationCloseHours DECIMAL(7,1) NULL,
  ADD COLUMN VolunteerRegistrationCloseHours DECIMAL(7,1) NULL,
  ADD COLUMN VolunteerCancellationCloseHours DECIMAL(7,1) NULL;
```

### Column Comments

```sql
COMMENT ON COLUMN Events.RegistrationOpenHours IS
  'Hours before/after event start when RSVP/Ticket registration opens. Positive=before event start, Negative=after event start (max -24). NULL=no restriction (any time before event).';

COMMENT ON COLUMN Events.RegistrationCloseHours IS
  'Hours before/after event start when RSVP/Ticket registration closes. Positive=before event start, Negative=after event start (max -24). NULL=no restriction (until event starts).';

COMMENT ON COLUMN Events.CancellationOpenHours IS
  'Hours before/after event start when RSVP/Ticket cancellation opens. Positive=before event start, Negative=after event start (max -24). NULL=no restriction (any time before event).';

COMMENT ON COLUMN Events.CancellationCloseHours IS
  'Hours before/after event start when RSVP/Ticket cancellation closes. Positive=before event start, Negative=after event start (max -24). NULL=no restriction (until event starts).';

COMMENT ON COLUMN Events.VolunteerRegistrationCloseHours IS
  'Hours before/after event start when volunteer signup closes. Positive=before event start, Negative=after event start (max -24). NULL=no restriction (until event starts).';

COMMENT ON COLUMN Events.VolunteerCancellationCloseHours IS
  'Hours before/after event start when volunteer assignment cancellation closes. Positive=before event start, Negative=after event start (max -24). NULL=no restriction (until event starts).';
```

### Check Constraints

```sql
-- Enforce -24 hour post-event maximum for all timing fields
ALTER TABLE Events
  ADD CONSTRAINT CK_Events_RegistrationOpenHours
    CHECK (RegistrationOpenHours IS NULL OR RegistrationOpenHours >= -24),

  ADD CONSTRAINT CK_Events_RegistrationCloseHours
    CHECK (RegistrationCloseHours IS NULL OR RegistrationCloseHours >= -24),

  ADD CONSTRAINT CK_Events_CancellationOpenHours
    CHECK (CancellationOpenHours IS NULL OR CancellationOpenHours >= -24),

  ADD CONSTRAINT CK_Events_CancellationCloseHours
    CHECK (CancellationCloseHours IS NULL OR CancellationCloseHours >= -24),

  ADD CONSTRAINT CK_Events_VolunteerRegistrationCloseHours
    CHECK (VolunteerRegistrationCloseHours IS NULL OR VolunteerRegistrationCloseHours >= -24),

  ADD CONSTRAINT CK_Events_VolunteerCancellationCloseHours
    CHECK (VolunteerCancellationCloseHours IS NULL OR VolunteerCancellationCloseHours >= -24);
```

### Business Logic Documentation

**Timing Calculation Rules**:
1. **Positive hours** = before event start (e.g., 168 = 7 days before)
2. **Negative hours** = after event start (e.g., -24 = 24 hours after)
3. **NULL** = no restriction for that timing control
4. **-24 minimum** = users cannot register/cancel more than 24 hours after event starts

**Field Usage by Action Type**:
- **RSVP Creation**: Uses `RegistrationOpenHours` and `RegistrationCloseHours`
- **RSVP Cancellation**: Uses `CancellationOpenHours` and `CancellationCloseHours`
- **Ticket Purchase**: Uses `RegistrationOpenHours` and `RegistrationCloseHours` (shared with RSVP)
- **Ticket Cancellation**: Uses `CancellationOpenHours` and `CancellationCloseHours` (shared with RSVP)
- **Volunteer Signup**: Uses `VolunteerRegistrationCloseHours` only (open time assumed always available)
- **Volunteer Cancel**: Uses `VolunteerCancellationCloseHours` only (open time assumed always available)

## 🎯 SUCCESS CRITERIA

### Migration Correctness
**Test Case**: Run migration on fresh database
- **Input**: Empty Events table
- **Expected Output**: 6 new columns exist, all nullable, all have check constraints

**Test Case**: Run migration on database with existing events
- **Input**: Events table with 10 existing events
- **Expected Output**: 6 new columns exist, all existing events have NULL values, events still accessible

**Test Case**: Attempt to insert invalid timing value
- **Input**: `INSERT INTO Events (..., RegistrationCloseHours) VALUES (..., -25)`
- **Expected Output**: Check constraint violation error

**Test Case**: Insert valid negative timing value
- **Input**: `INSERT INTO Events (..., CancellationCloseHours) VALUES (..., -24)`
- **Expected Output**: Success (exactly -24 is allowed)

**Test Case**: Insert NULL timing value
- **Input**: `INSERT INTO Events (..., RegistrationOpenHours) VALUES (..., NULL)`
- **Expected Output**: Success

**Test Case**: Insert decimal timing value
- **Input**: `INSERT INTO Events (..., RegistrationOpenHours) VALUES (..., 168.5)`
- **Expected Output**: Success (168.5 hours = 1 week + 30 minutes)

### Seed Data Correctness
**Test Case**: Verify all seeded events have timing values
- **Input**: Query EventsSeeder.cs
- **Expected Output**: All 6 timing fields populated with concrete values (no NULLs)

**Test Case**: Verify varied timing patterns exist
- **Input**: Review seeded events
- **Expected Output**: Different events show different timing strategies (social=lenient, workshop=strict, etc.)

## ⚠️ DO NOT IMPLEMENT

- ❌ DO NOT make any timing fields NOT NULL
- ❌ DO NOT use INTEGER type (must be DECIMAL for 0.5 hour support)
- ❌ DO NOT set any default values (NULL is the default)
- ❌ DO NOT delete PreStartBufferMinutes setting in this migration (Phase 5 only)
- ❌ DO NOT add indexes yet (wait for performance testing to identify needs)
- ❌ DO NOT add foreign key constraints (timing fields are simple values, not references)
- ❌ DO NOT leave seed data with NULL timing values (must demonstrate functionality)

## 📝 TERMINOLOGY DICTIONARY

| Term | Definition | Example |
|------|------------|---------|
| Timing Window | Period when a specific action is allowed | Registration window = 7 days before to 1 hour before |
| Registration Open Hours | How many hours before/after event start when registration becomes available | 168 = opens 1 week before event |
| Registration Close Hours | How many hours before/after event start when registration ends | 1 = closes 1 hour before event |
| Cancellation Window | Period when users can cancel their registration | Cancellation = 7 days before to 24 hours after |
| Post-Event Timing | Negative hour values indicating time AFTER event starts | -24 = 24 hours after event started |
| NULL Timing | No restriction - action allowed at any reasonable time | NULL registration open = can register any time before event |

## 🔗 NEXT AGENT INSTRUCTIONS

### Backend Developer Agent
**FIRST**: Read this handoff document completely
**SECOND**: Verify migration ran successfully:
```bash
# Check columns exist
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'Events'
  AND column_name LIKE '%Hours';

# Check constraints exist
SELECT constraint_name, check_clause
FROM information_schema.check_constraints
WHERE constraint_name LIKE 'CK_Events_%Hours';
```
**THIRD**: Update Event.cs entity class with 6 new properties
**FOURTH**: Read backend developer handoff for service layer implementation
**THEN**: Begin TimeZoneService refactoring

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: Business Requirements Agent
**Previous Phase Completed**: 2025-11-18 (Requirements Analysis)
**Key Finding**: Per-event timing controls with flexible values enables differentiated event types and demonstrates system capability through realistic seed data

**Next Agent Should Be**: Backend Developer Agent
**Next Phase**: Backend API Implementation (Phase 2)
**Estimated Effort**: 1 day for database migration, testing, entity updates, and seed data integration

---

## Exact File Paths for Implementation

**Migration File** (create new):
- `/apps/api/Infrastructure/Data/Migrations/YYYYMMDDHHMMSS_AddEventTimingControls.cs`

**Entity File** (update):
- `/apps/api/Features/Events/Models/Event.cs`

**Seeder File** (update):
- `/apps/api/Infrastructure/Data/Seeders/EventsSeeder.cs` - Add realistic timing values to all seeded events

**Settings Cleanup Migration** (Phase 5 only):
- `/apps/api/Infrastructure/Data/Migrations/YYYYMMDDHHMMSS_RemovePreStartBufferSetting.cs`

**Test Files** (create):
- `/tests/WitchCityRope.IntegrationTests/Migrations/EventTimingControlsMigrationTests.cs`
- `/tests/WitchCityRope.Core.Tests/Entities/EventTimingValidationTests.cs`

---

**This handoff document contains all information needed for database schema implementation and seed data setup. Proceed with confidence!**
