// features/events/api/mutations.ts
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../../lib/api/client'
import { queryKeys } from '../../../api/queryKeys'
import type { Event, UpdateEventData, EventRegistration } from '../../../types/api.types'
import type { components } from '@witchcityrope/shared-types'

// Use auto-generated CreateEventRequest from backend DTOs
type CreateEventRequest = components['schemas']['CreateEventRequest']

// Ticket purchase data interface
export interface TicketPurchaseData {
  eventId: string;
  ticketTypeId: string;
  quantity: number;
  paymentMethod: 'paypal' | 'venmo';
  totalAmount: number;
}

// RSVP data interface
export interface RSVPData {
  eventId: string;
  willAttend: boolean;
  alsoWantsToBuyTicket?: boolean;
}

// Type for the registration mutation variables
type RegistrationVariables = { eventId: string; action: 'register' | 'unregister' }

// Type for optimistic context returned by onMutate
interface OptimisticEventContext {
  previousEvent: Event | undefined;
  eventId: string;
}

// Type for cancel ticket variables
type CancelTicketVariables = { eventId: string; ticketId: string }

// Type for cancel RSVP variables (onMutate uses additional rsvpId)
type CancelRSVPMutateVariables = { eventId: string; rsvpId?: string }

// RSVP response type
interface RSVPResponse {
  id: string;
  eventId: string;
  confirmationCode: string;
  status: string;
}

export function useCreateEvent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: CreateEventRequest): Promise<Event> => {
      console.log('useCreateEvent: Sending request with data:', data)
      const response = await apiClient.post('/api/events', data)
      console.log('useCreateEvent: Response received:', response.data)
      return response.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.events() })
      queryClient.invalidateQueries({ queryKey: queryKeys.infiniteEvents({}) })
    },
    onError: (error: Error) => {
      console.error('Failed to create event:', error)
    },
  })
}

export function useUpdateEvent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (params: { id: string; data: UpdateEventData }): Promise<Event> => {
      const response = await apiClient.put(`/api/events/${params.id}`, params.data)
      return response.data
    },
    onSuccess: (updatedEvent: Event, variables: { id: string; data: UpdateEventData }) => {
      // Update single event cache
      queryClient.setQueryData(queryKeys.event(variables.id), updatedEvent)

      // Invalidate related queries
      queryClient.invalidateQueries({ queryKey: queryKeys.events() })
      queryClient.invalidateQueries({ queryKey: queryKeys.infiniteEvents({}) })
    },
    onError: (error: Error) => {
      console.error('Failed to update event:', error)
    },
  })
}

export function useDeleteEvent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (eventId: string): Promise<void> => {
      await apiClient.delete(`/api/events/${eventId}`)
    },
    onSuccess: (_: void, deletedEventId: string) => {
      queryClient.removeQueries({ queryKey: queryKeys.event(deletedEventId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.events() })
      queryClient.invalidateQueries({ queryKey: queryKeys.infiniteEvents({}) })
    },
    onError: (error: Error) => {
      console.error('Failed to delete event:', error)
    },
  })
}

export function useCopyEvent() {
  const queryClient = useQueryClient()

  return useMutation<Event, Error, {
    eventId: string;
    newStartDate: string;
    newTitle: string;
  }>({
    mutationFn: async ({ eventId, newStartDate, newTitle }: { eventId: string; newStartDate: string; newTitle: string }) => {
      const response = await apiClient.post<Event>(
        `/api/events/${eventId}/copy`,
        {
          newStartDate,
          newTitle,
        }
      );
      return response.data;
    },
    onSuccess: () => {
      // Invalidate events cache to show new event
      queryClient.invalidateQueries({ queryKey: queryKeys.events() })
      queryClient.invalidateQueries({ queryKey: queryKeys.infiniteEvents({}) })
    },
    onError: (error: Error) => {
      console.error('Failed to copy event:', error)
    },
  })
}

// Optimistic updates example for event registration
export function useEventRegistration() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ eventId, action }: RegistrationVariables) => {
      const response = await apiClient.post(`/api/events/${eventId}/registration`, { action })
      return response.data
    },
    onMutate: async ({ eventId, action }: RegistrationVariables) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: queryKeys.event(eventId) })

      // Snapshot previous value
      const previousEvent = queryClient.getQueryData(queryKeys.event(eventId)) as Event | undefined

      // Optimistically update event
      if (previousEvent) {
        queryClient.setQueryData(queryKeys.event(eventId), (old: Event) => ({
          ...old,
          registrationCount: action === 'register'
            ? (old.registrationCount || 0) + 1
            : Math.max(0, (old.registrationCount || 0) - 1),
        }))
      }

      // Return rollback context
      return { previousEvent, eventId }
    },
    onError: (error: Error, _variables: RegistrationVariables, context: OptimisticEventContext | undefined) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(
          queryKeys.event(context.eventId),
          context.previousEvent
        )
      }
      console.error('Registration failed:', error)
    },
    onSettled: (_data: unknown, _error: Error | null, variables: RegistrationVariables) => {
      // Always refetch to ensure server state
      queryClient.invalidateQueries({ queryKey: queryKeys.event(variables.eventId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.currentUser() })
    },
  })
}

