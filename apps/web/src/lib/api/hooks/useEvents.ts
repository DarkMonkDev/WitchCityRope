import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query'
import { apiClient } from '../client'
import { eventKeys, cacheUtils } from '../utils/cache'
// import { handleApiError } from '../utils/errors' // For future error handling
import type {
  EventDto,
  CreateEventDto,
  UpdateEventDto,
  EventFilters,
  RegistrationDto,
  EventSessionDto,
  EventTicketTypeDto,
  VolunteerPositionDto
} from '../types/events.types'
import type { ApiResponse, PaginatedResponse } from '../types/api.types'

// Pagination response structure (for future paginated endpoints)
interface ApiEventResponse {
  events: ApiEvent[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

// API event structure from backend (actual response structure)
interface ApiEvent {
  id: string
  title: string
  description: string
  startDate: string
  location: string
  eventType?: string
  capacity?: number
  // Optional fields that may be added later
  endDate?: string
  maxAttendees?: number
  currentAttendees?: number
  currentRSVPs?: number
  volunteerPositions?: any[]
  currentTickets?: number
  availableSpots?: number
  price?: number
  organizerName?: string
  requiresVetting?: boolean
  isPublished?: boolean
  // New fields added by backend
  sessions?: ApiEventSession[]
  ticketTypes?: ApiEventTicketType[]
  teacherIds?: string[]
}

// API session structure (matches actual API response)
interface ApiEventSession {
  id: string
  sessionIdentifier: string
  name: string
  date: string
  startTime: string
  endTime: string
  capacity: number
  registeredCount: number
}

// API ticket type structure (matches actual API response)
interface ApiEventTicketType {
  id: string
  name: string
  type: string
  sessionIdentifiers: string[]
  minPrice: number
  maxPrice: number
  quantityAvailable: number
  salesEndDate?: string | null
}

// Transform API event to frontend EventDto
function transformApiEvent(apiEvent: ApiEvent): EventDto {

  // Transform sessions to match frontend structure
  const transformedSessions: EventSessionDto[] = (apiEvent.sessions || []).map(session => ({
    id: session.id,
    sessionIdentifier: session.sessionIdentifier,
    name: session.name,
    date: session.date,
    startTime: session.startTime,
    endTime: session.endTime,
    capacity: session.capacity,
    registeredCount: session.registeredCount,
    description: '' // Add default description
  }));

  // Transform ticket types to match frontend structure
  const transformedTicketTypes: EventTicketTypeDto[] = (apiEvent.ticketTypes || []).map(ticket => ({
    id: ticket.id,
    name: ticket.name,
    type: ticket.type,
    price: ticket.minPrice,
    maxPrice: ticket.maxPrice,
    quantityAvailable: ticket.quantityAvailable,
    sessionIdentifiers: ticket.sessionIdentifiers,
    salesEndDate: ticket.salesEndDate || undefined
  }));

  // Transform volunteer positions - now using consistent field names
  const transformedVolunteerPositions = (apiEvent.volunteerPositions || []).map((vp: any) => ({
    id: vp.id,
    title: vp.title,
    description: vp.description || '',
    slotsNeeded: vp.slotsNeeded,
    slotsFilled: vp.slotsFilled,
    sessionId: vp.sessionId,
    requirements: vp.requirements,
    requiresExperience: vp.requiresExperience
  }));

  return {
    id: apiEvent.id,
    title: apiEvent.title,
    description: apiEvent.description,
    startDate: apiEvent.startDate,
    endDate: apiEvent.endDate || null,
    location: apiEvent.location,
    eventType: apiEvent.eventType,
    capacity: apiEvent.capacity || apiEvent.maxAttendees || 20, // Use capacity first, then maxAttendees
    registrationCount: apiEvent.currentAttendees || apiEvent.currentRSVPs || apiEvent.currentTickets || 0,
    createdAt: new Date().toISOString(), // Placeholder - should come from API
    updatedAt: new Date().toISOString(), // Placeholder - should come from API
    // Map new fields from API response with proper transformation
    sessions: transformedSessions,
    ticketTypes: transformedTicketTypes,
    teacherIds: apiEvent.teacherIds || [],
    volunteerPositions: transformedVolunteerPositions,
    isPublished: apiEvent.isPublished !== undefined ? apiEvent.isPublished : true
  }
}

// Fetch events list with filters
export function useEvents(filters: EventFilters = {}) {
  return useQuery({
    queryKey: eventKeys.list(filters),
    queryFn: async (): Promise<EventDto[]> => {
      const { data } = await apiClient.get<ApiResponse<ApiEvent[]>>('/api/events', {
        params: filters,
      })
      // Access events from the wrapped response data
      const events = data?.data || []
      return events.map(transformApiEvent)
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    enabled: true,
  })
}

// Infinite query for event pagination
export function useInfiniteEvents(filters: Omit<EventFilters, 'page'> = {}) {
  return useInfiniteQuery({
    queryKey: eventKeys.infiniteList(filters),
    queryFn: async ({ pageParam = 1 }): Promise<PaginatedResponse<EventDto>> => {
      const { data } = await apiClient.get<ApiResponse<PaginatedResponse<EventDto>>>('/api/events', {
        params: { ...filters, page: pageParam, limit: 10 },
      })
      return data.data || { items: [], page: 1, limit: 10, total: 0, totalPages: 0, hasNext: false, hasPrev: false }
    },
    getNextPageParam: (lastPage) => 
      lastPage.hasNext ? lastPage.page + 1 : undefined,
    initialPageParam: 1,
    staleTime: 5 * 60 * 1000,
  })
}

// Fetch single event
export function useEvent(id: string, enabled: boolean = true) {
  return useQuery({
    queryKey: eventKeys.detail(id),
    queryFn: async (): Promise<EventDto> => {
      const { data } = await apiClient.get<ApiResponse<ApiEvent>>(`/api/events/${id}`)
      if (!data?.data) throw new Error('Event not found')
      return transformApiEvent(data.data)
    },
    enabled: !!id && enabled,
    staleTime: 5 * 60 * 1000,
  })
}

// Create event mutation
export function useCreateEvent() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (eventData: CreateEventDto): Promise<EventDto> => {
      const { data } = await apiClient.post<ApiResponse<EventDto>>('/api/events', eventData)
      if (!data.data) throw new Error('Failed to create event')
      return data.data
    },
    onSuccess: (newEvent) => {
      // Invalidate events list to refetch
      cacheUtils.invalidateEvents(queryClient)
      
      // Optimistically add to cache
      queryClient.setQueryData(eventKeys.detail(newEvent.id), newEvent)
      
    },
    onError: (error) => {
    },
  })
}

