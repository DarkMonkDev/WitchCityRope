# Backend Integration Guide - Incident Reporting System
<!-- Last Updated: 2025-10-18 -->
<!-- Version: 1.0 -->
<!-- Owner: Backend Developer -->
<!-- Status: Implementation Guide - All Endpoints Pending -->

---

## 🎯 PURPOSE

This document provides complete specifications for all backend API endpoints required to integrate with the Incident Reporting System frontend. **All 12 endpoints are currently NOT IMPLEMENTED** and require backend development.

**Frontend Status**: ✅ Complete (19 components, 239+ tests)
**Backend Status**: 🔄 Schema complete, API endpoints pending
**Integration Status**: ⏸️ Blocked on backend implementation

---

## 📋 ENDPOINT SUMMARY

| Endpoint | Method | Purpose | Status |
|----------|--------|---------|--------|
| `/api/safety/incidents` | GET | List/filter incidents | ⏸️ Not Implemented |
| `/api/safety/incidents` | POST | Create incident | ⏸️ Not Implemented |
| `/api/safety/incidents/{id}` | GET | Get incident detail | ⏸️ Not Implemented |
| `/api/safety/incidents/{id}/status` | PUT | Update status | ⏸️ Not Implemented |
| `/api/safety/incidents/{id}/coordinator` | PUT | Assign coordinator | ⏸️ Not Implemented |
| `/api/safety/incidents/{id}/drive-links` | PUT | Update Drive links | ⏸️ Not Implemented |
| `/api/safety/incidents/{id}/notes` | GET | Get notes | ⏸️ Not Implemented |
| `/api/safety/incidents/{id}/notes` | POST | Add manual note | ⏸️ Not Implemented |
| `/api/safety/notes/{id}` | PUT | Edit manual note | ⏸️ Not Implemented |
| `/api/safety/notes/{id}` | DELETE | Delete manual note | ⏸️ Not Implemented |
| `/api/safety/my-reports` | GET | User's own reports | ⏸️ Not Implemented |
| `/api/safety/my-reports/{id}` | GET | User's report detail | ⏸️ Not Implemented |

**Total**: 12 endpoints (0 implemented)

---

## 🔐 AUTHENTICATION & AUTHORIZATION

### Authentication Method

**BFF Pattern**: httpOnly cookie-based authentication (already implemented in project)

**Frontend**: Uses existing auth context and httpOnly cookies
**Backend**: Requires auth middleware to extract userId from cookie

**Sample Code** (if not already present):
```csharp
// Get current user ID from auth context
var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
if (string.IsNullOrEmpty(currentUserId))
{
    return Unauthorized();
}
```

### Authorization Matrix

| Endpoint | Admin | Coordinator (Assigned) | User (Owner) | Anonymous |
|----------|-------|------------------------|--------------|-----------|
| GET /api/safety/incidents | ✅ Full access | ✅ Assigned only | ❌ Forbidden | ❌ Forbidden |
| GET /api/safety/incidents/{id} | ✅ Any incident | ✅ If assigned | ❌ Forbidden | ❌ Forbidden |
| POST /api/safety/incidents | ✅ Allowed | ✅ Allowed | ✅ Allowed | ✅ Allowed (anonymous) |
| PUT /api/safety/incidents/{id}/status | ✅ Any incident | ✅ If assigned | ❌ Forbidden | ❌ Forbidden |
| PUT /api/safety/incidents/{id}/coordinator | ✅ Allowed | ❌ Forbidden | ❌ Forbidden | ❌ Forbidden |
| PUT /api/safety/incidents/{id}/drive-links | ✅ Any incident | ✅ If assigned | ❌ Forbidden | ❌ Forbidden |
| GET /api/safety/incidents/{id}/notes | ✅ All notes | ✅ If assigned | ❌ Forbidden | ❌ Forbidden |
| POST /api/safety/incidents/{id}/notes | ✅ Any incident | ✅ If assigned | ❌ Forbidden | ❌ Forbidden |
| PUT /api/safety/notes/{id} | ✅ Any note | ✅ If author | ❌ Forbidden | ❌ Forbidden |
| DELETE /api/safety/notes/{id} | ✅ Any note | ✅ If author | ❌ Forbidden | ❌ Forbidden |
| GET /api/safety/my-reports | N/A | N/A | ✅ Own reports | ❌ Forbidden |
| GET /api/safety/my-reports/{id} | N/A | N/A | ✅ If owner | ❌ Forbidden |

### Authorization Helper Methods

