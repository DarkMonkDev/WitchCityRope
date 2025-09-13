// Safety System React Query Hooks
// Provides data fetching and caching for safety incidents

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { safetyApi } from '../api/safetyApi';
import type {
  SubmitIncidentRequest,
  SearchIncidentsRequest,
  UpdateIncidentRequest,
  IncidentSeverity,
  IncidentStatus
} from '../types/safety.types';

// Query keys for cache management
export const safetyKeys = {
  all: ['safety'] as const,
  incidents: () => [...safetyKeys.all, 'incidents'] as const,
  incident: (id: string) => [...safetyKeys.incidents(), id] as const,
  status: (referenceNumber: string) => [...safetyKeys.all, 'status', referenceNumber] as const,
  dashboard: () => [...safetyKeys.all, 'dashboard'] as const,
  userReports: () => [...safetyKeys.all, 'user-reports'] as const,
  search: (filters: SearchIncidentsRequest) => [...safetyKeys.incidents(), 'search', filters] as const
};

/**
 * Submit a new safety incident report
 * Supports both anonymous and authenticated submissions
 */
export function useSubmitIncident() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: SubmitIncidentRequest) => safetyApi.submitIncident(request),
    onSuccess: (data, variables) => {
      // Invalidate dashboard if this was from an authenticated user
      if (!variables.isAnonymous) {
        queryClient.invalidateQueries({ queryKey: safetyKeys.dashboard() });
        queryClient.invalidateQueries({ queryKey: safetyKeys.userReports() });
      }
      
      console.log('Incident submitted successfully:', data.referenceNumber);
    },
    onError: (error) => {
      console.error('Failed to submit incident:', error);
    }
  });
}

/**
 * Get incident status by reference number
 * Public endpoint for tracking incident progress
 */
export function useIncidentStatus(referenceNumber: string, enabled: boolean = true) {
  return useQuery({
    queryKey: safetyKeys.status(referenceNumber),
    queryFn: () => safetyApi.getIncidentStatus(referenceNumber),
    enabled: enabled && !!referenceNumber,
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: false // Don't retry if incident not found
  });
}

/**
 * Get detailed incident information for safety team
 * Requires safety team authorization
 */
export function useIncidentDetail(incidentId: string, enabled: boolean = true) {
  return useQuery({
    queryKey: safetyKeys.incident(incidentId),
    queryFn: () => safetyApi.getIncidentDetail(incidentId),
    enabled: enabled && !!incidentId,
    staleTime: 30 * 1000, // 30 seconds (incident data changes frequently)
    retry: (failureCount, error: any) => {
      // Don't retry on 403/404 errors
      if (error?.response?.status === 403 || error?.response?.status === 404) {
        return false;
      }
      return failureCount < 2;
    }
  });
}

/**
 * Get safety dashboard data for admin interface
 * Requires safety team authorization
 */
export function useSafetyDashboard(enabled: boolean = true) {
  return useQuery({
    queryKey: safetyKeys.dashboard(),
    queryFn: () => safetyApi.getDashboardData(),
    enabled,
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchInterval: 5 * 60 * 1000, // Auto-refresh every 5 minutes
    retry: (failureCount, error: any) => {
      // Don't retry on 403 errors
      if (error?.response?.status === 403) {
        return false;
      }
      return failureCount < 2;
    }
  });
}

/**
 * Search and filter incidents for admin management
 * Requires safety team authorization
 */
export function useSearchIncidents(request: SearchIncidentsRequest, enabled: boolean = true) {
  return useQuery({
    queryKey: safetyKeys.search(request),
    queryFn: () => safetyApi.searchIncidents(request),
    enabled,
    staleTime: 1 * 60 * 1000, // 1 minute
    keepPreviousData: true, // Keep previous results while loading new ones
    retry: (failureCount, error: any) => {
      // Don't retry on 403 errors
      if (error?.response?.status === 403) {
        return false;
      }
      return failureCount < 2;
    }
  });
}

/**
 * Update incident status and assignment
 * Requires safety team authorization with optimistic updates
 */
export function useUpdateIncident() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ incidentId, request }: { incidentId: string; request: UpdateIncidentRequest }) =>
      safetyApi.updateIncident(incidentId, request),
    
    onMutate: async ({ incidentId, request }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: safetyKeys.incident(incidentId) });
      
      // Snapshot previous value
      const previousIncident = queryClient.getQueryData(safetyKeys.incident(incidentId));
      
      // Optimistically update the incident
      queryClient.setQueryData(safetyKeys.incident(incidentId), (old: any) => {
        if (!old) return old;
        return { ...old, ...request, updatedAt: new Date().toISOString() };
      });
      
      return { previousIncident };
    },
    
    onError: (err, { incidentId }, context) => {
      // Rollback on error
      if (context?.previousIncident) {
        queryClient.setQueryData(safetyKeys.incident(incidentId), context.previousIncident);
      }
      console.error('Failed to update incident:', err);
    },
    
    onSuccess: (data, { incidentId }) => {
      // Update the cache with the response data
      queryClient.setQueryData(safetyKeys.incident(incidentId), data);
      
      // Invalidate related queries
      queryClient.invalidateQueries({ queryKey: safetyKeys.dashboard() });
      queryClient.invalidateQueries({ queryKey: safetyKeys.incidents() });
      
      console.log('Incident updated successfully:', data.referenceNumber);
    }
  });
}

/**
 * Get user's own incident reports
 * Requires authentication
 */
export function useUserReports(enabled: boolean = true) {
  return useQuery({
    queryKey: safetyKeys.userReports(),
    queryFn: () => safetyApi.getUserReports(),
    enabled,
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: (failureCount, error: any) => {
      // Don't retry on 401/403 errors
      if (error?.response?.status === 401 || error?.response?.status === 403) {
        return false;
      }
      return failureCount < 2;
    }
  });
}

/**
 * Composite hook for admin incident management
 * Combines dashboard data and recent incidents
 */
export function useSafetyAdminData(enabled: boolean = true) {
  const dashboardQuery = useSafetyDashboard(enabled);
  
  const searchQuery = useSearchIncidents({
    page: 1,
    pageSize: 25
  }, enabled);
  
  return {
    dashboard: dashboardQuery.data,
    recentIncidents: searchQuery.data?.items || [],
    totalIncidents: searchQuery.data?.total || 0,
    isLoading: dashboardQuery.isLoading || searchQuery.isLoading,
    error: dashboardQuery.error || searchQuery.error,
    refetch: async () => {
      await Promise.all([
        dashboardQuery.refetch(),
        searchQuery.refetch()
      ]);
    }
  };
}

/**
 * Helper hook to check if user has safety team access
 * Based on API response patterns
 */
export function useSafetyTeamAccess() {
  const dashboardQuery = useSafetyDashboard(true);
  
  return {
    hasAccess: !dashboardQuery.error || dashboardQuery.data !== undefined,
    isLoading: dashboardQuery.isLoading,
    error: dashboardQuery.error
  };
}