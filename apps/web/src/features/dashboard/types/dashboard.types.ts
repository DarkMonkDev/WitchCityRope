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
 * User event DTO for dashboard display
 * @generated from UserEventDto DTO
 */
export type UserEventDto = components['schemas']['UserEventDto']

/**
 * User profile DTO
 * @generated from UserProfileDto DTO
 */
export type UserProfileDto = components['schemas']['UserProfileDto']

/**
 * Update profile DTO (full profile update)
 * @generated from UpdateProfileDto DTO
 */
export type UpdateProfileDto = components['schemas']['UpdateProfileDto']

/**
 * Update profile request (partial profile update)
 * @generated from UpdateProfileRequest DTO
 */
export type UpdateProfileRequest = components['schemas']['UpdateProfileRequest']

/**
 * Admin dashboard response
 * @generated from AdminDashboardResponse DTO
 */
export type AdminDashboardResponse = components['schemas']['AdminDashboardResponse']

/**
 * Dashboard statistics response
 * @generated from DashboardStatisticsResponse DTO
 */
export type DashboardStatisticsResponse = components['schemas']['DashboardStatisticsResponse']

/**
 * User participation DTO
 * @generated from UserParticipationDto DTO
 */
export type UserParticipationDto = components['schemas']['UserParticipationDto']

/**
 * API response wrapper for user events list
 * @generated from ApiResponseOfListOfUserEventDto
 */
export type ApiResponseOfListOfUserEventDto = components['schemas']['ApiResponseOfListOfUserEventDto']

/**
 * API response wrapper for user profile
 * @generated from ApiResponseOfUserProfileDto
 */
export type ApiResponseOfUserProfileDto = components['schemas']['ApiResponseOfUserProfileDto']

// ============================================================================
// PLACEHOLDER TYPES - Backend Endpoints Not Yet Implemented
// ============================================================================

/**
 * User dashboard response from GET /api/dashboard
 * PLACEHOLDER: Backend endpoint not yet implemented
 * TODO: Implement backend endpoint and regenerate types
 */
export interface UserDashboardResponse {
  // Placeholder - implement when backend endpoint exists
  [key: string]: any
}

/**
 * User events response from GET /api/dashboard/events
 * PLACEHOLDER: Backend endpoint not yet implemented
 * TODO: Implement backend endpoint and regenerate types
 */
export interface UserEventsResponse {
  // Placeholder - implement when backend endpoint exists
  [key: string]: any
}

/**
 * User statistics response from GET /api/dashboard/statistics
 * PLACEHOLDER: Backend endpoint not yet implemented
 * TODO: Implement backend endpoint and regenerate types
 */
export interface UserStatisticsResponse {
  // Placeholder - implement when backend endpoint exists
  [key: string]: any
}

/**
 * Event information for dashboard display
 * PLACEHOLDER: Backend endpoint not yet implemented
 * TODO: Implement backend endpoint and regenerate types
 */
export interface DashboardEventDto {
  // Placeholder - implement when backend endpoint exists
  [key: string]: any
}

// ============================================================================
// FRONTEND-ONLY TYPES - UI State and Display Logic
// ============================================================================

/**
 * Vetting status type
 * FRONTEND-ONLY: Vetting workflow status values
 *
 * Note: Backend uses numeric enum, frontend uses string for display
 * TODO: Consider migrating to generated VettingStatus enum if exposed in API
 */
export type VettingStatus =
  | 'UnderReview'
  | 'InterviewApproved'
  | 'InterviewCompleted'
  | 'FinalReview'
  | 'Approved'
  | 'Denied'
  | 'OnHold'
  | 'Withdrawn'

/**
 * Vetting status display properties
 * FRONTEND-ONLY: UI presentation configuration for vetting statuses
 */
export interface VettingStatusDisplay {
  label: string
  color: string
  description: string
}

/**
 * Registration status display properties
 * FRONTEND-ONLY: UI presentation configuration for registration statuses
 */
