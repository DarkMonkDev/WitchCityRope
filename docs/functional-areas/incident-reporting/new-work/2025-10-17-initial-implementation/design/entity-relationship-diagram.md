# Entity Relationship Diagram: Incident Reporting System
<!-- Last Updated: 2025-10-18 -->
<!-- Version: 1.0 -->
<!-- Owner: Database Designer Agent -->
<!-- Status: Draft - Awaiting Approval -->

## Diagram Overview

This document provides the complete entity relationship diagram (ERD) for the Incident Reporting System database schema, showing all entities, relationships, cardinality, and cascade behaviors.

---

## Complete ERD (Text Format)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                             ApplicationUser                                  │
│                          (ASP.NET Identity - Existing)                      │
├─────────────────────────────────────────────────────────────────────────────┤
│ Id: UUID (PK)                                                               │
│ UserName: VARCHAR(256)                                                      │
│ Email: VARCHAR(256)                                                         │
│ SceneName: VARCHAR(100)                                                     │
│ RealName: VARCHAR(200)                                                      │
│ ... (other Identity fields)                                                 │
└─────────────────────────────────────────────────────────────────────────────┘
         │                    │                   │                │
         │ 1:N                │ 1:N               │ 1:N            │ 1:N
         │ Reporter           │ Coordinator       │ NoteAuthor     │ AuditUser
         │ (Optional)         │ (Optional)        │ (Optional)     │ (Optional)
         │                    │                   │                │
         ▼                    ▼                   │                │
┌────────────────────────────────────────────────┐                │
│            SafetyIncident (UPDATED)            │                │
├────────────────────────────────────────────────┤                │
│ Id: UUID (PK)                                  │                │
│ ReferenceNumber: VARCHAR(30) UNIQUE           │                │
│ ───────────────────────────────────────────    │                │
│ ReporterId: UUID (FK → AspNetUsers) NULL      │◄───────────────┘
│ CoordinatorId: UUID (FK → AspNetUsers) NULL ← NEW
│ AssignedTo: UUID (FK → AspNetUsers) NULL (legacy)
│ ───────────────────────────────────────────    │
│ Severity: INTEGER (1-4)                        │
│   1 = Low, 2 = Medium, 3 = High, 4 = Critical │
│ Status: INTEGER (1-5) ← UPDATED               │
│   1 = ReportSubmitted                          │
│   2 = InformationGathering                     │
│   3 = ReviewingFinalReport                     │
│   4 = OnHold                                   │
│   5 = Closed                                   │
│ ───────────────────────────────────────────    │
│ IncidentDate: TIMESTAMPTZ                      │
│ ReportedAt: TIMESTAMPTZ                        │
│ Location: VARCHAR(200)                         │
│ ───────────────────────────────────────────    │
│ EncryptedDescription: TEXT                     │
│ EncryptedInvolvedParties: TEXT NULL            │
│ EncryptedWitnesses: TEXT NULL                  │
│ EncryptedContactEmail: TEXT NULL               │
│ EncryptedContactPhone: TEXT NULL               │
│ ───────────────────────────────────────────    │
│ IsAnonymous: BOOLEAN                           │
│ RequestFollowUp: BOOLEAN                       │
│ ───────────────────────────────────────────    │
│ GoogleDriveFolderUrl: VARCHAR(500) NULL ← NEW  │
│ GoogleDriveFinalReportUrl: VARCHAR(500) NULL ← NEW
│ ───────────────────────────────────────────    │
│ CreatedAt: TIMESTAMPTZ                         │
│ UpdatedAt: TIMESTAMPTZ                         │
│ CreatedBy: UUID NULL                           │
│ UpdatedBy: UUID NULL                           │
└────────────────────────────────────────────────┘
         │                    │                   │
         │ 1:N                │ 1:N               │ 1:N
         │ CASCADE            │ CASCADE           │ CASCADE
         │                    │                   │
         ▼                    ▼                   ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────┐
