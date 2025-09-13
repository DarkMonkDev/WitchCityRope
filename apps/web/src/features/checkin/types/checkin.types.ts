// CheckIn System TypeScript Types
// Based on backend API design and mobile-first UI specifications

export interface CheckInAttendee {
  attendeeId: string;
  userId: string;
  sceneName: string;
  email: string;
  registrationStatus: RegistrationStatus;
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

export interface CheckInAttendeesResponse {
  eventId: string;
  eventTitle: string;
  eventDate: string;
  totalCapacity: number;
  checkedInCount: number;
  availableSpots: number;
  attendees: CheckInAttendee[];
  pagination: PaginationInfo;
}

export interface CheckInRequest {
  attendeeId: string;
  checkInTime: string; // ISO 8601 timestamp
  staffMemberId: string;
  notes?: string;
  overrideCapacity?: boolean;
  isManualEntry?: boolean;
  manualEntryData?: ManualEntryData;
}

export interface ManualEntryData {
  name: string;
  email: string;
  phone: string;
  dietaryRestrictions?: string;
  accessibilityNeeds?: string;
  hasCompletedWaiver: boolean;
}

export interface CheckInResponse {
  success: boolean;
  attendeeId: string;
  checkInTime: string;
  message: string;
  currentCapacity: CapacityInfo;
  auditLogId?: string;
}

export interface CapacityInfo {
  totalCapacity: number;
  checkedInCount: number;
  waitlistCount: number;
  availableSpots: number;
  isAtCapacity: boolean;
  canOverride: boolean;
}

export interface CheckInDashboard {
  eventId: string;
  eventTitle: string;
  eventDate: string;
  eventStatus: EventStatus;
  capacity: CapacityInfo;
  recentCheckIns: RecentCheckIn[];
  staffOnDuty: StaffMember[];
  syncStatus: SyncStatus;
}

export interface RecentCheckIn {
  attendeeId: string;
  sceneName: string;
  checkInTime: string;
  staffMemberName: string;
  isManualEntry: boolean;
}

export interface StaffMember {
  staffId: string;
  name: string;
  role: string;
  lastActivity: string;
}

export interface SyncStatus {
  pendingCount: number;
  lastSync: string;
  conflictCount: number;
}

export interface SyncRequest {
  deviceId: string;
  pendingCheckIns: PendingCheckIn[];
  lastSyncTimestamp: string;
}

export interface PendingCheckIn {
  localId: string;
  attendeeId: string;
  checkInTime: string;
  staffMemberId: string;
  notes?: string;
  isManualEntry?: boolean;
  manualEntryData?: ManualEntryData;
}

export interface SyncResponse {
  success: boolean;
  processedCount: number;
  conflicts: SyncConflict[];
  updatedAttendees: CheckInAttendee[];
  newSyncTimestamp: string;
}

export interface SyncConflict {
  localId: string;
  attendeeId: string;
  conflictType: ConflictType;
  serverData: any;
  localData: any;
  resolution: ConflictResolution;
  message: string;
}

export interface PaginationInfo {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AttendeeSearchParams {
  eventId: string;
  search?: string;
  status?: RegistrationStatus;
  page?: number;
  pageSize?: number;
}

// Enums
export enum RegistrationStatus {
  Confirmed = 'confirmed',
  Waitlist = 'waitlist',
  CheckedIn = 'checked-in',
  NoShow = 'no-show'
}

export enum EventStatus {
  Upcoming = 'upcoming',
  Active = 'active',
  Ended = 'ended'
}

export enum ConflictType {
  DuplicateCheckIn = 'duplicate_checkin',
  CapacityExceeded = 'capacity_exceeded',
  AttendeeNotFound = 'attendee_not_found'
}

export enum ConflictResolution {
  AutoResolved = 'auto_resolved',
  ManualRequired = 'manual_required'
}

// Frontend-specific types
export interface AttendeeSearchFormData {
  searchTerm: string;
  statusFilter: RegistrationStatus | 'all';
}

export interface ManualEntryFormData {
  name: string;
  email: string;
  phone: string;
  dietaryRestrictions: string;
  accessibilityNeeds: string;
  hasCompletedWaiver: boolean;
}

export interface CheckInConfirmationData {
  attendee: CheckInAttendee;
  checkInTime: string;
  notes?: string;
}

// Offline storage types
export interface OfflineAttendeeData {
  eventId: string;
  attendees: CheckInAttendee[];
  lastSync: string;
  capacity: CapacityInfo;
}

export interface OfflineQueue {
  pendingActions: PendingCheckIn[];
  conflicts: SyncConflict[];
  lastAttemptedSync?: string;
}

// Status configurations for UI
export const STATUS_CONFIGS: Record<RegistrationStatus, { label: string; color: string; icon: string }> = {
  [RegistrationStatus.Confirmed]: { 
    label: 'Expected', 
    color: '#DAA520', 
    icon: '⏳' 
  },
  [RegistrationStatus.Waitlist]: { 
    label: 'Waitlist', 
    color: '#DC143C', 
    icon: '⚠️' 
  },
  [RegistrationStatus.CheckedIn]: { 
    label: 'Checked In', 
    color: '#228B22', 
    icon: '✅' 
  },
  [RegistrationStatus.NoShow]: { 
    label: 'No Show', 
    color: '#8B8680', 
    icon: '❌' 
  }
};

// Event status configurations
export const EVENT_STATUS_CONFIGS: Record<EventStatus, { label: string; color: string }> = {
  [EventStatus.Upcoming]: { label: 'Upcoming', color: 'blue' },
  [EventStatus.Active]: { label: 'Active', color: 'green' },
  [EventStatus.Ended]: { label: 'Ended', color: 'gray' }
};

// Touch target sizes for mobile optimization
export const TOUCH_TARGETS = {
  MINIMUM: 44, // px - absolute minimum for accessibility
  PREFERRED: 48, // px - preferred size for comfortable interaction
  BUTTON_HEIGHT: 48, // px - standard button height
  SEARCH_INPUT_HEIGHT: 56, // px - search input height for typing
  CARD_MIN_HEIGHT: 72 // px - minimum card height for attendee info
} as const;