// Enhanced ticket purchase mutation for ticket-based events (Classes and Social Events)
export function usePurchaseTicket() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (ticketData: TicketPurchaseData): Promise<EventRegistration> => {
      const response = await apiClient.post(`/api/events/${ticketData.eventId}/tickets`, ticketData)
      return response.data
    },
    onMutate: async (ticketData: TicketPurchaseData) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: queryKeys.event(ticketData.eventId) })

      // Snapshot previous value
      const previousEvent = queryClient.getQueryData(queryKeys.event(ticketData.eventId)) as Event | undefined

      // Optimistically update event attendance
      if (previousEvent) {
        queryClient.setQueryData(queryKeys.event(ticketData.eventId), (old: Event) => ({
          ...old,
          registrationCount: (old.registrationCount || 0) + ticketData.quantity,
        }))
      }

      // Return rollback context
      return { previousEvent, eventId: ticketData.eventId }
    },
    onError: (error: Error, _variables: TicketPurchaseData, context: OptimisticEventContext | undefined) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(
          queryKeys.event(context.eventId),
          context.previousEvent
        )
      }
      console.error('Ticket purchase failed:', error)
    },
    onSettled: (_data: EventRegistration | undefined, _error: Error | null, variables: TicketPurchaseData) => {
      // Always refetch to ensure server state
      queryClient.invalidateQueries({ queryKey: queryKeys.event(variables.eventId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.events() })
      queryClient.invalidateQueries({ queryKey: queryKeys.currentUser() })
    },
  })
}

// Cancel ticket purchase mutation
export function useCancelTicket() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ eventId, ticketId }: CancelTicketVariables): Promise<void> => {
      await apiClient.delete(`/api/events/${eventId}/ticket/${ticketId}`)
    },
    onMutate: async ({ eventId, ticketId: _ticketId }: CancelTicketVariables) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: queryKeys.event(eventId) })

      // Snapshot previous value
      const previousEvent = queryClient.getQueryData(queryKeys.event(eventId)) as Event | undefined

      // Optimistically update event attendance (assuming quantity of 1 for cancellation)
      if (previousEvent) {
        queryClient.setQueryData(queryKeys.event(eventId), (old: Event) => ({
          ...old,
          registrationCount: Math.max(0, (old.registrationCount || 0) - 1),
        }))
      }

      // Return rollback context
      return { previousEvent, eventId }
    },
    onError: (error: Error, _variables: CancelTicketVariables, context: OptimisticEventContext | undefined) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(
          queryKeys.event(context.eventId),
          context.previousEvent
        )
      }
      console.error('Ticket cancellation failed:', error)
    },
    onSettled: (_data: void | undefined, _error: Error | null, variables: CancelTicketVariables) => {
      // Always refetch to ensure server state
      queryClient.invalidateQueries({ queryKey: queryKeys.event(variables.eventId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.events() })
      queryClient.invalidateQueries({ queryKey: queryKeys.currentUser() })
    },
  })
}

// RSVP mutation for Social Events (free RSVP)
export function useRSVPForEvent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (rsvpData: RSVPData): Promise<RSVPResponse> => {
      // Use the correct events-management endpoint for RSVP
      const response = await apiClient.post(`/api/events-management/${rsvpData.eventId}/rsvp`, {
        dietaryRestrictions: '',
        emergencyContactName: '',
        emergencyContactPhone: ''
      })
      return response.data
    },
    onMutate: async (rsvpData: RSVPData) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: queryKeys.event(rsvpData.eventId) })

      // Snapshot previous value
      const previousEvent = queryClient.getQueryData(queryKeys.event(rsvpData.eventId)) as Event | undefined

      // Note: RSVP doesn't affect registrationCount (only paid tickets do)
      // But we still want to track RSVP state

      // Return rollback context
      return { previousEvent, eventId: rsvpData.eventId }
    },
    onError: (error: Error, _variables: RSVPData, context: OptimisticEventContext | undefined) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(
          queryKeys.event(context.eventId),
          context.previousEvent
        )
      }
      console.error('RSVP failed:', error)
    },
    onSettled: (_data: RSVPResponse | undefined, _error: Error | null, variables: RSVPData) => {
      // Always refetch to ensure server state
      queryClient.invalidateQueries({ queryKey: queryKeys.event(variables.eventId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.events() })
      queryClient.invalidateQueries({ queryKey: queryKeys.currentUser() })
    },
  })
}

// Cancel RSVP mutation for Social Events
export function useCancelRSVP() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ eventId }: { eventId: string }): Promise<void> => {
      // Use the correct events-management endpoint for canceling RSVP
      await apiClient.delete(`/api/events-management/${eventId}/rsvp`)
    },
    onMutate: async ({ eventId, rsvpId: _rsvpId }: CancelRSVPMutateVariables) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: queryKeys.event(eventId) })

      // Snapshot previous value
      const previousEvent = queryClient.getQueryData(queryKeys.event(eventId)) as Event | undefined

      // Note: Canceling RSVP doesn't affect registrationCount (only paid tickets do)

      // Return rollback context
      return { previousEvent, eventId }
    },
    onError: (error: Error, _variables: { eventId: string }, context: OptimisticEventContext | undefined) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(
          queryKeys.event(context.eventId),
          context.previousEvent
        )
      }
      console.error('RSVP cancellation failed:', error)
    },
    onSettled: (_data: void | undefined, _error: Error | null, variables: { eventId: string }) => {
      // Always refetch to ensure server state
      queryClient.invalidateQueries({ queryKey: queryKeys.event(variables.eventId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.events() })
      queryClient.invalidateQueries({ queryKey: queryKeys.currentUser() })
    },
  })
}
