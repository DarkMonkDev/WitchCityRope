import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../client';
import type { components } from '@witchcityrope/shared-types';

type VenueDto = components['schemas']['VenueDto'];

interface ApiResponse<T> {
  success?: boolean;
  data?: T;
  error?: string | null;
  message?: string | null;
}

/**
 * Fetch a single venue by ID
 */
export function useVenue(venueId: number | null | undefined, enabled: boolean = true) {
  return useQuery({
    queryKey: ['venues', venueId],
    queryFn: async (): Promise<VenueDto | null> => {
      if (!venueId) return null;

      const { data } = await apiClient.get<ApiResponse<VenueDto>>(`/api/venues/${venueId}`);
      return data?.data || null;
    },
    enabled: enabled && !!venueId,
    staleTime: 10 * 60 * 1000, // 10 minutes - venues don't change often
  });
}

/**
 * Fetch all active venues (public)
 */
export function useVenues() {
  return useQuery({
    queryKey: ['venues'],
    queryFn: async (): Promise<VenueDto[]> => {
      const { data } = await apiClient.get<ApiResponse<VenueDto[]>>('/api/venues');
      return data?.data || [];
    },
    staleTime: 10 * 60 * 1000,
  });
}
