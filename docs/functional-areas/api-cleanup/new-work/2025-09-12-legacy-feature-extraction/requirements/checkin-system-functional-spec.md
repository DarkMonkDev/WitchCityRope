# CheckIn System Functional Specification - WitchCityRope
<!-- Last Updated: 2025-09-13 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Draft -->

## Executive Summary

The CheckIn System provides event entrance management for WitchCityRope community events, with a mobile-first design for volunteer staff using phones/tablets at event entrances. The system emphasizes quick processing, offline capability, and simple volunteer interface while maintaining critical safety and consent protocols required by the rope bondage community.

## Business Context

### Problem Statement
Event organizers need efficient check-in management for community events but currently lack:
- Mobile-optimized interface for volunteer staff
- Offline capability for unreliable venue WiFi
- Integration with attendee registration and capacity management
- Dietary, accessibility, and safety information display during check-in

### Business Value
- **Faster Check-In**: Reduce entrance wait times from 5+ minutes to under 1 minute per person
- **Improved Safety**: Display critical attendee information (dietary restrictions, emergency contacts) to staff
- **Better Capacity Management**: Real-time attendance tracking and waitlist management
- **Volunteer Efficiency**: Simple interface requiring minimal training for community volunteers
- **Data Quality**: Accurate attendance records for future event planning

### Success Metrics
- Check-in processing time: <30 seconds per attendee
- Staff training time: <10 minutes for new volunteers
- Offline capability: Function for >4 hours without connectivity
- Data accuracy: 99%+ attendance record accuracy
- Volunteer satisfaction: 4.5/5 stars for ease of use

## Functional Requirements

### 1. Staff Check-In Interface

#### FR-1.1: Attendee Search and Selection
**As a** volunteer staff member  
**I want to** quickly find attendees by name, email, or ticket number  
**So that** I can process check-ins efficiently even with large attendee lists

**Acceptance Criteria:**
- Given I am on the check-in interface
- When I enter 3+ characters in the search field
- Then I see filtered results updating in real-time
- And results include name, email, and registration status
- And search works on partial matches for names and email addresses
- And search is case-insensitive

**Business Rules:**
- Search supports Name (scene name or legal name), Email, and Ticket/Confirmation numbers
- Minimum 3 characters required for search activation
- Maximum 10 results displayed at once with pagination
- Search results persist across screen orientation changes

#### FR-1.2: Attendee Information Display
**As a** volunteer staff member  
**I want to** see essential attendee information before check-in  
**So that** I can provide appropriate assistance and ensure safety requirements are met

**Acceptance Criteria:**
- Given I select an attendee from the list
- When the attendee details modal opens
- Then I see: full name, email, first-time status, dietary restrictions, accessibility needs, emergency contact, pronouns, waiver status
- And critical information (allergies, accessibility) is visually emphasized
- And I can scroll through all information on mobile devices

**Business Rules:**
- First-time attendees flagged with special icon and welcome note
- Dietary restrictions and accessibility needs prominently displayed
- Emergency contact information always visible to staff
- Waiver status must show "Complete" before check-in allowed

#### FR-1.3: Check-In Confirmation Process
**As a** volunteer staff member  
**I want to** confirm check-in with a single action after reviewing attendee information  
**So that** the process is quick but includes necessary verification

**Acceptance Criteria:**
- Given I have reviewed attendee information
- When I tap the "CHECK IN" button
- Then I see a confirmation screen with attendee name and current time
- And the system records the check-in timestamp and staff member ID
- And I see a success animation confirming the action
- And the attendee status updates to "Checked In" immediately

**Business Rules:**
- Check-in can only be performed by users with CheckInStaff, Organizer, or Administrator roles
- Each attendee can only be checked in once per event
- Check-in time recorded in local timezone of event
- Staff member performing check-in is logged for audit purposes

### 2. Event Dashboard and Monitoring

#### FR-2.1: Real-Time Capacity Tracking
**As an** event organizer  
**I want to** monitor current attendance and capacity in real-time  
**So that** I can make informed decisions about waitlist admissions and capacity management

**Acceptance Criteria:**
- Given I am viewing the event dashboard
- When attendees are checked in
- Then I see updated capacity numbers immediately (checked in / total capacity)
- And I see visual progress bar indicating capacity percentage
- And I receive warnings when approaching 90% capacity
- And waitlist count updates automatically

