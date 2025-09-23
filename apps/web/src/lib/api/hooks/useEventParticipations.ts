import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../client'
import { eventKeys } from '../utils/cache'
import type { ApiResponse } from '../types/api.types'

export interface EventParticipationDto {
  id: string
  userId: string
  userSceneName: string
  userEmail: string
  participationType: 'RSVP' | 'Ticket'
  status: 'Active' | 'Cancelled'
  participationDate: string
  notes?: string
  canCancel: boolean
  metadata?: string | null
}

/**
 * Hook to fetch all participations (RSVPs and tickets) for a specific event
 * Admin only - requires Admin role
 */
export function useEventParticipations(eventId: string, enabled: boolean = true) {
  return useQuery({
    queryKey: eventKeys.participations(eventId),
    queryFn: async (): Promise<EventParticipationDto[]> => {
      const { data } = await apiClient.get<ApiResponse<EventParticipationDto[]>>(
        `/api/admin/events/${eventId}/participations`
      )
      return data?.data || []
    },
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000, // 2 minutes - fairly fresh for admin data
  })
}