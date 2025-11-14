import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../client';
import type { components } from '@witchcityrope/shared-types';

type VenueDto = components['schemas']['VenueDto'];

/**
 * Fetch a single venue by ID
 * Pattern B: Direct DTO response
 */
export function useVenue(venueId: number | null | undefined, enabled: boolean = true) {
  return useQuery({
    queryKey: ['venues', venueId],
    queryFn: async (): Promise<VenueDto | null> => {
      if (!venueId) return null;

      const { data } = await apiClient.get<VenueDto>(`/api/venues/${venueId}`);
      return data || null;
    },
    enabled: enabled && !!venueId,
    staleTime: 10 * 60 * 1000, // 10 minutes - venues don't change often
  });
}

/**
 * Fetch all active venues (public)
 * Pattern B: Direct DTO response
 */
export function useVenues() {
  return useQuery({
    queryKey: ['venues'],
    queryFn: async (): Promise<VenueDto[]> => {
      const { data } = await apiClient.get<VenueDto[]>('/api/venues');
      return data || [];
    },
    staleTime: 10 * 60 * 1000,
  });
}
