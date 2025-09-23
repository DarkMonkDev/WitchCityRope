import { useQuery } from '@tanstack/react-query';
import { vettingAdminApi } from '../services/vettingAdminApi';
import { vettingKeys } from './useVettingApplications';
import type { ApplicationDetailResponse } from '../types/vetting.types';

export function useVettingApplicationDetail(applicationId: string) {
  return useQuery<ApplicationDetailResponse>({
    queryKey: vettingKeys.applicationDetail(applicationId),
    queryFn: () => vettingAdminApi.getApplicationDetail(applicationId),
    enabled: !!applicationId,
    staleTime: 2 * 60 * 1000, // 2 minutes - reduce aggressive refetching
    refetchOnWindowFocus: false,
    refetchOnMount: false, // Only refetch if data is stale
  });
}