**Business Rules:**
- Capacity calculations include: Confirmed registrations + Waitlist + Manual entries
- Visual warnings: 90% capacity = yellow, 100% = red
- Only Organizers and Administrators can override capacity limits
- Real-time updates sync across all staff devices when online

#### FR-2.2: Recent Activity Monitoring
**As a** volunteer staff member  
**I want to** see recent check-in activity  
**So that** I can monitor check-in flow and identify any issues

**Acceptance Criteria:**
- Given I am on the check-in interface
- When I view the recent activity section
- Then I see the last 5 check-ins with name and timestamp
- And entries are sorted by most recent first
- And I can tap to view full attendee details for any recent check-in

**Business Rules:**
- Recent activity shows last 5 check-ins only
- Names displayed as scene names for privacy
- Timestamps shown in local event timezone
- Activity list updates in real-time when online

### 3. Waitlist and Capacity Management

#### FR-3.1: Waitlist Check-In Process
**As a** volunteer staff member  
**I want to** check in waitlist attendees when capacity allows  
**So that** waitlisted community members can attend when spots become available

**Acceptance Criteria:**
- Given I select a waitlist attendee
- When I attempt to check them in
- Then the system checks current capacity
- And if spots are available, processes normal check-in
- And if capacity is full, shows capacity warning with override option
- And only authorized staff can override capacity limits

**Business Rules:**
- Waitlist attendees processed in order of waitlist position
- Capacity overrides require Organizer or Administrator role
- Override requires confirmation dialog with reason
- Overridden check-ins flagged in audit log

#### FR-3.2: Manual Entry for Walk-Ins
**As a** volunteer staff member  
**I want to** manually add walk-in attendees who aren't pre-registered  
**So that** community members can attend events even without advance registration

**Acceptance Criteria:**
- Given I need to check in someone not on the attendee list
- When I tap "Manual Entry"
- Then I see a form for: Name, Email, Phone, Dietary restrictions, Accessibility needs
- And I can indicate if they've completed the waiver
- And system creates temporary registration and processes check-in
- And manual entries are clearly marked in attendance records

**Business Rules:**
- Manual entries require all mandatory fields: Name, Email, Phone
- Waiver completion must be verified before check-in
- Manual entries count toward event capacity
- Manual entries synchronized to registration system when online

### 4. Offline Capability and Synchronization

#### FR-4.1: Offline Operation
**As a** volunteer staff member  
**I want to** continue check-in operations when venue WiFi is unreliable  
**So that** event flow is not disrupted by connectivity issues

**Acceptance Criteria:**
- Given the device loses internet connectivity
- When I continue using the check-in interface
- Then I can still search pre-loaded attendee lists
- And I can process check-ins with local timestamp recording
- And I see clear offline indicator in the interface
- And pending actions are queued for synchronization

**Business Rules:**
- Attendee list and essential data cached locally before event start
- Offline mode supports up to 4 hours of check-in operations
- Pending check-ins stored in local queue with timestamps
- Clear visual indicators show connection status and pending sync count

#### FR-4.2: Data Synchronization
**As a** volunteer staff member  
**I want to** automatically sync check-in data when connectivity returns  
**So that** all attendance records are accurately maintained

**Acceptance Criteria:**
- Given I have pending check-ins from offline operation
- When internet connectivity is restored
- Then system automatically attempts to sync pending actions
- And I see sync status with success/failure indicators
- And any sync conflicts are flagged for manual resolution
- And I can manually trigger sync attempts

**Business Rules:**
- Automatic sync attempts every 30 seconds when online
- Sync conflicts (duplicate check-ins) require manual resolution
- Failed sync attempts are retried up to 5 times
- Successful sync removes items from pending queue

### 5. Security and Access Control

#### FR-5.1: Role-Based Access Control
**As a** system administrator  
**I want to** control who can perform check-in operations  
**So that** only authorized community members can manage event entrance

**Acceptance Criteria:**
- Given a user attempts to access check-in functionality
- When the system validates their role
- Then CheckInStaff role allows basic check-in operations
- And Organizer role allows capacity overrides and export functions
- And Administrator role allows all check-in functions and system settings
- And unauthorized users see appropriate error messages

