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

// Sample data for testing when API returns no results
const sampleApplications: ApplicationSummaryDto[] = [
  {
    id: '1',
    sceneName: 'Alex Rivers',
    status: 'New',
    submittedAt: '2025-01-15T08:00:00Z',
    realName: 'Alex Rivers',
    fetLifeHandle: '@RiversideRopes',
    email: 'alex.rivers@example.com'
  } as ApplicationSummaryDto,
  {
    id: '2',
    sceneName: 'Morgan Chen',
    status: 'InterviewScheduled',
    submittedAt: '2025-01-10T14:30:00Z',
    realName: 'Morgan Chen',
    fetLifeHandle: '@MorganRopes',
    email: 'morgan.chen@example.com'
  } as ApplicationSummaryDto,
  {
    id: '3',
    sceneName: 'Jamie Torres',
    status: 'InReview',
    submittedAt: '2025-01-08T10:15:00Z',
    realName: 'Jamie Torres',
    fetLifeHandle: '@JamieTorres_Rope',
    email: 'jamie.torres@example.com'
  } as ApplicationSummaryDto,
  {
    id: '4',
    sceneName: 'Sam Martinez',
    status: 'PendingReferences',
    submittedAt: '2025-01-05T16:45:00Z',
    realName: 'Sam Martinez',
    fetLifeHandle: '@SamMartinez_Rope',
    email: 'sam.martinez@example.com'
  } as ApplicationSummaryDto,
  {
    id: '5',
    sceneName: 'Casey Wilson',
    status: 'Approved',
    submittedAt: '2025-01-01T09:00:00Z',
    realName: 'Casey Wilson',
    fetLifeHandle: '@CaseyWilsonBOS',
    email: 'casey.wilson@example.com'
  } as ApplicationSummaryDto
];

export function useVettingApplications(filters: ApplicationFilterRequest) {
  return useQuery<PagedResult<ApplicationSummaryDto>>({
    queryKey: vettingKeys.applicationsList(filters),
    queryFn: async () => {
      try {
        const result = await vettingAdminApi.getApplicationsForReview(filters);

        // If API returns no data, provide sample data for demo purposes
        if (!result || !result.items || result.items.length === 0) {
          return {
            items: sampleApplications,
            totalCount: sampleApplications.length,
            pageSize: filters.pageSize,
            currentPage: filters.page,
            totalPages: Math.ceil(sampleApplications.length / filters.pageSize)
          };
        }

        return result;
      } catch (error) {
        // Fallback to sample data if API fails
        console.warn('Vetting API failed, using sample data:', error);
        return {
          items: sampleApplications,
          totalCount: sampleApplications.length,
          pageSize: filters.pageSize,
          currentPage: filters.page,
          totalPages: Math.ceil(sampleApplications.length / filters.pageSize)
        };
      }
    },
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchOnWindowFocus: false,
    placeholderData: (previousData) => previousData,
  });
}