│   IncidentNote   │  │ IncidentAuditLog │  │ IncidentNotification │
│      (NEW)       │  │    (EXISTING)    │  │     (EXISTING)       │
├──────────────────┤  ├──────────────────┤  ├──────────────────────┤
│ Id: UUID (PK)    │  │ Id: UUID (PK)    │  │ Id: UUID (PK)        │
│ IncidentId: UUID │  │ IncidentId: UUID │  │ IncidentId: UUID     │
│   (FK) ← CASCADE │  │   (FK) ← CASCADE │  │   (FK) ← CASCADE     │
│ ──────────────── │  │ ──────────────── │  │ ──────────────────── │
│ Content: TEXT    │  │ UserId: UUID     │  │ NotificationType:    │
│   (encrypted)    │  │   (FK → Users)   │  │   VARCHAR(20)        │
│ Type: INTEGER    │  │   NULL           │  │ RecipientType:       │
│   1 = Manual     │  │ ──────────────── │  │   VARCHAR(20)        │
│   2 = System     │  │ ActionType:      │  │ RecipientEmail:      │
│ IsPrivate: BOOL  │  │   VARCHAR(50)    │  │   VARCHAR(255)       │
│ AuthorId: UUID   │  │ ActionDescription│  │ Subject: VARCHAR(200)│
│   (FK → Users)   │  │   TEXT           │  │ MessageBody: TEXT    │
│   NULL ← System  │  │ OldValues: TEXT  │  │ Status: VARCHAR(20)  │
│ Tags: VARCHAR    │  │   (JSON)         │  │ SentAt: TIMESTAMPTZ  │
│   (200) NULL     │  │ NewValues: TEXT  │  │ RetryCount: INTEGER  │
│ CreatedAt:       │  │   (JSON)         │  │ CreatedAt:           │
│   TIMESTAMPTZ    │  │ IpAddress:       │  │   TIMESTAMPTZ        │
│ UpdatedAt:       │  │   VARCHAR(45)    │  │ UpdatedAt:           │
│   TIMESTAMPTZ    │  │ UserAgent:       │  │   TIMESTAMPTZ        │
│   NULL           │  │   VARCHAR(500)   │  └──────────────────────┘
└──────────────────┘  │ CreatedAt:       │
         │            │   TIMESTAMPTZ    │
         │ N:1        └──────────────────┘
         │ SET NULL              │
         │                       │ N:1
         │                       │ SET NULL
         ▼                       ▼
