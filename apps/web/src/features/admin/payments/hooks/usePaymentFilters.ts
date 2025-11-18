import { useState, useMemo } from 'react';
import { useDebounce } from '../../../../hooks/useDebounce';

export interface PaymentFiltersState {
  searchTerm: string;
  startDate: Date | null;
  endDate: Date | null;
  paymentMethods: string[];
  statuses: string[];
  minAmount: number | undefined;
  maxAmount: number | undefined;
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
  statuses: [],
  minAmount: undefined,
  maxAmount: undefined,
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
    setFilterState(prev => ({ ...prev, ...updates, page: 1 })); // Reset to page 1 on filter change
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
  const activeFilters = useMemo(() => {
    const filters: string[] = [];

    if (filterState.paymentMethods.length > 0) {
      filters.push(...filterState.paymentMethods);
    }
    if (filterState.statuses.length > 0) {
      filters.push(...filterState.statuses);
    }
    if (filterState.startDate && filterState.endDate) {
      filters.push(`Date Range: ${filterState.startDate.toLocaleDateString()} - ${filterState.endDate.toLocaleDateString()}`);
    }
    if (filterState.minAmount !== undefined || filterState.maxAmount !== undefined) {
      const min = filterState.minAmount !== undefined ? `$${filterState.minAmount}` : '$0';
      const max = filterState.maxAmount !== undefined ? `$${filterState.maxAmount}` : '∞';
      filters.push(`Amount: ${min} - ${max}`);
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
      minAmount: filterState.minAmount,
      maxAmount: filterState.maxAmount,
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
    filterState.minAmount,
    filterState.maxAmount,
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
