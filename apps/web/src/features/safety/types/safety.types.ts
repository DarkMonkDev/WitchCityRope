// features/safety/types/safety.types.ts
// AUTO-GENERATED TYPE RE-EXPORTS from @witchcityrope/shared-types
// DO NOT manually define interfaces - import from generated types package
// See: /docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md

import type { components } from '@witchcityrope/shared-types';

// =============================================================================
// Safety DTOs - Re-exported from Generated Types
// =============================================================================

/**
 * Safety Incident Data Transfer Object (Detailed)
 * Source: C# IncidentResponse via NSwag generation
 */
export type SafetyIncidentDto = components['schemas']['IncidentResponse'];

/**
 * Incident Summary Data Transfer Object
 * Source: C# IncidentSummaryDto via NSwag generation
 */
export type IncidentSummaryDto = components['schemas']['IncidentSummaryDto'];

/**
 * Audit Log Data Transfer Object
 * Source: C# AuditLogDto via NSwag generation
 */
export type AuditLogDto = components['schemas']['AuditLogDto'];

/**
 * Action Item Data Transfer Object
 * Source: C# ActionItem via NSwag generation
 */
export type ActionItem = components['schemas']['ActionItem'];

/**
 * Safety Statistics Data Transfer Object
 * Source: C# SafetyStatistics via NSwag generation
 */
export type SafetyStatistics = components['schemas']['SafetyStatistics'];

// =============================================================================
// Request/Response Types - Re-exported from Generated Types
// =============================================================================

/**
 * Submit Incident Request
 * Source: C# CreateIncidentRequest via NSwag generation
 */
export type SubmitIncidentRequest = components['schemas']['CreateIncidentRequest'];

/**
 * Submission Response
 * Source: C# SubmissionResponse via NSwag generation
 */
export type SubmissionResponse = components['schemas']['SubmissionResponse'];

/**
 * Incident Status Response
 * Source: C# IncidentStatusResponse via NSwag generation
 */
export type IncidentStatusResponse = components['schemas']['IncidentStatusResponse'];

/**
 * Update Status Request
 * Source: C# UpdateStatusRequest via NSwag generation
 */
export type UpdateIncidentRequest = components['schemas']['UpdateStatusRequest'];

/**
 * Safety Dashboard Response
 * Source: C# AdminDashboardResponse via NSwag generation
 */
export type SafetyDashboardResponse = components['schemas']['AdminDashboardResponse'];

/**
 * Incident Summary Response
 * Source: C# IncidentSummaryResponse via NSwag generation
 */
export type IncidentSummaryResponse = components['schemas']['IncidentSummaryResponse'];

// =============================================================================
// Enums - Re-exported from Generated Types
// =============================================================================

/**
 * Incident Status Enum (String Literal Union Type)
 * Source: C# IncidentStatus via NSwag generation
 */
export type IncidentStatus = components['schemas']['IncidentStatus'];

/**
 * Incident Status Enum Values (for runtime usage)
 * Extracted from the string literal union type for use in Object.values(), etc.
 */
export const IncidentStatus = {
  ReportSubmitted: 'ReportSubmitted' as const,
  InformationGathering: 'InformationGathering' as const,
  ReviewingFinalReport: 'ReviewingFinalReport' as const,
  OnHold: 'OnHold' as const,
  Closed: 'Closed' as const
} as const;

/**
 * Incident Type Enum (String Literal Union Type)
 * Source: C# IncidentType via NSwag generation
 */
export type IncidentType = components['schemas']['IncidentType'];

/**
 * Incident Type Enum Values (for runtime usage)
 * Extracted from the string literal union type for use in Object.values(), etc.
 */
export const IncidentType = {
  SafetyConcern: 'SafetyConcern' as const,
  BoundaryViolation: 'BoundaryViolation' as const,
  Harassment: 'Harassment' as const,
  OtherConcern: 'OtherConcern' as const
} as const;

/**
 * Where Occurred Enum (String Literal Union Type)
 * Source: C# WhereOccurred via NSwag generation
 */
export type WhereOccurred = components['schemas']['WhereOccurred'];

/**
 * Where Occurred Enum Values (for runtime usage)
 * Extracted from the string literal union type for use in Object.values(), etc.
 */
export const WhereOccurred = {
  AtEvent: 'AtEvent' as const,
  Online: 'Online' as const,
  PrivatePlay: 'PrivatePlay' as const,
  OtherSpace: 'OtherSpace' as const
} as const;

/**
 * Spoken to Person Status Enum (Nullable String Literal Union Type)
 * Source: C# NullableOfSpokenToPersonStatus via NSwag generation
 */
export type SpokenToPersonStatus = components['schemas']['NullableOfSpokenToPersonStatus'];

/**
 * Spoken to Person Status Enum Values (for runtime usage)
 * Extracted from the nullable string literal union type
 */
