// Simplified Vetting Application Types
// Based on approved UI mockups and functional specification

/**
 * Simplified application form data for the new streamlined process
 * Based on the approved UI mockups which reduced complexity
 */
export interface SimplifiedApplicationFormData {
  realName: string;
  pronouns?: string; // Optional field
  fetLifeHandle?: string; // Optional
  otherNames?: string; // Optional field
  whyJoin: string; // Required field
  experienceWithRope: string;
  agreesToCommunityStandards: boolean;
}

/**
 * Simplified application request for API
 * Maps to the simplified CreateApplicationRequest on backend
 */
export interface SimplifiedCreateApplicationRequest {
  realName: string;
  pronouns?: string;
  sceneName: string;
  fetLifeHandle?: string;
  otherNames?: string;
  email: string;
  whyJoin: string;
  experienceWithRope: string;
  agreesToCommunityStandards: boolean;
  howFoundUs?: string; // Optional field for tracking
}

/**
 * Simplified application status for user dashboard
 */
export interface SimplifiedApplicationStatus {
  id: string;
  status: 'submitted' | 'under-review' | 'interview-approved' | 'interview-scheduled' | 'approved' | 'denied' | 'on-hold';
  submittedAt: string;
  lastUpdated: string;
  statusMessage: string;
  canResubmit: boolean;
}

/**
 * Application submission response
 */
export interface SimplifiedApplicationSubmissionResponse {
  applicationId: string;
  applicationNumber: string;
  submittedAt: string;
  confirmationEmail: string;
  statusPageUrl: string;
  estimatedReviewTime: string;
}