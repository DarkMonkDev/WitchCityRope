// Vetting System TypeScript Types
// Based on backend API design and UI specifications

// ===== ENUMS =====

export enum ExperienceLevel {
  Beginner = 1,
  Intermediate = 2,
  Advanced = 3,
  Expert = 4
}

export enum ApplicationStatus {
  Draft = 'draft',
  Submitted = 'submitted',
  ReferencesContacted = 'references-contacted',
  UnderReview = 'under-review',
  PendingInterview = 'pending-interview',
  PendingAdditionalInfo = 'pending-additional-info',
  Approved = 'approved',
  Denied = 'denied',
  Withdrawn = 'withdrawn'
}

export enum ReferenceStatus {
  Pending = 'pending',
  Contacted = 'contacted',
  Responded = 'responded',
  Overdue = 'overdue'
}

export enum DecisionType {
  Approve = 'approve',
  Deny = 'deny',
  RequestInfo = 'request-info',
  ScheduleInterview = 'schedule-interview',
  Reassign = 'reassign'
}

export enum RecommendationLevel {
  StronglySupport = 'strongly-support',
  Support = 'support',
  Neutral = 'neutral',
  DoNotSupport = 'do-not-support'
}

// ===== REQUEST TYPES =====

/**
 * Reference information for application
 */
export interface ReferenceRequest {
  name: string;
  email: string;
  relationship: string;
  order: number; // 1, 2, or 3
}

/**
 * Complete application submission request
 * Maps to CreateApplicationRequest C# model
 */
export interface CreateApplicationRequest {
  // Personal Information (Step 1)
  fullName: string;
  sceneName: string;
  pronouns?: string;
  email: string;
  phone?: string;

  // Experience & Knowledge (Step 2)
  experienceLevel: ExperienceLevel;
  yearsExperience: number;
  experienceDescription: string;
  safetyKnowledge: string;
  consentUnderstanding: string;

  // Community Understanding (Step 3)
  whyJoinCommunity: string;
  skillsInterests: string[];
  expectationsGoals: string;
  agreesToGuidelines: boolean;

  // References (Step 4)
  references: ReferenceRequest[];

  // Review & Submit (Step 5)
  agreesToTerms: boolean;
  isAnonymous: boolean;
  consentToContact: boolean;
}

/**
 * Application filter request for reviewer dashboard
 */
export interface ApplicationFilterRequest {
  status?: ApplicationStatus;
  assignedTo?: string;
  dateFrom?: string; // ISO date
  dateTo?: string; // ISO date
  searchText?: string;
  page?: number;
  pageSize?: number;
}

/**
 * Review decision submission
 */
export interface ReviewDecisionRequest {
  decisionType: DecisionType;
  notes: string;
  reasoning?: string;
  score?: number;
}

// ===== RESPONSE TYPES =====

/**
 * Application submission response
 */
export interface ApplicationSubmissionResponse {
  applicationId: string;
  applicationNumber: string;
  statusToken: string;
  submittedAt: string; // ISO timestamp
  statusCheckUrl: string;
  estimatedTimeToReview: string;
}

/**
 * Public application status response
 * For applicant status checking
 */
export interface ApplicationStatusResponse {
  applicationNumber: string;
  status: ApplicationStatus;
  submittedAt: string; // ISO timestamp
  statusDescription: string;
  lastUpdateAt?: string; // ISO timestamp
  estimatedDaysRemaining?: number;
  progress: ApplicationProgressSummary;
  recentUpdates: StatusUpdateSummary[];
}

/**
 * Application progress breakdown
 */
export interface ApplicationProgressSummary {
  applicationSubmitted: boolean;
  referencesContacted: boolean;
  referencesReceived: boolean;
  underReview: boolean;
  interviewScheduled: boolean;
  decisionMade: boolean;
  progressPercentage: number; // 0-100
  currentPhase: string;
}

/**
 * Status update summary for applicant view
 */
export interface StatusUpdateSummary {
  updatedAt: string; // ISO timestamp
  message: string;
  type: string; // "StatusChange", "ReferenceUpdate", "Communication"
}

/**
 * Application summary for reviewer dashboard
 */
export interface ApplicationSummaryDto {
  id: string;
  applicationNumber: string;
  status: ApplicationStatus;
  submittedAt: string; // ISO timestamp
  lastActivityAt?: string; // ISO timestamp
  
  // Applicant information (privacy-masked)
  sceneName: string;
  experienceLevel: string;
  yearsExperience: number;
  isAnonymous: boolean;
  
  // Review information
  assignedReviewerName?: string;
  reviewStartedAt?: string; // ISO timestamp
  priority: number;
  daysInCurrentStatus: number;
  
  // Reference status
  referenceStatus: ApplicationReferenceStatus;
  