**Business Rules:**
- Only authenticated users with appropriate roles can access check-in system
- Role permissions enforced on both frontend and API
- Session timeout after 2 hours of inactivity for security
- All check-in actions logged with user identification for audit

#### FR-5.2: Data Privacy and Audit
**As a** community member  
**I want to** know my personal information is protected during check-in  
**So that** I can trust the platform with my sensitive information

**Acceptance Criteria:**
- Given my attendee information is displayed during check-in
- When staff view my details
- Then only essential information is shown (no payment details, full address)
- And all access is logged with timestamp and staff member ID
- And my data is not stored permanently on check-in devices
- And staff cannot export personal information without proper authorization

**Business Rules:**
- Personal information limited to: Name, Email, Dietary restrictions, Accessibility needs, Emergency contact, Pronouns
- No payment information, full addresses, or other sensitive data displayed
- All data access logged for audit purposes
- Local data automatically purged after 7 days post-event

## API Specifications

### Authentication
All API endpoints require authentication via cookie-based session management following the platform's existing authentication patterns.

### Endpoint Definitions

#### GET /api/checkin/events/{eventId}/attendees
**Purpose:** Retrieve attendee list for check-in interface

**Request Parameters:**
- `eventId` (required): Event GUID
- `search` (optional): Search term for filtering attendees
- `status` (optional): Filter by registration status (confirmed, waitlist, checked-in)

**Response:**
```typescript
interface CheckInAttendeesResponse {
  eventId: string;
  eventTitle: string;
  eventDate: string;
  totalCapacity: number;
  checkedInCount: number;
  attendees: CheckInAttendee[];
}

interface CheckInAttendee {
  attendeeId: string;
  userId: string;
  sceneName: string;
  email: string;
  registrationStatus: 'confirmed' | 'waitlist' | 'checked-in' | 'no-show';
  ticketNumber?: string;
  checkInTime?: string;
  isFirstTime: boolean;
  dietaryRestrictions?: string;
  accessibilityNeeds?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  pronouns?: string;
  hasCompletedWaiver: boolean;
  waitlistPosition?: number;
}
```

#### POST /api/checkin/events/{eventId}/checkin
**Purpose:** Process attendee check-in

**Request Body:**
```typescript
interface CheckInRequest {
  attendeeId: string;
  checkInTime: string; // ISO 8601 timestamp
  staffMemberId: string;
  notes?: string;
  overrideCapacity?: boolean;
  isManualEntry?: boolean;
  manualEntryData?: ManualEntryData;
}

interface ManualEntryData {
  name: string;
  email: string;
  phone: string;
  dietaryRestrictions?: string;
  accessibilityNeeds?: string;
  hasCompletedWaiver: boolean;
}
```

**Response:**
```typescript
interface CheckInResponse {
  success: boolean;
  attendeeId: string;
  checkInTime: string;
  message: string;
  currentCapacity: CapacityInfo;
}

interface CapacityInfo {
  totalCapacity: number;
  checkedInCount: number;
  waitlistCount: number;
  availableSpots: number;
  isAtCapacity: boolean;
}
```

#### GET /api/checkin/events/{eventId}/dashboard
**Purpose:** Retrieve real-time event check-in statistics

**Response:**
```typescript
interface CheckInDashboard {
  eventId: string;
  eventTitle: string;
  eventDate: string;
  eventStatus: 'upcoming' | 'active' | 'ended';
  capacity: CapacityInfo;
  recentCheckIns: RecentCheckIn[];
  staffOnDuty: StaffMember[];
}

interface RecentCheckIn {
  attendeeId: string;
  sceneName: string;
  checkInTime: string;
  staffMemberName: string;
}

interface StaffMember {
  userId: string;
  sceneName: string;
  role: string;
  lastActivity: string;
}
```

#### GET /api/checkin/events/{eventId}/export
**Purpose:** Export attendance data (requires Organizer+ role)

**Response:**
```typescript
interface AttendanceExport {
  eventId: string;
  eventTitle: string;
  exportedAt: string;
  exportedBy: string;
  attendees: ExportAttendee[];
}

interface ExportAttendee {
  sceneName: string;
  email: string;
  registrationStatus: string;
  checkInTime?: string;
  isFirstTime: boolean;
  dietaryRestrictions?: string;
  accessibilityNeeds?: string;
  staffMemberCheckedIn?: string;
}
```

