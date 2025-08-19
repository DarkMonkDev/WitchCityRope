// features/events/api/mutations.ts
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../../../api/client'
import { queryKeys } from '../../../api/queryKeys'
import type { Event, CreateEventData, UpdateEventData } from '../../../types/api.types'

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
      const previousEvent = queryClient.getQueryData<Event>(queryKeys.event(eventId))
      
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