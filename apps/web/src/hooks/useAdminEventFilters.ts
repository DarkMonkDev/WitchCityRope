import { useState, useMemo } from 'react';
import { useDebounce } from './useDebounce';
import type { EventDto } from '@witchcityrope/shared-types';

export interface AdminEventFiltersState {
  activeTypes: string[];
  searchTerm: string;
  showPastEvents: boolean;
  sortColumn: 'date' | 'title' | null;
  sortDirection: 'asc' | 'desc';
}

const initialFilterState: AdminEventFiltersState = {
  activeTypes: ['social', 'class'], // Start with both types selected (lowercase to match API)
  searchTerm: '',
  showPastEvents: false,
  sortColumn: 'date', // Default sort by date
  sortDirection: 'asc'
};

export const useAdminEventFilters = () => {
  const [filterState, setFilterState] = useState<AdminEventFiltersState>(initialFilterState);
  
  // Debounce search term for performance (300ms as specified)
  const debouncedSearchTerm = useDebounce(filterState.searchTerm, 300);
  
  const updateFilter = (updates: Partial<AdminEventFiltersState>) => {
    setFilterState(prev => ({ ...prev, ...updates }));
  };

  const handleSort = (column: 'date' | 'title') => {
    setFilterState(prev => ({
      ...prev,
      sortColumn: column,
      sortDirection: prev.sortColumn === column && prev.sortDirection === 'asc' ? 'desc' : 'asc'
    }));
  };

  // Helper function to check if event is past
  const isPastEvent = (event: EventDto): boolean => {
    const eventEndDate = new Date(event.endDateTime || event.startDateTime || '');
    return eventEndDate < new Date();
  };

  // Process and filter events
  const processEvents = useMemo(() => {
    return (events: EventDto[]): EventDto[] => {
      if (!events) return [];

      return events
        .filter(event => {
          // Type filtering - only filter by type if:
          // 1. We have selected types AND
          // 2. The event actually has a type set
          // If no filters selected (length === 0), don't show any events
          // If filters selected but event has no type, show the event
          if (filterState.activeTypes.length === 0) {
            return false; // No filters selected = no events shown
          }
          // If event has a type, check if it matches selected filters
          if (event.eventType) {
            const eventType = event.eventType.toLowerCase();
            if (!filterState.activeTypes.includes(eventType)) {
              return false;
            }
          }
          // If event has no type, show it when any filter is selected

          // Search filtering (case-insensitive, searches title and description)
          if (debouncedSearchTerm) {
            const searchLower = debouncedSearchTerm.toLowerCase();
            const titleMatch = event.title?.toLowerCase().includes(searchLower);
            const descriptionMatch = event.description?.toLowerCase().includes(searchLower);
            
            if (!titleMatch && !descriptionMatch) {
              return false;
            }
          }

          // Past events filtering
          if (!filterState.showPastEvents && isPastEvent(event)) {
            return false;
          }

          return true;
        })
        .sort((a, b) => {
          if (!filterState.sortColumn) {
            // Default sort by date ascending
            const dateA = new Date(a.startDateTime || '').getTime();
            const dateB = new Date(b.startDateTime || '').getTime();
            return dateA - dateB;
          }

          let aValue: string | Date;
          let bValue: string | Date;

          if (filterState.sortColumn === 'date') {
            aValue = new Date(a.startDateTime || '');
            bValue = new Date(b.startDateTime || '');
          } else {
            aValue = a.title || '';
            bValue = b.title || '';
          }

          let comparison = 0;
          if (aValue < bValue) comparison = -1;
          if (aValue > bValue) comparison = 1;

          return filterState.sortDirection === 'asc' ? comparison : -comparison;
        });
    };
  }, [filterState.activeTypes, debouncedSearchTerm, filterState.showPastEvents, filterState.sortColumn, filterState.sortDirection]);

  return {
    filterState: {
      ...filterState,
      searchTerm: debouncedSearchTerm // Return debounced version for display consistency
    },
    rawSearchTerm: filterState.searchTerm, // Raw search term for input control
    updateFilter,
    handleSort,
    processEvents,
    isPastEvent
  };
};