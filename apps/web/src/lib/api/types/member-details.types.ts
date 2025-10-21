/**
 * Member Details Types
 * TypeScript interfaces matching backend DTOs from MemberDetailsModels.cs
 */

export interface MemberDetailsResponse {
  userId: string
  sceneName: string
  email?: string
  discordName?: string
  fetLifeHandle?: string
  role: string
  isActive: boolean
  createdAt: string
  lastLoginAt?: string

  // Participation summary
  totalEventsAttended: number
  totalEventsRegistered: number
  activeRegistrations: number
  lastEventAttended?: string

  // Vetting status
  vettingStatus: number
  vettingStatusDisplay: string
  hasVettingApplication: boolean
}

export interface VettingDetailsResponse {
  hasApplication: boolean
  applicationId?: string
  applicationNumber?: string
  submittedAt?: string
  workflowStatus?: number
  workflowStatusDisplay?: string
  lastReviewedAt?: string
  decisionMadeAt?: string

  // Questionnaire responses
  sceneName?: string
  realName?: string
  otherNames?: string
  email?: string
  phone?: string
  fetLifeHandle?: string
  pronouns?: string
  aboutYourself?: string
  experienceLevel?: number
  yearsExperience?: number
  experienceDescription?: string
  experienceWithRope?: string
  safetyKnowledge?: string
  consentUnderstanding?: string
  whyJoin?: string
  whyJoinCommunity?: string
  skillsInterests?: string
  expectationsGoals?: string
  agreesToGuidelines?: boolean
  agreesToTerms?: boolean
  updatedAt?: string

  // Admin notes
  adminNotes?: string
}

export interface EventHistoryRecord {
  eventId: string
  eventTitle: string
  eventType: string
  eventDate: string
  registrationType: string // "RSVP", "Ticket", "Attended"
  participationStatus?: string // "Active", "Cancelled", etc.
  registeredAt?: string
  cancelledAt?: string
  amountPaid?: number
  attended: boolean
}

export interface EventHistoryResponse {
  events: EventHistoryRecord[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface VolunteerHistoryRecord {
  eventId: string
  eventTitle: string
  eventDate: string
  role: string
  showedUp: boolean
}

export interface VolunteerHistoryResponse {
  volunteers: VolunteerHistoryRecord[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface MemberIncidentRecord {
  incidentId: string
  referenceNumber: string
  title?: string
  incidentDate: string
  reportedAt: string
  status: string
  location: string
  description: string
  involvedParties?: string
  witnesses?: string
  userInvolvementType: string // "Reporter", "Subject", "Witness"
}

export interface MemberIncidentsResponse {
  incidents: MemberIncidentRecord[]
  totalCount: number
}

export interface UserNoteResponse {
  id: string
  userId: string
  content: string
  noteType: string // "Vetting", "General", "Administrative", "StatusChange"
  authorId?: string
  authorSceneName?: string
  createdAt: string
  isArchived: boolean
}

export interface CreateUserNoteRequest {
  content: string
  noteType: string // "Vetting", "General", "Administrative", "StatusChange"
}

export interface UpdateMemberStatusRequest {
  isActive: boolean
  reason?: string
}

export interface UpdateMemberRoleRequest {
  role: string // "Administrator", "Teacher", "SafetyTeam" (or empty string for regular member)
}
