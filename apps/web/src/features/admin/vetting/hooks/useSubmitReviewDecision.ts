import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { vettingAdminApi } from '../services/vettingAdminApi';
import { vettingKeys } from './useVettingApplications';
import type { ReviewDecisionRequest } from '../types/vetting.types';

interface SubmitDecisionParams {
  applicationId: string;
  decision: ReviewDecisionRequest;
}

export function useSubmitReviewDecision(onSuccess?: () => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ applicationId, decision }: SubmitDecisionParams) =>
      vettingAdminApi.submitReviewDecision(applicationId, decision),
    onSuccess: (data, variables) => {
      // Invalidate and refetch related queries
      queryClient.invalidateQueries({
        queryKey: vettingKeys.applications(),
      });
      queryClient.invalidateQueries({
        queryKey: vettingKeys.applicationDetail(variables.applicationId),
      });

      // Show success notification
      notifications.show({
        title: 'Decision Submitted',
        message: `Application status updated to: ${data.updatedStatus}`,
        color: 'green',
      });

      // Call the optional success callback
      onSuccess?.();
    },
    onError: (error: any) => {
      // Show error notification
      notifications.show({
        title: 'Error Submitting Decision',
        message: error?.detail || error?.message || 'Failed to submit review decision',
        color: 'red',
      });
    },
  });
}