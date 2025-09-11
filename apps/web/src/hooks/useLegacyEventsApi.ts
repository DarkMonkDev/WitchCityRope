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
  console.log('🔍 useLegacyEvents hook called');
  
  return useQuery({
    queryKey: legacyEventsKeys.eventsList(),
    queryFn: () => {
      console.log('🔍 useLegacyEvents queryFn executing...');
      return legacyEventsApiService.getEvents();
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // Keep in cache for 10 minutes
    refetchOnWindowFocus: false,
    refetchOnMount: false, // Don't refetch on component mount if data is fresh
    refetchInterval: false, // Disable automatic refetching
    refetchOnReconnect: false, // Don't refetch on reconnect
    retry: (failureCount, error: any) => {
      // Don't retry on client errors (4xx)
      if (error?.response?.status >= 400 && error?.response?.status < 500) {
        console.log('🔍 Not retrying client error:', error?.response?.status);
        return false;
      }
      return failureCount < 3;
    },
  });
}

/**
 * Hook to fetch event details using current API
 */
export function useLegacyEventDetails(eventId: string, enabled: boolean = true) {
  console.log('🔍 useLegacyEventDetails hook called', { eventId, enabled });
  
  return useQuery({
    queryKey: legacyEventsKeys.eventDetails(eventId),
    queryFn: () => {
      console.log('🔍 useLegacyEventDetails queryFn executing for eventId:', eventId);
      return legacyEventsApiService.getEventDetails(eventId);
    },
    enabled: !!eventId && enabled,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000, // Keep in cache for 10 minutes
    refetchOnWindowFocus: false,
    refetchOnMount: false, // Don't refetch on component mount if data is fresh
    refetchInterval: false, // Disable automatic refetching
    refetchOnReconnect: false, // Don't refetch on reconnect
    retry: (failureCount, error: any) => {
      // Don't retry on specific error messages
      if (error?.message === 'Event not found') {
        console.log('🔍 Not retrying - event not found');
        return false;
      }
      // Don't retry on client errors (4xx)
      if (error?.response?.status >= 400 && error?.response?.status < 500) {
        console.log('🔍 Not retrying client error:', error?.response?.status);
        return false;
      }
      return failureCount < 3;
    },
  });
}