  // Activity indicators
  hasRecentNotes: boolean;
  hasPendingActions: boolean;
  interviewScheduledFor?: string; // ISO timestamp
  
  // Skills/interests tags
  skillsTags: string[];
}

/**
 * Reference status summary
 */
export interface ApplicationReferenceStatus {
  totalReferences: number;
  contactedReferences: number;
  respondedReferences: number;
  allReferencesComplete: boolean;
  oldestPendingReferenceDate?: string; // ISO timestamp
}

/**
 * Detailed application information for reviewer
 */
export interface ApplicationDetailResponse {
  id: string;
  applicationNumber: string;
  status: ApplicationStatus;
  submittedAt: string; // ISO timestamp
  lastActivityAt?: string; // ISO timestamp
  
  // Personal Information (encrypted fields visible to reviewers)
  personalInfo: {
    fullName: string;
    sceneName: string;
    pronouns?: string;
    email: string;
    phone?: string;
  };
  
  // Experience Details
  experience: {
    level: ExperienceLevel;
    yearsExperience: number;
    description: string;
    safetyKnowledge: string;
    consentUnderstanding: string;
  };
  
  // Community Understanding
  community: {
    whyJoin: string;
    skillsInterests: string[];
    expectations: string;
    agreesToGuidelines: boolean;
  };
  
  // References
  references: ApplicationReference[];
  
  // Privacy Settings
  privacy: {
    isAnonymous: boolean;
    agreesToTerms: boolean;
    consentToContact: boolean;
  };
  
  // System Fields
  assignedReviewerId?: string;
  assignedReviewerName?: string;
  reviewNotes: ReviewNote[];
  decisionHistory: DecisionRecord[];
  auditTrail: AuditEntry[];
}

/**
 * Application reference with response data
 */
export interface ApplicationReference {
  id: string;
  name: string;
  email: string;
  relationship: string;
  status: ReferenceStatus;
  contactedAt?: string; // ISO timestamp
  respondedAt?: string; // ISO timestamp
  response?: ReferenceResponse;
  remindersSent: number;
  lastReminderSent?: string; // ISO timestamp
}

/**
 * Reference response data
 */
export interface ReferenceResponse {
  relationshipDuration: string;
  experienceAssessment: string;
  safetyConcerns?: string;
  communityReadiness: string;
  recommendation: RecommendationLevel;
  additionalComments?: string;
  respondedAt: string; // ISO timestamp
}

/**
 * Review note for internal team use
 */
export interface ReviewNote {
  id: string;
  reviewerId: string;
  reviewerName: string;
  createdAt: string; // ISO timestamp
  content: string;
  isPrivate: boolean;
  tags: string[];
}

/**
 * Decision record for audit trail
 */
export interface DecisionRecord {
  id: string;
  reviewerId: string;
  reviewerName: string;
  decisionType: DecisionType;
  reasoning: string;
  createdAt: string; // ISO timestamp
  score?: number;
}

/**
 * Audit trail entry
 */
export interface AuditEntry {
  id: string;
  action: string;
  description: string;
  userId?: string;
  userName?: string;
  timestamp: string; // ISO timestamp
  metadata?: Record<string, any>;
}

/**
 * Paginated response wrapper
 */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// ===== FRONTEND-SPECIFIC TYPES =====

/**
 * Multi-step form data structure
 */
export interface ApplicationFormData {
  // Step 1: Personal Information
  personalInfo: {
    fullName: string;
    sceneName: string;
    pronouns: string;
    email: string;
    phone: string;
  };
  
  // Step 2: Experience & Knowledge
  experience: {
    level: ExperienceLevel;
    yearsExperience: number;
    description: string;
    safetyKnowledge: string;
    consentUnderstanding: string;
  };
  
  // Step 3: Community Understanding
  community: {
    whyJoin: string;
    skillsInterests: string[];
    expectations: string;
    agreesToGuidelines: boolean;
  };
  
  // Step 4: References
  references: {
    reference1: ReferenceFormData;
    reference2: ReferenceFormData;
    reference3: ReferenceFormData;
  };
  
  // Step 5: Review & Submit
  review: {
    agreesToTerms: boolean;
    isAnonymous: boolean;
    consentToContact: boolean;
  };
}

/**
 * Individual reference form data
 */
export interface ReferenceFormData {
  name: string;
  email: string;
  relationship: string;
}

/**
 * Dashboard filter state
 */
export interface DashboardFilters {
  status: ApplicationStatus | 'all';
  assignedTo: string | 'all' | 'unassigned' | 'mine';
  dateRange: 'last7' | 'last30' | 'custom';
  customDateFrom?: string;
  customDateTo?: string;
  search: string;
}

/**
 * Dashboard statistics
 */
