// Safety System React Query Hooks
// Provides data fetching and caching for safety incidents

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { safetyApi } from '../api/safetyApi';
import type {
  SubmitIncidentRequest,
  SearchIncidentsRequest,
  UpdateIncidentRequest,
  IncidentSeverity,
  IncidentStatus,
  SafetyIncidentDto
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
 * Public endpoint, no auth required
 */
export function useIncidentStatus(referenceNumber: string, enabled: boolean = true) {
  return useQuery({
    queryKey: safetyKeys.status(referenceNumber),
    queryFn: () => safetyApi.getIncidentStatus(referenceNumber),
    enabled: enabled && !!referenceNumber,
    staleTime: 60 * 1000, // 1 minute (status doesn't change rapidly)
    retry: false // Don't retry failed status lookups
  });
}

/**
 * Get detailed incident information for safety team
 * Requires safety team authorization
 */
export function useIncidentDetail(incidentId: string, enabled: boolean = true) {
  return useQuery<SafetyIncidentDto>({
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
 * Get safety dashboard data for admin overview
 * Requires safety team authorization
 */
export function useSafetyDashboard() {
  return useQuery({
    queryKey: safetyKeys.dashboard(),
    queryFn: () => safetyApi.getSafetyDashboard(),
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchOnWindowFocus: true, // Dashboard should be current when visible
    retry: (failureCount, error: any) => {
      if (error?.response?.status === 403) {
        return false;
      }
      return failureCount < 2;
    }
  });
}

/**
 * Search incidents with filtering
 * Requires safety team authorization
 */
export function useSearchIncidents(filters: SearchIncidentsRequest) {
  return useQuery({
    queryKey: safetyKeys.search(filters),
    queryFn: () => safetyApi.searchIncidents(filters),
    enabled: Object.keys(filters).some(key => filters[key as keyof SearchIncidentsRequest] !== undefined),
    staleTime: 60 * 1000, // 1 minute
    keepPreviousData: true // Maintain search results while fetching new ones
  });
}

/**
 * Get user's own incident reports
 * For authenticated users to track their reports
 */
export function useUserReports() {
  return useQuery({
    queryKey: safetyKeys.userReports(),
    queryFn: () => safetyApi.getUserReports(),
    staleTime: 5 * 60 * 1000, // 5 minutes (user reports don't change often)
    retry: (failureCount, error: any) => {
      if (error?.response?.status === 403) {
        return false;
      }
      return failureCount < 2;
    }
  });
}

/**
 * Update incident status, assignment, or add notes
 * Requires safety team authorization
 */
export function useUpdateIncident() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ incidentId, request }: { incidentId: string; request: UpdateIncidentRequest }) =>
      safetyApi.updateIncident(incidentId, request),
    onSuccess: (_, { incidentId }) => {
      // Invalidate related queries
      queryClient.invalidateQueries({ queryKey: safetyKeys.incident(incidentId) });
      queryClient.invalidateQueries({ queryKey: safetyKeys.dashboard() });
      queryClient.invalidateQueries({ queryKey: safetyKeys.incidents() });
    },
    onError: (error) => {
      console.error('Failed to update incident:', error);
    }
  });
}

/**
 * Delete incident (admin only)
 * Requires admin authorization
 */
export function useDeleteIncident() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (incidentId: string) => safetyApi.deleteIncident(incidentId),
    onSuccess: () => {
      // Invalidate all incident-related queries
      queryClient.invalidateQueries({ queryKey: safetyKeys.all });
    },
    onError: (error) => {
      console.error('Failed to delete incident:', error);
    }
  });
}

/**
 * Bulk actions on incidents
 * For admin batch operations
 */
export function useBulkUpdateIncidents() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ incidentIds, updates }: { incidentIds: string[]; updates: UpdateIncidentRequest }) =>
      safetyApi.bulkUpdateIncidents([{ incidentIds, ...updates }]),
    onSuccess: () => {
      // Invalidate all incident-related queries
      queryClient.invalidateQueries({ queryKey: safetyKeys.all });
    },
    onError: (error) => {
      console.error('Failed to bulk update incidents:', error);
    }
  });
}

/**
 * Export incidents data
 * For compliance and reporting
 */
export function useExportIncidents() {
  return useMutation({
    mutationFn: (filters: SearchIncidentsRequest) => safetyApi.exportIncidents(filters),
    onError: (error) => {
      console.error('Failed to export incidents:', error);
    }
  });
}