/**
 * React hooks for volunteer assignment API operations
 * Provides hooks for managing volunteer position assignments and member search
 *
 * DTO ALIGNMENT STRATEGY - CRITICAL RULES:
 * ════════════════════════════════════════
 * 1. API DTOs (C#) are the SOURCE OF TRUTH
 * 2. TypeScript types are AUTO-GENERATED from OpenAPI spec via @witchcityrope/shared-types
 * 3. NEVER manually create TypeScript interfaces for API response data
 * 4. If a type is missing, expose it in the backend API (add .Produces<> to endpoint)
 * 5. Regenerate types: cd packages/shared-types && npm run generate
 *
 * WHY: Prevents type mismatches, ensures type safety, eliminates manual sync work
 * SEE: /docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md
 * ════════════════════════════════════════
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../client';
import { notifications } from '@mantine/notifications';
import { useMemo } from 'react';
import type { components } from '@witchcityrope/shared-types';

// ============================================================================
// Generated API DTOs
// ============================================================================

/**
 * Volunteer Assignment DTO
 * @generated from C# VolunteerAssignmentDto via NSwag
 * Note: Using VolunteerAssignmentDto (not VolunteerAssignmentDto2) for list endpoint
 */
export type VolunteerAssignmentDto = components['schemas']['VolunteerAssignmentDto'];

/**
 * User Search Result DTO
 * @generated from C# UserSearchResultDto via NSwag
 */
export type UserSearchResultDto = components['schemas']['UserSearchResultDto'];

// ============================================================================
// Frontend-Only Types (NOT sent to API)
// ============================================================================

/**
 * Request to assign a member to a volunteer position
 * Frontend request structure (userId is the only field needed)
 */
export interface AssignVolunteerRequest {
  userId: string;
}

/**
 * API Response wrapper (generic)
 * Note: Backend uses ApiResponse<T> pattern
 */
interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message?: string | null;
  error?: string | null;
}

// ============================================================================
// React Query Hooks
// ============================================================================

/**
 * Hook to get assigned members for a volunteer position
 * @param positionId - The volunteer position ID
 * @returns Query result with list of assigned members
 */
export function usePositionSignups(positionId: string | undefined) {
  return useQuery<VolunteerAssignmentDto[]>({
    queryKey: ['volunteer-signups', positionId],
    queryFn: async () => {
      if (!positionId) {
        throw new Error('Position ID is required');
      }

      const response = await apiClient.get<ApiResponse<VolunteerAssignmentDto[]>>(
        `/api/volunteer-positions/${positionId}/signups`
      );

      if (!response.data.success || !response.data.data) {
        throw new Error(response.data.error || 'Failed to load volunteer assignments');
      }

      return response.data.data;
    },
    enabled: !!positionId,
    staleTime: 30 * 1000, // 30 seconds
    refetchOnWindowFocus: true,
  });
}

/**
 * Hook to assign a member to a volunteer position
 * @returns Mutation for assigning members with automatic cache invalidation
 */
export function useAssignMember() {
  const queryClient = useQueryClient();

  return useMutation<
    VolunteerAssignmentDto,
    Error,
    { positionId: string; userId: string }
  >({
    mutationFn: async ({ positionId, userId }) => {
      const response = await apiClient.post<ApiResponse<VolunteerAssignmentDto>>(
        `/api/volunteer-positions/${positionId}/signups`,
        { userId }
      );

      if (!response.data.success || !response.data.data) {
        throw new Error(response.data.error || 'Failed to assign member to position');
      }

      return response.data.data;
    },
    onSuccess: (data, variables) => {
      // Invalidate the signups list for this position
      queryClient.invalidateQueries({
        queryKey: ['volunteer-signups', variables.positionId],
      });

      notifications.show({
        title: 'Member Assigned',
        message: `Successfully assigned ${data.sceneName} to volunteer position`,
        color: 'green',
      });
    },
    onError: (error) => {
      notifications.show({
        title: 'Assignment Failed',
        message: error.message || 'Failed to assign member to position',
        color: 'red',
      });
    },
  });
}

/**
 * Hook to remove a volunteer assignment
 * @returns Mutation for removing assignments with automatic cache invalidation
 */
export function useRemoveAssignment() {
  const queryClient = useQueryClient();

  return useMutation<
    void,
    Error,
    { signupId: string; positionId: string }
  >({
    mutationFn: async ({ signupId }) => {
      const response = await apiClient.delete<ApiResponse<null>>(
        `/api/volunteer-signups/${signupId}`
      );

      if (!response.data.success) {
        throw new Error(response.data.error || 'Failed to remove volunteer assignment');
      }
    },
    onSuccess: (_, variables) => {
      // Invalidate the signups list for this position
      queryClient.invalidateQueries({
        queryKey: ['volunteer-signups', variables.positionId],
      });

      notifications.show({
        title: 'Assignment Removed',
        message: 'Successfully removed member assignment',
        color: 'blue',
      });
    },
    onError: (error) => {
      notifications.show({
        title: 'Removal Failed',
        message: error.message || 'Failed to remove volunteer assignment',
        color: 'red',
      });
    },
  });
}

/**
 * Hook to search for members with debouncing
 * @param searchQuery - Search query (minimum 3 characters)
 * @param debounceMs - Debounce delay in milliseconds (default: 300)
 * @returns Query result with list of matching members
 */
export function useSearchMembers(searchQuery: string, debounceMs: number = 300) {
  // Debounce implementation
  const debouncedQuery = useMemo(() => {
    const trimmed = searchQuery.trim();
    return trimmed.length >= 3 ? trimmed : '';
  }, [searchQuery]);

  return useQuery<UserSearchResultDto[]>({
    queryKey: ['users-search', debouncedQuery],
    queryFn: async () => {
      if (!debouncedQuery) {
        return [];
      }

      const response = await apiClient.get<ApiResponse<UserSearchResultDto[]>>(
        `/api/users/search`,
        {
          params: { q: debouncedQuery },
        }
      );

      if (!response.data.success || !response.data.data) {
        throw new Error(response.data.error || 'Failed to search members');
      }

      return response.data.data;
    },
    enabled: debouncedQuery.length >= 3,
    staleTime: 60 * 1000, // 1 minute
    refetchOnWindowFocus: false,
  });
}
