import { useState, useMemo } from 'react';
import { useDebounce } from '../../../../hooks/useDebounce';

export interface PaymentFiltersState {
  searchTerm: string;
  startDate: Date | null;
  endDate: Date | null;
  paymentMethods: string[];
  statuses: string[];
  sortBy: string;
  sortDirection: 'Asc' | 'Desc';
  page: number;
  pageSize: number;
}

const initialFilterState: PaymentFiltersState = {
  searchTerm: '',
  startDate: null,
  endDate: null,
  paymentMethods: [],
  // Default to meaningful payment statuses, excluding Failed (declined cards)
  // and Pending (incomplete transactions) to reduce clutter.
  // AwaitingManualRefund is included by default so admins immediately see any
  // cancellations awaiting manual refund processing (M2b — 2026-04-12, BE-12).
  statuses: ['Completed', 'Refunded', 'PartiallyRefunded', 'AwaitingManualRefund'],
  sortBy: 'paymentDate',
  sortDirection: 'Desc',
  page: 1,
  pageSize: 50
};

export const usePaymentFilters = () => {
  const [filterState, setFilterState] = useState<PaymentFiltersState>(initialFilterState);

  // Debounce search term for performance (500ms as specified in wireframe)
  const debouncedSearchTerm = useDebounce(filterState.searchTerm, 500);

  const updateFilter = (updates: Partial<PaymentFiltersState>) => {
    // If only updating page, don't reset to page 1
    // Otherwise reset page to 1 when filters change
    if ('page' in updates && Object.keys(updates).length === 1) {
      setFilterState(prev => ({ ...prev, ...updates }));
    } else {
      setFilterState(prev => ({ ...prev, ...updates, page: 1 }));
    }
  };

  const clearFilters = () => {
    setFilterState(initialFilterState);
  };

  const handleSort = (column: string) => {
    setFilterState(prev => ({
      ...prev,
      sortBy: column,
      sortDirection: prev.sortBy === column && prev.sortDirection === 'Asc' ? 'Desc' : 'Asc'
    }));
  };

  // Get active filter labels for display
  // Note: Date range display uses user's local timezone for UI display
  const activeFilters = useMemo(() => {
    const filters: string[] = [];

    if (filterState.paymentMethods.length > 0) {
      filters.push(...filterState.paymentMethods);
    }
    if (filterState.statuses.length > 0) {
      filters.push(...filterState.statuses);
    }
    if (filterState.startDate && filterState.endDate) {
      // Display uses local timezone for filter labels (this is for UI display only)
      filters.push(`Date Range: ${filterState.startDate.toLocaleDateString()} - ${filterState.endDate.toLocaleDateString()}`);
    }

    return filters;
  }, [filterState]);

  // Build API query filters from state
  const apiFilters = useMemo(() => {
    return {
      searchTerm: debouncedSearchTerm || undefined,
      startDate: filterState.startDate?.toISOString().split('T')[0],
      endDate: filterState.endDate?.toISOString().split('T')[0],
      paymentMethods: filterState.paymentMethods.length > 0 ? filterState.paymentMethods : undefined,
      statuses: filterState.statuses.length > 0 ? filterState.statuses : undefined,
      sortBy: filterState.sortBy,
      sortDirection: filterState.sortDirection,
      page: filterState.page,
      pageSize: filterState.pageSize
    };
  }, [
    debouncedSearchTerm,
    filterState.startDate,
    filterState.endDate,
    filterState.paymentMethods,
    filterState.statuses,
    filterState.sortBy,
    filterState.sortDirection,
    filterState.page,
    filterState.pageSize
  ]);

  return {
    filterState: {
      ...filterState,
      searchTerm: debouncedSearchTerm // Return debounced version for display consistency
    },
    rawSearchTerm: filterState.searchTerm, // Raw search term for input control
    updateFilter,
    clearFilters,
    handleSort,
    activeFilters,
    apiFilters
  };
};
