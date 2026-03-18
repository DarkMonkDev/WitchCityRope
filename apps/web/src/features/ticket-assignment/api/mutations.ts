/**
 * Ticket Assignment - TanStack Query mutation hooks
 *
 * Mutations for assigning, accepting, declining, and reassigning tickets.
 * Follows project patterns from events/api/mutations.ts and
 * authorized-contacts/api/mutations.ts.
 *
 * CSRF tokens are automatically attached by the apiClient request interceptor
 * for all POST/PUT/DELETE requests (see lib/api/client.ts).
 *
 * API endpoints from api-design.md Sections 2C:
 * - EP-08: POST /api/events/{eventId}/tickets/{attendanceId}/assign
 * - EP-09: POST /api/events/{eventId}/tickets/{attendanceId}/accept
 * - EP-10: POST /api/events/{eventId}/tickets/{attendanceId}/decline
 * - EP-11: POST /api/events/{eventId}/tickets/{attendanceId}/reassign
 */
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../../lib/api/client'
import { ticketAssignmentKeys } from './queries'
import { queryKeys } from '../../../api/queryKeys'
import type {
  TicketAssignmentDto,
  AssignTicketRequest,
  AcceptAssignmentRequest,
  DeclineAssignmentRequest,
} from '../types/ticketAssignment.types'

/**
 * Assign a ticket to an authorized contact (post-purchase).
 * Used when a purchaser wants to assign an unassigned ticket from their dashboard.
 *
 * EP-08: POST /api/events/{eventId}/tickets/{attendanceId}/assign
 */
export function useAssignTicket(eventId: string, attendanceId: string) {
  const queryClient = useQueryClient()

  return useMutation<TicketAssignmentDto, Error, AssignTicketRequest>({
    mutationFn: async (data: AssignTicketRequest): Promise<TicketAssignmentDto> => {
      const response = await apiClient.post(
        `/api/events/${eventId}/tickets/${attendanceId}/assign`,
        data
      )
      return response.data
    },
    onSuccess: () => {
      // Invalidate both pending and assigned queries
      queryClient.invalidateQueries({ queryKey: ticketAssignmentKeys.all })
      // Also invalidate the user events query (dashboard may show updated status)
      queryClient.invalidateQueries({ queryKey: ['user-events'] })
      // Invalidate event detail for capacity updates
      queryClient.invalidateQueries({ queryKey: queryKeys.event(eventId) })
    },
    onError: (error: Error) => {
      console.error('Failed to assign ticket:', error)
    },
  })
}

/**
 * Accept an assigned ticket or proxy RSVP.
 * Called when the assignee accepts a pending assignment from their dashboard.
 *
 * EP-09: POST /api/events/{eventId}/tickets/{attendanceId}/accept
 */
export function useAcceptAssignment(eventId: string, attendanceId: string) {
  const queryClient = useQueryClient()

  return useMutation<TicketAssignmentDto, Error, AcceptAssignmentRequest>({
    mutationFn: async (data: AcceptAssignmentRequest): Promise<TicketAssignmentDto> => {
      const response = await apiClient.post(
        `/api/events/${eventId}/tickets/${attendanceId}/accept`,
        data
      )
      return response.data
    },
    onSuccess: () => {
      // Invalidate pending assignments (accepted item should disappear)
      queryClient.invalidateQueries({ queryKey: ticketAssignmentKeys.pendingAssignments() })
      // Invalidate user events (new event should appear in My Events)
      queryClient.invalidateQueries({ queryKey: ['user-events'] })
      // Invalidate event detail
      queryClient.invalidateQueries({ queryKey: queryKeys.event(eventId) })
      // Invalidate current user (ToS may have been accepted)
      queryClient.invalidateQueries({ queryKey: queryKeys.currentUser() })
    },
    onError: (error: Error) => {
      console.error('Failed to accept assignment:', error)
    },
  })
}

/**
 * Decline an assigned ticket or proxy RSVP.
 * Called when the assignee declines a pending assignment from their dashboard.
 * The ticket reverts to the purchaser who can then reassign it.
 *
 * EP-10: POST /api/events/{eventId}/tickets/{attendanceId}/decline
 */
export function useDeclineAssignment(eventId: string, attendanceId: string) {
  const queryClient = useQueryClient()

  return useMutation<TicketAssignmentDto, Error, DeclineAssignmentRequest | undefined>({
    mutationFn: async (data?: DeclineAssignmentRequest): Promise<TicketAssignmentDto> => {
      const response = await apiClient.post(
        `/api/events/${eventId}/tickets/${attendanceId}/decline`,
        data || {}
      )
      return response.data
    },
    onSuccess: () => {
      // Invalidate pending assignments (declined item should disappear)
      queryClient.invalidateQueries({ queryKey: ticketAssignmentKeys.pendingAssignments() })
      // Invalidate assigned tickets (purchaser now sees declined status)
      queryClient.invalidateQueries({ queryKey: ticketAssignmentKeys.assignedTickets() })
      // Invalidate user events
      queryClient.invalidateQueries({ queryKey: ['user-events'] })
      // Invalidate event detail
      queryClient.invalidateQueries({ queryKey: queryKeys.event(eventId) })
    },
    onError: (error: Error) => {
      console.error('Failed to decline assignment:', error)
    },
  })
}

/**
 * Reassign a declined ticket to a new authorized contact.
 * Used when a ticket was declined and the purchaser wants to assign it to someone else.
 *
 * EP-11: POST /api/events/{eventId}/tickets/{attendanceId}/reassign
 */
export function useReassignTicket(eventId: string, attendanceId: string) {
  const queryClient = useQueryClient()

  return useMutation<TicketAssignmentDto, Error, AssignTicketRequest>({
    mutationFn: async (data: AssignTicketRequest): Promise<TicketAssignmentDto> => {
      const response = await apiClient.post(
        `/api/events/${eventId}/tickets/${attendanceId}/reassign`,
        data
      )
      return response.data
    },
    onSuccess: () => {
      // Invalidate both pending and assigned queries
      queryClient.invalidateQueries({ queryKey: ticketAssignmentKeys.all })
      // Invalidate user events
      queryClient.invalidateQueries({ queryKey: ['user-events'] })
      // Invalidate event detail
      queryClient.invalidateQueries({ queryKey: queryKeys.event(eventId) })
    },
    onError: (error: Error) => {
      console.error('Failed to reassign ticket:', error)
    },
  })
}
