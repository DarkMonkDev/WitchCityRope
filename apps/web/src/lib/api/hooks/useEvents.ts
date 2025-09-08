import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query'
import { apiClient } from '../client'
import { eventKeys, cacheUtils } from '../utils/cache'
// import { handleApiError } from '../utils/errors' // For future error handling
import type { 
  EventDto, 
  CreateEventDto, 
  UpdateEventDto, 
  EventFilters,
  RegistrationDto 
} from '../types/events.types'
import type { ApiResponse, PaginatedResponse } from '../types/api.types'

// API response structure from backend
interface ApiEventResponse {
  events: ApiEvent[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

// API event structure from backend
interface ApiEvent {
  id: string
  title: string
  description: string
  startDateTime: string
  endDateTime: string
  location: string
  maxAttendees: number
  currentAttendees: number
  availableSpots: number
  price: number
  organizerName: string
  requiresVetting?: boolean
}

// Transform API event to frontend EventDto
function transformApiEvent(apiEvent: ApiEvent): EventDto {
  return {
    id: apiEvent.id,
    title: apiEvent.title,
    description: apiEvent.description,
    startDate: apiEvent.startDateTime,
    endDate: apiEvent.endDateTime,
    location: apiEvent.location,
    capacity: apiEvent.maxAttendees,
    registrationCount: apiEvent.currentAttendees,
    createdAt: new Date().toISOString(), // Placeholder - should come from API
    updatedAt: new Date().toISOString()  // Placeholder - should come from API
  }
}

// Fetch events list with filters
export function useEvents(filters: EventFilters = {}) {
  return useQuery({
    queryKey: eventKeys.list(filters),
    queryFn: async (): Promise<EventDto[]> => {
      const { data } = await apiClient.get<ApiEventResponse>('/api/events', {
        params: filters,
      })
      return data.events?.map(transformApiEvent) || []
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
      const { data } = await apiClient.get<ApiEvent>(`/api/events/${id}`)
      if (!data) throw new Error('Event not found')
      return transformApiEvent(data)
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
      
      console.log('Event created successfully:', newEvent.title)
    },
    onError: (error) => {
      console.error('Create event failed:', error)
    },
  })
}

// Update event mutation with optimistic updates
export function useUpdateEvent() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (eventData: UpdateEventDto): Promise<EventDto> => {
      const { data } = await apiClient.put<ApiResponse<EventDto>>(`/api/events/${eventData.id}`, eventData)
      if (!data.data) throw new Error('Failed to update event')
      return data.data
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
      console.error('Update event failed, rolling back:', err)
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
      console.log('Event deleted successfully')
    },
    onError: (error) => {
      console.error('Delete event failed:', error)
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
      console.error('Event registration failed, rolling back:', err)
    },
    onSuccess: (_registration, eventId) => {
      // Refresh registration data
      queryClient.invalidateQueries({ queryKey: eventKeys.registrations(eventId) })
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) })
      console.log('Successfully registered for event')
    },
  })
}