/**
 * Legacy Events API hooks
 * Temporary hooks to demonstrate API integration patterns
 * @created 2025-09-06
 */

import { useQuery } from '@tanstack/react-query';
import { legacyEventsApiService, type LegacyEventDto } from '../api/services/legacyEventsApi.service';

/**
 * Query keys for Legacy Events API
 */
export const legacyEventsKeys = {
  all: ['legacyEvents'] as const,
  events: () => [...legacyEventsKeys.all, 'events'] as const,
  eventsList: () => [...legacyEventsKeys.events(), 'list'] as const,
  eventDetails: (eventId: string) => [...legacyEventsKeys.events(), 'details', eventId] as const,
};

/**
 * Hook to fetch list of events using current API
 */
export function useLegacyEvents() {
  return useQuery({
    queryKey: legacyEventsKeys.eventsList(),
    queryFn: () => legacyEventsApiService.getEvents(),
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // Keep in cache for 10 minutes
    refetchOnWindowFocus: false,
    refetchOnMount: false, // Don't refetch on component mount if data is fresh
    retry: 3,
  });
}

/**
 * Hook to fetch event details using current API
 */
export function useLegacyEventDetails(eventId: string, enabled: boolean = true) {
  return useQuery({
    queryKey: legacyEventsKeys.eventDetails(eventId),
    queryFn: () => legacyEventsApiService.getEventDetails(eventId),
    enabled: !!eventId && enabled,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000, // Keep in cache for 10 minutes
    refetchOnWindowFocus: false,
    refetchOnMount: false, // Don't refetch on component mount if data is fresh
    retry: (failureCount, error: any) => {
      if (error?.message === 'Event not found') {
        return false;
      }
      return failureCount < 3;
    },
  });
}