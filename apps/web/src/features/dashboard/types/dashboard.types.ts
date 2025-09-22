// Dashboard TypeScript Types
// Types that match the backend Dashboard API Response DTOs

/**
 * User dashboard response from GET /api/dashboard
 * Contains basic user profile information and vetting status
 */
export interface UserDashboardResponse {
  /** User's scene name for the rope bondage community */
  sceneName: string;
  
  /** User's role in the system (Member, VettedMember, Teacher, Organizer, Administrator) */
  role: string;
  
  /** Current vetting status (0 = Submitted, 1 = InReview, 2 = Approved, 3 = Rejected) */
  vettingStatus: number;
  
  /** Whether the user is currently vetted */
  isVetted: boolean;
  
  /** User's email address */
  email: string;
  
  /** When the user joined the community */
  joinDate: string; // ISO date string from API
  
  /** User's pronouns */
  pronouns: string;
}

/**
 * User events response from GET /api/dashboard/events
 * Contains upcoming events the user is registered for
 */
export interface UserEventsResponse {
  /** List of upcoming events the user is registered for */
  upcomingEvents: DashboardEventDto[];
}

/**
 * Event information for dashboard display
 * Simplified event data focused on user's registration
 */
export interface DashboardEventDto {
  /** Event ID */
  id: string;
  
  /** Event title */
  title: string;
  
  /** Event start date and time */
  startDate: string; // ISO date string from API
  
  /** Event end date and time */
  endDate: string; // ISO date string from API
  
  /** Event location */
  location: string;
  
  /** Type of event (Workshop, SkillShare, Social, Performance) */
  eventType: string;
  
  /** Name of the instructor/organizer */
  instructorName: string;
  
  /** User's registration status for this event (Registered, Waitlisted, etc.) */
  registrationStatus: string;
  
  /** Registration/ticket ID */
  ticketId: string;
  
  /** Confirmation code for the registration */
  confirmationCode: string;
}

/**
 * User statistics response from GET /api/dashboard/statistics
 * Contains attendance history and membership metrics
 */
export interface UserStatisticsResponse {
  /** Whether the user is currently vetted */
  isVerified: boolean;
  
  /** Total number of events the user has attended (checked in) */
  eventsAttended: number;
  
  /** Number of months the user has been a member */
  monthsAsMember: number;
  
  /** Number of events attended in the last 6 months (simplified consecutive metric) */
  recentEvents: number;
  
  /** Date the user joined the community */
  joinDate: string; // ISO date string from API
  
  /** Current vetting status (0 = Submitted, 1 = InReview, 2 = Approved, 3 = Rejected) */
  vettingStatus: number;
  
  /** Next interview date (if applicable and scheduled) */
  nextInterviewDate?: string; // ISO date string from API, nullable
  
  /** Total number of events the user is currently registered for (future events) */
  upcomingRegistrations: number;
  
  /** Number of events the user has registered for but cancelled */
  cancelledRegistrations: number;
}

/**
 * Helper type for vetting status values
 */
export enum VettingStatus {
  Draft = 0,
  Submitted = 1,
  UnderReview = 2,
  PendingReferences = 3,
  PendingInterview = 4,
  PendingAdditionalInfo = 5,
  Approved = 6,
  Denied = 7,
  Withdrawn = 8
}

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
   */
  getVettingStatusDisplay(status: number): VettingStatusDisplay {
    switch (status) {
      case VettingStatus.Draft:
        return {
          label: 'Draft',
          color: 'gray',
          description: 'Application in progress'
        };
      case VettingStatus.Submitted:
        return {
          label: 'Submitted',
          color: 'blue',
          description: 'Application submitted for review'
        };
      case VettingStatus.UnderReview:
        return {
          label: 'Under Review',
          color: 'yellow',
          description: 'Application being reviewed'
        };
      case VettingStatus.PendingReferences:
        return {
          label: 'Pending References',
          color: 'orange',
          description: 'Waiting for reference checks'
        };
      case VettingStatus.PendingInterview:
        return {
          label: 'Interview Scheduled',
          color: 'teal',
          description: 'Interview scheduled'
        };
      case VettingStatus.PendingAdditionalInfo:
        return {
          label: 'Additional Info Required',
          color: 'orange',
          description: 'More information needed'
        };
      case VettingStatus.Approved:
        return {
          label: 'Approved',
          color: 'green',
          description: 'Vetting approved - welcome to the community!'
        };
      case VettingStatus.Denied:
        return {
          label: 'Denied',
          color: 'red',
          description: 'Application not approved at this time'
        };
      case VettingStatus.Withdrawn:
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