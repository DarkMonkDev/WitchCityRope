import { useQuery } from '@tanstack/react-query';
import { vettingAdminApi } from '../services/vettingAdminApi';
import { vettingKeys } from './useVettingApplications';
import type { ApplicationDetailResponse } from '../types/vetting.types';

export function useVettingApplicationDetail(applicationId: string) {
  return useQuery<ApplicationDetailResponse>({
    queryKey: vettingKeys.applicationDetail(applicationId),
    queryFn: () => vettingAdminApi.getApplicationDetail(applicationId),
    enabled: !!applicationId,
    staleTime: 30 * 1000, // 30 seconds - fresher data for detail view
    refetchOnWindowFocus: false,
  });
}