// types/api.types.ts
// AUTO-GENERATED TYPE RE-EXPORTS from @witchcityrope/shared-types
// DO NOT manually define interfaces - import from generated types package
// See: /docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md

import type { components } from '@witchcityrope/shared-types';

// =============================================================================
// Core DTOs - Re-exported from Generated Types
// =============================================================================

/**
 * User Data Transfer Object
 * Source: C# UserDto via NSwag generation
 */
export type UserDto = components['schemas']['UserDto'];

/**
 * Event Data Transfer Object
 * Source: C# EventDto via NSwag generation
 */
export type EventDto = components['schemas']['EventDto'];

// EventDto2 removed - does not exist in generated schema

/**
 * User Event Data Transfer Object
 * Source: C# UserEventDto via NSwag generation
 */
export type UserEventDto = components['schemas']['UserEventDto'];

/**
 * Event Participation Data Transfer Object
 * Source: C# EventParticipationDto via NSwag generation
 */
export type EventParticipationDto = components['schemas']['EventParticipationDto'];

/**
 * User Profile Data Transfer Object
 * Source: C# UserProfileDto via NSwag generation
 */
export type UserProfileDto = components['schemas']['UserProfileDto'];

// =============================================================================
// Request/Response Types - Re-exported from Generated Types
// =============================================================================

/**
 * Update Event Request
 * Source: C# UpdateEventRequest via NSwag generation
 */
export type UpdateEventRequest = components['schemas']['UpdateEventRequest'];

/**
 * Update Profile Request
 * Source: C# UpdateProfileRequest via NSwag generation
 */
export type UpdateProfileRequest = components['schemas']['UpdateProfileRequest'];

/**
 * Update User Roles Request
 * Source: C# UpdateUserRolesRequest via NSwag generation
 */
export type UpdateUserRolesRequest = components['schemas']['UpdateUserRolesRequest'];

// =============================================================================
// API Response Wrappers - REMOVED (Pattern B: Direct DTOs)
// =============================================================================
// These wrapper types do not exist in the generated schema.
// The API returns DTOs directly, not wrapped in ApiResponse<T> objects.
// Frontend should consume DTOs directly from endpoints.

// =============================================================================
// Pagination - Re-exported from Generated Types
// =============================================================================

/**
 * Paged Result for Application Summaries
 * Source: C# PagedResult<ApplicationSummaryDto> via NSwag generation
 */
export type PagedResultOfApplicationSummaryDto = components['schemas']['PagedResultOfApplicationSummaryDto'];

/**
 * User List Response with Pagination
 * Source: C# UserListResponse via NSwag generation
 */
export type UserListResponse = components['schemas']['UserListResponse'];

/**
 * Paginated Incident List Response
 * Source: C# PaginatedIncidentListResponse via NSwag generation
 */
export type PaginatedIncidentListResponse = components['schemas']['PaginatedIncidentListResponse'];

// =============================================================================
// Enums - Re-exported from Generated Types
// =============================================================================

/**
 * Attendance Status Enum
 * Source: C# AttendanceStatus enum via NSwag generation
 */
export type AttendanceStatus = components['schemas']['AttendanceStatus'];

/**
 * Attendance Type Enum
 * Source: C# AttendanceType enum via NSwag generation
 */
export type AttendanceType = components['schemas']['AttendanceType'];

/**
 * Incident Status Enum
 * Source: C# IncidentStatus enum via NSwag generation
 */
export type IncidentStatus = components['schemas']['IncidentStatus'];

// =============================================================================
// Error Types - Re-exported from Generated Types
// =============================================================================

/**
 * Problem Details for HTTP errors
 * Source: C# ProblemDetails via NSwag generation
 */
export type ProblemDetails = components['schemas']['ProblemDetails'];


// =============================================================================
// Generic Pagination Type (Frontend Convenience)
// =============================================================================

/**
 * Generic paginated response type
 * Note: This is a frontend convenience type for components that don't use specific backend pagination types
 * Prefer using specific backend types when available (e.g., UserListResponse, PagedResultOfApplicationSummaryDto)
 *
 * ESLint Exception: This is a generic frontend wrapper, not a backend DTO
 */