┌────────────────────────────────────────────┐
│           ApplicationUser                  │
│     (Author/User - Same table as above)    │
└────────────────────────────────────────────┘
```

---

## Entity Details

### 1. SafetyIncident (Primary Entity - UPDATED)

**Purpose**: Core incident tracking entity with new coordinator assignment and Google Drive integration.

**Table Name**: `SafetyIncidents`

**Primary Key**: `Id` (UUID)

**Unique Constraints**:
- `ReferenceNumber` (unique)

**Indexes**:
- `IX_SafetyIncidents_ReferenceNumber` (unique)
- `IX_SafetyIncidents_Status`
- `IX_SafetyIncidents_ReporterId`
- `IX_SafetyIncidents_AssignedTo` (legacy)
- `IX_SafetyIncidents_CoordinatorId_Status` (NEW - workload tracking)
- `IX_SafetyIncidents_Status_CoordinatorId` (NEW - partial, unassigned queue)
- `IX_SafetyIncidents_UpdatedAt` (NEW - partial, stale incidents)

**Foreign Keys**:
1. `ReporterId` → `AspNetUsers.Id`
   - **Cardinality**: Many-to-One (optional)
   - **Delete Behavior**: SET NULL
   - **Purpose**: Anonymous reports have NULL ReporterId

2. `CoordinatorId` → `AspNetUsers.Id` (NEW)
   - **Cardinality**: Many-to-One (optional)
   - **Delete Behavior**: SET NULL
   - **Purpose**: ANY user can be assigned as coordinator
   - **Business Rule**: One coordinator per incident, NULL = unassigned

3. `AssignedTo` → `AspNetUsers.Id` (legacy, deprecated)
   - **Cardinality**: Many-to-One (optional)
   - **Delete Behavior**: SET NULL
   - **Purpose**: Legacy field for backward compatibility

4. `CreatedBy` → `AspNetUsers.Id`
   - **Cardinality**: Many-to-One (optional)
   - **Delete Behavior**: SET NULL

5. `UpdatedBy` → `AspNetUsers.Id`
   - **Cardinality**: Many-to-One (optional)
   - **Delete Behavior**: SET NULL

**Relationships (Navigation Properties)**:
- `Reporter`: ApplicationUser (optional)
- `Coordinator`: ApplicationUser (optional) ← NEW
- `AssignedUser`: ApplicationUser (optional) - legacy
- `CreatedByUser`: ApplicationUser (optional)
- `UpdatedByUser`: ApplicationUser (optional)
- `Notes`: ICollection<IncidentNote> ← NEW
- `AuditLogs`: ICollection<IncidentAuditLog>
- `Notifications`: ICollection<IncidentNotification>

---

### 2. IncidentNote (NEW Entity)

**Purpose**: Investigation notes and system-generated activity logs for incidents.

**Table Name**: `IncidentNotes`

**Primary Key**: `Id` (UUID)

**Indexes**:
- `IX_IncidentNotes_IncidentId_CreatedAt` (composite, descending on CreatedAt)
- `IX_IncidentNotes_AuthorId` (partial, WHERE AuthorId IS NOT NULL)
- `IX_IncidentNotes_Type`

**Foreign Keys**:
1. `IncidentId` → `SafetyIncidents.Id`
   - **Cardinality**: Many-to-One (required)
   - **Delete Behavior**: CASCADE
   - **Purpose**: Notes belong to specific incident
   - **Business Rule**: When incident deleted, all notes deleted

2. `AuthorId` → `AspNetUsers.Id`
   - **Cardinality**: Many-to-One (optional)
   - **Delete Behavior**: SET NULL
   - **Purpose**: Manual notes have author, system notes have NULL
   - **Business Rule**: Preserve note even if author deleted

**Check Constraints**:
- `CHK_IncidentNotes_Type`: Type IN (1, 2)
- `CHK_IncidentNotes_Content_NotEmpty`: LENGTH(TRIM(Content)) > 0

**Relationships (Navigation Properties)**:
- `Incident`: SafetyIncident (required)
- `Author`: ApplicationUser (optional)

---

### 3. IncidentAuditLog (EXISTING - No Changes)

**Purpose**: Comprehensive audit trail for all incident-related actions.

**Table Name**: `IncidentAuditLog`

**Primary Key**: `Id` (UUID)

**Indexes**:
- `IX_IncidentAuditLog_IncidentId`
- `IX_IncidentAuditLog_UserId`
- `IX_IncidentAuditLog_CreatedAt`

**Foreign Keys**:
1. `IncidentId` → `SafetyIncidents.Id`
   - **Cardinality**: Many-to-One (required)
   - **Delete Behavior**: CASCADE
   - **Purpose**: Audit entries belong to specific incident

2. `UserId` → `AspNetUsers.Id`
   - **Cardinality**: Many-to-One (optional)
   - **Delete Behavior**: SET NULL
   - **Purpose**: System actions have NULL UserId

**Relationships (Navigation Properties)**:
- `Incident`: SafetyIncident (required)
- `User`: ApplicationUser (optional)

---

### 4. IncidentNotification (EXISTING - No Changes)

**Purpose**: Email notification tracking and delivery status.

**Table Name**: `IncidentNotification`

**Primary Key**: `Id` (UUID)

**Indexes**:
- `IX_IncidentNotification_IncidentId`
- `IX_IncidentNotification_Status`

**Foreign Keys**:
1. `IncidentId` → `SafetyIncidents.Id`
   - **Cardinality**: Many-to-One (required)
   - **Delete Behavior**: CASCADE
   - **Purpose**: Notifications belong to specific incident

**Relationships (Navigation Properties)**:
- `Incident`: SafetyIncident (required)

---

## Cascade Delete Behaviors

### Visualization

```
SafetyIncident (DELETE)
    │
    ├─→ IncidentNote (CASCADE DELETE)
    │   └─→ AuthorId (SET NULL if user deleted)
    │
    ├─→ IncidentAuditLog (CASCADE DELETE)
    │   └─→ UserId (SET NULL if user deleted)
    │
    └─→ IncidentNotification (CASCADE DELETE)

ApplicationUser (DELETE)
    │
    ├─→ SafetyIncident.ReporterId (SET NULL)
    ├─→ SafetyIncident.CoordinatorId (SET NULL)
    ├─→ SafetyIncident.AssignedTo (SET NULL)
    ├─→ IncidentNote.AuthorId (SET NULL)
    └─→ IncidentAuditLog.UserId (SET NULL)
