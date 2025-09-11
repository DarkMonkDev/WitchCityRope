import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { eventSessionsApi } from '../services/eventSessions'
import type { 
  EventSessionDto, 
  CreateEventSessionDto, 
  UpdateEventSessionDto,
  EventTicketTypeDto,
  CreateEventTicketTypeDto,
  UpdateEventTicketTypeDto
} from '../services/eventSessions'

// Query keys for Event Session Matrix
const eventKeys = {
  all: ['events'] as const,
  details: () => [...eventKeys.all, 'detail'] as const,
  detail: (id: string) => [...eventKeys.details(), id] as const,
}

// Session Management Hooks
export function useEventSessions(eventId: string, enabled: boolean = true) {
  return useQuery({
    queryKey: [...eventKeys.detail(eventId), 'sessions'],
    queryFn: () => eventSessionsApi.getSessions(eventId),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000, // 2 minutes - sessions change less frequently than registrations
  })
}

export function useCreateEventSession(eventId: string) {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: (sessionData: CreateEventSessionDto) => 
      eventSessionsApi.createSession(eventId, sessionData),
    onSuccess: () => {
      // Invalidate sessions for this event
      queryClient.invalidateQueries({ 
        queryKey: [...eventKeys.detail(eventId), 'sessions'] 
      })
      console.log('Event session created successfully')
    },
    onError: (error) => {
      console.error('Create event session failed:', error)
    },
  })
}

export function useUpdateEventSession() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: ({ sessionId, sessionData }: { 
      sessionId: string, 
      sessionData: Partial<UpdateEventSessionDto> 
    }) => eventSessionsApi.updateSession(sessionId, sessionData),
    onSuccess: () => {
      // Invalidate all session-related queries using query key patterns
      queryClient.invalidateQueries({ 
        predicate: (query) => {
          const key = query.queryKey
          return Array.isArray(key) && key.includes('sessions')
        }
      })
      
      console.log('Event session updated successfully')
    },
    onError: (error) => {
      console.error('Update event session failed:', error)
    },
  })
}

export function useDeleteEventSession() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: (sessionId: string) => eventSessionsApi.deleteSession(sessionId),
    onSuccess: () => {
      // Invalidate all session-related queries
      queryClient.invalidateQueries({ 
        predicate: (query) => {
          const key = query.queryKey
          return Array.isArray(key) && key.includes('sessions')
        }
      })
      
      console.log('Event session deleted successfully')
    },
    onError: (error) => {
      console.error('Delete event session failed:', error)
    },
  })
}

// Ticket Type Management Hooks
export function useEventTicketTypes(eventId: string, enabled: boolean = true) {
  return useQuery({
    queryKey: [...eventKeys.detail(eventId), 'ticket-types'],
    queryFn: () => eventSessionsApi.getTicketTypes(eventId),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000, // 2 minutes
  })
}

export function useCreateEventTicketType(eventId: string) {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: (ticketTypeData: CreateEventTicketTypeDto) => 
      eventSessionsApi.createTicketType(eventId, ticketTypeData),
    onSuccess: () => {
      // Invalidate ticket types for this event
      queryClient.invalidateQueries({ 
        queryKey: [...eventKeys.detail(eventId), 'ticket-types'] 
      })
      console.log('Event ticket type created successfully')
    },
    onError: (error) => {
      console.error('Create event ticket type failed:', error)
    },
  })
}

export function useUpdateEventTicketType() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: ({ ticketTypeId, ticketTypeData }: { 
      ticketTypeId: string, 
      ticketTypeData: Partial<UpdateEventTicketTypeDto> 
    }) => eventSessionsApi.updateTicketType(ticketTypeId, ticketTypeData),
    onSuccess: () => {
      // Invalidate all ticket-type queries
      queryClient.invalidateQueries({ 
        predicate: (query) => {
          const key = query.queryKey
          return Array.isArray(key) && key.includes('ticket-types')
        }
      })
      
      console.log('Event ticket type updated successfully')
    },
    onError: (error) => {
      console.error('Update event ticket type failed:', error)
    },
  })
}

export function useDeleteEventTicketType() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: (ticketTypeId: string) => eventSessionsApi.deleteTicketType(ticketTypeId),
    onSuccess: () => {
      // Invalidate all ticket-type-related queries
      queryClient.invalidateQueries({ 
        predicate: (query) => {
          const key = query.queryKey
          return Array.isArray(key) && key.includes('ticket-types')
        }
      })
      
      console.log('Event ticket type deleted successfully')
    },
    onError: (error) => {
      console.error('Delete event ticket type failed:', error)
    },
  })
}

// Composite hook for managing both sessions and ticket types for an event
export function useEventSessionMatrix(eventId: string, enabled: boolean = true) {
  const sessionsQuery = useEventSessions(eventId, enabled)
  const ticketTypesQuery = useEventTicketTypes(eventId, enabled)
  
  return {
    // Sessions data and state
    sessions: (sessionsQuery.data as EventSessionDto[]) || [],
    sessionsLoading: sessionsQuery.isLoading,
    sessionsError: sessionsQuery.error,
    
    // Ticket types data and state
    ticketTypes: (ticketTypesQuery.data as EventTicketTypeDto[]) || [],
    ticketTypesLoading: ticketTypesQuery.isLoading,
    ticketTypesError: ticketTypesQuery.error,
    
    // Overall loading state
    isLoading: sessionsQuery.isLoading || ticketTypesQuery.isLoading,
    hasError: !!sessionsQuery.error || !!ticketTypesQuery.error,
    
    // Refetch functions
    refetchSessions: sessionsQuery.refetch,
    refetchTicketTypes: ticketTypesQuery.refetch,
    refetchAll: async () => {
      await Promise.all([
        sessionsQuery.refetch(),
        ticketTypesQuery.refetch()
      ])
    }
  }
}