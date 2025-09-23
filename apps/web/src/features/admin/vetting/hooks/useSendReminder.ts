import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { vettingAdminApi } from '../services/vettingAdminApi';
import { vettingKeys } from './useVettingApplications';

interface SendReminderParams {
  applicationId: string;
  message: string;
}

export function useSendReminder(onSuccess?: () => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ applicationId, message }: SendReminderParams) =>
      vettingAdminApi.sendApplicationReminder(applicationId, message),
    onSuccess: (data, variables) => {
      // Invalidate and refetch related queries to update last activity
      queryClient.invalidateQueries({
        queryKey: vettingKeys.applications(),
      });
      queryClient.invalidateQueries({
        queryKey: vettingKeys.applicationDetail(variables.applicationId),
      });

      // Show success notification
      notifications.show({
        title: 'Reminder Sent',
        message: data.message,
        color: 'green',
      });

      // Call the optional success callback
      onSuccess?.();
    },
    onError: (error: any) => {
      // Show error notification
      notifications.show({
        title: 'Error Sending Reminder',
        message: error?.detail || error?.message || 'Failed to send reminder',
        color: 'red',
      });
    },
  });
}