import React from 'react';
import { Box, Text, Title, Button, Alert, Loader } from '@mantine/core';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Event } from '../../types/Event';
import { EventDto } from '@witchcityrope/shared-types';
import { EventCard } from './EventCard';
import { apiClient } from '../../lib/api/client';
import { queryKeys } from '../../api/queryKeys';

interface EventsListProps {
  /** Section title */
  title?: string;
  /** Maximum number of events to display */
  maxEvents?: number;
  /** Show "View Full Calendar" button */
  showViewMore?: boolean;
  /** Custom loading state */
  isLoading?: boolean;
  /** Custom error state */
  error?: string | null;
  /** Custom events data */
  events?: EventDto[];
}

// Custom hook for fetching events - follows existing patterns from features/events/api/queries.ts
const useEventsForHomepage = (enabled: boolean = true) => {
  return useQuery({
    queryKey: queryKeys.events(),
    queryFn: async (): Promise<EventDto[]> => {
      try {
        const response = await apiClient.get('/api/events');
        // Access events from the wrapped ApiResponse format
        return response.data?.data || [];
      } catch (error: any) {
        // Gracefully handle 401s on public pages - return empty array instead of throwing
        // This prevents console errors when unauthenticated users visit the homepage
        if (error?.response?.status === 401) {
          return [];
        }
        // Re-throw other errors
        throw error;
      }
    },
    enabled, // Allow disabling the query when custom events are provided
    staleTime: 5 * 60 * 1000, // 5 minutes - same as existing pattern
    retry: (failureCount, error: any) => {
      // Don't retry 401s - follows existing auth patterns
      if (error?.response?.status === 401) {
        return false;
      }
      return failureCount < 3;
    }
  });
};

export const EventsList: React.FC<EventsListProps> = ({
  title = "Upcoming Classes & Events",
  maxEvents = 4,
  showViewMore = true,
  isLoading: customLoading,
  error: customError,
  events: customEvents
}) => {
  // Use TanStack Query for real API data (only if custom events not provided)
  const { 
    data: apiEvents, 
    isLoading: queryLoading, 
    error: queryError 
  } = useEventsForHomepage(!customEvents); // Disable query if custom events provided

  // Determine which data source to use - ensure type safety with explicit casting
  const events: EventDto[] = customEvents || (Array.isArray(apiEvents) ? apiEvents : []);
  const isLoading = customLoading ?? (customEvents ? false : queryLoading);
  const errorState = customError ?? (queryError ? 'Failed to load events' : null);

  if (isLoading) {
    return (
      <Box
        component="section"
        style={{
          padding: 'var(--space-2xl) 40px',
          maxWidth: '1200px',
          margin: '0 auto',
          background: 'var(--color-ivory)',
          borderRadius: '16px',
          marginTop: 'var(--space-2xl)',
          boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
          textAlign: 'center',
          minHeight: '200px',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Loader size="lg" style={{ marginBottom: 'var(--space-md)' }} />
        <Text style={{ color: 'var(--color-stone)' }}>Loading events...</Text>
      </Box>
    );
  }

  if (errorState) {
    return (
      <Box
        component="section"
        style={{
          padding: 'var(--space-2xl) 40px',
          maxWidth: '1200px',
          margin: '0 auto',
          background: 'var(--color-ivory)',
          borderRadius: '16px',
          marginTop: 'var(--space-2xl)',
          boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
        }}
      >
        <Alert color="red" style={{ marginBottom: 'var(--space-lg)' }}>
          <strong>Error:</strong> {errorState}
        </Alert>
      </Box>
    );
  }

  const displayEvents = events.slice(0, maxEvents);

  if (displayEvents.length === 0) {
    return (
      <Box
        component="section"
        style={{
          padding: 'var(--space-2xl) 40px',
          maxWidth: '1200px',
          margin: '0 auto',
          background: 'var(--color-ivory)',
          borderRadius: '16px',
          marginTop: 'var(--space-2xl)',
          boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
          textAlign: 'center',
        }}
      >
        <Title order={3} style={{ marginBottom: 'var(--space-sm)', color: 'var(--color-burgundy)' }}>
          No events available
        </Title>
        <Text style={{ color: 'var(--color-stone)' }}>
          Check back soon for new classes and events.
        </Text>
      </Box>
    );
  }

  return (
    <Box
      component="section"
      style={{
        padding: 'var(--space-2xl) 40px',
        maxWidth: '1200px',
        margin: '0 auto',
        background: 'var(--color-ivory)',
        borderRadius: '16px',
        marginTop: 'var(--space-2xl)',
        boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
      }}
    >
      <Title
        order={2}
        style={{
          fontFamily: 'var(--font-heading)',
          fontSize: '48px',
          fontWeight: 800,
          marginBottom: 'var(--space-xl)',
          textAlign: 'center',
          color: 'var(--color-burgundy)',
          position: 'relative',
          textTransform: 'uppercase',
          letterSpacing: '3px',
        }}
      >
        {title}
        <Box
          style={{
            content: '""',
            display: 'block',
            width: '100px',
            height: '2px',
            background: 'linear-gradient(90deg, transparent, var(--color-rose-gold), transparent)',
            margin: 'var(--space-sm) auto 0',
          }}
        />
      </Title>

      <Box
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(350px, 1fr))',
          gap: 'var(--space-lg)',
          marginBottom: 'var(--space-xl)'
        }}
      >
        {displayEvents.map((event) => {
          // Calculate price display from real ticket types
          const ticketTypes = (event as any).ticketTypes || [];
          let priceDisplay = '$0';

          if (ticketTypes.length > 0) {
            const prices = ticketTypes.map((t: any) => t.minPrice || t.maxPrice || 0);
            const minPrice = Math.min(...prices);
            const maxPrice = Math.max(...prices);

            if (minPrice === maxPrice) {
              priceDisplay = `$${minPrice.toFixed(2)}`;
            } else {
              priceDisplay = `$${minPrice.toFixed(2)}-${maxPrice.toFixed(2)}`;
            }
          }

          return (
            <EventCard
              key={event.id}
              event={event}
              price={priceDisplay}
              onClick={() => {
                // Handle event click - could navigate to event details
                console.log('Event clicked:', event.id);
              }}
            />
          );
        })}
      </Box>

      {showViewMore && (
        <Box style={{ textAlign: 'center', marginTop: 'var(--space-xl)' }}>
          <Box
            component={Link}
            to="/events"
            className="btn btn-secondary"
          >
            View Full Calendar
          </Box>
        </Box>
      )}
    </Box>
  );
};