import { useMemo, useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';
import type { EventFiltersState } from '../components/events/public/EventFilters';

export const useEventFilters = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  
  const filters = useMemo((): EventFiltersState => ({
    eventType: (searchParams.get('type') as EventFiltersState['eventType']) || 'all',
    instructor: searchParams.get('instructor') || null,
    dateRange: (searchParams.get('date') as EventFiltersState['dateRange']) || 'month'
  }), [searchParams]);
  
  const updateFilters = useCallback((newFilters: EventFiltersState) => {
    const params = new URLSearchParams();
    
    // Only set non-default values in URL
    if (newFilters.eventType !== 'all') {
      params.set('type', newFilters.eventType);
    }
    
    if (newFilters.instructor) {
      params.set('instructor', newFilters.instructor);
    }
    
    if (newFilters.dateRange !== 'month') {
      params.set('date', newFilters.dateRange);
    }
    
    setSearchParams(params);
  }, [setSearchParams]);
  
  const clearFilters = useCallback(() => {
    setSearchParams(new URLSearchParams());
  }, [setSearchParams]);
  
  return { 
    filters, 
    updateFilters, 
    clearFilters 
  };
};