import { useQuery } from '@tanstack/react-query';
import { vettingAdminApi } from '../services/vettingAdminApi';
import { vettingKeys } from './useVettingApplications';

export interface VettingStats {
  underReviewCount: number;
  needsReviewCount: number;
}

/**
 * Hook to fetch vetting application statistics for the admin dashboard
 * Returns count of applications that need review (UnderReview + FinalReview statuses)
 */
export function useVettingStats() {
  return useQuery<VettingStats>({
    queryKey: [...vettingKeys.all, 'stats'],
    queryFn: async () => {
      try {
        // Fetch applications in UnderReview or FinalReview status
        const result = await vettingAdminApi.getApplicationsForReview({
          page: 1,
          pageSize: 1, // We only need the count
          statusFilters: ['UnderReview', 'FinalReview'],
          priorityFilters: [],
          experienceLevelFilters: [],
          skillsFilters: [],
          sortBy: 'SubmittedAt',
          sortDirection: 'desc'
        });

        return {
          underReviewCount: result.totalCount || 0, // Legacy field for backward compatibility
          needsReviewCount: result.totalCount || 0 // Combined count of UnderReview + FinalReview
        };
      } catch (error: any) {
        console.error('useVettingStats: Failed to fetch stats:', error.message || error);

        // Return zero count on error to prevent breaking the dashboard
        // The dashboard card will still be functional, just with 0 count
        return {
          underReviewCount: 0,
          needsReviewCount: 0
        };
      }
    },
    staleTime: 2 * 60 * 1000, // 2 minutes - refresh more frequently than full list
    refetchOnWindowFocus: true, // Refresh when user returns to dashboard
    refetchOnMount: true,
    // Don't throw errors - return 0 count instead
    throwOnError: false,
    // Provide placeholder data while loading
    placeholderData: { underReviewCount: 0, needsReviewCount: 0 }
  });
}
