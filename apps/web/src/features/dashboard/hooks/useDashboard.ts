// Dashboard React Hooks
// Custom hooks for dashboard data fetching with React Query

import { useQuery } from '@tanstack/react-query';
import { dashboardApi, dashboardApiUtils } from '../api/dashboardApi';
import type {
  UserDashboardResponse,
  UserEventsResponse,
  UserStatisticsResponse
} from '../types/dashboard.types';

/**
 * Query keys for dashboard data
 * Centralized to ensure consistency and easy cache invalidation
 */
export const dashboardQueryKeys = {
  all: ['dashboard'] as const,
  dashboard: () => [...dashboardQueryKeys.all, 'user'] as const,
  events: (count?: number) => [...dashboardQueryKeys.all, 'events', count] as const,
  statistics: () => [...dashboardQueryKeys.all, 'statistics'] as const,
} as const;

/**
 * Hook for fetching user dashboard data
 * @returns React Query result with dashboard data, loading state, and error
 */
export function useUserDashboard() {
  return useQuery<UserDashboardResponse>({
    queryKey: dashboardQueryKeys.dashboard(),
    queryFn: () => dashboardApi.getUserDashboard(),
    staleTime: 5 * 60 * 1000, // 5 minutes - dashboard data doesn't change frequently
    refetchOnWindowFocus: false,
    retry: (failureCount, error) => {
      // Don't retry on auth errors
      if (dashboardApiUtils.isAuthError(error)) {
        return false;
      }
      // Retry up to 2 times for other errors
      return failureCount < 2;
    },
    meta: {
      errorMessage: 'Failed to load your dashboard. Please try refreshing the page.',
    },
  });
}

/**
 * Hook for fetching user's upcoming events
 * @param count Number of events to retrieve (default: 3)
 * @returns React Query result with upcoming events, loading state, and error
 */
export function useUserEvents(count: number = 3) {
  return useQuery<UserEventsResponse>({
    queryKey: dashboardQueryKeys.events(count),
    queryFn: () => dashboardApi.getUserEvents(count),
    staleTime: 2 * 60 * 1000, // 2 minutes - event data might change more frequently
    refetchOnWindowFocus: false,
    retry: (failureCount, error) => {
      // Don't retry on auth errors
      if (dashboardApiUtils.isAuthError(error)) {
        return false;
      }
      // Retry up to 2 times for other errors
      return failureCount < 2;
    },
    meta: {
      errorMessage: 'Failed to load your upcoming events.',
    },
  });
}

/**
 * Hook for fetching user's membership statistics
 * @returns React Query result with statistics data, loading state, and error
 */
export function useUserStatistics() {
  return useQuery<UserStatisticsResponse>({
    queryKey: dashboardQueryKeys.statistics(),
    queryFn: () => dashboardApi.getUserStatistics(),
    staleTime: 10 * 60 * 1000, // 10 minutes - statistics change infrequently
    refetchOnWindowFocus: false,
    retry: (failureCount, error) => {
      // Don't retry on auth errors
      if (dashboardApiUtils.isAuthError(error)) {
        return false;
      }
      // Retry up to 2 times for other errors
      return failureCount < 2;
    },
    meta: {
      errorMessage: 'Failed to load your membership statistics.',
    },
  });
}

/**
 * Combined hook for fetching all dashboard data
 * Useful when you need all dashboard data in one component
 * @param eventsCount Number of events to retrieve (default: 3)
 * @returns Object with all dashboard queries
 */
export function useDashboardData(eventsCount: number = 3) {
  const dashboardQuery = useUserDashboard();
  const eventsQuery = useUserEvents(eventsCount);
  const statisticsQuery = useUserStatistics();

  return {
    dashboard: dashboardQuery,
    events: eventsQuery,
    statistics: statisticsQuery,
    
    // Aggregated loading and error states
    isLoading: dashboardQuery.isLoading || eventsQuery.isLoading || statisticsQuery.isLoading,
    isError: dashboardQuery.isError || eventsQuery.isError || statisticsQuery.isError,
    error: dashboardQuery.error || eventsQuery.error || statisticsQuery.error,
    
    // Success state - all queries succeeded and have data
    isSuccess: dashboardQuery.isSuccess && eventsQuery.isSuccess && statisticsQuery.isSuccess,
    
    // Data objects
    data: {
      dashboard: dashboardQuery.data,
      events: eventsQuery.data,
      statistics: statisticsQuery.data,
    },
    
    // Refetch functions
    refetchAll: () => {
      dashboardQuery.refetch();
      eventsQuery.refetch();
      statisticsQuery.refetch();
    },
  };
}

/**
 * Hook for dashboard error handling
 * Provides consistent error message formatting
 */
export function useDashboardError(error: Error | null) {
  if (!error) return null;

  return {
    message: dashboardApiUtils.getErrorMessage(error),
    isNetworkError: dashboardApiUtils.isNetworkError(error),
    isAuthError: dashboardApiUtils.isAuthError(error),
    isServerError: dashboardApiUtils.isServerError(error),
  };
}