import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../lib/api/client';

interface PublicSettings {
  eventTimeZone: string;
}

/**
 * Hook to get the configured event timezone from public settings
 * Defaults to 'America/New_York' if not set or while loading
 */
export function useEventTimeZone(): string {
  const { data } = useQuery<PublicSettings>({
    queryKey: ['publicSettings'],
    queryFn: async () => {
      const response = await apiClient.get<PublicSettings>('/api/settings/public');
      return response.data;
    },
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
  });

  return data?.eventTimeZone || 'America/New_York';
}
