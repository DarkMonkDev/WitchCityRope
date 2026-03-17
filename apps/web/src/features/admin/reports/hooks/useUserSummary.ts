import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../../../lib/api/client';

/**
 * User summary data returned by the admin reports API.
 * Provides aggregate counts for the Reports Dashboard "Community Overview" section.
 */
export interface UserSummaryData {
  totalUsers: number;
  verifiedUsers: number;
  unverifiedUsers: number;
  vettedMembers: number;
  vettingApplicationsSubmitted: number;
}

/**
 * TanStack Query hook for fetching user summary statistics.
 * Used on the Reports Dashboard home page to show community overview cards.
 */
export const useUserSummary = () => {
  return useQuery<UserSummaryData>({
    queryKey: ['admin-reports', 'user-summary'],
    queryFn: async () => {
      const response = await apiClient.get('/api/admin/reports/user-summary');
      return response.data;
    },
    staleTime: 5 * 60 * 1000, // 5 minutes — summary stats don't change frequently
    refetchOnWindowFocus: false,
  });
};
