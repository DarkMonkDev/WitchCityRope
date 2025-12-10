import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../lib/api/client';

interface Settings {
  EventTimeZone: string;
}

/**
 * Hook to get the configured event timezone from admin settings
 * Defaults to 'America/New_York' if not set or while loading
 */
export function useEventTimeZone(): string {
  const { data } = useQuery<Settings>({
    queryKey: ['adminSettings'],
    queryFn: async () => {
      const response = await apiClient.get<Settings>('/api/admin/settings');
      return response.data;
    },
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
  });

  return data?.EventTimeZone || 'America/New_York';
}