// Update event mutation with optimistic updates
export function useUpdateEvent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (eventData: UpdateEventDto): Promise<EventDto> => {
      const { data } = await apiClient.put<ApiResponse<ApiEvent>>(`/api/events/${eventData.id}`, eventData)
      if (!data.data) throw new Error('Failed to update event')
      // Transform the API response to match our EventDto structure
      return transformApiEvent(data.data)
    },
    onMutate: async (updatedEvent) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: eventKeys.detail(updatedEvent.id) })

      // Snapshot previous value
      const previousEvent = queryClient.getQueryData(eventKeys.detail(updatedEvent.id)) as EventDto | undefined

      // Optimistically update cache
      queryClient.setQueryData(eventKeys.detail(updatedEvent.id), (old: EventDto | undefined) => {
        if (!old) return old
        return { ...old, ...updatedEvent }
      })

      return { previousEvent }
    },
    onError: (err, updatedEvent, context) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(eventKeys.detail(updatedEvent.id), context.previousEvent)
      }
    },
    onSettled: (_data, _error, updatedEvent) => {
      // Always refetch after error or success
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(updatedEvent.id) })
      cacheUtils.invalidateEvents(queryClient, updatedEvent.id)
    },
  })
}

// Delete event mutation
export function useDeleteEvent() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (id: string): Promise<string> => {
      await apiClient.delete(`/api/events/${id}`)
      return id
    },
    onSuccess: (deletedId) => {
      queryClient.removeQueries({ queryKey: eventKeys.detail(deletedId) })
      cacheUtils.invalidateEvents(queryClient)
    },
    onError: (error) => {
    },
  })
}

// Event registration hooks
export function useEventRegistrations(eventId: string) {
  return useQuery({
    queryKey: eventKeys.registrations(eventId),
    queryFn: async (): Promise<RegistrationDto[]> => {
      const { data } = await apiClient.get<ApiResponse<RegistrationDto[]>>(`/api/events/${eventId}/registrations`)
      return data.data || []
    },
    enabled: !!eventId,
    staleTime: 2 * 60 * 1000, // 2 minutes - more frequent updates for registrations
  })
}

export function useRegisterForEvent() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (eventId: string): Promise<RegistrationDto> => {
      const { data } = await apiClient.post<ApiResponse<RegistrationDto>>(`/api/events/${eventId}/register`)
      if (!data.data) throw new Error('Registration failed')
      return data.data
    },
    onMutate: async (eventId) => {
      // Optimistically update event registration count
      await queryClient.cancelQueries({ queryKey: eventKeys.detail(eventId) })
      
      const previousEvent = queryClient.getQueryData(eventKeys.detail(eventId)) as EventDto | undefined
      
      queryClient.setQueryData(eventKeys.detail(eventId), (old: EventDto | undefined) => {
        if (!old) return old
        return {
          ...old,
          registrationCount: (old.registrationCount || 0) + 1
        }
      })
      
      return { previousEvent }
    },
    onError: (err, eventId, context) => {
      // Rollback registration count on error
      if (context?.previousEvent) {
        queryClient.setQueryData(eventKeys.detail(eventId), context.previousEvent)
      }
    },
    onSuccess: (_registration, eventId) => {
      // Refresh registration data
      queryClient.invalidateQueries({ queryKey: eventKeys.registrations(eventId) })
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) })
    },
  })
}