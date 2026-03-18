/**
 * Proxy RSVP - TanStack Query mutation hooks
 *
 * Mutation for creating proxy RSVPs on behalf of authorized contacts.
 * Follows project patterns from hooks/useParticipation.ts and
 * features/authorized-contacts/api/mutations.ts.
 *
 * CSRF tokens are automatically attached by the apiClient request interceptor
 * for all POST/PUT/DELETE requests.
 */
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { notifications } from '@mantine/notifications'
import { apiClient } from '../../../lib/api/client'
import { participationKeys } from '../../../hooks/useParticipation'
import { authorizedContactKeys } from '../../authorized-contacts/api/queries'
import type { CreateProxyRsvpRequest } from '../types/proxyRsvp.types'
import type { TicketAssignmentDto } from '../../ticket-assignment/types/ticketAssignment.types'

/**
 * Create a proxy RSVP for an authorized contact (principal).
 * The current user (delegate) creates an RSVP on behalf of a principal.
 *
 * POST /api/events/{eventId}/proxy-rsvp
 * Body: { rsvpForUserId: string, notes?: string }
 *
 * On success:
 * - Invalidates participation queries to refresh event detail page
 * - Invalidates principal contacts to remove the RSVP'd contact from dropdown
 * - Shows success notification
 */
export function useCreateProxyRsvp(eventId: string) {
  const queryClient = useQueryClient()

  return useMutation<TicketAssignmentDto, Error, CreateProxyRsvpRequest>({
    mutationFn: async (data: CreateProxyRsvpRequest): Promise<TicketAssignmentDto> => {
      const response = await apiClient.post(`/api/events/${eventId}/proxy-rsvp`, data)
      return response.data
    },
    onSuccess: () => {
      // Invalidate participation data to refresh the event detail page
      queryClient.invalidateQueries({
        queryKey: participationKeys.eventStatus(eventId),
      })

      // Invalidate user participations for dashboard
      queryClient.invalidateQueries({
        queryKey: participationKeys.userParticipations(),
      })

      // Invalidate principal contacts so the dropdown updates
      // (the RSVP'd contact should no longer appear as eligible)
      queryClient.invalidateQueries({
        queryKey: authorizedContactKeys.principals(eventId),
      })

      // Invalidate event data to refresh registration count
      queryClient.invalidateQueries({
        queryKey: ['events', 'detail', eventId],
      })

      // Invalidate user events for dashboard
      queryClient.invalidateQueries({
        queryKey: ['user-events'],
      })
    },
    onError: (error: Error) => {
      // apiClient interceptor extracts RFC 9457 message to error.message
      const errorMessage = error.message || 'Failed to create RSVP. Please try again.'

      notifications.show({
        title: 'RSVP Failed',
        message: errorMessage,
        color: 'red',
        autoClose: 7000,
      })

      console.error('Proxy RSVP failed:', error)
    },
  })
}
