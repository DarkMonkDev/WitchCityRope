/**
 * Legacy Events API hooks
 * Temporary hooks to demonstrate API integration patterns
 * @created 2025-09-06
 */

import { useQuery } from '@tanstack/react-query';
import { legacyEventsApiService } from '../api/services/legacyEventsApi.service';
import { debugLog } from '../utils/debug';

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
  debugLog('🔍 useLegacyEvents hook called');
  
  return useQuery({
    queryKey: legacyEventsKeys.eventsList(),
    queryFn: () => {
      debugLog('🔍 useLegacyEvents queryFn executing...');
      return legacyEventsApiService.getEvents();
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // Keep in cache for 10 minutes
    refetchOnWindowFocus: false,
    refetchOnMount: false, // Don't refetch on component mount if data is fresh
    refetchInterval: false, // Disable automatic refetching
    refetchOnReconnect: false, // Don't refetch on reconnect
    retry: (failureCount: number, error: Error) => {
      // Don't retry on client errors (4xx)
      if ((error as any)?.response?.status >= 400 && (error as any)?.response?.status < 500) {
        debugLog('🔍 Not retrying client error:', (error as any)?.response?.status);
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
  debugLog('🔍 useLegacyEventDetails hook called', { eventId, enabled });
  
  return useQuery({
    queryKey: legacyEventsKeys.eventDetails(eventId),
    queryFn: () => {
      debugLog('🔍 useLegacyEventDetails queryFn executing for eventId:', eventId);
      return legacyEventsApiService.getEventDetails(eventId);
    },
    enabled: !!eventId && enabled,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000, // Keep in cache for 10 minutes
    refetchOnWindowFocus: false,
    refetchOnMount: false, // Don't refetch on component mount if data is fresh
    refetchInterval: false, // Disable automatic refetching
    refetchOnReconnect: false, // Don't refetch on reconnect
    retry: (failureCount: number, error: any) => {
      // Don't retry on specific error messages
      if (error?.message === 'Event not found') {
        debugLog('🔍 Not retrying - event not found');
        return false;
      }
      // Don't retry on client errors (4xx)
      if (error?.response?.status >= 400 && error?.response?.status < 500) {
        debugLog('🔍 Not retrying client error:', error?.response?.status);
        return false;
      }
      return failureCount < 3;
    },
  });
}