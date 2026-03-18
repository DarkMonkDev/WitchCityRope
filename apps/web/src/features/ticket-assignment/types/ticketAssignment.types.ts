/**
 * Ticket Assignment Types
 *
 * Types for the Ticket Assignment & Proxy RSVP feature.
 * Includes checkout types (Section 1B) and dashboard/assignment types (Sections 1C, 1E).
 *
 * When backend auto-generated types are available, replace these
 * with imports from @witchcityrope/shared-types.
 */

// ---------------------------------------------------------------------------
// Checkout Types (API Section 1B)
// ---------------------------------------------------------------------------

/** A single ticket selection with quantity and optional assignees */
export interface TicketSelectionItem {
  /** The ticket type being purchased */
  ticketTypeId: string
  /** Number of tickets of this type (1-10) */
  quantity: number
  /**
   * Optional per-ticket assignments.
   * Length should be quantity - 1 (first ticket is always for the purchaser).
   * null entries = "assign later".
   */
  assignees?: (string | null)[]
}

/** Result of a ticket assignment from checkout response */
export interface TicketAssignmentResultDto {
  attendanceId: string
  ticketPurchaseId: string
  ticketTypeName: string
  assignedToUserId?: string | null
  assignedToSceneName?: string | null
  status: string // "Active" or "PendingAcceptance"
}

// ---------------------------------------------------------------------------
// Assignment Operation Types (API Section 1C)
// ---------------------------------------------------------------------------

/** Response for assignment operations (EP-08, EP-09, EP-10, EP-11) */
export interface TicketAssignmentDto {
  attendanceId: string
  eventId: string
  eventTitle: string
  ticketTypeName: string
  status: string
  assignedToSceneName?: string
  assignedToUserId?: string
  assignedBySceneName?: string
  assignedByUserId?: string
  assignedAt?: string
  createdAt: string
}

/** Request body for assigning a ticket (EP-08, EP-11) */
export interface AssignTicketRequest {
  assignToUserId: string
}

/** Request body for accepting an assigned ticket or proxy RSVP (EP-09) */
export interface AcceptAssignmentRequest {
  eventWaiverAccepted: boolean
  termsOfServiceAccepted: boolean
}

/** Request body for declining - optional reason (EP-10) */
export interface DeclineAssignmentRequest {
  reason?: string
}

// ---------------------------------------------------------------------------
// Dashboard Types (API Section 1E)
// ---------------------------------------------------------------------------

/**
 * Pending assignment for the current user's dashboard (EP-16).
 * Represents a ticket or RSVP that someone purchased/created on behalf of this user
 * and is awaiting their acceptance.
 *
 * GET /api/user/pending-assignments
 */
export interface PendingAssignmentDto {
  attendanceId: string
  eventId: string
  eventTitle: string
  eventDate: string
  assignedBySceneName: string
  attendanceType: 'Ticket' | 'RSVP'
  ticketTypeName: string
  sessionNames: string[]
  assignedAt: string
}

/**
 * Tickets the current user purchased that are assigned to others or unassigned (EP-17).
 * Used on the purchaser's dashboard to show status and allow assign/reassign actions.
 *
 * GET /api/user/assigned-tickets
 */
export interface AssignedTicketStatusDto {
  attendanceId: string
  ticketPurchaseId: string
  eventId: string
  eventTitle: string
  eventDate: string
  ticketTypeName: string
  status: string
  assignedToUserId?: string
  assignedToSceneName?: string
  canReassign: boolean
  isUnassigned: boolean
}