```

### Business Impact

**When SafetyIncident is deleted**:
- ✅ All IncidentNotes are deleted (CASCADE)
- ✅ All IncidentAuditLogs are deleted (CASCADE)
- ✅ All IncidentNotifications are deleted (CASCADE)
- **Rationale**: Notes and audit logs have no meaning without the incident

**When ApplicationUser is deleted**:
- ✅ SafetyIncident.ReporterId is set to NULL (preserve incident)
- ✅ SafetyIncident.CoordinatorId is set to NULL (preserve incident)
- ✅ IncidentNote.AuthorId is set to NULL (preserve note, show "Deleted User")
- ✅ IncidentAuditLog.UserId is set to NULL (preserve audit trail)
- **Rationale**: Incidents and notes remain for audit/legal purposes

**Production Safeguard**:
- SafetyIncident deletion should be SOFT DELETE only
- Hard deletes should require admin override and legal approval
- Closed incidents archived after 7 years (legal retention requirement)

---

## Cardinality Summary

| Relationship | From Entity | To Entity | Cardinality | FK | Delete Behavior |
|--------------|-------------|-----------|-------------|-----|-----------------|
| Reporter | SafetyIncident | ApplicationUser | N:1 (optional) | ReporterId | SET NULL |
| Coordinator | SafetyIncident | ApplicationUser | N:1 (optional) | CoordinatorId | SET NULL |
| Incident Notes | IncidentNote | SafetyIncident | N:1 (required) | IncidentId | CASCADE |
| Note Author | IncidentNote | ApplicationUser | N:1 (optional) | AuthorId | SET NULL |
| Incident Audits | IncidentAuditLog | SafetyIncident | N:1 (required) | IncidentId | CASCADE |
| Audit User | IncidentAuditLog | ApplicationUser | N:1 (optional) | UserId | SET NULL |
| Incident Notifications | IncidentNotification | SafetyIncident | N:1 (required) | IncidentId | CASCADE |

**Legend**:
- N:1 = Many-to-One
- Optional = Foreign key can be NULL
- Required = Foreign key cannot be NULL
- CASCADE = Child records deleted when parent deleted
- SET NULL = Foreign key set to NULL when referenced record deleted

---

## Data Flow Diagrams

### Incident Lifecycle Flow

```
1. SUBMISSION
   User → SafetyIncident.Create
   └─→ Status = ReportSubmitted (1)
   └─→ ReporterId = UserId OR NULL (anonymous)
   └─→ CoordinatorId = NULL (unassigned)
   └─→ IncidentAuditLog.Create (ActionType = "Submitted")

2. ASSIGNMENT
   Admin → SafetyIncident.Update
   └─→ CoordinatorId = SelectedUserId
   └─→ Status = InformationGathering (2)
   └─→ IncidentNote.Create (Type = System, "Assigned to [coordinator]")
   └─→ IncidentAuditLog.Create (ActionType = "Assigned")
   └─→ IncidentNotification.Create (RecipientEmail = coordinator.Email)

3. INVESTIGATION
   Coordinator → IncidentNote.Create (multiple times)
   └─→ Type = Manual (1)
   └─→ AuthorId = CoordinatorId
   └─→ Content = Investigation details
   └─→ IncidentAuditLog.Create (ActionType = "NoteAdded")

4. STATUS TRANSITIONS
   Coordinator → SafetyIncident.Update (Status change)
   └─→ Status = ReviewingFinalReport (3)
   └─→ IncidentNote.Create (Type = System, "Status changed to ReviewingFinalReport")
   └─→ IncidentAuditLog.Create (ActionType = "StatusChanged", OldValues, NewValues)

5. ON HOLD (optional)
   Coordinator → SafetyIncident.Update
   └─→ Status = OnHold (4)
   └─→ IncidentNote.Create (Type = System, "HOLD: [reason]")
   └─→ IncidentAuditLog.Create (ActionType = "PlacedOnHold")