**Suggested Implementation**:
```csharp
public class IncidentAuthorizationService
{
    public bool CanAccessIncident(SafetyIncident incident, string userId, bool isAdmin)
    {
        // Admin can access any incident
        if (isAdmin) return true;

        // Coordinator can access assigned incidents
        if (incident.CoordinatorId == Guid.Parse(userId)) return true;

        return false;
    }

    public bool CanModifyIncident(SafetyIncident incident, string userId, bool isAdmin)
    {
        // Only admin or assigned coordinator can modify
        return isAdmin || incident.CoordinatorId == Guid.Parse(userId);
    }

    public bool IsOwner(SafetyIncident incident, string userId)
    {
        // User owns the incident if they are the reporter
        return incident.ReporterId == userId && !incident.IsAnonymous;
    }
}
```

---

## 📡 ENDPOINT SPECIFICATIONS

### 1. GET /api/safety/incidents

**Purpose**: List all incidents with filtering, sorting, and pagination

**Authentication**: Required (admin or coordinator)

**Authorization**:
- Admins: See all incidents
- Coordinators: See only assigned incidents

**Query Parameters**:
```typescript
{
  status?: IncidentStatus;           // Filter by status
  severity?: 'Low' | 'Medium' | 'High' | 'Critical';  // Filter by severity
  coordinatorId?: string;            // Filter by coordinator (UUID)
  unassigned?: boolean;              // Filter unassigned incidents (coordinatorId IS NULL)
  startDate?: string;                // Filter incidents after date (ISO 8601)
  endDate?: string;                  // Filter incidents before date (ISO 8601)
  page?: number;                     // Page number (default: 1)
  pageSize?: number;                 // Items per page (default: 20)
  sortBy?: string;                   // Sort column (default: 'reportedAt')
  sortOrder?: 'asc' | 'desc';        // Sort direction (default: 'desc')
}
```

**Response Schema** (200 OK):
```typescript
{
  incidents: SafetyIncidentDto[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
}
```

**SafetyIncidentDto**:
```typescript
{
  id: string;
  referenceNumber: string;           // SAF-YYYYMMDD-NNNN
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  status: IncidentStatus;            // ReportSubmitted, InformationGathering, etc.
  incidentDate: string;              // ISO 8601
  reportedAt: string;                // ISO 8601
  lastUpdatedAt: string;             // ISO 8601
  location: string;
  description: string;
  isAnonymous: boolean;
  reporterId?: string;               // NULL if anonymous
  reporterName?: string;             // NULL if anonymous
  coordinatorId?: string;            // NULL if unassigned
  coordinatorName?: string;          // NULL if unassigned
  involvedParties?: string;
  witnesses?: string;
  googleDriveFolderUrl?: string;
  googleDriveFinalReportUrl?: string;
  noteCount: number;                 // Total notes on incident
}
```

**Error Responses**:
- `401 Unauthorized`: Not authenticated
- `403 Forbidden`: Not admin or coordinator
- `400 Bad Request`: Invalid query parameters

**Implementation Notes**:
- Coordinators should only see `WHERE CoordinatorId = currentUserId` OR `WHERE CoordinatorId IS NULL` (can see unassigned queue)
- Default sort: `ORDER BY ReportedAt DESC`
- Use EF Core `.Include()` to eager load Coordinator navigation property for coordinatorName
- Use EF Core `.Select()` to count notes: `noteCount = incident.Notes.Count()`