export interface RegistrationStatusDisplay {
  label: string
  color: string
  icon?: string
}

// ============================================================================
// UTILITY FUNCTIONS - Frontend Display Logic
// ============================================================================

/**
 * Dashboard utility functions
 * FRONTEND-ONLY: Presentation logic for dashboard data
 */
export const DashboardUtils = {
  /**
   * Get display properties for vetting status
   * NOTE: Using string instead of auto-generated VettingStatus type to handle backend enum updates
   * TODO: Regenerate shared types after backend VettingStatus enum is updated
   */
  getVettingStatusDisplay(status: string): VettingStatusDisplay {
    switch (status) {
      case 'UnderReview':
        return {
          label: 'Under Review',
          color: 'gray',
          description: 'Application under review',
        }
      case 'InterviewApproved':
        return {
          label: 'Interview Approved',
          color: 'cyan',
          description: 'Approved for interview - waiting to schedule',
        }
      case 'InterviewCompleted':
        return {
          label: 'Interview Completed',
          color: 'blue',
          description: 'Interview completed - awaiting final review',
        }
      case 'FinalReview':
        return {
          label: 'Final Review',
          color: 'violet',
          description: 'Application in final review',
        }
      case 'Approved':
        return {
          label: 'Approved',
          color: 'green',
          description: 'Vetting approved - welcome to the community!',
        }
      case 'Denied':
        return {
          label: 'Denied',
          color: 'red',
          description: 'Application not approved at this time',
        }
      case 'OnHold':
        return {
          label: 'On Hold',
          color: 'yellow',
          description: 'Application on hold',
        }
      case 'Withdrawn':
        return {
          label: 'Withdrawn',
          color: 'gray',
          description: 'Application withdrawn',
        }
      default:
        return {
          label: 'Unknown',
          color: 'gray',
          description: 'Status unknown',
        }
    }
  },

  /**
   * Get display properties for registration status
   */
  getRegistrationStatusDisplay(status: string): RegistrationStatusDisplay {
    switch (status.toLowerCase()) {
      case 'registered':
        return {
          label: 'Registered',
          color: 'green',
          icon: 'check',
        }
      case 'rsvp confirmed':
        return {
          label: 'RSVP Confirmed',
          color: 'green',
          icon: 'check',
        }
      case 'ticket purchased':
        return {
          label: 'Ticket Purchased',
          color: 'green',
          icon: 'check',
        }
      case 'payment pending':
        return {
          label: 'Payment Pending',
          color: 'yellow',
          icon: 'clock',
        }
      case 'waitlisted':
        return {
          label: 'Waitlisted',
          color: 'orange',
          icon: 'hourglass',
        }
      case 'cancelled':
        return {
          label: 'Cancelled',
          color: 'red',
          icon: 'x',
        }
      case 'payment failed':
        return {
          label: 'Payment Failed',
          color: 'red',
          icon: 'alert-triangle',
        }
      case 'refunded':
        return {
          label: 'Refunded',
          color: 'blue',
          icon: 'arrow-back',
        }
      default:
        return {
          label: status,
          color: 'gray',
          icon: 'question',
        }
    }
  },

  /**
   * Format date for display in user's timezone
   */
  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    })
  },

  /**
   * Format date and time for display in user's timezone
   */
  formatDateTime(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    })
  },

  /**
   * Calculate membership duration in a readable format
   */
  formatMembershipDuration(monthsAsMember: number): string {
    if (monthsAsMember < 1) {
      return 'Less than a month'
    } else if (monthsAsMember < 12) {
      return `${monthsAsMember} month${monthsAsMember === 1 ? '' : 's'}`
    } else {
      const years = Math.floor(monthsAsMember / 12)
      const remainingMonths = monthsAsMember % 12
      if (remainingMonths === 0) {
        return `${years} year${years === 1 ? '' : 's'}`
      } else {
        return `${years} year${years === 1 ? '' : 's'}, ${remainingMonths} month${
          remainingMonths === 1 ? '' : 's'
        }`
      }
    }
  },
}
