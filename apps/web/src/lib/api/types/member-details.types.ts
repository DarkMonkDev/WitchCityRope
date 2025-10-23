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

// ============================================================================
// API DTOS - AUTO-GENERATED FROM C# BACKEND
// ============================================================================

/**
 * Member details with participation summary and vetting status
 * @generated from MemberDetailsResponse DTO
 */
export type MemberDetailsResponse = components['schemas']['MemberDetailsResponse']

/**
 * Vetting application details and questionnaire responses
 * @generated from VettingDetailsResponse DTO
 */
export type VettingDetailsResponse = components['schemas']['VettingDetailsResponse']

/**
 * Event participation history record
 * @generated from EventHistoryRecord DTO
 * @note Missing 'attended' field in generated types - using custom extension below
 */
export type EventHistoryRecordGenerated = components['schemas']['EventHistoryRecord']

/**
 * Paginated event history response
 * @generated from EventHistoryResponse DTO
 */
export type EventHistoryResponse = components['schemas']['EventHistoryResponse']

/**
 * Member incident record (safety incidents)
 * @generated from MemberIncidentRecord DTO
 */
export type MemberIncidentRecord = components['schemas']['MemberIncidentRecord']

/**
 * Member incidents collection response
 * @generated from MemberIncidentsResponse DTO
 */
export type MemberIncidentsResponse = components['schemas']['MemberIncidentsResponse']

/**
 * User note response
 * @generated from UserNoteResponse DTO
 */
export type UserNoteResponse = components['schemas']['UserNoteResponse']

/**
 * Create user note request
 * @generated from CreateUserNoteRequest DTO
 */
export type CreateUserNoteRequest = components['schemas']['CreateUserNoteRequest']

/**
 * Update member status request
 * @generated from UpdateMemberStatusRequest DTO
 */
export type UpdateMemberStatusRequest = components['schemas']['UpdateMemberStatusRequest']

/**
 * Update member role request
 * @generated from UpdateMemberRoleRequest DTO
 */
export type UpdateMemberRoleRequest = components['schemas']['UpdateMemberRoleRequest']

// ============================================================================
// FRONTEND EXTENSIONS - Field Mappings and Missing Properties
// ============================================================================

/**
 * Event history record with attended field
 * TEMPORARY: Backend DTO is missing 'attended' boolean field
 * TODO: Add 'attended' field to C# EventHistoryRecord DTO and regenerate types
 */
export interface EventHistoryRecord extends EventHistoryRecordGenerated {
  /** Whether the member attended the event (not in generated DTO yet) */
  attended: boolean
}

/**
 * Extended vetting details response with additional frontend-expected fields
 * COMPATIBILITY: Some components expect fields not in the generated DTO
 *
 * Field mapping issues:
 * - otherNames: Not in backend DTO (should be removed from frontend)
 * - whyJoin: Maps to whyJoinCommunity in backend DTO
 * - experienceWithRope: Maps to experienceDescription in backend DTO
 *
 * TODO: Update components (MemberVettingTab, MemberOverviewTab) to use correct field names
 * from generated VettingDetailsResponse and remove this extension type.
 */
export type VettingDetailsResponseExtended = VettingDetailsResponse & {
  /** Not in backend DTO - frontend-only field that should be removed */
  otherNames?: string
  /** Maps to whyJoinCommunity in backend DTO */
  whyJoin?: string
  /** Maps to experienceDescription in backend DTO */
  experienceWithRope?: string
}

// ============================================================================
// FRONTEND-ONLY TYPES - Not Sent to API
// ============================================================================

/**
 * Volunteer history record
 * FRONTEND-ONLY: Backend endpoint not implemented yet
 * See useMemberVolunteerHistory hook - returns empty data
 */
export interface VolunteerHistoryRecord {
  eventId: string
  eventTitle: string
  eventDate: string
  role: string
  showedUp: boolean
}

/**
 * Paginated volunteer history response
 * FRONTEND-ONLY: Backend endpoint not implemented yet
 * See useMemberVolunteerHistory hook - returns empty data
 */
export interface VolunteerHistoryResponse {
  volunteers: VolunteerHistoryRecord[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
