import React, { useCallback } from 'react';
import { Box, Text, Title, Button, Alert, Loader, useMantineTheme } from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Event } from '../../types/Event';
import { EventDto } from '@witchcityrope/shared-types';
import { PublicEventCard } from '../events/PublicEventCard';
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
      const response = await apiClient.get('/api/events');
      // Handle both wrapped ApiResponse format and direct array response
      // Backend returns direct array: [EventDto, EventDto, ...]
      const events = Array.isArray(response.data) ? response.data : (response.data?.data || []);
      return events;
    },
    enabled, // Allow disabling the query when custom events are provided
    staleTime: 5 * 60 * 1000, // 5 minutes - same as existing pattern
    retry: 3, // Standard retry logic - let TanStack Query handle errors appropriately
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
  const navigate = useNavigate();
  const theme = useMantineTheme();
  const isMobile = useMediaQuery(`(max-width: 991px)`);

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

  // Handle event card click - navigate to event details
  // Use setTimeout to ensure navigation happens AFTER React finishes current render cycle
  // This allows proper component mounting/unmounting (see react-developer-lessons-learned.md)
  const handleEventClick = useCallback((eventId: string) => {
    setTimeout(() => {
      navigate(`/events/${eventId}`);
    }, 0);
  }, [navigate]);

  if (isLoading) {
    return (
      <Box
        component="section"
        data-testid="loading-spinner"
        pt={{ base: 0, sm: 'var(--space-2xl)' }}
        pb={{ base: 0, sm: 'var(--space-2xl)' }}
        px={{ base: '16px', xs: '40px' }}
        style={{
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
        data-testid="error-message"
        pt={{ base: 0, sm: 'var(--space-2xl)' }}
        pb={{ base: 0, sm: 'var(--space-2xl)' }}
        px={{ base: '16px', xs: '40px' }}
        style={{
          maxWidth: '1200px',
          margin: '0 auto',
          background: 'var(--color-ivory)',
          borderRadius: '16px',
          marginTop: 'var(--space-2xl)',
          boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
        }}
      >
        <Alert color="red" style={{ marginBottom: 'var(--space-lg)' }}>
          <strong>Error:</strong> Failed to load events
        </Alert>
        <Text style={{ color: 'var(--color-stone)', textAlign: 'center' }}>
          API service is running on http://localhost:5655
        </Text>
      </Box>
    );
  }

  const displayEvents = events.slice(0, maxEvents);

  // Determine the display title based on viewport
  const displayTitle = isMobile ? "Upcoming Events" : title;

  if (displayEvents.length === 0) {
    return (
      <Box
        component="section"
        data-testid="empty-state"
        pt={{ base: 0, sm: 'var(--space-2xl)' }}
        pb={{ base: 0, sm: 'var(--space-2xl)' }}
        px={{ base: '16px', xs: '40px' }}
        style={{
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
      mt={0}
      p={{ base: '24px 0', xs: 'var(--space-2xl) 40px' }}
      style={{
        maxWidth: '1200px',
        margin: '0 auto',
        background: 'var(--color-ivory)',
        boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
        borderRadius: '16px',
      }}
    >
      <Title
        order={2}
        mb={{ base: 24, xs: 'var(--space-xl)' }}
        style={{
          fontFamily: 'var(--font-heading)',
          fontSize: 'var(--font-size-h2)',
          lineHeight: 'var(--line-height-h2)',
          fontWeight: 800,
          textAlign: 'center',
          color: 'var(--color-burgundy)',
          position: 'relative',
          textTransform: 'uppercase',
          letterSpacing: '3px',
        }}
      >
        {displayTitle}
        <Box
          style={{
            content: '""',
            display: isMobile ? 'none' : 'block',
            width: '100px',
            height: '2px',
            background: 'linear-gradient(90deg, transparent, var(--color-rose-gold), transparent)',
            margin: 'var(--space-sm) auto 0',
          }}
        />
      </Title>

      <Box
        data-testid="events-grid"
        className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3"
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(min(350px, 100%), 1fr))',
          marginBottom: 'var(--space-xl)',
          gap: isMobile ? '0px' : 'var(--space-lg)',
        }}
      >
        {displayEvents.map((event) => (
          <PublicEventCard
            key={event.id}
            event={event}
            variant={isMobile ? "list" : "homepage"}
            onClick={() => handleEventClick(event.id)}
          />
        ))}
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