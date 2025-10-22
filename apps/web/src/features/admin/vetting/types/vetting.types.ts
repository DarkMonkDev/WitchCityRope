// Import generated types from shared-types package
import type { components } from '@witchcityrope/shared-types';

// Re-export API types for convenience
export type ApplicationSummaryDto = components['schemas']['ApplicationSummaryDto'];
export type ApplicationReferenceStatus = components['schemas']['ApplicationReferenceStatus'];
export type ApplicationDetailResponse = components['schemas']['ApplicationDetailResponse'];
export type ReferenceDetailDto = components['schemas']['ReferenceDetailDto'];
export type ReferenceResponseDto = components['schemas']['ReferenceResponseDto'];
export type ApplicationNoteDto = components['schemas']['ApplicationNoteDto'];
export type ReviewDecisionDto = components['schemas']['ReviewDecisionDto'];
export type ReviewDecisionRequest = components['schemas']['ReviewDecisionRequest'];
export type ReviewDecisionResponse = components['schemas']['ReviewDecisionResponse'];

// Generic PagedResult type
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// Filter request (not in API, used for frontend state)
export interface ApplicationFilterRequest {
  // Pagination
  page: number;
  pageSize: number;

  // Status filtering
  statusFilters: string[];

  // Assignment filtering
  onlyMyAssignments?: boolean;
  onlyUnassigned?: boolean;
  assignedReviewerId?: string;

  // Priority filtering
  priorityFilters: number[];

  // Experience filtering
  experienceLevelFilters: number[];
  minYearsExperience?: number;
  maxYearsExperience?: number;

  // Skills/interests filtering
  skillsFilters: string[];

  // Date range filtering
  submittedAfter?: string;
  submittedBefore?: string;
  lastActivityAfter?: string;
  lastActivityBefore?: string;

  // Search functionality
  searchQuery?: string;

  // Reference status filtering
  onlyCompleteReferences?: boolean;
  onlyPendingReferences?: boolean;

  // Sorting
  sortBy: string;
  sortDirection: string;
}

// API Error types
export interface ApiError {
  title: string;
  detail: string;
  status: number;
  type?: string;
  extensions?: Record<string, any>;
}
