import { useQuery } from '@tanstack/react-query';
import { vettingAdminApi } from '../services/vettingAdminApi';
import { vettingKeys } from './useVettingApplications';
import { STATUSES_REQUIRING_REVIEW } from '../../../vetting/constants/vettingStatusConfig';

export interface VettingStats {
  underReviewCount: number;
  needsReviewCount: number;
}

/**
 * Hook to fetch vetting application statistics for the admin dashboard.
 * Returns count of applications in statuses that require reviewer action
 * (UnderReview + FinalReview). The exact set of "needs review" statuses
 * lives in STATUSES_REQUIRING_REVIEW in the single-source vetting config
 * so the definition stays in lockstep with any future status additions.
 */
export function useVettingStats() {
  return useQuery<VettingStats>({
    queryKey: [...vettingKeys.all, 'stats'],
    queryFn: async () => {
      try {
        // Phase 2g migration: statusFilters previously had a hardcoded
        // ['UnderReview', 'FinalReview'] array that duplicated the
        // definition of "needs review" from the single-source config.
        // Now sourced from STATUSES_REQUIRING_REVIEW so both the admin
        // dashboard card and any future "needs review" consumer stay
        // aligned automatically.
        const result = await vettingAdminApi.getApplicationsForReview({
          page: 1,
          pageSize: 1, // We only need the count
          statusFilters: [...STATUSES_REQUIRING_REVIEW],
          priorityFilters: [],
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
