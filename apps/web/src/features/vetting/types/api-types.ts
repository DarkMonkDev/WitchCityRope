// Vetting API Types - ALIGNED WITH SHARED-TYPES PACKAGE
// These types extend/re-export the generated API types for better developer experience
import type { components } from '@witchcityrope/shared-types';

// Main API DTO types from generated schemas (source of truth)
export type CreateApplicationRequest = components['schemas']['CreateApplicationRequest'];
export type ReferenceRequest = components['schemas']['ReferenceRequest'];
export type ApplicationSubmissionResponse = components['schemas']['ApplicationSubmissionResponse'];
export type ApplicationStatusResponse = components['schemas']['ApplicationStatusResponse'];
export type ApplicationSummaryDto = components['schemas']['ApplicationSummaryDto'];
export type ApplicationDetailResponse = components['schemas']['ApplicationDetailResponse'];

// Form data type that matches the API request exactly
export interface VettingApplicationData extends CreateApplicationRequest {
  // No additional fields - form should match API exactly
}

// Helper type for reference form handling (UI convenience)
export interface ReferenceFormInput {
  name: string;
  email: string;
  relationship: string;
}

// Form state type for multi-step form (UI convenience only - not sent to API)
export interface ApplicationFormState {
  // Step 1: Personal Information
  fullName: string;
  sceneName: string;
  pronouns?: string;
  email: string;
  phone?: string;
  
  // Step 2: Experience & Knowledge  
  experienceLevel: number; // 1-4
  yearsExperience: number;
  experienceDescription: string;
  safetyKnowledge: string;
  consentUnderstanding: string;
  
  // Step 3: Community Understanding
  whyJoinCommunity: string;
  skillsInterests: string[];
  expectationsGoals: string;
  agreesToGuidelines: boolean;
  
  // Step 4: References (will be converted to array for API)
  reference1: ReferenceFormInput;
  reference2: ReferenceFormInput;
  reference3: ReferenceFormInput;
  
  // Step 5: Review & Submit
  agreesToTerms: boolean;
  isAnonymous: boolean;
  consentToContact: boolean;
}

// Transformation function to convert form state to API request
export function transformFormStateToApiRequest(formState: ApplicationFormState): CreateApplicationRequest {
  const references: ReferenceRequest[] = [
    {
      name: formState.reference1.name,
      email: formState.reference1.email,
      relationship: formState.reference1.relationship,
      order: 1
    },
    {
      name: formState.reference2.name,
      email: formState.reference2.email,
      relationship: formState.reference2.relationship,
      order: 2
    }
  ];

  // Add third reference if provided
  if (formState.reference3.name && formState.reference3.email) {
    references.push({
      name: formState.reference3.name,
      email: formState.reference3.email,
      relationship: formState.reference3.relationship,
      order: 3
    });
  }

  return {
    fullName: formState.fullName,
    sceneName: formState.sceneName,
    pronouns: formState.pronouns || null,
    email: formState.email,
    phone: formState.phone || null,
    experienceLevel: formState.experienceLevel,
    yearsExperience: formState.yearsExperience,
    experienceDescription: formState.experienceDescription,
    safetyKnowledge: formState.safetyKnowledge,
    consentUnderstanding: formState.consentUnderstanding,
    whyJoinCommunity: formState.whyJoinCommunity,
    skillsInterests: formState.skillsInterests,
    expectationsGoals: formState.expectationsGoals,
    agreesToGuidelines: formState.agreesToGuidelines,
    references,
    agreesToTerms: formState.agreesToTerms,
    isAnonymous: formState.isAnonymous,
    consentToContact: formState.consentToContact
  };
}