import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { apiClient } from '../../../../lib/api/client';

interface RefundTicketRequest {
  ticketId: string;
  refundReason: string;
  alsoRemoveRsvp: boolean;
}

interface RefundTicketResponse {
  message: string;
  refundId: string;
  amount: number;
  refundDate: string;
}

export const useRefundTicket = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: RefundTicketRequest): Promise<RefundTicketResponse> => {
      const response = await apiClient.post(`/api/admin/refunds/${request.ticketId}`, {
        refundReason: request.refundReason,
        alsoRemoveRsvp: request.alsoRemoveRsvp
      });
      return response.data;
    },
    onSuccess: (data) => {
      // Invalidate payments query to refresh the list
      queryClient.invalidateQueries({ queryKey: ['admin-payments'] });

      notifications.show({
        title: 'Refund Processed',
        message: `Refund of $${data.amount.toFixed(2)} has been processed successfully. The user will receive an email confirmation.`,
        color: 'green',
        autoClose: 5000
      });
    },
    onError: (error: Error) => {
      notifications.show({
        title: 'Refund Failed',
        message: error.message,
        color: 'red',
        autoClose: 7000
      });
    }
  });
};
