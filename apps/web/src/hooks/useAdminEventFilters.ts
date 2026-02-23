import { useState, useMemo } from 'react';
import { useDebounce } from './useDebounce';
import type { EventListItemDto } from '@witchcityrope/shared-types';

export interface AdminEventFiltersState {
  searchTerm: string;
  showPastEvents: boolean;
  sortColumn: 'date' | 'title' | null;
  sortDirection: 'asc' | 'desc';
}

const initialFilterState: AdminEventFiltersState = {
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

  // Helper function to get effective display date from sessions
  // Uses same logic as EventsTableView.tsx getDisplaySession()
  const getEventDisplayDate = (event: EventListItemDto): Date => {
    // If no sessions, fall back to event.startDate
    if (!event.sessions || event.sessions.length === 0) {
      return new Date(event.startDate || '');
    }

    const now = new Date();

    // Find next upcoming session (session with startTime >= now)
    const upcomingSessions = event.sessions
      .filter(session => session.startTime && new Date(session.startTime) >= now)
      .sort((a, b) => {
        const aTime = new Date(a.startTime || 0).getTime();
        const bTime = new Date(b.startTime || 0).getTime();
        return aTime - bTime;
      });

    // Return next upcoming session's start time, or fall back to first session's start time
    const displaySession = upcomingSessions.length > 0 ? upcomingSessions[0] : event.sessions[0];
    return new Date(displaySession.startTime || event.startDate || '');
  };

  // Helper function to check if event is past
  const isPastEvent = (event: EventListItemDto): boolean => {
    // Check if ALL sessions have ended
    if (event.sessions && event.sessions.length > 0) {
      const now = new Date();
      const allSessionsEnded = event.sessions.every(session => {
        const sessionEnd = session.endTime ? new Date(session.endTime) : null;
        return sessionEnd && sessionEnd < now;
      });
      return allSessionsEnded;
    }

    // If no sessions, fall back to event.endDate
    const eventEndDate = new Date(event.endDate || event.startDate || '');
    return eventEndDate < new Date();
  };

  // Process and filter events
  const processEvents = useMemo(() => {
    return (events: EventListItemDto[]): EventListItemDto[] => {
      if (!events) return [];

      return events
        .filter(event => {
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
            // Default sort by date ascending (using session-based dates)
            const dateA = getEventDisplayDate(a).getTime();
            const dateB = getEventDisplayDate(b).getTime();
            return dateA - dateB;
          }

          let aValue: string | Date;
          let bValue: string | Date;

          if (filterState.sortColumn === 'date') {
            // Use session-based dates for sorting
            aValue = getEventDisplayDate(a);
            bValue = getEventDisplayDate(b);
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
  }, [debouncedSearchTerm, filterState.showPastEvents, filterState.sortColumn, filterState.sortDirection]);

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