**Sample Query** (C#):
```csharp
var query = _context.SafetyIncidents
    .Include(i => i.Coordinator)
    .Include(i => i.Reporter)
    .AsQueryable();

// Apply filters
if (status.HasValue)
    query = query.Where(i => i.Status == status.Value);

if (severity.HasValue)
    query = query.Where(i => i.Severity == severity.Value);

if (!isAdmin)
    query = query.Where(i => i.CoordinatorId == currentUserId || i.CoordinatorId == null);

// Pagination
var totalCount = await query.CountAsync();
var incidents = await query
    .OrderByDescending(i => i.ReportedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(i => new SafetyIncidentDto
    {
        // ... map properties
        NoteCount = i.Notes.Count()
    })
    .ToListAsync();
```

---

### 2. POST /api/safety/incidents

**Purpose**: Create new incident report (public form submission)

**Authentication**: Optional (can be anonymous)

**Authorization**: Public endpoint (anyone can submit)

**Request Schema**:
```typescript
{
  severity: 'Low' | 'Medium' | 'High' | 'Critical';  // REQUIRED
  incidentDate: string;              // ISO 8601, REQUIRED
  location: string;                  // REQUIRED, min 3 chars
  description: string;               // REQUIRED, min 10 chars
  isAnonymous: boolean;              // REQUIRED, default false
  reporterId?: string;               // NULL if anonymous, auto-set from auth if identified
  involvedParties?: string;          // Optional
  witnesses?: string;                // Optional
}
```

**Response Schema** (201 Created):
```typescript
{
  id: string;
  referenceNumber: string;           // Generated: SAF-YYYYMMDD-NNNN
  status: 'ReportSubmitted';         // Always initial status
  reportedAt: string;                // ISO 8601, server timestamp
  // ... other fields from SafetyIncidentDto
}
```

**Response Schema** (200 OK - Anonymous):
```typescript
{
  message: "Your report has been submitted. Thank you for reporting this incident.";
  // NO reference number or ID returned for anonymous submissions
}
```

**Error Responses**:
- `400 Bad Request`: Validation errors (missing required fields, invalid dates)
- `422 Unprocessable Entity`: Business rule violations

**Implementation Notes**:
- Generate reference number: `SAF-{IncidentDate:yyyyMMdd}-{SequenceNumber:D4}`
- Sequence number resets daily (use COUNT of same-day incidents + 1)
- Set initial status: `IncidentStatus.ReportSubmitted`
- Set reportedAt: `DateTimeOffset.UtcNow`
- If anonymous: `ReporterId = NULL`, `IsAnonymous = true`
- If identified: `ReporterId = currentUserId`, `IsAnonymous = false`
- Create initial system note: "Incident report submitted"
- **CRITICAL**: For anonymous submissions, do NOT return reference number or ID in response (privacy)

**Reference Number Generation** (C#):
```csharp
var incidentDate = request.IncidentDate.Date;
var dailyCount = await _context.SafetyIncidents
    .Where(i => i.IncidentDate.Date == incidentDate)
    .CountAsync();

var sequenceNumber = dailyCount + 1;
var referenceNumber = $"SAF-{incidentDate:yyyyMMdd}-{sequenceNumber:D4}";
```

**System Note Creation**:
```csharp
var systemNote = new IncidentNote
{
    IncidentId = incident.Id,
    Content = "Incident report submitted.",
    Type = IncidentNoteType.System,
    IsPrivate = false,
    AuthorId = null  // System notes have no author
};
await _context.IncidentNotes.AddAsync(systemNote);
```

---

### 3. GET /api/safety/incidents/{id}

**Purpose**: Get single incident detail (admin/coordinator full view)

**Authentication**: Required (admin or coordinator)

**Authorization**:
- Admins: Can view any incident
- Coordinators: Can only view assigned incidents

**Path Parameters**:
- `id` (string, UUID): Incident ID

**Response Schema** (200 OK):
```typescript
SafetyIncidentDto & {
  notes: IncidentNoteDto[];          // All notes, ordered by CreatedAt DESC
}
```

**IncidentNoteDto**:
```typescript
{
  id: string;
  incidentId: string;
  content: string;                   // Decrypted
  type: 'Manual' | 'System';         // IncidentNoteType enum
  isPrivate: boolean;
  authorId?: string;                 // NULL for system notes
  authorName?: string;               // NULL for system notes
  tags?: string;                     // Comma-separated
  createdAt: string;                 // ISO 8601
  updatedAt?: string;                // ISO 8601, NULL if never edited
}
```

**Error Responses**:
- `401 Unauthorized`: Not authenticated
- `403 Forbidden`: Not admin or assigned coordinator
- `404 Not Found`: Incident doesn't exist

**Implementation Notes**:
- Use `.Include(i => i.Notes).ThenInclude(n => n.Author)` to eager load notes
- Order notes: `notes.OrderByDescending(n => n.CreatedAt)`
- Decrypt note content before returning (if using encryption)
- Authorization check BEFORE returning data

**Authorization Check** (C#):
```csharp
var incident = await _context.SafetyIncidents
    .Include(i => i.Notes)
        .ThenInclude(n => n.Author)
    .Include(i => i.Coordinator)
    .FirstOrDefaultAsync(i => i.Id == id);

if (incident == null)
    return NotFound();

// Authorization
if (!isAdmin && incident.CoordinatorId != currentUserId)
    return Forbid();

return Ok(MapToDto(incident));
```

---

### 4. PUT /api/safety/incidents/{id}/status

**Purpose**: Update incident status (stage transition)

**Authentication**: Required (admin or assigned coordinator)

**Authorization**:
- Admins: Can update any incident
- Coordinators: Can only update assigned incidents

**Path Parameters**:
- `id` (string, UUID): Incident ID

**Request Schema**:
```typescript
{
  newStatus: IncidentStatus;         // REQUIRED, one of 5 enum values
  reason?: string;                   // Optional, recommended for OnHold
}
```

**Response Schema** (200 OK):
```typescript
{
  id: string;
  status: IncidentStatus;            // Updated status
  lastUpdatedAt: string;             // Updated timestamp
  systemNoteCreated: boolean;        // Always true (note generated)
}
```

**Error Responses**:
- `401 Unauthorized`: Not authenticated
- `403 Forbidden`: Not admin or assigned coordinator
- `404 Not Found`: Incident doesn't exist
- `400 Bad Request`: Invalid status transition

**Implementation Notes**:
- Validate status transition (prevent skipping stages if business rules require sequence)
- Update `LastUpdatedAt` to `DateTimeOffset.UtcNow`
- Create system note documenting status change
- If transitioning to `OnHold`, require reason in note
- Frontend shows StageGuidanceModal BEFORE calling this endpoint (guidance only, not enforced)

**System Note Content** (C#):
```csharp
var oldStatus = incident.Status;
var newStatus = request.NewStatus;

var noteContent = $"Status changed from {oldStatus} to {newStatus}.";
if (!string.IsNullOrEmpty(request.Reason))
{
    noteContent += $" Reason: {request.Reason}";
}

var systemNote = new IncidentNote
{
    IncidentId = incident.Id,
    Content = noteContent,
    Type = IncidentNoteType.System,
    IsPrivate = false,
    AuthorId = currentUserId  // Track who made the change
};
```

---

### 5. PUT /api/safety/incidents/{id}/coordinator

**Purpose**: Assign or unassign coordinator to incident

**Authentication**: Required (admin ONLY)

**Authorization**: Admin role required

**Path Parameters**:
- `id` (string, UUID): Incident ID

**Request Schema**:
```typescript
{
  coordinatorId?: string;            // UUID or NULL to unassign
}
```

**Response Schema** (200 OK):
```typescript
{
  id: string;
  coordinatorId?: string;            // Updated coordinator ID or NULL
  coordinatorName?: string;          // Coordinator name or NULL
  status: IncidentStatus;            // Auto-updated to InformationGathering if assigned
  lastUpdatedAt: string;             // Updated timestamp
  systemNoteCreated: boolean;        // Always true
}
```

**Error Responses**:
- `401 Unauthorized`: Not authenticated
- `403 Forbidden`: Not admin
- `404 Not Found`: Incident or coordinator doesn't exist
- `400 Bad Request`: Invalid coordinator ID

**Implementation Notes**:
- **CRITICAL**: coordinatorId can be ANY user (not just admins) - per stakeholder requirement
- If assigning (coordinatorId != NULL):
  - Auto-transition status to `InformationGathering` (if currently `ReportSubmitted`)
  - Create system note: "Coordinator assigned: {coordinatorName}"
- If unassigning (coordinatorId == NULL):
  - Status reverts to `ReportSubmitted` if currently `InformationGathering`
  - Create system note: "Coordinator unassigned."
- Validate coordinator exists: `_context.Users.AnyAsync(u => u.Id == coordinatorId)`

**Assignment Logic** (C#):
```csharp
if (request.CoordinatorId.HasValue)
{
    // Assign
    var coordinator = await _context.Users.FindAsync(request.CoordinatorId.Value);
    if (coordinator == null)
        return NotFound("Coordinator not found.");

    incident.CoordinatorId = coordinator.Id;

    // Auto-transition status
    if (incident.Status == IncidentStatus.ReportSubmitted)
    {
        incident.Status = IncidentStatus.InformationGathering;
    }

    // System note
    var systemNote = new IncidentNote
    {
        Content = $"Coordinator assigned: {coordinator.FirstName} {coordinator.LastName}",
        Type = IncidentNoteType.System,
        IsPrivate = false,
        AuthorId = currentUserId
    };
}
else
{
    // Unassign
    incident.CoordinatorId = null;

    // Revert status if appropriate
    if (incident.Status == IncidentStatus.InformationGathering)
    {
        incident.Status = IncidentStatus.ReportSubmitted;
    }

    // System note
    var systemNote = new IncidentNote
    {
        Content = "Coordinator unassigned.",
        Type = IncidentNoteType.System,
        IsPrivate = false,
        AuthorId = currentUserId
    };
}
```

---

### 6. PUT /api/safety/incidents/{id}/drive-links

**Purpose**: Update Google Drive links (Phase 1 manual entry)

**Authentication**: Required (admin or assigned coordinator)

**Authorization**:
- Admins: Can update any incident
- Coordinators: Can only update assigned incidents

**Path Parameters**:
- `id` (string, UUID): Incident ID

**Request Schema**:
```typescript
{
  googleDriveFolderUrl?: string;     // Optional, URL or NULL
  googleDriveFinalReportUrl?: string;  // Optional, URL or NULL
}
```

**Response Schema** (200 OK):
```typescript
{
  id: string;
  googleDriveFolderUrl?: string;     // Updated URL or NULL
  googleDriveFinalReportUrl?: string;  // Updated URL or NULL
  lastUpdatedAt: string;             // Updated timestamp
  systemNoteCreated: boolean;        // Always true
}
```

**Error Responses**:
- `401 Unauthorized`: Not authenticated
- `403 Forbidden`: Not admin or assigned coordinator
- `404 Not Found`: Incident doesn't exist
- `400 Bad Request`: Invalid URL format

**Implementation Notes**:
- Validate URL format (basic check, not full validation)
- Allow setting to NULL (clearing link)
- Create system note documenting change
- No Google Drive API integration in Phase 1 (manual links only)

**System Note Content** (C#):
```csharp
var changes = new List<string>();

if (request.GoogleDriveFolderUrl != incident.GoogleDriveFolderUrl)
{
    changes.Add("Google Drive folder link updated.");
}

if (request.GoogleDriveFinalReportUrl != incident.GoogleDriveFinalReportUrl)
{
    changes.Add("Final report link updated.");
}

if (changes.Any())
{
    var systemNote = new IncidentNote
    {
        Content = string.Join(" ", changes),
        Type = IncidentNoteType.System,
        IsPrivate = false,
        AuthorId = currentUserId
    };
}
```

---

### 7. GET /api/safety/incidents/{id}/notes

**Purpose**: Get all notes for an incident

**Authentication**: Required (admin or assigned coordinator)

**Authorization**:
- Admins: Can view all notes
- Coordinators: Can only view notes for assigned incidents

**Path Parameters**:
- `id` (string, UUID): Incident ID

**Query Parameters**:
```typescript
{
  type?: 'Manual' | 'System';        // Filter by note type
  includePrivate?: boolean;          // Default: true (admin/coordinator can see private)
}
```

**Response Schema** (200 OK):
```typescript
{
  notes: IncidentNoteDto[];          // Ordered by CreatedAt DESC
}
```

**Error Responses**:
- `401 Unauthorized`: Not authenticated
- `403 Forbidden`: Not admin or assigned coordinator
- `404 Not Found`: Incident doesn't exist

**Implementation Notes**:
- Order: `ORDER BY CreatedAt DESC` (newest first)
- Decrypt content before returning
- Include author name for manual notes
- System notes have `authorId = NULL`

---

### 8. POST /api/safety/incidents/{id}/notes

**Purpose**: Add manual note to incident

**Authentication**: Required (admin or assigned coordinator)

**Authorization**:
- Admins: Can add notes to any incident
- Coordinators: Can only add notes to assigned incidents

**Path Parameters**:
- `id` (string, UUID): Incident ID

**Request Schema**:
```typescript
{
  content: string;                   // REQUIRED, min 3 chars
  isPrivate: boolean;                // REQUIRED, default false
  tags?: string;                     // Optional, comma-separated
}
```

**Response Schema** (201 Created):
```typescript
IncidentNoteDto                      // Newly created note
```

**Error Responses**:
- `401 Unauthorized`: Not authenticated
- `403 Forbidden`: Not admin or assigned coordinator
- `404 Not Found`: Incident doesn't exist
- `400 Bad Request`: Content validation failed

**Implementation Notes**:
- Set `Type = IncidentNoteType.Manual`
- Set `AuthorId = currentUserId`
- Encrypt content before storing (if using encryption)
- Validate content length (min 3 chars, max 10,000 chars recommended)
- Update incident `LastUpdatedAt` timestamp

---

### 9. PUT /api/safety/notes/{id}

**Purpose**: Edit existing manual note

**Authentication**: Required (admin or note author)

**Authorization**:
- Admins: Can edit any manual note
- Coordinators: Can only edit their own notes
- System notes: CANNOT be edited

**Path Parameters**:
- `id` (string, UUID): Note ID

**Request Schema**:
```typescript
{
  content: string;                   // REQUIRED, min 3 chars
  isPrivate: boolean;                // REQUIRED
  tags?: string;                     // Optional
}
```

**Response Schema** (200 OK):
```typescript
IncidentNoteDto                      // Updated note with new UpdatedAt timestamp
```

**Error Responses**:
- `401 Unauthorized`: Not authenticated
- `403 Forbidden`: Not admin or note author, or attempting to edit system note
- `404 Not Found`: Note doesn't exist
- `400 Bad Request`: Content validation failed

**Implementation Notes**:
- CANNOT edit system notes: `if (note.Type == IncidentNoteType.System) return Forbid()`
- Authorization: `if (!isAdmin && note.AuthorId != currentUserId) return Forbid()`
- Set `UpdatedAt = DateTimeOffset.UtcNow`
- Update incident `LastUpdatedAt` timestamp

---

### 10. DELETE /api/safety/notes/{id}

**Purpose**: Delete manual note (soft delete recommended)

**Authentication**: Required (admin or note author)

**Authorization**:
- Admins: Can delete any manual note
- Coordinators: Can only delete their own notes
- System notes: CANNOT be deleted

**Path Parameters**:
- `id` (string, UUID): Note ID

**Response Schema** (204 No Content):
```
(Empty body)
```

**Error Responses**:
- `401 Unauthorized`: Not authenticated
- `403 Forbidden`: Not admin or note author, or attempting to delete system note
- `404 Not Found`: Note doesn't exist

**Implementation Notes**:
- CANNOT delete system notes: `if (note.Type == IncidentNoteType.System) return Forbid()`
- Authorization: `if (!isAdmin && note.AuthorId != currentUserId) return Forbid()`
- Recommended: Soft delete (add `IsDeleted` flag, filter in queries)
- Alternative: Hard delete (actual DB deletion)
- Update incident `LastUpdatedAt` timestamp

---

### 11. GET /api/safety/my-reports

**Purpose**: Get user's own identified incident reports

**Authentication**: Required (user must be authenticated)

**Authorization**: User can only see their own reports

**Query Parameters**:
```typescript
{
  page?: number;                     // Page number (default: 1)
  pageSize?: number;                 // Items per page (default: 20)
}
```

**Response Schema** (200 OK):
```typescript
{
  reports: MyReportSummaryDto[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
}
```

**MyReportSummaryDto** (Limited Fields):
```typescript
{
  id: string;
  incidentDate: string;              // ISO 8601
  location: string;
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  status: IncidentStatus;
  reportedAt: string;                // ISO 8601
  lastUpdatedAt: string;             // ISO 8601
  // NOTE: NO reference number, NO coordinator info (privacy)
}
```

**Error Responses**:
- `401 Unauthorized`: Not authenticated

**Implementation Notes**:
- **CRITICAL**: Filter `WHERE ReporterId = currentUserId AND IsAnonymous = false`
- **CRITICAL**: Do NOT return reference number (privacy restriction)
- **CRITICAL**: Do NOT return coordinator information (privacy restriction)
- Order: `ORDER BY LastUpdatedAt DESC`
- Pagination required

**Query Sample** (C#):
```csharp
var reports = await _context.SafetyIncidents
    .Where(i => i.ReporterId == currentUserId && !i.IsAnonymous)
    .OrderByDescending(i => i.LastUpdatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(i => new MyReportSummaryDto
    {
        Id = i.Id,
        IncidentDate = i.IncidentDate,
        Location = i.Location,
        Severity = i.Severity,
        Status = i.Status,
        ReportedAt = i.ReportedAt,
        LastUpdatedAt = i.LastUpdatedAt
        // NO ReferenceNumber
        // NO CoordinatorId/CoordinatorName
    })
    .ToListAsync();
```

---

### 12. GET /api/safety/my-reports/{id}

**Purpose**: Get user's own report detail (limited view)

**Authentication**: Required (user must be authenticated)

**Authorization**: User must be the report owner

**Path Parameters**:
- `id` (string, UUID): Incident ID

**Response Schema** (200 OK):
```typescript
{
  id: string;
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  status: IncidentStatus;
  incidentDate: string;              // ISO 8601
  reportedAt: string;                // ISO 8601
  lastUpdatedAt: string;             // ISO 8601
  location: string;
  description: string;               // What user submitted
  involvedParties?: string;          // What user submitted
  witnesses?: string;                // What user submitted
  isAnonymous: boolean;              // Should always be false for this endpoint
  // NOTE: Excludes referenceNumber, coordinatorId, notes, Drive links
}
```

**Error Responses**:
- `401 Unauthorized`: Not authenticated
- `403 Forbidden`: Not the report owner
- `404 Not Found`: Report doesn't exist or is anonymous

**Implementation Notes**:
- **CRITICAL**: Verify ownership `WHERE ReporterId = currentUserId AND IsAnonymous = false`
- **CRITICAL**: Return 403 Forbidden if user is not owner
- **CRITICAL**: Return 404 if report is anonymous (cannot view own anonymous reports)
- **CRITICAL**: Do NOT return reference number, coordinator, notes, or Drive links
- This is a LIMITED view - users only see what they submitted

**Authorization Check** (C#):
```csharp
var incident = await _context.SafetyIncidents
    .FirstOrDefaultAsync(i => i.Id == id);

if (incident == null)
    return NotFound();

// Authorization: Must be owner and NOT anonymous
if (incident.ReporterId != currentUserId || incident.IsAnonymous)
    return Forbid();

// Map to limited DTO (exclude admin fields)
return Ok(new MyReportDetailDto
{
    // ... LIMITED fields only
});
```

---

## 🔧 SYSTEM NOTE GENERATION REQUIREMENTS

### When to Generate System Notes

**Automatic system notes required for**:
1. Incident created (POST /api/safety/incidents)
2. Status changed (PUT /api/safety/incidents/{id}/status)
3. Coordinator assigned (PUT /api/safety/incidents/{id}/coordinator)
4. Coordinator unassigned (PUT /api/safety/incidents/{id}/coordinator)
5. Google Drive links updated (PUT /api/safety/incidents/{id}/drive-links)
6. Data migration performed (manual script)

### System Note Format

```csharp
public class IncidentNote
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public string Content { get; set; }  // Human-readable message
    public IncidentNoteType Type { get; set; } = IncidentNoteType.System;
    public bool IsPrivate { get; set; } = false;  // System notes are public
    public Guid? AuthorId { get; set; }  // NULL for true system notes, userId for user-triggered actions
    public string? Tags { get; set; }  // Optional: "status-change", "coordinator-assigned", etc.
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }  // NULL for system notes (never edited)
}
```

### System Note Content Examples

```csharp
// Incident created
"Incident report submitted."

// Status change
"Status changed from ReportSubmitted to InformationGathering."

// Coordinator assigned
"Coordinator assigned: John Doe"

// Coordinator unassigned
"Coordinator unassigned."

// Google Drive links updated
"Google Drive folder link updated. Final report link updated."

// Data migration
"Status migrated from Resolved to Closed during system upgrade."
```

---

## 📧 EMAIL NOTIFICATION REQUIREMENTS

### Notification Triggers (Future Phase)

**Phase 1**: No email notifications required (manual process)

**Phase 2** (Future):
1. **Coordinator Assigned**: Email to coordinator
2. **Status Changed to OnHold**: Email to reporter (if identified)
3. **Incident Closed**: Email to reporter (if identified)

### Email Templates (Placeholder)

**Coordinator Assignment**:
```
Subject: You've been assigned to incident {referenceNumber}

Hi {coordinatorName},

You have been assigned as the coordinator for incident {referenceNumber}.

Severity: {severity}
Location: {location}
Incident Date: {incidentDate}

Please review the incident and begin the information gathering process.

View Incident: {dashboardUrl}/admin/incident-management/{incidentId}
```

**Status Change (OnHold)**:
```
Subject: Your incident report requires additional information

Your incident report from {incidentDate} has been placed on hold pending additional information.

What happens next:
- The coordinator may reach out for more details
- You will be notified when the review resumes

Thank you for your patience.
```

**Incident Closed**:
```
Subject: Your incident report has been resolved

Your incident report from {incidentDate} has been resolved and closed.

Thank you for reporting this incident and contributing to our community's safety.

If you have any questions, please contact our safety team.
```

---

## 🧪 TESTING CHECKLIST

### Endpoint Testing

**For Each Endpoint**:
- [ ] Authentication: Verify 401 for unauthenticated requests
- [ ] Authorization: Verify 403 for unauthorized users
- [ ] Not Found: Verify 404 for non-existent resources
- [ ] Validation: Verify 400 for invalid input
- [ ] Success: Verify 200/201/204 for valid requests
- [ ] Data Integrity: Verify response matches database state
- [ ] Side Effects: Verify system notes created, timestamps updated

### Integration Testing Scenarios

1. **Full Workflow**:
   - User submits incident (anonymous)
   - Admin assigns coordinator
   - Coordinator changes status to InformationGathering
   - Coordinator adds manual notes
   - Coordinator updates Google Drive links
   - Coordinator changes status to Closed
   - Verify system notes at each step

2. **Authorization Scenarios**:
   - Admin can access all incidents
   - Coordinator can only access assigned incidents
   - User can only access own identified reports
   - Verify 403 for unauthorized access attempts

3. **Report Ownership**:
   - User creates identified report
   - User can view own report in "My Reports"
   - User CANNOT view another user's report
   - User CANNOT view own anonymous report

4. **System Note Generation**:
   - Verify system note created on incident creation
   - Verify system note created on status change
   - Verify system note created on coordinator assignment
   - Verify note content includes relevant details

5. **Privacy Restrictions**:
   - Verify user's "My Reports" excludes reference number
   - Verify user's "My Reports" excludes coordinator info
   - Verify user's "My Reports" excludes notes
   - Verify anonymous reports are fully anonymous (no tracking)

---

## 🚨 CRITICAL IMPLEMENTATION WARNINGS

### 1. Privacy: No Reference Numbers in User Views

**CRITICAL**: Reference numbers (SAF-YYYYMMDD-NNNN) are ADMIN ONLY

**Affected Endpoints**:
- GET /api/safety/my-reports (response excludes referenceNumber)
- GET /api/safety/my-reports/{id} (response excludes referenceNumber)

**Why**: Users identify reports by incident date and location. Reference numbers are internal tracking only.

**Testing**: Verify response DTOs do NOT include referenceNumber field

---

### 2. Authorization: Report Ownership Verification

**CRITICAL**: Backend MUST verify user owns report before showing details

**Affected Endpoints**:
- GET /api/safety/my-reports/{id}

**Implementation**:
```csharp
if (incident.ReporterId != currentUserId || incident.IsAnonymous)
{
    return Forbid(); // 403 Forbidden
}
```

**Why**: Users could guess/modify URL parameters. Privacy violation if users see others' reports.

---

### 3. Anonymous Reports: Fully Anonymous

**CRITICAL**: Anonymous reports have ZERO tracking capability

**Affected Endpoints**:
- POST /api/safety/incidents (anonymous response excludes referenceNumber and id)
- GET /api/safety/my-reports (filters WHERE IsAnonymous = false)
- GET /api/safety/my-reports/{id} (returns 404 if IsAnonymous = true)

**Why**: Anonymous means NO follow-up ability. Users who submit anonymously cannot view their own reports.

---

### 4. Coordinator Assignment: Not Role-Restricted

**CRITICAL**: ANY user can be assigned as coordinator (not just admins)

**Affected Endpoints**:
- PUT /api/safety/incidents/{id}/coordinator

**Implementation**:
```csharp
// Do NOT filter by role
var coordinator = await _context.Users.FindAsync(request.CoordinatorId);
```

**Why**: Stakeholder requirement - allows expertise-based assignment (e.g., experienced member for mediation incident).

---

### 5. Status Enum Migration: Lossy Data

**WARNING**: Data migration from old enum values to new enum values is LOSSY

**Old → New Mapping**:
- New (1) → ReportSubmitted (1)
- InProgress (2) → InformationGathering (2)
- Resolved (3) → Closed (5)
- Archived (4) → Closed (5)

**Issue**: Cannot distinguish between Resolved and Archived post-migration (both become Closed)

**Mitigation**: System notes document migration, audit logs preserve original values

---

## 📊 PERFORMANCE CONSIDERATIONS

### Database Indexes

**Required Indexes** (already in EF Core migration):
- `IX_SafetyIncidents_CoordinatorId_Status` - Coordinator workload queries
- `IX_SafetyIncidents_Status_CoordinatorId` - Unassigned queue (partial index on NULL)
- `IX_IncidentNotes_IncidentId_CreatedAt` - Chronological note retrieval (DESC)
- `IX_IncidentNotes_AuthorId` - Author activity tracking (partial index on NOT NULL)
- `IX_IncidentNotes_Type` - System vs manual note filtering

### Query Optimization

**Use Eager Loading**:
```csharp
.Include(i => i.Coordinator)
.Include(i => i.Reporter)
.Include(i => i.Notes)
    .ThenInclude(n => n.Author)
```

**Use Projection for Lists**:
```csharp
.Select(i => new SafetyIncidentDto
{
    // Only select needed fields
    NoteCount = i.Notes.Count()  // Avoid loading all notes just for count
})
```

**Pagination Required**:
```csharp
.Skip((page - 1) * pageSize)
.Take(pageSize)
```

### Caching Considerations

**Cache Candidates**:
- User list for coordinator dropdown (refresh every 5 minutes)
- Dashboard metrics (refresh every 30 seconds)

**Do NOT Cache**:
- Incident details (real-time updates needed)
- Notes (real-time collaboration)

---

## 📋 DEPLOYMENT CHECKLIST

### Pre-Deployment

- [ ] All 12 endpoints implemented and tested
- [ ] Database migrations applied (EF Core + data migration)
- [ ] NSwag types regenerated
- [ ] Frontend integration complete
- [ ] Unit tests passing (100% endpoint coverage)
- [ ] Integration tests passing (full workflow scenarios)
- [ ] Authorization logic verified (admin, coordinator, user roles)
- [ ] System note generation tested
- [ ] Privacy restrictions tested (no reference number in user views)
- [ ] Performance testing complete (load testing with realistic data)

### Post-Deployment

- [ ] Monitor for 401/403 errors (authentication/authorization issues)
- [ ] Monitor system note generation (should auto-create on state changes)
- [ ] Monitor for 500 errors (unexpected backend failures)
- [ ] User feedback on "My Reports" feature
- [ ] Admin feedback on coordinator assignment workflow

---

**Document Version**: 1.0
**Last Updated**: 2025-10-18
**Maintained By**: Backend Developer
**Status**: Implementation Guide - All Endpoints Pending
