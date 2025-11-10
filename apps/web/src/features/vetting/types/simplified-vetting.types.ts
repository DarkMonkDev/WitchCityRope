// Simplified Vetting Application Types
// Based on approved UI mockups and functional specification

/**
 * Simplified application form data for the new streamlined process
 * Based on the approved UI mockups which reduced complexity
 */
export interface SimplifiedApplicationFormData {
  firstName: string;
  lastName: string;
  pronouns?: string; // Optional field
  fetLifeHandle?: string; // Optional
  otherNames?: string; // Optional field
  whyJoin: string; // Required field
  experienceWithRope: string;
  agreeToCommunityStandards: boolean; // Matches backend AgreeToCommunityStandards
}

/**
 * Simplified application request for API
 * Maps to SimplifiedApplicationRequest on backend
 */
export interface SimplifiedCreateApplicationRequest {
  firstName: string;
  lastName: string;
  pronouns?: string;
  preferredSceneName: string; // Matches backend's PreferredSceneName
  fetLifeHandle?: string;
  otherNames?: string;
  email: string;
  whyJoin: string;
  experienceWithRope: string;
  agreeToCommunityStandards: boolean; // Matches backend's AgreeToCommunityStandards
}

/**
 * Simplified application status for user dashboard
 */
export interface SimplifiedApplicationStatus {
  applicationId: string;
  applicationNumber: string;
  submittedAt: string;
  confirmationMessage: string;
  emailSent: boolean;
  nextSteps: string;
  pronouns?: string;
  otherNames?: string;
  firstName: string;
  lastName: string;
  email: string;
  preferredSceneName: string;
  fetLifeHandle?: string;
  whyJoin: string;
  experienceWithRope: string;
  agreeToCommunityStandards: boolean;
  status: string;
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