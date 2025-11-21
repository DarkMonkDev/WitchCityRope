import { useQuery } from '@tanstack/react-query';

// Temporary type definition until backend API endpoint is created
// TODO: Replace with auto-generated type from @witchcityrope/shared-types once API is implemented
export interface PaymentTransactionDto {
  id?: string;
  ticketId?: string;
  paymentDate?: string;
  userName?: string;
  userEmail?: string;
  eventName?: string;
  sessionName?: string;
  paymentMethod?: string;
  amount?: number;
  currency?: string;
  status?: string;
  isRefundable?: boolean; // Backend will use this field name
  refundId?: string;
  refundDate?: string;
  remainingRefundableAmount?: number; // Amount still available to refund (for variable refunds)
}

export interface PaymentListResponse {
  transactions?: PaymentTransactionDto[];
  totalCount?: number;
  currentPage?: number;
  pageSize?: number;
  totalPages?: number;
}

export interface PaymentFilters {
  searchTerm?: string;
  startDate?: string;
  endDate?: string;
  paymentMethods?: string[];
  statuses?: string[];
  minAmount?: number;
  maxAmount?: number;
  sortBy?: string;
  sortDirection?: 'Asc' | 'Desc';
  page?: number;
  pageSize?: number;
}

export const usePayments = (filters: PaymentFilters) => {
  return useQuery<PaymentListResponse>({
    queryKey: ['admin-payments', filters],
    queryFn: async () => {
      const params = new URLSearchParams();

      if (filters.searchTerm) params.append('searchTerm', filters.searchTerm);
      if (filters.startDate) params.append('startDate', filters.startDate);
      if (filters.endDate) params.append('endDate', filters.endDate);
      if (filters.paymentMethods) {
        filters.paymentMethods.forEach(m => params.append('paymentMethods', m));
      }
      if (filters.statuses) {
        filters.statuses.forEach(s => params.append('statuses', s));
      }
      if (filters.minAmount !== undefined) {
        params.append('minAmount', filters.minAmount.toString());
      }
      if (filters.maxAmount !== undefined) {
        params.append('maxAmount', filters.maxAmount.toString());
      }
      if (filters.sortBy) params.append('sortBy', filters.sortBy);
      if (filters.sortDirection) params.append('sortDirection', filters.sortDirection);
      params.append('page', (filters.page || 1).toString());
      params.append('pageSize', (filters.pageSize || 50).toString());

      const response = await fetch(`/api/admin/payments?${params}`, {
        credentials: 'include'
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.detail || errorData.message || 'Failed to fetch payments');
      }

      return response.json();
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: false
  });
};
