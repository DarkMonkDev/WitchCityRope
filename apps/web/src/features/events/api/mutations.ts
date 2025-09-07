// features/events/api/mutations.ts
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../../../api/client'
import { queryKeys } from '../../../api/queryKeys'
import type { Event, CreateEventData, UpdateEventData, EventRegistration } from '../../../types/api.types'

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

export function useCreateEvent() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (data: CreateEventData): Promise<Event> => {
      const response = await api.post('/api/events', data)
      return response.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.events() })
      queryClient.invalidateQueries({ queryKey: queryKeys.infiniteEvents({}) })
    },
    onError: (error) => {
      console.error('Failed to create event:', error)
    },
  })
}

export function useUpdateEvent() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (params: { id: string; data: UpdateEventData }): Promise<Event> => {
      const response = await api.put(`/api/events/${params.id}`, params.data)
      return response.data
    },
    onSuccess: (updatedEvent, variables) => {
      // Update single event cache
      queryClient.setQueryData(queryKeys.event(variables.id), updatedEvent)
      
      // Invalidate related queries
      queryClient.invalidateQueries({ queryKey: queryKeys.events() })
      queryClient.invalidateQueries({ queryKey: queryKeys.infiniteEvents({}) })
    },
    onError: (error) => {
      console.error('Failed to update event:', error)
    },
  })
}

export function useDeleteEvent() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (eventId: string): Promise<void> => {
      await api.delete(`/api/events/${eventId}`)
    },
    onSuccess: (_, deletedEventId) => {
      queryClient.removeQueries({ queryKey: queryKeys.event(deletedEventId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.events() })
      queryClient.invalidateQueries({ queryKey: queryKeys.infiniteEvents({}) })
    },
    onError: (error) => {
      console.error('Failed to delete event:', error)
    },
  })
}

// Optimistic updates example for event registration
export function useEventRegistration() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async ({ eventId, action }: { eventId: string; action: 'register' | 'unregister' }) => {
      const response = await api.post(`/api/events/${eventId}/registration`, { action })
      return response.data
    },
    onMutate: async ({ eventId, action }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: queryKeys.event(eventId) })
      
      // Snapshot previous value
      const previousEvent = queryClient.getQueryData(queryKeys.event(eventId)) as Event | undefined
      
      // Optimistically update event
      if (previousEvent) {
        queryClient.setQueryData(queryKeys.event(eventId), (old: Event) => ({
          ...old,
          currentAttendees: action === 'register' 
            ? old.currentAttendees + 1
            : old.currentAttendees - 1,
        }))
      }
      
      // Return rollback context
      return { previousEvent, eventId }
    },
    onError: (error, variables, context) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(
          queryKeys.event(context.eventId),
          context.previousEvent
        )
      }
      console.error('Registration failed:', error)
    },
    onSettled: (data, error, variables) => {
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
      const response = await api.post(`/api/events/${ticketData.eventId}/purchase-ticket`, ticketData)
      return response.data
    },
    onMutate: async (ticketData) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: queryKeys.event(ticketData.eventId) })
      
      // Snapshot previous value
      const previousEvent = queryClient.getQueryData(queryKeys.event(ticketData.eventId)) as Event | undefined
      
      // Optimistically update event attendance
      if (previousEvent) {
        queryClient.setQueryData(queryKeys.event(ticketData.eventId), (old: Event) => ({
          ...old,
          currentAttendees: old.currentAttendees + ticketData.quantity,
        }))
      }
      
      // Return rollback context
      return { previousEvent, eventId: ticketData.eventId }
    },
    onError: (error, variables, context) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(
          queryKeys.event(context.eventId),
          context.previousEvent
        )
      }
      console.error('Ticket purchase failed:', error)
    },
    onSettled: (data, error, variables) => {
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
    mutationFn: async ({ eventId, ticketId }: { eventId: string; ticketId: string }): Promise<void> => {
      await api.delete(`/api/events/${eventId}/ticket/${ticketId}`)
    },
    onMutate: async ({ eventId, ticketId }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: queryKeys.event(eventId) })
      
      // Snapshot previous value
      const previousEvent = queryClient.getQueryData(queryKeys.event(eventId)) as Event | undefined
      
      // Optimistically update event attendance (assuming quantity of 1 for cancellation)
      if (previousEvent) {
        queryClient.setQueryData(queryKeys.event(eventId), (old: Event) => ({
          ...old,
          currentAttendees: Math.max(0, old.currentAttendees - 1),
        }))
      }
      
      // Return rollback context
      return { previousEvent, eventId }
    },
    onError: (error, variables, context) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(
          queryKeys.event(context.eventId),
          context.previousEvent
        )
      }
      console.error('Ticket cancellation failed:', error)
    },
    onSettled: (data, error, variables) => {
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
    mutationFn: async (rsvpData: RSVPData): Promise<{ id: string; eventId: string; willAttend: boolean }> => {
      const response = await api.post(`/api/events/${rsvpData.eventId}/rsvp`, rsvpData)
      return response.data
    },
    onMutate: async (rsvpData) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: queryKeys.event(rsvpData.eventId) })
      
      // Snapshot previous value
      const previousEvent = queryClient.getQueryData(queryKeys.event(rsvpData.eventId)) as Event | undefined
      
      // Note: RSVP doesn't affect currentAttendees (only paid tickets do)
      // But we still want to track RSVP state
      
      // Return rollback context
      return { previousEvent, eventId: rsvpData.eventId }
    },
    onError: (error, variables, context) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(
          queryKeys.event(context.eventId),
          context.previousEvent
        )
      }
      console.error('RSVP failed:', error)
    },
    onSettled: (data, error, variables) => {
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
    mutationFn: async ({ eventId, rsvpId }: { eventId: string; rsvpId: string }): Promise<void> => {
      await api.delete(`/api/events/${eventId}/rsvp/${rsvpId}`)
    },
    onMutate: async ({ eventId, rsvpId }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: queryKeys.event(eventId) })
      
      // Snapshot previous value
      const previousEvent = queryClient.getQueryData(queryKeys.event(eventId)) as Event | undefined
      
      // Note: Canceling RSVP doesn't affect currentAttendees (only paid tickets do)
      
      // Return rollback context
      return { previousEvent, eventId }
    },
    onError: (error, variables, context) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(
          queryKeys.event(context.eventId),
          context.previousEvent
        )
      }
      console.error('RSVP cancellation failed:', error)
    },
    onSettled: (data, error, variables) => {
      // Always refetch to ensure server state
      queryClient.invalidateQueries({ queryKey: queryKeys.event(variables.eventId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.events() })
      queryClient.invalidateQueries({ queryKey: queryKeys.currentUser() })
    },
  })
}