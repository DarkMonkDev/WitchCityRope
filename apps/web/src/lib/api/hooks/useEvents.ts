import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query'
import { apiClient } from '../client'
import { eventKeys, cacheUtils } from '../utils/cache'
// import { handleApiError } from '../utils/errors' // For future error handling
import type { EventDto, CreateEventDto, UpdateEventDto, EventFilters, RegistrationDto, EventSessionDto, EventTicketTypeDto } from '../types/events.types'
import type { PaginatedResponse } from '../types/api.types'

// API event structure from backend (actual response structure)
interface ApiEvent {
  id: string
  title: string
  shortDescription?: string | null
  description: string
  policies?: string | null
  startDate: string
  venueId?: number | null // Venue reference (preferred)
  venueLocation?: string | null // Public location (city, state) for privacy-aware display
  location: string // DEPRECATED: Legacy location string
  // Boolean flags replace eventType enum
  allowRsvps?: boolean
  requireTicketPurchase?: boolean
  vettedMembersOnly?: boolean
  capacity?: number
  registrationCount?: number
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
  ticketTypes?: EventTicketTypeDto[]
  teacherIds?: string[]
  // Timing control fields
  registrationOpenHours?: number | null
  registrationCloseHours?: number | null
  cancellationCloseHours?: number | null
  volunteerRegistrationCloseHours?: number | null
  volunteerCancellationCloseHours?: number | null
}

// API session structure (matches actual API response)
interface ApiEventSession {
  id: string
  sessionIdentifier: string
  name: string
  startDate: string  // Renamed from date to match backend SessionDto
  endDate?: string   // End date for multi-day sessions (derived from EndTime on backend)
  startTime: string
  endTime: string
  capacity: number
  registrationCount: number // Fixed: API returns registrationCount, not registeredCount
}

// Ticket type uses auto-generated TicketTypeDto — no manual interface needed
// API response fields flow through directly via spread operator

// Transform API event to frontend EventDto
function transformApiEvent(apiEvent: ApiEvent): EventDto {

  // Transform sessions to match frontend structure
  const transformedSessions: EventSessionDto[] = (apiEvent.sessions || []).map(session => ({
    id: session.id,
    sessionIdentifier: session.sessionIdentifier,
    name: session.name,
    startDate: session.startDate, // Renamed from date
    endDate: session.endDate, // End date for multi-day sessions (derived from EndTime on backend)
    startTime: session.startTime,
    endTime: session.endTime,
    capacity: session.capacity,
    registrationCount: session.registrationCount, // Fixed: use registrationCount from API
    description: '' // Add default description
  }));

  // Pass ticket types through directly — auto-generated TicketTypeDto already matches API response
  // Using spread prevents field-dropping bugs when backend adds new fields
  const transformedTicketTypes: EventTicketTypeDto[] = apiEvent.ticketTypes || [];

  // Transform volunteer positions - now using consistent field names
  const transformedVolunteerPositions = (apiEvent.volunteerPositions || []).map((vp: any) => ({
    id: vp.id,
    title: vp.title,
    description: vp.description || '',
    slotsNeeded: vp.slotsNeeded,
    slotsFilled: vp.slotsFilled,
    sessionId: vp.sessionId,
    startTime: vp.startTime,
    endTime: vp.endTime,
    isPublicFacing: vp.isPublicFacing,
  }));

  return {
    id: apiEvent.id,
    title: apiEvent.title,
    shortDescription: apiEvent.shortDescription || null,
    description: apiEvent.description,
    policies: apiEvent.policies || null,
    startDate: apiEvent.startDate,
    endDate: apiEvent.endDate || undefined,
    venueId: apiEvent.venueId ?? undefined,
    venueLocation: apiEvent.venueLocation || null,
    // Map boolean flags from API
    allowRsvps: apiEvent.allowRsvps ?? false,
    requireTicketPurchase: apiEvent.requireTicketPurchase ?? true,
    vettedMembersOnly: apiEvent.vettedMembersOnly ?? false,
    capacity: apiEvent.capacity || apiEvent.maxAttendees || 20, // Use capacity first, then maxAttendees
    registrationCount: apiEvent.registrationCount || apiEvent.currentAttendees || apiEvent.currentRSVPs || apiEvent.currentTickets || 0,
    // Map new fields from API response with proper transformation
    sessions: transformedSessions,
    ticketTypes: transformedTicketTypes,
    teacherIds: apiEvent.teacherIds || [],
    volunteerPositions: transformedVolunteerPositions,
    isPublished: apiEvent.isPublished !== undefined ? apiEvent.isPublished : true,
    // Timing control fields
    registrationOpenHours: apiEvent.registrationOpenHours ?? null,
    registrationCloseHours: apiEvent.registrationCloseHours ?? null,
    cancellationCloseHours: apiEvent.cancellationCloseHours ?? null,
    volunteerRegistrationCloseHours: apiEvent.volunteerRegistrationCloseHours ?? null,
    volunteerCancellationCloseHours: apiEvent.volunteerCancellationCloseHours ?? null
  }
}

