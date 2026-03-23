import { useMutation, useQueryClient } from '@tanstack/react-query';
import { vettingAdminApi } from '../services/vettingAdminApi';
import { vettingKeys } from './useVettingApplications';

interface UpdateApplicantInfoParams {
  applicationId: string;
  data: {
    sceneName: string;
    firstName?: string | null;
    lastName?: string | null;
    email: string;
    pronouns?: string | null;
    fetLifeHandle?: string | null;
    otherNames?: string | null;
  };
}

/**
 * Mutation hook for updating applicant info on a vetting application.
 * Invalidates the application detail and applications list cache on success.
 */
export function useUpdateApplicantInfo() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ applicationId, data }: UpdateApplicantInfoParams) =>
      vettingAdminApi.updateApplicantInfo(applicationId, data),
    onSuccess: (_data: any, variables: UpdateApplicantInfoParams) => {
      // Invalidate application detail to refetch updated data
      queryClient.invalidateQueries({
        queryKey: vettingKeys.applicationDetail(variables.applicationId),
      });
      // Invalidate applications list so the list shows updated info
      queryClient.invalidateQueries({
        queryKey: vettingKeys.applications(),
      });
    },
    onError: (error: any) => {
      console.error('Failed to update applicant info:', error);
    },
  });
}
