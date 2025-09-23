import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { vettingAdminApi } from '../services/vettingAdminApi';
import { vettingKeys } from './useVettingApplications';

interface ApproveApplicationParams {
  applicationId: string;
  reasoning: string;
}

export function useApproveApplication(onSuccess?: () => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ applicationId, reasoning }: ApproveApplicationParams) =>
      vettingAdminApi.approveApplication(applicationId, reasoning),
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
        title: 'Application Approved',
        message: `Application status updated to: ${data.updatedStatus}`,
        color: 'green',
      });

      // Call the optional success callback
      onSuccess?.();
    },
    onError: (error: any) => {
      // Show error notification
      notifications.show({
        title: 'Error Approving Application',
        message: error?.detail || error?.message || 'Failed to approve application',
        color: 'red',
      });
    },
  });
}