// Context type for optimistic event update rollback
interface OptimisticEventContext {
  previousEvent: EventDto | undefined
}

// Fetch events list with filters
export function useEvents(filters: EventFilters = {}) {
  return useQuery({
    queryKey: eventKeys.list(filters),
    queryFn: async (): Promise<EventDto[]> => {
      const { data } = await apiClient.get<ApiEvent[]>('/api/events', {
        params: filters,
      })

      // CRITICAL: Throw error if API didn't return data
      // This ensures proper error handling in the UI instead of showing empty state
      if (!data) {
        throw new Error('API returned no data - possible server connection issue')
      }

      // Return transformed events array
      return data.map(transformApiEvent)
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
      const { data } = await apiClient.get<PaginatedResponse<EventDto>>('/api/events', {
        params: { ...filters, page: pageParam, limit: 10 },
      })
      return data || { items: [], page: 1, limit: 10, total: 0, totalPages: 0, hasNext: false, hasPrev: false }
    },
    getNextPageParam: (lastPage: PaginatedResponse<EventDto>) =>
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
      const { data } = await apiClient.post<EventDto>('/api/events', eventData)
      if (!data) throw new Error('Failed to create event')
      return data
    },
    onSuccess: (newEvent: EventDto) => {
      // Invalidate events list to refetch
      cacheUtils.invalidateEvents(queryClient)

      // Optimistically add to cache
      if (newEvent.id) {
        queryClient.setQueryData(eventKeys.detail(newEvent.id), newEvent)
      }

    },
    onError: (_error: Error) => {
    },
  })
}

// Update event mutation with optimistic updates
export function useUpdateEvent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (eventData: UpdateEventDto): Promise<EventDto> => {
      const { data } = await apiClient.put<ApiEvent>(`/api/events/${eventData.id}`, eventData)
      if (!data) throw new Error('Failed to update event')
      // Transform the API response to match our EventDto structure
      return transformApiEvent(data)
    },
    onMutate: async (updatedEvent: UpdateEventDto) => {
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
    onSuccess: (data: EventDto, updatedEvent: UpdateEventDto) => {
      // CRITICAL: Immediately update cache with actual API response
      // This ensures the UI shows the correct saved values instead of optimistic values
      queryClient.setQueryData(eventKeys.detail(updatedEvent.id), data)
    },
    onError: (_err: Error, updatedEvent: UpdateEventDto, context: OptimisticEventContext | undefined) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(eventKeys.detail(updatedEvent.id), context.previousEvent)
      }
    },
    onSettled: () => {
      // Invalidate ALL event queries to ensure consistency across different query key patterns
      // This covers both eventKeys.lists() and queryKeys.events() patterns used in the app
      queryClient.invalidateQueries({ queryKey: ['events'] })
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
    onSuccess: (deletedId: string) => {
      queryClient.removeQueries({ queryKey: eventKeys.detail(deletedId) })
      cacheUtils.invalidateEvents(queryClient)
    },
    onError: (_error: Error) => {
    },
  })
}

// Event registration hooks
export function useEventRegistrations(eventId: string) {
  return useQuery({
    queryKey: eventKeys.registrations(eventId),
    queryFn: async (): Promise<RegistrationDto[]> => {
      const { data } = await apiClient.get<RegistrationDto[]>(`/api/events/${eventId}/registrations`)
      return data || []
    },
    enabled: !!eventId,
    staleTime: 2 * 60 * 1000, // 2 minutes - more frequent updates for registrations
  })
}

// Context type for optimistic registration rollback
interface OptimisticRegistrationContext {
  previousEvent: EventDto | undefined
}

export function useRegisterForEvent() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (eventId: string): Promise<RegistrationDto> => {
      const { data } = await apiClient.post<RegistrationDto>(`/api/events/${eventId}/register`)
      if (!data) throw new Error('Registration failed')
      return data
    },
    onMutate: async (eventId: string) => {
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
    onError: (_err: Error, eventId: string, context: OptimisticRegistrationContext | undefined) => {
      // Rollback registration count on error
      if (context?.previousEvent) {
        queryClient.setQueryData(eventKeys.detail(eventId), context.previousEvent)
      }
    },
    onSuccess: (_registration: RegistrationDto, eventId: string) => {
      // Refresh registration data
      queryClient.invalidateQueries({ queryKey: eventKeys.registrations(eventId) })
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) })
    },
  })
}
