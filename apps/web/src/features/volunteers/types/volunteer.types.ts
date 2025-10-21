/**
 * Volunteer position response from API
 * IMPORTANT: This interface must match the C# DTO exactly
 * Source: apps/api/Features/Volunteers/Models/VolunteerModels.cs - VolunteerPositionDto
 */
export interface VolunteerPosition {
  id: string;
  eventId: string;
  sessionId?: string | null;
  title: string;
  description: string;
  slotsNeeded: number;
  slotsFilled: number;
  slotsRemaining: number;
  requiresExperience: boolean;
  requirements: string;
  isPublicFacing: boolean;
  isFullyStaffed: boolean;

  // Session information if session-specific (from backend DTO)
  sessionName?: string | null;
  sessionStartTime?: string | null;  // DateTime? in C# becomes string | null in TypeScript
  sessionEndTime?: string | null;    // DateTime? in C# becomes string | null in TypeScript

  // User's signup status for this position (if authenticated)
  hasUserSignedUp: boolean;
  userSignupId?: string | null;
}

/**
 * Request to sign up for a volunteer position
 */
export interface VolunteerSignupRequest {
  // Empty - no additional data required for signup
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
