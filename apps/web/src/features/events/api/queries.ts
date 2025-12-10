// features/events/api/queries.ts
import { useQuery, useInfiniteQuery } from '@tanstack/react-query'
import { apiClient } from '../../../lib/api/client'
import { queryKeys } from '../../../api/queryKeys'
import type { EventDto } from '@witchcityrope/shared-types'
import type { PaginatedResponse, EventFilters } from '../../../types/api.types'

export function useEvent(eventId: string) {
  return useQuery<EventDto>({
    queryKey: queryKeys.event(eventId),
    queryFn: async (): Promise<EventDto> => {
      const response = await apiClient.get(`/api/events/${eventId}`)
      // Pattern B: Direct DTO response
      return response.data
    },
    enabled: !!eventId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  })
}

export function useEvents(options: { includeUnpublished?: boolean } = {}) {
  return useQuery<EventDto[]>({
    queryKey: queryKeys.events(options),
    queryFn: async (): Promise<EventDto[]> => {
      const params: Record<string, any> = {}
      if (options.includeUnpublished) {
        params.includeUnpublished = true
      }

      const response = await apiClient.get('/api/events', { params })
      // Pattern B: Direct DTO response
      return response.data || []
    },
    staleTime: 5 * 60 * 1000,
  })
}

export function useInfiniteEvents(filters: EventFilters = {}) {
  return useInfiniteQuery<{ events: EventDto[], page: number, totalPages: number }>({
    queryKey: queryKeys.infiniteEvents(filters),
    queryFn: async ({ pageParam = 1 }): Promise<{ events: EventDto[], page: number, totalPages: number }> => {
      const response = await apiClient.get('/api/events', {
        params: { page: pageParam, pageSize: 20, ...filters }
      })
      // Pattern B: Direct DTO response
      return response.data || { events: [], page: 1, totalPages: 1 }
    },
    initialPageParam: 1,
    getNextPageParam: (lastPage) => {
      // EventListResponse has page, totalPages but no hasNext property
      const currentPage = lastPage.page || 1
      const totalPages = lastPage.totalPages || 1
      return currentPage < totalPages ? currentPage + 1 : undefined
    },
    maxPages: 10, // v5 feature - limit memory usage
    staleTime: 2 * 60 * 1000, // 2 minutes
  })
}

// Query for checking user's attendance status (RSVP or ticket)
export function useEventAttendanceStatus(eventId: string) {
  return useQuery({
    queryKey: ['event-attendance', eventId],
    queryFn: async () => {
      const response = await apiClient.get(`/api/events-management/${eventId}/attendance`)
      return response.data as {
        hasRSVP: boolean
        hasTicket: boolean
        rsvp: { id: string; confirmationCode: string; status: string } | null
        ticket: { id: string; confirmationCode: string; status: string } | null
        canRSVP: boolean
        canPurchaseTicket: boolean
      }
    },
    enabled: !!eventId,
    staleTime: 1 * 60 * 1000, // 1 minute
  })
}