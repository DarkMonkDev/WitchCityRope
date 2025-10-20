/**
 * Volunteer position response from API
 */
export interface VolunteerPosition {
  id: string;
  eventId: string;
  sessionId?: string;
  title: string;
  description: string;
  slotsNeeded: number;
  slotsFilled: number;
  slotsRemaining: number;
  requiresExperience: boolean;
  requirements: string;
  isPublicFacing: boolean;
  isFullyStaffed: boolean;

  // Session information if session-specific
  sessionName?: string;
  sessionStartTime?: string;
  sessionEndTime?: string;

  // User's signup status for this position (if authenticated)
  hasUserSignedUp: boolean;
  userSignupId?: string;
}

/**
 * Request to sign up for a volunteer position
 */
export interface VolunteerSignupRequest {
  notes?: string;
}

/**
 * Volunteer signup response from API
 */
export interface VolunteerSignup {
  id: string;
  volunteerPositionId: string;
  userId: string;
  status: string;
  signedUpAt: string;
  notes?: string;
  hasCheckedIn: boolean;
  checkedInAt?: string;
  hasCompleted: boolean;
  completedAt?: string;

  // Position information
  positionTitle: string;
  eventTitle: string;
  eventStartDate: string;
}
