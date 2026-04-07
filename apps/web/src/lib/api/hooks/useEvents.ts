import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query'
import { apiClient } from '../client'
import { eventKeys, cacheUtils } from '../utils/cache'
import type { EventDto, CreateEventDto, UpdateEventDto, EventFilters, RegistrationDto } from '../types/events.types'
import type { PaginatedResponse } from '../types/api.types'

// Pattern B: Direct DTO response — no manual transform layer.
// Auto-generated EventDto from @witchcityrope/shared-types is the source of truth.
// API response fields flow through directly without field-by-field mapping.
// See DTO-ALIGNMENT-STRATEGY.md for why this matters.

// Context type for optimistic event update rollback
interface OptimisticEventContext {
  previousEvent: EventDto | undefined
}

// Fetch events list with filters
export function useEvents(filters: EventFilters = {}) {
  return useQuery<EventDto[]>({
    queryKey: eventKeys.list(filters),
    queryFn: async (): Promise<EventDto[]> => {
      const { data } = await apiClient.get<EventDto[]>('/api/events', {
        params: filters,
      })

      // CRITICAL: Throw error if API didn't return data
      // This ensures proper error handling in the UI instead of showing empty state
      if (!data) {
        throw new Error('API returned no data - possible server connection issue')
      }

      return data
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
  return useQuery<EventDto>({
    queryKey: eventKeys.detail(id),
    queryFn: async (): Promise<EventDto> => {
      const { data } = await apiClient.get<EventDto>(`/api/events/${id}`)
      if (!data) throw new Error('Event not found')
      return data
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
      const { data } = await apiClient.put<EventDto>(`/api/events/${eventData.id}`, eventData)
      if (!data) throw new Error('Failed to update event')
      return data
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
