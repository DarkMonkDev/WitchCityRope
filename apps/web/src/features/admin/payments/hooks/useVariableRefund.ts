import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { apiClient } from '../../../../lib/api/client';

interface VariableRefundRequest {
  transactionId: string;
  refundAmount: number;
  refundReason: string;
  /** When true, cancels the ticket (revokes event access) */
  cancelTicket?: boolean;
  /** When true, also removes the RSVP if event uses RSVPs */
  alsoRemoveRsvp?: boolean;
}

interface VariableRefundResponse {
  refundId: string;
  amount: number;
  currency: string;
  status: string;
  message: string;
  remainingRefundableAmount: number;
  paymentStatus: string;
  /** Whether the ticket was cancelled (access revoked) */
  ticketCancelled: boolean;
  /** Whether the RSVP was also removed */
  rsvpRemoved: boolean;
}

export const useVariableRefund = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      transactionId,
      refundAmount,
      refundReason,
      cancelTicket = false,
      alsoRemoveRsvp = false
    }: VariableRefundRequest): Promise<VariableRefundResponse> => {
      const response = await apiClient.post(`/api/payments/transactions/${transactionId}/refund`, {
        refundAmount,
        refundReason,
        cancelTicket,
        alsoRemoveRsvp
      });
      return response.data;
    },
    onSuccess: (data: VariableRefundResponse) => {
      queryClient.invalidateQueries({ queryKey: ['admin-payments'] });

      // Build a contextual success message based on what actually happened
      const parts: string[] = [];
      if (data.amount > 0) {
        parts.push(`Refund of $${data.amount.toFixed(2)} processed.`);
      }
      if (data.ticketCancelled) {
        parts.push('Ticket cancelled.');
      }
      if (data.rsvpRemoved) {
        parts.push('RSVP removed.');
      }
      if (!data.ticketCancelled && data.amount > 0) {
        parts.push(`Remaining refundable: $${data.remainingRefundableAmount.toFixed(2)}.`);
      }

      notifications.show({
        title: data.ticketCancelled ? 'Ticket Cancelled' : 'Refund Processed',
        message: parts.join(' ') || data.message,
        color: 'green',
        autoClose: 7000
      });
    },
    onError: (error: Error) => {
      notifications.show({
        title: 'Refund Failed',
        message: error.message,
        color: 'red',
        autoClose: 5000
      });
    }
  });
};
