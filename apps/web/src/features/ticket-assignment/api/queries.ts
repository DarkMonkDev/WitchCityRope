/**
 * Ticket Assignment - TanStack Query hooks
 *
 * Query hooks for fetching pending assignments and assigned ticket statuses
 * for the user dashboard. Follows project patterns from events/api/queries.ts
 * and authorized-contacts/api/queries.ts.
 *
 * All queries use the shared apiClient (Axios instance with CSRF, auth cookies,
 * and RFC 9457 error extraction).
 *
 * API endpoints from api-design.md Section 2F:
 * - EP-16: GET /api/user/pending-assignments
 * - EP-17: GET /api/user/assigned-tickets
 */
import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../../lib/api/client'
import type {
  PendingAssignmentDto,
  AssignedTicketStatusDto,
} from '../types/ticketAssignment.types'

/**
 * Query key factory for ticket-assignment domain.
 * Follows the pattern from authorized-contacts/api/queries.ts.
 */
export const ticketAssignmentKeys = {
  all: ['ticket-assignments'] as const,
  pendingAssignments: () => [...ticketAssignmentKeys.all, 'pending'] as const,
  assignedTickets: () => [...ticketAssignmentKeys.all, 'assigned'] as const,
}

/**
 * Fetch pending assignments for the current user's dashboard.
 * Returns tickets and RSVPs that have been assigned to the current user
 * and are awaiting acceptance.
 *
 * EP-16: GET /api/user/pending-assignments
 */
export function usePendingAssignments() {
  return useQuery<PendingAssignmentDto[]>({
    queryKey: ticketAssignmentKeys.pendingAssignments(),
    queryFn: async (): Promise<PendingAssignmentDto[]> => {
      const response = await apiClient.get('/api/user/pending-assignments')
      return response.data || []
    },
    staleTime: 1 * 60 * 1000, // 1 minute - pending items may change frequently
    refetchOnWindowFocus: true, // Refetch when user returns to tab
  })
}

/**
 * Fetch tickets the current user purchased that are assigned to others
 * or are unassigned extras. Shows assignment status and whether each
 * can be reassigned.
 *
 * EP-17: GET /api/user/assigned-tickets
 */
export function useAssignedTickets() {
  return useQuery<AssignedTicketStatusDto[]>({
    queryKey: ticketAssignmentKeys.assignedTickets(),
    queryFn: async (): Promise<AssignedTicketStatusDto[]> => {
      const response = await apiClient.get('/api/user/assigned-tickets')
      return response.data || []
    },
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchOnWindowFocus: true,
  })
}
