// CheckIn System TypeScript Types
// ✅ COMPLIANT WITH DTO ALIGNMENT STRATEGY
// Uses auto-generated types from @witchcityrope/shared-types package
// Backend DTOs are source of truth - NEVER manually create API types

import type { components } from '@witchcityrope/shared-types';

// ============================================================================
// AUTO-GENERATED TYPE EXPORTS (from backend DTOs via OpenAPI)
// ============================================================================

/**
 * Attendee information for check-in operations
 * Source: C# AttendeeResponse via NSwag generation
 */
export type AttendeeResponse = components['schemas']['AttendeeResponse'];

/**
 * Response containing attendees list with search and filtering
 * Source: C# CheckInAttendeesResponse via NSwag generation
 */
export type CheckInAttendeesResponse = components['schemas']['CheckInAttendeesResponse'];

/**
 * Pagination metadata for attendee lists
 * Source: C# PaginationInfo via NSwag generation
 */
export type PaginationInfo = components['schemas']['PaginationInfo'];

/**
 * Registration status enum - UNION TYPE, not enum object
 * Source: C# RegistrationStatus via NSwag generation
 * Values: "Confirmed" | "Waitlist" | "CheckedIn" | "NoShow"
 */
export type RegistrationStatus = components['schemas']['RegistrationStatus'];

/**
 * Check-in request payload
 * Source: C# CheckInRequest via NSwag generation
 */
export type CheckInRequest = components['schemas']['CheckInRequest'];

/**
 * Manual entry data for walk-in attendees
 * Source: C# ManualEntryData via NSwag generation
 */
export type ManualEntryData = components['schemas']['ManualEntryData'];

/**
 * Check-in response with success status and capacity info
 * Source: C# CheckInResponse via NSwag generation
 */
export type CheckInResponse = components['schemas']['CheckInResponse'];

/**
 * Event capacity information
 * Source: C# CapacityInfo via NSwag generation
 */
export type CapacityInfo = components['schemas']['CapacityInfo'];

/**
 * Dashboard data for check-in interface
 * Source: C# DashboardResponse via NSwag generation
 */
export type CheckInDashboard = components['schemas']['DashboardResponse'];

/**
 * Recent check-in activity item
 * Source: C# RecentCheckIn via NSwag generation
 */
export type RecentCheckIn = components['schemas']['RecentCheckIn'];

/**
 * Staff member information
 * Source: C# StaffMember via NSwag generation
 */
export type StaffMember = components['schemas']['StaffMember'];

/**
 * Sync status for offline operations
 * Source: C# SyncStatus via NSwag generation
 */
export type SyncStatus = components['schemas']['SyncStatus'];

/**
 * Sync request payload for offline operations
 * Source: C# SyncRequest via NSwag generation
 */
export type SyncRequest = components['schemas']['SyncRequest'];

/**
 * Pending check-in for offline sync
 * Source: C# PendingCheckIn via NSwag generation
 */
export type PendingCheckIn = components['schemas']['PendingCheckIn'];

/**
 * Sync response with conflicts and updates
 * TODO: Replace with backend type when available
 * Frontend-only until backend implements SyncResponse DTO
 */
export interface SyncResponse {
  success: boolean;
  processedCount: number;
  conflicts: SyncConflict[];
  updatedAttendees: CheckInAttendee[];
  newSyncTimestamp: string;
}

/**
 * Sync conflict information
 * TODO: Replace with backend type when available
 * Frontend-only until backend implements SyncConflict DTO
 */
export interface SyncConflict {
  localId: string;
  attendeeId: string;
  conflictType: ConflictType;
  serverData: any;
  localData: any;
  resolution: ConflictResolution;
  message: string;
}

// ============================================================================
// FRONTEND-ONLY TYPES (not from backend)
// ============================================================================

/**
 * Frontend-specific attendee type with additional client-side fields
 * Maps from backend AttendeeResponse with frontend enhancements
 */
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
  paymentStatus?: 'ticket' | 'rsvp' | 'paidAtDoor';  // Added for streamlined workflow
  hasTicketPurchase?: boolean;  // Added for functional spec v2.0
  sessionNames?: string[];  // Session names for multi-session display
}

/**
 * Search parameters for attendee filtering (frontend-only logic)
 */
export interface AttendeeSearchParams {
  eventId: string;
  search?: string;
  status?: RegistrationStatus;
  page?: number;
  pageSize?: number;
}

/**
 * Event status for check-in dashboard (frontend-only)
 * TODO: Replace with backend enum when available
 */