#### POST /api/checkin/events/{eventId}/sync
**Purpose:** Synchronize offline check-in data

**Request Body:**
```typescript
interface SyncRequest {
  pendingCheckIns: PendingCheckIn[];
  lastSyncTimestamp: string;
}

interface PendingCheckIn {
  localId: string;
  attendeeId: string;
  checkInTime: string;
  staffMemberId: string;
  notes?: string;
  isManualEntry?: boolean;
  manualEntryData?: ManualEntryData;
}
```

**Response:**
```typescript
interface SyncResponse {
  success: boolean;
  processedCount: number;
  conflicts: SyncConflict[];
  updatedAttendees: CheckInAttendee[];
}

interface SyncConflict {
  localId: string;
  attendeeId: string;
  conflictType: 'duplicate_checkin' | 'capacity_exceeded' | 'attendee_not_found';
  message: string;
  requiresManualResolution: boolean;
}
```

## Business Rules

### Check-In Authorization
1. **Staff Roles:** Only users with CheckInStaff, Organizer, or Administrator roles can perform check-ins
2. **Session Management:** Check-in sessions timeout after 2 hours of inactivity
3. **Audit Logging:** All check-in actions logged with staff member ID and timestamp
4. **Device Limits:** Maximum 10 concurrent check-in sessions per event

### Event Timing and Capacity
1. **Check-In Window:** Check-in opens 30 minutes before event start time
2. **Capacity Enforcement:** 
   - Standard check-ins blocked when at 100% capacity
   - Organizers and Administrators can override capacity limits
   - Overrides require confirmation and reason logging
3. **Waitlist Management:** Waitlist attendees processed in order of waitlist position
4. **Late Arrivals:** Check-in remains open until 30 minutes after event start

### Registration and Waiver Requirements
1. **Waiver Completion:** All attendees must have completed waiver before check-in
2. **First-Time Attendees:** Special handling and welcome information displayed
3. **Manual Entry Requirements:**
   - Name, Email, Phone (all required)
   - Waiver completion verification required
   - Counts toward event capacity
4. **Duplicate Prevention:** Each attendee can only be checked in once per event

### Data Handling and Privacy
1. **Information Display:** Limited to essential check-in information only
2. **Local Storage:** 
   - Attendee data cached locally for offline operation
   - Local data purged 7 days after event
   - No permanent storage of personal information on devices
3. **Search Privacy:** Search results limited to registered attendees for current event
4. **Export Restrictions:** Data export requires Organizer+ role and is logged

### Offline Operation
1. **Cache Duration:** Attendee data cached for up to 4 hours of offline operation
2. **Sync Frequency:** Automatic sync attempts every 30 seconds when online
3. **Conflict Resolution:** Duplicate check-ins flagged for manual resolution
4. **Data Integrity:** Failed syncs retried up to 5 times before manual intervention required

## Data Requirements

### Attendee Information Storage
The system requires the following attendee data to be available for check-in operations:

#### Core Registration Data
- **Attendee ID:** Unique identifier linking to registration system
- **User ID:** Link to user account for permission checking
- **Scene Name:** Primary display name for privacy
- **Email:** For identification and communication
- **Registration Status:** confirmed, waitlist, checked-in, no-show
- **Ticket/Confirmation Number:** For alternative lookup methods

#### Check-In Specific Data
- **Check-In Timestamp:** Local timezone timestamp of check-in
- **Staff Member ID:** User who performed check-in
- **Check-In Notes:** Optional notes added during check-in
- **Override Flag:** Indicates if capacity was overridden for this check-in

#### Attendee Support Information
- **First-Time Flag:** Boolean indicating if this is attendee's first event
- **Dietary Restrictions:** Text field for allergies and dietary needs
- **Accessibility Needs:** Text field for mobility or other assistance needs
- **Emergency Contact Name:** Emergency contact person
- **Emergency Contact Phone:** Emergency contact phone number
- **Pronouns:** Preferred pronouns for respectful address
- **Waiver Status:** Boolean indicating waiver completion

#### Event Context Data
- **Event ID:** Current event identifier
- **Event Title:** Event name for display
- **Event Date/Time:** For timing validation
- **Total Capacity:** Maximum attendees allowed
- **Current Capacity:** Real-time count of checked-in attendees
- **Waitlist Count:** Number of people on waitlist

