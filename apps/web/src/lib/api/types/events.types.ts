/**
 * DTO ALIGNMENT STRATEGY - CRITICAL RULES:
 * 1. API DTOs (C#) are the SOURCE OF TRUTH
 * 2. TypeScript types are AUTO-GENERATED via @witchcityrope/shared-types
 * 3. NEVER manually create TypeScript interfaces for API data
 *
 * @generated This file uses auto-generated types from packages/shared-types
 * Last migration: 2025-10-23
 * Phase: Priority 2-5 (Phase 4 - High Complexity)
 */

import type { components } from '@witchcityrope/shared-types'
import type { PaginationParams } from './api.types'

// ============================================================================
// API DTOS - AUTO-GENERATED FROM C# BACKEND
// ============================================================================

/**
 * Event DTO with summary information
 * @generated from EventDto DTO
 */
export type EventDto = components['schemas']['EventDto']

/**
 * Event DTO with full details (extended version)
 * @generated from EventDto2 DTO
 */
export type EventDto2 = components['schemas']['EventDto2']

/**
 * Event session DTO
 * @generated from SessionDto DTO
 */
export type EventSessionDto = components['schemas']['SessionDto']

/**
 * Event ticket type DTO
 * @generated from TicketTypeDto DTO
 * @note Frontend uses 'price' field, backend has 'minPrice' - see mapping below
 */
export type EventTicketTypeDto = components['schemas']['TicketTypeDto']

/**
 * Volunteer position DTO
 * @generated from VolunteerPositionDto DTO
 */
export type VolunteerPositionDto = components['schemas']['VolunteerPositionDto']

/**
 * Volunteer position DTO (extended version with event details)
 * @generated from VolunteerPositionDto2 DTO
 */
export type VolunteerPositionDto2 = components['schemas']['VolunteerPositionDto2']

/**
 * Update event request
 * @generated from UpdateEventRequest DTO
 */
export type UpdateEventRequest = components['schemas']['UpdateEventRequest']

/**
 * Event registration response
 * @generated from EventParticipationDto DTO
 */
export type EventParticipationDto = components['schemas']['EventParticipationDto']

// ============================================================================
// FRONTEND EXTENSIONS - Field Mappings and Compatibility
// ============================================================================

/**
 * Update event DTO with frontend-expected fields
 * COMPATIBILITY: Frontend components expect 'id' field and field mappings
 *
 * Field differences from generated UpdateEventRequest:
 * - Added 'id' field (required for update operations)
 * - Added 'eventType' field for event categorization
 * - TicketTypeDto mapping: frontend uses 'price', backend uses 'minPrice'
 * - VolunteerPositionDto mapping: frontend adds 'requirements', 'requiresExperience'
 *
 * TODO: Migrate components to use UpdateEventRequest directly with separate id parameter
 */
export interface UpdateEventDto extends Omit<UpdateEventRequest, 'ticketTypes' | 'volunteerPositions'> {
  /** Event ID (not in generated UpdateEventRequest - passed separately in API) */
  id: string

  /** Event type classification */
  eventType?: 'Class' | 'Social'

  /** Ticket types with frontend field names */
  ticketTypes?: Array<Omit<EventTicketTypeDto, 'minPrice'> & {
    /** Price field (maps to minPrice in backend) */
    price: number
  }>

  /** Volunteer positions with frontend fields */
  volunteerPositions?: Array<VolunteerPositionDto & {
    /** Position requirements (not in generated DTO) */
    requirements?: string
    /** Requires prior experience flag (not in generated DTO) */
    requiresExperience?: boolean
  }>
}

/**
 * Session DTO with extended fields
 * COMPATIBILITY: Frontend expects additional fields not in generated SessionDto
 *
 * Field additions:
 * - description: Session description text (not in generated DTO)
 * - registeredCount: Uses registrationCount from generated DTO
 *
 * TODO: Add 'description' to backend SessionDto and regenerate types
 */
export interface EventSessionDtoExtended extends EventSessionDto {
  /** Session description (not in generated DTO yet) */
  description?: string
  /** Registered count - alias for registrationCount */
  registeredCount?: number
}

// ============================================================================
// FRONTEND-ONLY TYPES - Not Sent to API
// ============================================================================

/**
 * Create event DTO
 * FRONTEND-ONLY: No dedicated CreateEvent endpoint in generated types
 * Backend likely uses UpdateEventRequest for creation as well
 *
 * TODO: Verify if backend has a CreateEvent endpoint and regenerate types
 */
export interface CreateEventDto {
  title: string
  description: string
  startDate: string
  endDate?: string
  location: string
  capacity?: number
}

/**
 * Event filters for list queries
 * FRONTEND-ONLY: Query parameters for filtering events
 */
export interface EventFilters extends PaginationParams {
  startDate?: string
  endDate?: string
  location?: string
  search?: string
  hasCapacity?: boolean
}

/**
 * Registration DTO
 * FRONTEND-ONLY: Simplified registration model
 * Backend uses EventParticipationDto instead
 *
 * @deprecated Use EventParticipationDto from generated types
 */
export interface RegistrationDto {
  id: string
  eventId: string
  userId: string
  registrationDate: string
  status: 'confirmed' | 'waitlist' | 'cancelled'
}

/**
 * Query keys for type safety
 * FRONTEND-ONLY: React Query key structure
 */
export type EventQueryKey =
  | ['events']
  | ['events', 'list']
  | ['events', 'list', EventFilters]
  | ['events', 'detail']
  | ['events', 'detail', string]
  | ['events', 'registrations']
  | ['events', 'registrations', string]
