/**
 * Ticket Assignment Feature - Public API
 *
 * Re-exports all public components, hooks, and types for the
 * ticket assignment feature. Other features should import from
 * this barrel file rather than reaching into internal directories.
 */

// Components
export { PendingTicketsCard } from './components/PendingTicketsCard'
export { TicketAcceptanceModal } from './components/TicketAcceptanceModal'
export { TicketDeclineModal } from './components/TicketDeclineModal'
export { TicketStatusBadge } from './components/TicketStatusBadge'
export { AssignTicketDropdown } from './components/AssignTicketDropdown'
export { TicketQuantitySelector } from './components/TicketQuantitySelector'

// Query hooks
export { usePendingAssignments, useAssignedTickets, ticketAssignmentKeys } from './api/queries'

// Mutation hooks
export {
  useAssignTicket,
  useAcceptAssignment,
  useDeclineAssignment,
  useReassignTicket,
} from './api/mutations'

// Types
export type {
  TicketAssignmentDto,
  PendingAssignmentDto,
  AssignedTicketStatusDto,
  AssignTicketRequest,
  AcceptAssignmentRequest,
  DeclineAssignmentRequest,
  TicketSelectionItem,
  TicketAssignmentResultDto,
} from './types/ticketAssignment.types'