export const SpokenToPersonStatus = {
  Yes: 'Yes' as const,
  No: 'No' as const,
  NotApplicable: 'NotApplicable' as const
} as const;

// =============================================================================
// Type Transformations - Handle Backend Field Name Differences
// =============================================================================

/**
 * Frontend-compatible version of IncidentResponse
 * Maps backend field names to frontend expectations
 *
 * Backend uses: assignedTo, assignedUserName
 * Frontend expects: coordinatorId, coordinatorName
 *
 * Note: IncidentSummaryDto already has correct field names (coordinatorId, coordinatorName)
 */
export interface FrontendIncidentDetails extends Omit<SafetyIncidentDto, 'assignedTo' | 'assignedUserName'> {
  coordinatorId?: string | null;
  coordinatorName?: string | null;
  // NOTE: contactPhone does NOT exist in backend - field removed from UI
}

/**
 * Transform IncidentResponse from backend to frontend-compatible format
 * Maps assignedTo -> coordinatorId and assignedUserName -> coordinatorName
 */
export function mapIncidentDetailFromBackend(
  backendDto: SafetyIncidentDto
): FrontendIncidentDetails {
  // Destructure to remove backend-specific fields
  const { assignedTo, assignedUserName, ...rest } = backendDto;

  return {
    ...rest,
    coordinatorId: assignedTo,
    coordinatorName: assignedUserName,
  };
}

/**
 * IncidentSummaryDto already has correct field names (coordinatorId, coordinatorName)
 * No transformation needed - can be used directly in the UI
 *
 * Type alias for clarity in component props
 */
export type FrontendIncidentSummary = IncidentSummaryDto;

// =============================================================================
// Frontend-Only Types (NOT from API)
// =============================================================================
// These types are UI state only and should NOT be in the generated types package

/**
 * Incident Form Data (Frontend-only)
 * Form state structure for incident submission form
 * NOT an API DTO - this is frontend form state
 */
export interface IncidentFormData {
  // Wireframe fields (title removed - auto-generated by backend)
  incidentType: IncidentType;
  incidentDate: string; // Date in YYYY-MM-DD format
  incidentTime: string; // Time in HH:MM format
  whereOccurred: WhereOccurred;
  eventName: string;
  location: string;
  description: string;
  witnesses: string;
  involvedParties: string;
  hasSpokenToPerson: SpokenToPersonStatus | '';
  desiredOutcomes: string; // Free-text field
  futureInteractionPreference: string; // What interaction with person going forward
  anonymousDuringInvestigation: boolean; // Anonymous during investigation
  anonymousInFinalReport: boolean; // Anonymous in final report
  // Privacy fields
  isAnonymous: boolean;
  requestFollowUp: boolean;
  contactEmail: string;
  contactName: string;
}

/**
 * Search Incidents Request (Frontend-only)
 * Query parameters for incident search - may not match backend exactly
 */
export interface SearchIncidentsRequest {
  status?: string; // Status filter (comma-separated for multiple values)
  assignedTo?: string;
  dateFrom?: string;
  dateTo?: string;
  searchText?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string; // Sort field (default: 'reportedAt')
  sortDirection?: 'asc' | 'desc'; // Sort direction (default: 'desc')
}

// =============================================================================
// UI Configuration Constants (Frontend-only)
// =============================================================================
// These provide human-readable labels and colors for the UI

/**
 * Status configurations for UI display
 * Maps IncidentStatus enum values to display labels and colors
 */
export const STATUS_CONFIGS: Record<string, { label: string; color: string }> = {
  ReportSubmitted: { label: 'Report Submitted', color: 'blue' },
  InformationGathering: { label: 'Information Gathering', color: 'yellow' },
  ReviewingFinalReport: { label: 'Reviewing Final Report', color: 'orange' },
  OnHold: { label: 'On Hold', color: 'gray' },
  Closed: { label: 'Closed', color: 'green' }
};

/**
 * Incident type display labels
 * Maps IncidentType enum values to human-readable strings
 */
export const INCIDENT_TYPE_LABELS: Record<string, string> = {
  SafetyConcern: 'Safety Concern',
  BoundaryViolation: 'Boundary Violation',
  Harassment: 'Harassment',
  OtherConcern: 'Other Concern'
};

/**
 * Where occurred display labels
 * Maps WhereOccurred enum values to human-readable strings
 */
export const WHERE_OCCURRED_LABELS: Record<string, string> = {
  AtEvent: 'At a Witch City Rope event',
  Online: 'Online (Discord, social media, etc.)',
  PrivatePlay: 'Private play/interaction',
  OtherSpace: 'Other community space'
};

/**
 * Spoken to person display labels
 * Maps SpokenToPersonStatus enum values to human-readable strings
 */
export const SPOKEN_TO_PERSON_LABELS: Record<string, string> = {
  Yes: 'Yes',
  No: 'No',
  NotApplicable: 'Not Applicable'
};
