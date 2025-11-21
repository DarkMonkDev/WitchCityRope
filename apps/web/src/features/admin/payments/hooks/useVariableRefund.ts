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
  remainingRefundableAmount: number;
  paymentStatus: string;
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
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['admin-payments'] });
      notifications.show({
        title: 'Refund Processed',
        message: `Refund of $${data.amount.toFixed(2)} processed successfully. Remaining refundable: $${data.remainingRefundableAmount.toFixed(2)}. Payment status: ${data.paymentStatus}. RSVP/ticket was NOT cancelled.`,
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
