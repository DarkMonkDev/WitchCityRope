// Dashboard TypeScript Types
//
// DTO ALIGNMENT STRATEGY - CRITICAL RULES:
// ════════════════════════════════════════
// 1. API DTOs (C#) are the SOURCE OF TRUTH
// 2. TypeScript types are AUTO-GENERATED from OpenAPI spec via @witchcityrope/shared-types
// 3. NEVER manually create TypeScript interfaces for API response data
// 4. If a type is missing, expose it in the backend API (add .Produces<> to endpoint)
// 5. Regenerate types: cd packages/shared-types && npm run generate
//
// WHY: Prevents type mismatches, ensures type safety, eliminates manual sync work
// SEE: /docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md
// ════════════════════════════════════════

import type { components } from '@witchcityrope/shared-types';

/**
 * User dashboard response from GET /api/dashboard
 * AUTO-GENERATED from backend UserDashboardResponse DTO
 */
export type UserDashboardResponse = components['schemas']['UserDashboardResponse'];

/**
 * VettingStatus enum
 * AUTO-GENERATED from backend VettingStatus enum
 */
export type VettingStatus = components['schemas']['VettingStatus'];

/**
 * User events response from GET /api/dashboard/events
 * AUTO-GENERATED from backend UserEventsResponse DTO
 */
export type UserEventsResponse = components['schemas']['UserEventsResponse'];

/**
 * Event information for dashboard display
 * AUTO-GENERATED from backend DashboardEventDto DTO
 */
export type DashboardEventDto = components['schemas']['DashboardEventDto'];

/**
 * User statistics response from GET /api/dashboard/statistics
 * AUTO-GENERATED from backend UserStatisticsResponse DTO
 */
export type UserStatisticsResponse = components['schemas']['UserStatisticsResponse'];


/**
 * Helper type for vetting status display properties
 */
export interface VettingStatusDisplay {
  label: string;
  color: string;
  description: string;
}

/**
 * Helper type for registration status display properties
 */
export interface RegistrationStatusDisplay {
  label: string;
  color: string;
  icon?: string;
}

/**
 * Utility functions for dashboard data
 */
export const DashboardUtils = {
  /**
   * Get display properties for vetting status
   * Uses auto-generated VettingStatus type from backend
   */
  getVettingStatusDisplay(status: VettingStatus): VettingStatusDisplay {
    switch (status) {
      case 'UnderReview':
        return {
          label: 'Under Review',
          color: 'gray',
          description: 'Application under review'
        };
      case 'InterviewApproved':
        return {
          label: 'Interview Approved',
          color: 'cyan',
          description: 'Approved for interview - waiting to schedule'
        };
      case 'InterviewScheduled':
        return {
          label: 'Interview Scheduled',
          color: 'blue',
          description: 'Interview scheduled'
        };
      case 'FinalReview':
        return {
          label: 'Final Review',
          color: 'violet',
          description: 'Application in final review'
        };
      case 'Approved':
        return {
          label: 'Approved',
          color: 'green',
          description: 'Vetting approved - welcome to the community!'
        };
      case 'Denied':
        return {
          label: 'Denied',
          color: 'red',
          description: 'Application not approved at this time'
        };
      case 'OnHold':
        return {
          label: 'On Hold',
          color: 'yellow',
          description: 'Application on hold'
        };
      case 'Withdrawn':
        return {
          label: 'Withdrawn',
          color: 'gray',
          description: 'Application withdrawn'
        };
      default:
        return {
          label: 'Unknown',
          color: 'gray',
          description: 'Status unknown'
        };
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
          icon: 'check'
        };
      case 'rsvp confirmed':
        return {
          label: 'RSVP Confirmed',
          color: 'green',
          icon: 'check'
        };
      case 'ticket purchased':
        return {
          label: 'Ticket Purchased',
          color: 'green',
          icon: 'check'
        };
      case 'payment pending':
        return {
          label: 'Payment Pending',
          color: 'yellow',
          icon: 'clock'
        };
      case 'waitlisted':
        return {
          label: 'Waitlisted',
          color: 'orange',
          icon: 'hourglass'
        };
      case 'cancelled':
        return {
          label: 'Cancelled',
          color: 'red',
          icon: 'x'
        };
      case 'payment failed':
        return {
          label: 'Payment Failed',
          color: 'red',
          icon: 'alert-triangle'
        };
      case 'refunded':
        return {
          label: 'Refunded',
          color: 'blue',
          icon: 'arrow-back'
        };
      default:
        return {
          label: status,
          color: 'gray',
          icon: 'question'
        };
    }
  },

  /**
   * Format date for display in user's timezone
   */
  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
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
      hour12: true
    });
  },

  /**
   * Calculate membership duration in a readable format
   */
  formatMembershipDuration(monthsAsMember: number): string {
    if (monthsAsMember < 1) {
      return 'Less than a month';
    } else if (monthsAsMember < 12) {
      return `${monthsAsMember} month${monthsAsMember === 1 ? '' : 's'}`;
    } else {
      const years = Math.floor(monthsAsMember / 12);
      const remainingMonths = monthsAsMember % 12;
      if (remainingMonths === 0) {
        return `${years} year${years === 1 ? '' : 's'}`;
      } else {
        return `${years} year${years === 1 ? '' : 's'}, ${remainingMonths} month${remainingMonths === 1 ? '' : 's'}`;
      }
    }
  }
};