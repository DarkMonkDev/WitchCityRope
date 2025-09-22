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
    queryFn: () => vettingAdminApi.getApplicationsForReview(filters),
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchOnWindowFocus: false,
    placeholderData: (previousData) => previousData,
  });
}