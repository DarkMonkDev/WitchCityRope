// features/events/api/queries.ts
import { useQuery, useInfiniteQuery } from '@tanstack/react-query'
import { api } from '../../../api/client'
import { queryKeys } from '../../../api/queryKeys'
import type { EventDto, EventListResponse } from '@witchcityrope/shared-types'
import type { PaginatedResponse, EventFilters } from '../../../types/api.types'

export function useEvent(eventId: string) {
  return useQuery<EventDto, Error>({
    queryKey: queryKeys.event(eventId),
    queryFn: async (): Promise<EventDto> => {
      const response = await api.get(`/api/events/${eventId}`)
      return response.data
    },
    enabled: !!eventId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  })
}

export function useEvents() {
  return useQuery<EventDto[], Error>({
    queryKey: queryKeys.events(),
    queryFn: async (): Promise<EventDto[]> => {
      const response = await api.get<EventListResponse>('/api/events')
      return response.data.events || []
    },
    staleTime: 5 * 60 * 1000,
  })
}

export function useInfiniteEvents(filters: EventFilters = {}) {
  return useInfiniteQuery<EventListResponse, Error>({
    queryKey: queryKeys.infiniteEvents(filters),
    queryFn: async ({ pageParam = 1 }): Promise<EventListResponse> => {
      const response = await api.get<EventListResponse>('/api/events', {
        params: { page: pageParam, pageSize: 20, ...filters }
      })
      return response.data
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