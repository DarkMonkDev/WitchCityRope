import { useQuery } from '@tanstack/react-query';
import { vettingAdminApi } from '../services/vettingAdminApi';
import type { ApplicationFilterRequest, ApplicationSummaryDto, PagedResult } from '../types/vetting.types';

export const vettingKeys = {
  all: ['vetting'] as const,
  applications: () => [...vettingKeys.all, 'applications'] as const,
  applicationsList: (filters: ApplicationFilterRequest) =>
    [...vettingKeys.applications(), 'list', filters] as const,
  applicationDetail: (id: string) =>
    [...vettingKeys.applications(), 'detail', id] as const,
};

export function useVettingApplications(filters: ApplicationFilterRequest) {
  return useQuery<PagedResult<ApplicationSummaryDto>>({
    queryKey: vettingKeys.applicationsList(filters),
    queryFn: async () => {
      console.log('useVettingApplications: Fetching applications with filters:', filters);

      try {
        const result = await vettingAdminApi.getApplicationsForReview(filters);

        console.log('useVettingApplications: API response received:', {
          hasResult: !!result,
          hasItems: !!result?.items,
          itemCount: result?.items?.length || 0,
          totalCount: result?.totalCount || 0
        });

        // Return actual API result or proper empty result
        if (!result) {
          console.warn('useVettingApplications: API returned null/undefined result');
          return {
            items: [],
            totalCount: 0,
            pageSize: filters.pageSize,
            pageNumber: filters.page,
            totalPages: 0
          };
        }

        return result;
      } catch (error: any) {
        // Log the error details for debugging
        console.error('useVettingApplications: API call failed:', {
          error: error.message || error,
          status: error.response?.status,
          statusText: error.response?.statusText,
          filters
        });

        // Enhance error message based on HTTP status
        if (error.response?.status === 401) {
          throw new Error('Authentication required. Please log in to view vetting applications.');
        } else if (error.response?.status === 403) {
          throw new Error('Access denied. You do not have permission to view vetting applications.');
        } else if (error.response?.status === 404) {
          throw new Error('Vetting applications endpoint not found. Please contact support.');
        } else if (error.response?.status >= 500) {
          throw new Error('Server error occurred while loading vetting applications. Please try again later.');
        } else if (error.message?.includes('Network')) {
          throw new Error('Network error. Please check your internet connection and try again.');
        }

        // Re-throw the error to let React Query handle it properly
        throw error;
      }
    },
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchOnWindowFocus: false,
    placeholderData: (previousData) => previousData,
    // Ensure errors are thrown instead of silently returning fallback data
    throwOnError: true,
  });
}