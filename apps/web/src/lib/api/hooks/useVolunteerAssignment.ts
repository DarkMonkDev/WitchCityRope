/**
 * React hooks for volunteer assignment API operations
 * Provides hooks for managing volunteer position assignments and member search
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../client';
import { notifications } from '@mantine/notifications';
import { useMemo } from 'react';

// ============================================================================
// TypeScript Interfaces (Matching Backend DTOs)
// ============================================================================

/**
 * Volunteer assignment DTO (matches VolunteerAssignmentDto from backend)
 */
export interface VolunteerAssignmentDto {
  signupId: string;
  userId: string;
  volunteerPositionId: string;
  sceneName: string;
  email: string;
  fetLifeName?: string | null;
  discordName?: string | null;
  status: string;
  signedUpAt: string; // ISO 8601 date string
  hasCheckedIn: boolean;
  checkedInAt?: string | null; // ISO 8601 date string
}

/**
 * User search result DTO (matches UserSearchResultDto from backend)
 */
export interface UserSearchResultDto {
  userId: string;
  sceneName: string;
  email: string;
  discordName?: string | null;
  realName?: string | null;
}

/**
 * Request to assign a member to a volunteer position (matches AssignVolunteerRequest)
 */
export interface AssignVolunteerRequest {
  userId: string;
}

/**
 * API response wrapper (matches backend ApiResponse<T>)
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