export interface DashboardStats {
  totalApplications: number;
  newApplications: number;
  inReview: number;
  pendingInterview: number;
  awaitingReferences: number;
  averageReviewTime: number;
  approvalRate: number;
  thisWeekSubmissions: number;
}

// ===== UI CONFIGURATION TYPES =====

/**
 * Experience level configuration for UI
 */
export interface ExperienceLevelConfig {
  value: ExperienceLevel;
  label: string;
  description: string;
  color: string;
}

/**
 * Application status configuration for UI
 */
export interface ApplicationStatusConfig {
  value: ApplicationStatus;
  label: string;
  color: string;
  description: string;
  icon: string;
}

// ===== UI CONFIGURATIONS =====

export const EXPERIENCE_LEVEL_CONFIGS: Record<ExperienceLevel, ExperienceLevelConfig> = {
  [ExperienceLevel.Beginner]: {
    value: ExperienceLevel.Beginner,
    label: 'Beginner',
    description: 'New to rope bondage, eager to learn',
    color: '#4285F4'
  },
  [ExperienceLevel.Intermediate]: {
    value: ExperienceLevel.Intermediate,
    label: 'Intermediate',
    description: 'Some experience with basic techniques',
    color: '#34A853'
  },
  [ExperienceLevel.Advanced]: {
    value: ExperienceLevel.Advanced,
    label: 'Advanced',
    description: 'Extensive experience and knowledge',
    color: '#FBBC05'
  },
  [ExperienceLevel.Expert]: {
    value: ExperienceLevel.Expert,
    label: 'Expert',
    description: 'Professional level expertise',
    color: '#EA4335'
  }
};

export const APPLICATION_STATUS_CONFIGS: Record<ApplicationStatus, ApplicationStatusConfig> = {
  [ApplicationStatus.Draft]: {
    value: ApplicationStatus.Draft,
    label: 'Draft',
    color: 'gray',
    description: 'Application in progress',
    icon: '📝'
  },
  [ApplicationStatus.Submitted]: {
    value: ApplicationStatus.Submitted,
    label: 'Submitted',
    color: 'blue',
    description: 'Application submitted, awaiting review',
    icon: '📬'
  },
  [ApplicationStatus.ReferencesContacted]: {
    value: ApplicationStatus.ReferencesContacted,
    label: 'References Contacted',
    color: 'cyan',
    description: 'References have been contacted for verification',
    icon: '📞'
  },
  [ApplicationStatus.UnderReview]: {
    value: ApplicationStatus.UnderReview,
    label: 'Under Review',
    color: 'yellow',
    description: 'Being reviewed by vetting team',
    icon: '👀'
  },
  [ApplicationStatus.PendingInterview]: {
    value: ApplicationStatus.PendingInterview,
    label: 'Pending Interview',
    color: 'orange',
    description: 'Interview scheduled or pending',
    icon: '🎤'
  },
  [ApplicationStatus.PendingAdditionalInfo]: {
    value: ApplicationStatus.PendingAdditionalInfo,
    label: 'Additional Info Needed',
    color: 'indigo',
    description: 'Waiting for additional information',
    icon: '❓'
  },
  [ApplicationStatus.Approved]: {
    value: ApplicationStatus.Approved,
    label: 'Approved',
    color: 'green',
    description: 'Application approved, welcome to community',
    icon: '✅'
  },
  [ApplicationStatus.Denied]: {
    value: ApplicationStatus.Denied,
    label: 'Denied',
    color: 'red',
    description: 'Application not approved',
    icon: '❌'
  },
  [ApplicationStatus.Withdrawn]: {
    value: ApplicationStatus.Withdrawn,
    label: 'Withdrawn',
    color: 'gray',
    description: 'Application withdrawn by applicant',
    icon: '🚫'
  }
};

/**
 * Skills and interests predefined options
 */
export const SKILLS_INTERESTS_OPTIONS = [
  'Rope Bondage',
  'Suspension',
  'Shibari',
  'Kinbaku',
  'Photography',
  'Performance',
  'Teaching',
  'Safety',
  'Rigging',
  'Floor Work',
  'Partial Suspension',
  'Dynamic Rope',
  'Competition',
  'Community Building',
  'Event Planning',
  'Mentoring',
  'Creative Arts',
  'Dance Movement',
  'Meditation',
  'Healing Arts'
];

/**
 * Touch targets for mobile optimization
 */
export const TOUCH_TARGETS = {
  MINIMUM: 44, // px - absolute minimum for accessibility
  PREFERRED: 48, // px - preferred size for comfortable interaction
  BUTTON_HEIGHT: 56, // px - standard button height for forms
  INPUT_HEIGHT: 56, // px - form input height for comfortable typing
  CARD_MIN_HEIGHT: 80 // px - minimum card height for application cards
} as const;