6. CLOSURE
   Coordinator → SafetyIncident.Update
   └─→ Status = Closed (5)
   └─→ GoogleDriveFinalReportUrl = [manual link]
   └─→ IncidentNote.Create (Type = System, "CLOSED: [summary]")
   └─→ IncidentAuditLog.Create (ActionType = "Closed")
   └─→ IncidentNotification.Create (if ReporterId not NULL)
```

### Note Creation Flow

```
MANUAL NOTE:
   Coordinator → IncidentNote.Create
   └─→ IncidentId = currentIncident.Id
   └─→ Content = user input (encrypted)
   └─→ Type = Manual (1)
   └─→ AuthorId = currentUserId
   └─→ IsPrivate = user selection
   └─→ CreatedAt = NOW()
   └─→ IncidentAuditLog.Create (ActionType = "NoteAdded")

SYSTEM NOTE (auto-created):
   System → IncidentNote.Create (on status change)
   └─→ IncidentId = incident.Id
   └─→ Content = "Status changed from X to Y by [user]"
   └─→ Type = System (2)
   └─→ AuthorId = NULL
   └─→ IsPrivate = false
   └─→ CreatedAt = NOW()
   └─→ IncidentAuditLog.Create (ActionType = "SystemNoteCreated")
```

---

## Query Patterns and Performance

### Common Query Patterns

#### Pattern 1: Load Incident with All Related Data
```csharp
var incident = await _context.SafetyIncidents
    .Include(i => i.Reporter)
    .Include(i => i.Coordinator)  // NEW
    .Include(i => i.Notes
        .OrderByDescending(n => n.CreatedAt))
    .ThenInclude(n => n.Author)
    .Include(i => i.AuditLogs
        .OrderByDescending(a => a.CreatedAt))
    .FirstOrDefaultAsync(i => i.Id == incidentId);
```

**Performance**:
- Uses: `PK_SafetyIncidents`, `FK_Notes_Incidents`, `FK_AuditLogs_Incidents`
- Expected: <300ms for typical incident (10-20 notes, 5-10 audit logs)

#### Pattern 2: Get Unassigned Incidents (Admin Dashboard)
```csharp
var unassigned = await _context.SafetyIncidents
    .Where(i => i.Status == IncidentStatus.ReportSubmitted
        && i.CoordinatorId == null)  // NEW field
    .OrderByDescending(i => i.ReportedAt)
    .ToListAsync();
```

**Performance**:
- Uses: `IX_SafetyIncidents_Status_CoordinatorId` (partial index)
- Expected: <100ms for 100 unassigned incidents

#### Pattern 3: Get Coordinator Workload
```csharp
var myIncidents = await _context.SafetyIncidents
    .Where(i => i.CoordinatorId == currentUserId  // NEW field
        && i.Status != IncidentStatus.Closed)
    .OrderByDescending(i => i.UpdatedAt)
    .ToListAsync();
```

**Performance**:
- Uses: `IX_SafetyIncidents_CoordinatorId_Status`
- Expected: <50ms for typical coordinator workload (5-10 active incidents)

#### Pattern 4: Get Notes for Incident
```csharp
var notes = await _context.IncidentNotes
    .Where(n => n.IncidentId == incidentId)
    .Include(n => n.Author)
    .OrderByDescending(n => n.CreatedAt)
    .ToListAsync();
