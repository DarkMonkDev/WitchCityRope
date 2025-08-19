// features/events/api/queries.ts
import { useQuery, useInfiniteQuery } from '@tanstack/react-query'
import { api } from '../../../api/client'
import { queryKeys } from '../../../api/queryKeys'
import type { Event, PaginatedResponse, EventFilters } from '../../../types/api.types'

export function useEvent(eventId: string) {
  return useQuery({
    queryKey: queryKeys.event(eventId),
    queryFn: async (): Promise<Event> => {
      const response = await api.get(`/api/events/${eventId}`)
      return response.data
    },
    enabled: !!eventId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  })
}

export function useEvents() {
  return useQuery({
    queryKey: queryKeys.events(),
    queryFn: async (): Promise<Event[]> => {
      const response = await api.get('/api/events')
      return response.data
    },
    staleTime: 5 * 60 * 1000,
  })
}

export function useInfiniteEvents(filters: EventFilters = {}) {
  return useInfiniteQuery({
    queryKey: queryKeys.infiniteEvents(filters),
    queryFn: async ({ pageParam = 1 }): Promise<PaginatedResponse<Event>> => {
      const response = await api.get('/api/events', {
        params: { page: pageParam, pageSize: 20, ...filters }
      })
      return response.data
    },
    initialPageParam: 1,
    getNextPageParam: (lastPage) => {
      return lastPage.hasNext ? lastPage.page + 1 : undefined
    },
    maxPages: 10, // v5 feature - limit memory usage
    staleTime: 2 * 60 * 1000, // 2 minutes
  })
}