### Offline Data Synchronization
When operating offline, the system maintains:

#### Pending Actions Queue
- **Local Action ID:** Temporary identifier for offline actions
- **Action Type:** checkin, manual_entry, capacity_override
- **Timestamp:** When action was taken locally
- **Action Data:** Complete action payload for later sync
- **Retry Count:** Number of failed sync attempts

#### Sync Conflict Resolution
- **Conflict Type:** duplicate_checkin, capacity_exceeded, attendee_not_found
- **Local Data:** What was attempted offline
- **Server Data:** Current server state
- **Resolution Status:** pending, resolved, manual_intervention_required

## Acceptance Criteria

### Mobile Performance Standards
- **Initial Load Time:** Check-in interface loads in <3 seconds on 3G connection
- **Search Response:** Attendee search results appear in <500ms
- **Check-In Processing:** Single check-in completes in <1 second locally
- **Offline Transition:** Seamless transition to offline mode without data loss
- **Battery Efficiency:** Interface operates for 6+ hours on typical mobile device

### User Experience Requirements
- **Touch Targets:** All interactive elements minimum 44px touch targets
- **Single-Hand Operation:** Primary functions accessible with thumb on mobile
- **Error Recovery:** Clear error messages with suggested actions
- **Success Feedback:** Immediate visual confirmation of successful actions
- **Accessibility:** WCAG 2.1 AA compliance for screen readers and keyboard navigation

### Data Accuracy Standards
- **Sync Success Rate:** 99%+ successful synchronization of offline data
- **Duplicate Prevention:** Zero duplicate check-ins through system validation
- **Audit Trail:** 100% of check-in actions logged with complete context
- **Data Consistency:** Real-time capacity counts accurate across all devices

### Security and Privacy Compliance
- **Role Enforcement:** 100% accurate role-based access control
- **Data Encryption:** All sensitive data encrypted in transit and at rest
- **Session Security:** Automatic logout after inactivity period
- **Audit Logging:** Complete audit trail for all data access and modifications

## Integration Points

### Existing System Dependencies
1. **Authentication System:** Cookie-based session management for role validation
2. **Event Management:** Event details, timing, and capacity information
3. **Registration System:** Attendee registration data and status
4. **User Management:** Staff roles and permissions
5. **Notification System:** Capacity alerts and staff notifications

### External Service Requirements
1. **Email Service:** For manual entry confirmation and audit notifications
2. **Real-Time Updates:** WebSocket or polling for live capacity updates
3. **Offline Storage:** Browser local storage for offline capability
4. **Export Services:** CSV/PDF generation for attendance reports

### Future Enhancement Opportunities
1. **QR Code Integration:** Scannable codes for faster check-in
2. **Multi-Session Events:** Support for events with multiple sessions
3. **Badge Printing:** Integration with label printer for name badges
4. **Analytics Dashboard:** Detailed attendance analytics and reporting
5. **Mobile App:** Native mobile app for enhanced offline capability

## Implementation Notes

### Technology Stack Alignment
- **Frontend:** React + TypeScript following existing platform patterns
- **API:** .NET minimal API with vertical slice architecture
- **Authentication:** Cookie-based auth consistent with platform standards
- **Data Storage:** PostgreSQL with existing entity framework patterns
- **Real-Time:** SignalR for live updates when available

### Development Considerations
1. **Mobile-First Design:** All components designed for mobile then enhanced for desktop
2. **Progressive Enhancement:** Core functionality works offline, enhanced features when online
3. **Error Resilience:** Graceful degradation when dependencies unavailable
4. **Performance Optimization:** Lazy loading and caching for large attendee lists
5. **Testing Strategy:** Offline functionality testing and cross-device compatibility

### Deployment and Monitoring
1. **Feature Flags:** Gradual rollout capability for check-in functionality
2. **Performance Monitoring:** Real-time monitoring of check-in processing times
3. **Error Tracking:** Comprehensive error logging for offline/online transition issues
4. **Usage Analytics:** Track check-in patterns for future optimization

This functional specification provides the complete blueprint for implementing the CheckIn System, bridging the approved UI design with technical requirements while maintaining the simplicity and volunteer-friendly approach essential for the WitchCityRope community.