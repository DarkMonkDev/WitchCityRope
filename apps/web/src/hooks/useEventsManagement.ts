/**
 * TanStack Query hooks for Events Management API
 * Provides React hooks for the Events Management System
 * @created 2025-09-06
 */

import { useQuery } from '@tanstack/react-query';
import { eventsManagementService } from '../api/services/eventsManagement.service';
import type {
  EventSummaryDto,
  EventDetailsDto,
  EventAvailabilityDto,
  EventsFilters
} from '@witchcityrope/shared-types';

/**
 * Query keys for Events Management API
 */
export const eventsManagementKeys = {
  all: ['eventsManagement'] as const,
  events: () => [...eventsManagementKeys.all, 'events'] as const,
  eventsList: (filters: EventsFilters) => [...eventsManagementKeys.events(), 'list', filters] as const,
  eventDetails: (eventId: string) => [...eventsManagementKeys.events(), 'details', eventId] as const,
  eventAvailability: (eventId: string) => [...eventsManagementKeys.events(), 'availability', eventId] as const,
};

/**
 * Hook to fetch list of published events
 * Uses EventSummaryDto for list view performance
 */
export function useEventsManagement(filters: EventsFilters = {}, options: { enabled?: boolean } = {}) {
  return useQuery({
    queryKey: eventsManagementKeys.eventsList(filters),
    queryFn: () => eventsManagementService.getEvents(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes - events don't change frequently
    refetchOnWindowFocus: false,
    retry: 3,
    enabled: options.enabled ?? true,
  });
}

/**
 * Hook to fetch complete event details with sessions and ticket types
 * Uses EventDetailsDto with all related data
 */
export function useEventDetails(eventId: string, enabled: boolean = true) {
  return useQuery({
    queryKey: eventsManagementKeys.eventDetails(eventId),
    queryFn: () => eventsManagementService.getEventDetails(eventId),
    enabled: !!eventId && enabled,
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: false,
    retry: (failureCount, error: any) => {
      // Don't retry on 404 errors
      if (error?.message === 'Event not found') {
        return false;
      }
      return failureCount < 3;
    },
  });
}

/**
 * Hook to fetch real-time event availability
 * More frequent updates for availability data
 */
export function useEventAvailability(eventId: string, enabled: boolean = true) {
  return useQuery({
    queryKey: eventsManagementKeys.eventAvailability(eventId),
    queryFn: () => eventsManagementService.getEventAvailability(eventId),
    enabled: !!eventId && enabled,
    staleTime: 30 * 1000, // 30 seconds - availability changes frequently
    refetchInterval: 2 * 60 * 1000, // Refetch every 2 minutes when component is mounted
    refetchOnWindowFocus: true, // Always check when user returns to window
    retry: (failureCount, error: any) => {
      // Don't retry on 404 errors
      if (error?.message === 'Event not found') {
        return false;
      }
      return failureCount < 3;
    },
  });
}

/**
 * Combined hook to fetch both event details and availability
 * Useful for pages that need both data sets
 */
export function useEventWithAvailability(eventId: string, enabled: boolean = true) {
  const eventDetails = useEventDetails(eventId, enabled);
  const eventAvailability = useEventAvailability(eventId, enabled && eventDetails.isSuccess);
  
  return {
    event: eventDetails.data,
    availability: eventAvailability.data,
    isLoading: eventDetails.isLoading || (eventDetails.isSuccess && eventAvailability.isLoading),
    isError: eventDetails.isError || eventAvailability.isError,
    error: eventDetails.error || eventAvailability.error,
    isSuccess: eventDetails.isSuccess && eventAvailability.isSuccess,
    refetch: () => {
      eventDetails.refetch();
      eventAvailability.refetch();
    }
  };
}