```

**Performance**:
- Uses: `IX_IncidentNotes_IncidentId_CreatedAt`
- Expected: <10ms for 20 notes

---

## Database Constraints Summary

### SafetyIncident Constraints

**Primary Key**:
- `PK_SafetyIncidents` on `Id`

**Unique Constraints**:
- `UQ_SafetyIncidents_ReferenceNumber` on `ReferenceNumber`

**Foreign Key Constraints**:
- `FK_SafetyIncidents_Reporter_ReporterId` → AspNetUsers (SET NULL)
- `FK_SafetyIncidents_Coordinator_CoordinatorId` → AspNetUsers (SET NULL) ← NEW
- `FK_SafetyIncidents_AssignedUser_AssignedTo` → AspNetUsers (SET NULL)
- `FK_SafetyIncidents_CreatedBy` → AspNetUsers (SET NULL)
- `FK_SafetyIncidents_UpdatedBy` → AspNetUsers (SET NULL)

**Check Constraints**:
- `CHK_SafetyIncidents_Severity` (implicit enum: 1-4)
- `CHK_SafetyIncidents_Status` (implicit enum: 1-5) ← UPDATED

### IncidentNote Constraints (NEW)

**Primary Key**:
- `PK_IncidentNotes` on `Id`

**Foreign Key Constraints**:
- `FK_IncidentNotes_Incidents_IncidentId` → SafetyIncidents (CASCADE)
- `FK_IncidentNotes_Users_AuthorId` → AspNetUsers (SET NULL)

**Check Constraints**:
- `CHK_IncidentNotes_Type`: `Type IN (1, 2)`
- `CHK_IncidentNotes_Content_NotEmpty`: `LENGTH(TRIM(Content)) > 0`

### IncidentAuditLog Constraints (EXISTING)

**Primary Key**:
- `PK_IncidentAuditLog` on `Id`

**Foreign Key Constraints**:
- `FK_IncidentAuditLog_Incidents_IncidentId` → SafetyIncidents (CASCADE)
- `FK_IncidentAuditLog_Users_UserId` → AspNetUsers (SET NULL)

### IncidentNotification Constraints (EXISTING)

**Primary Key**:
- `PK_IncidentNotification` on `Id`

**Foreign Key Constraints**:
- `FK_IncidentNotification_Incidents_IncidentId` → SafetyIncidents (CASCADE)

---

## Visual ERD Legend

### Symbols Used

```
┌──────┐
│ PK   │ = Primary Key
│ FK   │ = Foreign Key
│ UQ   │ = Unique Constraint
│ IX   │ = Index
│ CHK  │ = Check Constraint
└──────┘

Relationships:
───────► = One-to-Many (1:N)
◄─────── = Many-to-One (N:1)
◄──────► = One-to-One (1:1)

Delete Behaviors:
CASCADE    = Delete child when parent deleted
SET NULL   = Set FK to NULL when referenced record deleted
RESTRICT   = Prevent parent deletion if children exist
NO ACTION  = Database error if children exist
```

### Entity Box Structure

```
┌────────────────────────────┐
│     Table Name             │ ← Header
├────────────────────────────┤
│ PrimaryKey: TYPE (PK)      │ ← Primary Key
│ ForeignKey: TYPE (FK → T)  │ ← Foreign Keys
│ ─────────────────────      │ ← Separator
│ Column1: TYPE              │ ← Regular Columns
│ Column2: TYPE NULL         │ ← Nullable Columns
│ Column3: TYPE ← NEW        │ ← New Fields
└────────────────────────────┘
```

---

## Appendix: Complete SQL DDL

### Create All Tables (From Scratch)

```sql
-- ========================================
-- COMPLETE DATABASE SCHEMA
-- Incident Reporting System
-- ========================================

-- SafetyIncident table (with updates)
CREATE TABLE "SafetyIncidents" (
    "Id" UUID NOT NULL DEFAULT gen_random_uuid(),
    "ReferenceNumber" VARCHAR(30) NOT NULL,
    "ReporterId" UUID NULL,
    "Severity" INTEGER NOT NULL DEFAULT 2,
    "IncidentDate" TIMESTAMPTZ NOT NULL,
    "ReportedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "Location" VARCHAR(200) NOT NULL,
    "EncryptedDescription" TEXT NOT NULL,
    "EncryptedInvolvedParties" TEXT NULL,
    "EncryptedWitnesses" TEXT NULL,
    "EncryptedContactEmail" TEXT NULL,
    "EncryptedContactPhone" TEXT NULL,
    "IsAnonymous" BOOLEAN NOT NULL DEFAULT FALSE,
    "RequestFollowUp" BOOLEAN NOT NULL DEFAULT FALSE,
    "Status" INTEGER NOT NULL DEFAULT 1,
    "AssignedTo" UUID NULL,  -- Legacy
    "CoordinatorId" UUID NULL,  -- NEW
    "GoogleDriveFolderUrl" VARCHAR(500) NULL,  -- NEW
    "GoogleDriveFinalReportUrl" VARCHAR(500) NULL,  -- NEW
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy" UUID NULL,
    "UpdatedBy" UUID NULL,

    CONSTRAINT "PK_SafetyIncidents" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_SafetyIncidents_ReferenceNumber" UNIQUE ("ReferenceNumber"),
    CONSTRAINT "FK_SafetyIncidents_Reporter_ReporterId"
        FOREIGN KEY ("ReporterId") REFERENCES "AspNetUsers"("Id")
        ON DELETE SET NULL,
    CONSTRAINT "FK_SafetyIncidents_Coordinator_CoordinatorId"
        FOREIGN KEY ("CoordinatorId") REFERENCES "AspNetUsers"("Id")
        ON DELETE SET NULL,
    CONSTRAINT "FK_SafetyIncidents_AssignedUser_AssignedTo"
        FOREIGN KEY ("AssignedTo") REFERENCES "AspNetUsers"("Id")
        ON DELETE SET NULL
);

