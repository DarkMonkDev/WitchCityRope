import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { vettingAdminApi } from '../services/vettingAdminApi';
import { vettingKeys } from './useVettingApplications';

interface SendReminderParams {
  applicationId: string;
  customMessage?: string;
}

/**
 * Mutation hook for sending interview reminder emails to applicants.
 * Calls the send-reminder API endpoint which uses the InterviewReminder email template.
 * Invalidates application queries on success to refresh reminder count display.
 */
export function useSendReminder(onSuccess?: () => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ applicationId, customMessage }: SendReminderParams) =>
      vettingAdminApi.sendApplicationReminder(applicationId, customMessage),
    onSuccess: (data: { remindersSentCount: number; lastReminderSentAt: string | null }, variables: SendReminderParams) => {
      // Invalidate and refetch related queries to update reminder count and last activity
      queryClient.invalidateQueries({
        queryKey: vettingKeys.applications(),
      });
      queryClient.invalidateQueries({
        queryKey: vettingKeys.applicationDetail(variables.applicationId),
      });

      notifications.show({
        title: 'Reminder Sent',
        message: `Interview reminder email sent successfully (${data.remindersSentCount} total)`,
        color: 'green',
      });

      onSuccess?.();
    },
    onError: (error: any) => {
      notifications.show({
        title: 'Error Sending Reminder',
        message: error?.response?.data?.detail || error?.detail || error?.message || 'Failed to send reminder email',
        color: 'red',
      });
    },
  });
}
