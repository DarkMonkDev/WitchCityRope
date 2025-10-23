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

/**
 * Event Data Transfer Object (Alternative version)
 * Source: C# EventDto2 via NSwag generation
 */
export type EventDto2 = components['schemas']['EventDto2'];

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
// API Response Wrappers - Re-exported from Generated Types
// =============================================================================

/**
 * API Response wrapper for list of EventDto
 * Source: C# ApiResponse<List<EventDto>> via NSwag generation
 */
export type ApiResponseOfListOfEventDto = components['schemas']['ApiResponseOfListOfEventDto'];

/**
 * API Response wrapper for EventDto
 * Source: C# ApiResponse<EventDto> via NSwag generation
 */
export type ApiResponseOfEventDto = components['schemas']['ApiResponseOfEventDto'];

/**
 * API Response wrapper for list of UserEventDto
 * Source: C# ApiResponse<List<UserEventDto>> via NSwag generation
 */
export type ApiResponseOfListOfUserEventDto = components['schemas']['ApiResponseOfListOfUserEventDto'];

/**
 * API Response wrapper for list of EventParticipationDto
 * Source: C# ApiResponse<List<EventParticipationDto>> via NSwag generation
 */
export type ApiResponseOfListOfEventParticipationDto = components['schemas']['ApiResponseOfListOfEventParticipationDto'];

/**
 * API Response wrapper for UserProfileDto
 * Source: C# ApiResponse<UserProfileDto> via NSwag generation
 */
export type ApiResponseOfUserProfileDto = components['schemas']['ApiResponseOfUserProfileDto'];

/**
 * API Response wrapper for generic object
 * Source: C# ApiResponse<object> via NSwag generation
 */
export type ApiResponseOfObject = components['schemas']['ApiResponseOfObject'];

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
 * Participation Status Enum
 * Source: C# ParticipationStatus enum via NSwag generation
 */
export type ParticipationStatus = components['schemas']['ParticipationStatus'];

/**
 * Payment Status Enum
 * Source: C# PaymentStatus enum via NSwag generation
 */
export type PaymentStatus = components['schemas']['PaymentStatus'];

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

/**
 * Validation Problem Details for validation errors
 * Source: C# ValidationProblemDetails via NSwag generation
 */
export type ValidationProblemDetails = components['schemas']['ValidationProblemDetails'];

// =============================================================================
// Generic Pagination Type (Frontend Convenience)
// =============================================================================

/**
 * Generic paginated response type
 * Note: This is a frontend convenience type for components that don't use specific backend pagination types
 * Prefer using specific backend types when available (e.g., UserListResponse, PagedResultOfApplicationSummaryDto)
 */
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
  eventType?: 'class' | 'social';
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
 * @deprecated Use ProblemDetails or ValidationProblemDetails from generated types instead
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

/**
 * Create event data for frontend forms
 * Note: This is a frontend-only type, not from backend DTOs
 * Backend may use different request structure
 */
export interface CreateEventData {
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  capacity: number;
  location?: string;
}

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
 * Generic API Response wrapper (Frontend convenience)
 * Note: Prefer using specific ApiResponseOf* types from generated types when available
 */
export interface ApiResponse<T = unknown> {
  success: boolean;
  data?: T;
  error?: string | null;
  message?: string | null;
  timestamp?: string;
}

/**
 * User role type (Frontend convenience)
 * Note: Backend may have different role system
 */
export type UserRole = 'Administrator' | 'Teacher' | 'SafetyTeam' | '';

/**
 * Event type (Frontend convenience)
 * Note: Backend uses eventType as string field in EventDto
 */
export type EventType = 'Workshop' | 'Social' | 'Performance' | 'Other';

/**
 * Event status (Frontend convenience)
 * Note: Backend uses isPublished boolean in EventDto, not status enum
 */
export type EventStatus = 'Draft' | 'Published' | 'Cancelled' | 'Completed';