-- IncidentNote table (NEW)
CREATE TABLE "IncidentNotes" (
    "Id" UUID NOT NULL DEFAULT gen_random_uuid(),
    "IncidentId" UUID NOT NULL,
    "Content" TEXT NOT NULL,
    "Type" INTEGER NOT NULL DEFAULT 1,
    "IsPrivate" BOOLEAN NOT NULL DEFAULT FALSE,
    "AuthorId" UUID NULL,
    "Tags" VARCHAR(200) NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NULL,

    CONSTRAINT "PK_IncidentNotes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_IncidentNotes_Incidents_IncidentId"
        FOREIGN KEY ("IncidentId") REFERENCES "SafetyIncidents"("Id")
        ON DELETE CASCADE,
    CONSTRAINT "FK_IncidentNotes_Users_AuthorId"
        FOREIGN KEY ("AuthorId") REFERENCES "AspNetUsers"("Id")
        ON DELETE SET NULL,
    CONSTRAINT "CHK_IncidentNotes_Type"
        CHECK ("Type" IN (1, 2)),
    CONSTRAINT "CHK_IncidentNotes_Content_NotEmpty"
        CHECK (LENGTH(TRIM("Content")) > 0)
);

-- Create all indexes
CREATE INDEX "IX_SafetyIncidents_ReferenceNumber" ON "SafetyIncidents"("ReferenceNumber");
CREATE INDEX "IX_SafetyIncidents_Status" ON "SafetyIncidents"("Status");
CREATE INDEX "IX_SafetyIncidents_ReporterId" ON "SafetyIncidents"("ReporterId");
CREATE INDEX "IX_SafetyIncidents_AssignedTo" ON "SafetyIncidents"("AssignedTo");
CREATE INDEX "IX_SafetyIncidents_CoordinatorId_Status" ON "SafetyIncidents"("CoordinatorId", "Status");
CREATE INDEX "IX_SafetyIncidents_Status_CoordinatorId" ON "SafetyIncidents"("Status", "CoordinatorId")
    WHERE "CoordinatorId" IS NULL;
CREATE INDEX "IX_SafetyIncidents_UpdatedAt" ON "SafetyIncidents"("UpdatedAt" DESC)
    WHERE "Status" IN (1, 2, 3, 4);

CREATE INDEX "IX_IncidentNotes_IncidentId_CreatedAt" ON "IncidentNotes"("IncidentId", "CreatedAt" DESC);
CREATE INDEX "IX_IncidentNotes_AuthorId" ON "IncidentNotes"("AuthorId")
    WHERE "AuthorId" IS NOT NULL;
CREATE INDEX "IX_IncidentNotes_Type" ON "IncidentNotes"("Type");

-- Add table comments
COMMENT ON TABLE "SafetyIncidents" IS 'Safety incident tracking with 5-stage workflow and coordinator assignment';
COMMENT ON TABLE "IncidentNotes" IS 'Investigation notes mirroring vetting ApplicationNoteDto pattern';

-- Add column comments
COMMENT ON COLUMN "SafetyIncidents"."CoordinatorId" IS 'Assigned coordinator - ANY user can be assigned';
COMMENT ON COLUMN "SafetyIncidents"."GoogleDriveFolderUrl" IS 'Phase 1: Manual link. Phase 2: Automated';
COMMENT ON COLUMN "SafetyIncidents"."GoogleDriveFinalReportUrl" IS 'Phase 1: Manual link. Phase 2: Automated';
```

---

**Created**: 2025-10-18
**Author**: Database Designer Agent
**Version**: 1.0
**Status**: Ready for Backend Developer Review