export enum EventStatus {
  Upcoming = 'upcoming',
  Active = 'active',
  Ended = 'ended'
}

/**
 * Conflict type for sync operations (frontend-only)
 * TODO: Replace with backend enum when available
 */
export enum ConflictType {
  DuplicateCheckIn = 'duplicate_checkin',
  CapacityExceeded = 'capacity_exceeded',
  AttendeeNotFound = 'attendee_not_found'
}

/**
 * Conflict resolution status (frontend-only)
 * TODO: Replace with backend enum when available
 */
export enum ConflictResolution {
  AutoResolved = 'auto_resolved',
  ManualRequired = 'manual_required'
}

/**
 * Search form data structure (frontend-only)
 */
export interface AttendeeSearchFormData {
  searchTerm: string;
  statusFilter: RegistrationStatus | 'all';
}

/**
 * Manual entry form data structure (frontend-only)
 */
export interface ManualEntryFormData {
  name: string;
  email: string;
  phone: string;
  dietaryRestrictions: string;
  accessibilityNeeds: string;
  hasCompletedWaiver: boolean;
}

/**
 * Check-in confirmation data (frontend-only)
 */
export interface CheckInConfirmationData {
  attendee: CheckInAttendee;
  checkInTime: string;
  notes?: string;
}

/**
 * Offline storage types (frontend-only)
 */
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

// ============================================================================
// UI CONFIGURATION (frontend-only)
// ============================================================================

/**
 * Status configurations for UI display
 * Uses string literals matching backend RegistrationStatus union type
 */
export const STATUS_CONFIGS: Record<RegistrationStatus, { label: string; color: string; icon: string }> = {
  'Confirmed': {
    label: 'Expected',
    color: '#DAA520',
    icon: '⏳'
  },
  'Waitlist': {
    label: 'Waitlist',
    color: '#DC143C',
    icon: '⚠️'
  },
  'CheckedIn': {
    label: 'Checked In',
    color: '#228B22',
    icon: '✅'
  },
  'NoShow': {
    label: 'No Show',
    color: '#8B8680',
    icon: '❌'
  }
};

/**
 * Event status configurations for UI display
 */
export const EVENT_STATUS_CONFIGS: Record<EventStatus, { label: string; color: string }> = {
  [EventStatus.Upcoming]: { label: 'Upcoming', color: 'blue' },
  [EventStatus.Active]: { label: 'Active', color: 'green' },
  [EventStatus.Ended]: { label: 'Ended', color: 'gray' }
};

/**
 * Touch target sizes for mobile optimization (frontend-only constants)
 */
export const TOUCH_TARGETS = {
  MINIMUM: 44, // px - absolute minimum for accessibility
  PREFERRED: 48, // px - preferred size for comfortable interaction
  BUTTON_HEIGHT: 48, // px - standard button height
  SEARCH_INPUT_HEIGHT: 56, // px - search input height for typing
  CARD_MIN_HEIGHT: 72 // px - minimum card height for attendee info
} as const;

// ============================================================================
// STREAMLINED CHECK-IN WORKFLOW TYPES (frontend-only)
// ============================================================================

/**
 * Ticket type information for door payment
 * Frontend-only until backend adds TicketTypeDto
 */
export interface TicketType {
  id: string;
  name: string;
  price?: number; // For fixed-price tickets
  minPrice?: number; // For sliding-scale tickets
  maxPrice?: number; // For sliding-scale tickets
  description?: string;
}

/**
 * Cash payment data for door ticket purchase
 * Includes ticket type selection per functional spec v2.0
 */
export interface CashPaymentData {
  ticketTypeId: string;
  amount: number;
  notes?: string;
}

/**
 * Create cash ticket purchase request
 * Maps to backend CashPaymentRequest DTO
 * TODO: Replace with auto-generated type when backend adds DTO
 */
export interface CreateCashTicketPurchaseRequest {
  attendeeId: string;  // User ID (backend expects this field name)
  ticketTypeId: string;
  amount: number;
  notes?: string;
  // recordedByStaffId is automatically extracted from session token by backend
}

/**
 * Cash payment response
 * Source: C# CashPaymentResponse via NSwag generation
 * ✅ AUTO-GENERATED TYPE - DO NOT MANUALLY EDIT
 */
export type CashPaymentResponse = components['schemas']['CashPaymentResponse'];

/**
 * Ticket purchase response
 * Frontend-only until backend adds TicketPurchaseResponse DTO
 * TODO: Replace with auto-generated type when backend implements it
 */
export interface TicketPurchaseResponse {
  success: boolean;
  ticketId: string;
  participationId: string;
  message?: string;
}
