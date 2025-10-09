/**
 * Dashboard-specific TypeScript types
 * These correspond to C# DTOs in /apps/api/Features/Dashboard/Models/
 *
 * CRITICAL: These types match backend DTOs - do not modify without backend changes
 */

/**
 * User's registered event information for dashboard display
 * CRITICAL: This is NOT PublicEventDto - different fields for dashboard context
 * This is for the user's own events dashboard, NOT public sales page
 */
export interface UserEventDto {
  id: string;
  title: string;
  startDate: string; // ISO 8601 date string
  endDate: string; // ISO 8601 date string
  location: string;
  description?: string | null;
  /**
   * Registration status: "RSVP Confirmed", "Ticket Purchased", "Attended"
   */
  registrationStatus: 'RSVP Confirmed' | 'Ticket Purchased' | 'Attended';
  isSocialEvent: boolean;
  hasTicket: boolean;
  isPastEvent: boolean;
  // NO pricing fields - this is user dashboard, not sales page
  // NO capacity fields - user doesn't need to see event capacity
}

/**
 * User's vetting status for alert box display on dashboard
 */
export interface VettingStatusDto {
  /**
   * Vetting status: "Pending", "ApprovedForInterview", "OnHold", "Denied", "Vetted"
   */
  status: 'Pending' | 'ApprovedForInterview' | 'OnHold' | 'Denied' | 'Vetted';
  lastUpdatedAt: string; // ISO 8601 date string
  message: string;
  interviewScheduleUrl?: string | null;
  reapplyInfoUrl?: string | null;
}

/**
 * User profile information for settings page
 */
export interface UserProfileDto {
  userId: string;
  sceneName: string;
  firstName?: string | null;
  lastName?: string | null;
  email: string;
  pronouns?: string | null;
  bio?: string | null;
  discordName?: string | null;
  fetLifeName?: string | null;
  phoneNumber?: string | null;
  vettingStatus: string;
}

/**
 * Update profile request DTO
 */
export interface UpdateProfileDto {
  sceneName: string;
  firstName?: string | null;
  lastName?: string | null;
  email: string;
  pronouns?: string | null;
  bio?: string | null;
  discordName?: string | null;
  fetLifeName?: string | null;
  phoneNumber?: string | null;
}

/**
 * Change password request DTO
 */
export interface ChangePasswordDto {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}
