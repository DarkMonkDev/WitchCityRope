import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';

interface VariableRefundRequest {
  transactionId: string;
  refundAmount: number;
  refundReason: string;
}

interface VariableRefundResponse {
  refundId: string;
  amount: number;
  currency: string;
  status: string;
  message: string;
}

export const useVariableRefund = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ transactionId, refundAmount, refundReason }: VariableRefundRequest): Promise<VariableRefundResponse> => {
      const response = await fetch(`/api/payments/transactions/${transactionId}/refund`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ refundAmount, refundReason })
      });

      if (!response.ok) {
        const error = await response.json().catch(() => ({}));
        throw new Error(error.detail || error.message || 'Refund failed');
      }

      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-payments'] });
      notifications.show({
        title: 'Refund Processed',
        message: 'Financial refund processed successfully. RSVP/ticket was NOT cancelled.',
        color: 'green',
        autoClose: 5000
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