// eslint-disable-next-line no-restricted-syntax
export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

// =============================================================================
// Legacy Compatibility Types (DEPRECATED - Use Generated Types Above)
// =============================================================================

/**
 * @deprecated Use EventDto from generated types instead
 * Legacy Event interface from Blazor migration
 * This will be removed in future versions
 */
export interface Event {
  id: string;
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  capacity: number;
  registrationCount?: number;
  isRegistrationOpen: boolean;
  instructorId: string;
  instructor?: UserDto;
  attendees?: UserDto[];
  // Replaced eventType enum with boolean flags
  allowRsvps?: boolean;
  requireTicketPurchase?: boolean;
  vettedMembersOnly?: boolean;
  status?: 'Draft' | 'Published' | 'Cancelled' | 'Completed';
}

/**
 * @deprecated Use EventParticipationDto from generated types instead
 * Legacy EventRegistration interface
 */
export interface EventRegistration {
  id: string;
  eventId: string;
  userId: string;
  registeredAt: string;
  status: RegistrationStatus;
}

/**
 * @deprecated Use ParticipationStatus from generated types instead
 * Legacy registration status type
 */
export type RegistrationStatus = 'Confirmed' | 'Cancelled' | 'Waitlisted';

/**
 * @deprecated Use ProblemDetails from generated types instead
 * Legacy error type - replaced by generated ProblemDetails types
 */
export interface ApiError {
  message: string;
  status: number;
  code?: string;
  details?: Record<string, string[]>;
}

// =============================================================================
// Frontend-Only Types (Not from Backend)
// =============================================================================

/**
 * Event filters for frontend filtering
 * Note: This is a frontend-only type, not from backend DTOs
 */
export interface EventFilters {
  search?: string;
  startDate?: string;
  endDate?: string;
  category?: string;
}

// ✅ REMOVED: Manual CreateEventData interface (violates DTO Alignment Strategy)
// Use CreateEventRequest from @witchcityrope/shared-types instead

/**
 * Update event data for frontend forms
 * Note: Prefer using UpdateEventRequest from generated types when possible
 */
export interface UpdateEventData {
  title?: string;
  description?: string;
  startDate?: string;
  endDate?: string;
  capacity?: number;
  location?: string;
}

/**
 * @deprecated DO NOT USE - ApiResponse<T> wrapper pattern has been removed
 * Pattern B: API returns DTOs directly, not wrapped in ApiResponse<T>
 *
 * OLD (WRONG):
 * const response: ApiResponse<UserDto> = await api.get('/users/1');
 * const user = response.data;
 *
 * NEW (CORRECT):
 * const user: UserDto = await api.get('/users/1');
 *
 * For error handling, use try/catch with RFC 9457 Problem Details
 *
 * ESLint Exception: This is a generic frontend wrapper, not a backend DTO
 */
// eslint-disable-next-line no-restricted-syntax
export interface ApiResponse<T = unknown> {
  /** @deprecated */
  success: boolean;
  /** @deprecated */
  data?: T;
  /** @deprecated */
  error?: string | null;
  /** @deprecated */
  message?: string | null;
  /** @deprecated */
  timestamp?: string;
}

/**
 * User role type (Frontend convenience)
 * Note: Backend may have different role system
 */
export type UserRole = 'Administrator' | 'Teacher' | 'SafetyTeam' | 'EventOrganizer' | 'DungeonMonitor' | 'VettingTeam' | '';

/**
 * @deprecated Event type replaced with boolean flags (allowRsvps, requireTicketPurchase, vettedMembersOnly)
 * Backend no longer uses eventType enum - migrated to boolean flags
 */
export type EventType = 'Workshop' | 'Social' | 'Performance' | 'Other';

/**
 * Event status (Frontend convenience)
 * Note: Backend uses isPublished boolean in EventDto, not status enum
 */
export type EventStatus = 'Draft' | 'Published' | 'Cancelled' | 'Completed';
