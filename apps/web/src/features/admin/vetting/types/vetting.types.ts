// Note: In production, these should be imported from @witchcityrope/shared-types
// For now, we'll define them locally based on the API DTOs

export interface ApplicationSummaryDto {
  id: string;
  applicationNumber: string;
  status: string;
  submittedAt: string;
  lastActivityAt?: string;

  // Applicant information (masked for privacy)
  sceneName: string;
  experienceLevel: string;
  yearsExperience: number;
  isAnonymous: boolean;

  // Review information
  assignedReviewerName?: string;
  reviewStartedAt?: string;
  priority: number;
  daysInCurrentStatus: number;

  // Reference status
  referenceStatus: ApplicationReferenceStatus;

  // Recent activity indicators
  hasRecentNotes: boolean;
  hasPendingActions: boolean;
  interviewScheduledFor?: string;

  // Skills/interests tags (for filtering)
  skillsTags: string[];
}

export interface ApplicationReferenceStatus {
  totalReferences: number;
  contactedReferences: number;
  respondedReferences: number;
  allReferencesComplete: boolean;
  oldestPendingReferenceDate?: string;
}

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

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface ApplicationDetailResponse {
  id: string;
  applicationNumber: string;
  status: string;
  submittedAt: string;
  lastActivityAt?: string;

  // Applicant Information (Decrypted)
  fullName: string;
  sceneName: string;
  pronouns?: string;
  email: string;
  phone?: string;

  // Experience Information
  experienceLevel: string;
  yearsExperience: number;
  experienceDescription: string;
  safetyKnowledge: string;
  consentUnderstanding: string;

  // Community Information
  whyJoinCommunity: string;
  skillsInterests: string[];
  expectationsGoals: string;
  agreesToGuidelines: boolean;

  // Privacy Settings
  isAnonymous: boolean;
  agreesToTerms: boolean;
  consentToContact: boolean;

  // Review Information
  assignedReviewerName?: string;
  reviewStartedAt?: string;
  priority: number;
  interviewScheduledFor?: string;

  // References
  references: ReferenceDetailDto[];

  // Notes and Decisions
  notes: ApplicationNoteDto[];
  decisions: ReviewDecisionDto[];
}

export interface ReferenceDetailDto {
  id: string;
  name: string;
  email: string;
  relationship: string;
  order: number;
  status: string;
  contactedAt?: string;
  respondedAt?: string;
  formExpiresAt?: string;

  // Response data (if available)
  response?: ReferenceResponseDto;
}

export interface ReferenceResponseDto {
  relationshipDuration: string;
  experienceAssessment: string;
  safetyConcerns?: string;
  communityReadiness: string;
  recommendation: string;
  additionalComments?: string;
  respondedAt: string;
}

export interface ApplicationNoteDto {
  id: string;
  content: string;
  type: string;
  isPrivate: boolean;
  tags: string[];
  reviewerName: string;
  createdAt: string;
  updatedAt: string;
}

export interface ReviewDecisionDto {
  id: string;
  decisionType: string;
  reasoning: string;
  score?: number;
  isFinalDecision: boolean;
  additionalInfoRequested?: string;
  additionalInfoDeadline?: string;
  proposedInterviewTime?: string;
  interviewNotes?: string;
  reviewerName: string;
  createdAt: string;
}

export interface ReviewDecisionRequest {
  decisionType: string;
  reasoning: string;
  score?: number;
  isFinalDecision: boolean;
  additionalInfoRequested?: string;
  additionalInfoDeadline?: string;
  proposedInterviewTime?: string;
  interviewNotes?: string;
}

export interface ReviewDecisionResponse {
  success: boolean;
  message: string;
  updatedStatus: string;
}

// API Error types
export interface ApiError {
  title: string;
  detail: string;
  status: number;
  type?: string;
  extensions?: Record<string, any>;
}