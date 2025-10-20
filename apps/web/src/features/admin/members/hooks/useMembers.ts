/**
 * Admin Members Management Hook
 *
 * Fetches member list from /api/admin/users endpoint
 * Pattern: Copy from useVettingApplications pattern
 */

import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../../../lib/api/client';
import type { UserListResponse, MemberFilterRequest } from '../types/members.types';

/**
 * Query keys for members data
 * Following TanStack Query best practices
 */
export const membersKeys = {
  all: ['admin', 'members'] as const,
  lists: () => [...membersKeys.all, 'list'] as const,
  list: (filters: MemberFilterRequest) => [...membersKeys.lists(), filters] as const,
};

/**
 * Fetch members list from API with filters
 */
async function fetchMembers(filters: MemberFilterRequest): Promise<UserListResponse> {
  console.log('useMembers: Fetching members with filters:', filters);

  try {
    const params: any = {
      Page: filters.page,
      PageSize: filters.pageSize,
      SortBy: filters.sortBy,
      SortDescending: filters.sortDirection === 'Desc',
    };

    // Add search query if present
    if (filters.searchQuery && filters.searchQuery.trim()) {
      params.SearchTerm = filters.searchQuery.trim();
    }

    // Add role filters if present (sent as array for OR filtering)
    if (filters.roleFilters && filters.roleFilters.length > 0) {
      params.RoleFilters = filters.roleFilters;
    }

    const response = await apiClient.get<UserListResponse>('/api/admin/users', { params });

    console.log('useMembers: API response received:', {
      hasResponse: !!response,
      hasData: !!response?.data,
      userCount: response?.data?.users?.length || 0,
      totalCount: response?.data?.totalCount || 0,
    });

    return response.data;
  } catch (error: any) {
    console.error('useMembers: API call failed:', {
      error: error.message || error,
      status: error.response?.status,
      filters,
    });

    // Enhance error message based on HTTP status
    if (error.response?.status === 401) {
      throw new Error('Authentication required. Please log in to view members.');
    } else if (error.response?.status === 403) {
      throw new Error('Access denied. You do not have permission to view members.');
    }

    // Re-throw the error to let React Query handle it properly
    throw error;
  }
}

/**
 * Hook to fetch and manage members list
 *
 * @param filters - Member filter criteria
 * @returns TanStack Query result with members data
 */
export function useMembers(filters: MemberFilterRequest) {
  return useQuery<UserListResponse>({
    queryKey: membersKeys.list(filters),
    queryFn: () => fetchMembers(filters),
    // Ensure errors are thrown instead of silently returning fallback data
    throwOnError: true,
  });
}
