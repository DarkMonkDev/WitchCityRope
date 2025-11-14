import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../client'
import { eventKeys } from '../utils/cache'
import type { components } from '@witchcityrope/shared-types'

// Use generated type from OpenAPI spec - NEVER manually define this
export type EventParticipationDto = components['schemas']['EventParticipationDto']

/**
 * Hook to fetch all participations (RSVPs and tickets) for a specific event
 * Admin only - requires Admin role
 */
export function useEventParticipations(eventId: string, enabled: boolean = true) {
  return useQuery({
    queryKey: eventKeys.participations(eventId),
    queryFn: async (): Promise<EventParticipationDto[]> => {
      const { data } = await apiClient.get<EventParticipationDto[]>(
        `/api/admin/events/${eventId}/participations`
      )
      return data || []
    },
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000, // 2 minutes - fairly fresh for admin data
  })
}