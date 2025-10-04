/**
 * useVettingStatus Hook
 * Fetches and caches current user's vetting application status
 */
import { useQuery } from '@tanstack/react-query';
import { useIsAuthenticated } from '../../../stores/authStore';
import type { MyApplicationStatusResponse, VettingStatusApiResponse } from '../types/vettingStatus';

/**
 * Fetch vetting status from API
 * Uses fetch directly with credentials for httpOnly cookie authentication
 */
const fetchVettingStatus = async (): Promise<MyApplicationStatusResponse> => {
  const response = await fetch('/api/vetting/status', {
    credentials: 'include' // Include httpOnly cookies
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch vetting status: ${response.status}`);
  }

  const data: VettingStatusApiResponse = await response.json();

  if (!data.success || !data.data) {
    throw new Error(data.error || 'Failed to retrieve vetting status');
  }

  return data.data;
};

/**
 * Hook to fetch current user's vetting status
 *
 * Features:
 * - 5-minute cache (staleTime)
 * - Automatic refetch on window focus
 * - Fail-open error handling
 * - Only fetches when user is authenticated
 *
 * @returns TanStack Query result with vetting status data
 *
 * @example
 * ```typescript
 * const { data, isLoading, error } = useVettingStatus();
 *
 * if (data?.hasApplication) {
 *   console.log('Status:', data.application.status);
 * }
 * ```
 */
export const useVettingStatus = () => {
  const isAuthenticated = useIsAuthenticated();

  return useQuery<MyApplicationStatusResponse>({
    queryKey: ['vetting', 'status'],
    queryFn: fetchVettingStatus,
    enabled: isAuthenticated, // Only fetch if user is logged in
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes (garbage collection)
    retry: 1, // Retry once on failure
    retryDelay: 1000, // 1 second retry delay
    refetchOnWindowFocus: true, // Refresh when user returns to tab
    refetchOnMount: true, // Refresh on component mount
    throwOnError: false // Don't throw errors - handle gracefully
  });
};
