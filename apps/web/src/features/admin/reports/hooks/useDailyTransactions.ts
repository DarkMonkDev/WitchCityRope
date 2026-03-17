import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../../../lib/api/client';

/**
 * A single day's transaction aggregation from the backend.
 * The backend returns `date` as a DateOnly string ("2026-03-15").
 */
export interface DailyTransactionDay {
  date: string;
  completedCount: number;
  completedAmount: number;
  failedCount: number;
  incompleteCount: number;
}

export interface DailyTransactionsData {
  days: DailyTransactionDay[];
}

/**
 * TanStack Query hook for fetching daily transaction aggregation.
 * Used on the Transactions Report page to populate the bar chart.
 *
 * @param dateFrom - ISO date string ("2026-02-15") for the start of the range
 * @param dateTo   - ISO date string ("2026-03-17") for the end of the range
 */
export const useDailyTransactions = (dateFrom: string, dateTo: string) => {
  return useQuery<DailyTransactionsData>({
    queryKey: ['admin-reports', 'daily-transactions', dateFrom, dateTo],
    queryFn: async () => {
      const response = await apiClient.get(
        `/api/admin/reports/daily-transactions?dateFrom=${dateFrom}&dateTo=${dateTo}`
      );
      return response.data;
    },
    staleTime: 5 * 60 * 1000, // 5 minutes — transaction data doesn't change rapidly
    refetchOnWindowFocus: false,
    // Only fetch when both dates are provided
    enabled: !!dateFrom && !!dateTo,
  });
};
